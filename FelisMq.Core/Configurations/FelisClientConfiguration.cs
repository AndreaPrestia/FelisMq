namespace FelisMq.Core.Configurations;

public sealed record FelisClientConfiguration
{
    public const string FelisMq = nameof(FelisMq);
    public string ClientId { get; set; } = null!;
    public FelisClientMqttConfiguration? Mqtt { get; set; }
    public FelisClientMqttCredentials? Credentials { get; set; }
    public FelisClientCacheConfiguration? Cache { get; set; }
}

public sealed record FelisClientMqttConfiguration
{
    public const string Mqtt = nameof(Mqtt);
    public string Host { get; set; } = null!;
    public int? Port { get; set; }
}

public sealed record FelisClientMqttCredentials
{
    public const string Credentials = nameof(Credentials);
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public sealed record FelisClientCacheConfiguration
{
    public const string Cache = nameof(Cache);
    public double SlidingExpiration { get; set; }
    public double AbsoluteExpiration { get; set; }
    public long MaxSizeBytes { get; set; }
}