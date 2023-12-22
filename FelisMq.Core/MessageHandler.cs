using FelisMq.Core.Attributes;
using FelisMq.Core.Configurations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace FelisMq.Core;

internal abstract class MessageHandler
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly FelisClientConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheEntryOptions;

    protected MessageHandler(ILogger<MessageHandler> logger, IOptionsMonitor<FelisClientConfiguration> configuration,
        IServiceProvider serviceProvider, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configuration.CurrentValue);

        _logger = logger;
        _configuration = configuration.CurrentValue;

        if (_configuration.Cache == null)
        {
            throw new ArgumentNullException($"No Cache configuration provided");
        }

        _serviceProvider = serviceProvider;
        _cache = cache;

        _cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromSeconds(_configuration.Cache.SlidingExpiration))
            .SetAbsoluteExpiration(TimeSpan.FromSeconds(_configuration.Cache.AbsoluteExpiration))
            .SetSize(_configuration.Cache.MaxSizeBytes)
            .SetPriority(CacheItemPriority.High);
    }

    internal async Task Subscribe(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new MqttFactory();
            var client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithClientId(_configuration.ClientId)
                .WithTcpServer(_configuration.Mqtt?.Host, _configuration.Mqtt?.Port)
                .WithCredentials(_configuration.Credentials?.Username, _configuration.Credentials?.Password)
                .WithCleanSession()
                .Build();

            client.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    _logger.LogInformation($"Incoming message: {Serialize(e)}");

                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                    _logger.LogInformation($"Payload: {payload}");

                    var consumerSearchResult = GetConsumer(e.ApplicationMessage.Topic);

                    if (consumerSearchResult.Consumer == null!)
                    {
                        _logger.LogInformation(
                            $"Consumer not found for topic {e.ApplicationMessage.Topic}");
                        return Task.FromResult(Task.CompletedTask);
                    }

                    var consumer = consumerSearchResult.Consumer;

                    var entityType = consumerSearchResult.MessageType;

                    var processMethod = GetProcessMethod(consumer, entityType);

                    if (processMethod == null)
                    {
                        throw new EntryPointNotFoundException(
                            $"No implementation of method {entityType?.Name} Process({entityType?.Name} entity)");
                    }

                    var entity = Deserialize(payload, entityType);

                    if (entity == null)
                    {
                        throw new ArgumentNullException(nameof(entity));
                    }

                    processMethod.Invoke(consumer, new[] { entity, stoppingToken });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }

                return Task.CompletedTask;
            };

            client.ConnectedAsync += async _ =>
            {
                _logger.LogInformation("Connection established");

                await client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("#").Build(),
                    cancellationToken: stoppingToken);
            };

            await client.ConnectAsync(options, CancellationToken.None);

            _logger.LogInformation($"Connection established");

            client.DisconnectedAsync += async _ =>
            {
                _logger.LogWarning("Connection terminated I'll try to connect again in 5 seconds");

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                try
                {
                    await client.ConnectAsync(options, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    #region Private methods

    private ConsumerSearchResult GetConsumer(string? topic)
    {
        var consumerSearchResult = GetFromCache(topic);

        if (consumerSearchResult != null)
        {
            return consumerSearchResult;
        }

        var constructed = AppDomain.CurrentDomain.GetAssemblies()
            .First(x => x.GetName().Name == AppDomain.CurrentDomain.FriendlyName).GetTypes().FirstOrDefault(t =>
                t.BaseType?.FullName != null
                && t.BaseType.FullName.Contains("Felis.Client.Consume") &&
                t is { IsInterface: false, IsAbstract: false }
                && t.GetCustomAttributes<TopicAttribute>().Count(x =>
                    string.Equals(topic, x.Value, StringComparison.InvariantCultureIgnoreCase)) == 1
                && t.GetMethods().Any(x => x.Name == "Process"
                                           && x.GetParameters().Count() ==
                                           1));

        if (constructed == null)
        {
            throw new InvalidOperationException($"Not found implementation of Consumer for topic {topic}");
        }

        var firstConstructor = constructed.GetConstructors().FirstOrDefault();

        var parameters = new List<object>();

        if (firstConstructor == null)
        {
            throw new NotSupportedException($"Constructor not implemented in {constructed.Name}");
        }

        foreach (var param in firstConstructor.GetParameters())
        {
            using var serviceScope = _serviceProvider.CreateScope();
            var provider = serviceScope.ServiceProvider;

            var service = provider.GetService(param.ParameterType);

            parameters.Add(service!);
        }

        var processParameterInfo = constructed.GetMethod("Process")?.GetParameters().FirstOrDefault();

        if (processParameterInfo == null)
        {
            throw new InvalidOperationException($"Not found parameter of Consumer.Process for topic {topic}");
        }

        var parameterType = processParameterInfo.ParameterType;

        var instance = Activator.CreateInstance(constructed, parameters.ToArray())!;

        if (instance == null)
        {
            throw new ApplicationException($"Cannot create an instance of {constructed.Name}");
        }

        consumerSearchResult = new ConsumerSearchResult()
        {
            MessageType = parameterType,
            Consumer = instance
        };

        SetInCache(topic, consumerSearchResult);

        return consumerSearchResult;
    }

    private MethodInfo? GetProcessMethod(object instance, Type? entityType)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(entityType);

        return instance.GetType().GetMethods().Where(t => t.Name.Equals("Process")
                                                          && t.GetParameters().Length == 1 &&
                                                          t.GetParameters().FirstOrDefault()!.ParameterType
                                                              .Name.Equals(entityType.Name)
            ).Select(x => x)
            .FirstOrDefault();
    }


    private object? Serialize(object? content) => content == null
        ? null
        : JsonSerializer.Serialize(content, new JsonSerializerOptions()
        {
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        });

    private object? Deserialize(string? content, Type? type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return string.IsNullOrWhiteSpace(content)
            ? null
            : JsonSerializer.Deserialize(content, type, new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });
    }

    private void SetInCache(string? topic, ConsumerSearchResult value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(value);

        _cache.Remove(topic);
        _cache.Set(topic, value, _cacheEntryOptions);
    }

    private ConsumerSearchResult? GetFromCache(string? topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);

        var found = _cache.TryGetValue(topic, out ConsumerSearchResult? result);

        return !found ? default : result;
    }

    private class ConsumerSearchResult
    {
        public object? Consumer { get; init; }
        public Type? MessageType { get; init; }
    }

    #endregion
}