namespace FelisMq.Core.Configurations;

public sealed record FelisMqConfiguration
{
    public const string FelisMq = nameof(FelisMq);
    public string ClientId { get; set; } = null!;
    public MqttConfiguration? Mqtt { get; set; }
    public CredentialsConfiguration? Credentials { get; set; }
    public CacheConfiguration? Cache { get; set; }
}

public sealed record MqttConfiguration
{
    public const string Mqtt = nameof(Mqtt);
    public string Host { get; set; } = null!;
    public int? Port { get; set; }
}

public sealed record CredentialsConfiguration
{
    public const string Credentials = nameof(Credentials);
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public sealed record CacheConfiguration
{
    public const string Cache = nameof(Cache);
    public double SlidingExpiration { get; set; }
    public double AbsoluteExpiration { get; set; }
    public long MaxSizeBytes { get; set; }
}