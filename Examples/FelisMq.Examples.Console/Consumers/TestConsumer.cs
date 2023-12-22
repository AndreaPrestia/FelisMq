using System.Text.Json;
using FelisMq.Core;
using FelisMq.Core.Attributes;

namespace FelisMq.Examples.Console.Consumers;

[Topic("Test")]
public class TestConsumer : Consume<TestMessage>
{
    public override Task Process(TestMessage entity, CancellationToken cancellationToken = default)
    {
        System.Console.WriteLine(JsonSerializer.Serialize(entity));
        return Task.CompletedTask;
    }
}

public class TestMessage
{
    public string? Header { get; set; }
    public string? Message { get; set; }
}