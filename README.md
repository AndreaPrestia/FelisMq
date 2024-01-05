# FelisMq
A library that implements a way to consume MQTT messages by topic, avoiding to write every time the listening part. It let's you group all the logic for every topic in single classes, trying to reach the goal of single responsibility for every topic.

**How can I use it?**

You should reference the project and just the following line in your startup part (Program.cs or whatever you prefer):

```
builder.AddFelisClient();
```

Let's define a class with the metadata of the message that we want to consume:
```
public class TestMessage
{
    public string? Header { get; set; }
    public string? Message { get; set; }
}
```
Now let's implement the abstract class **Consume<>** as here below:

```
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
```
To keep everything simple for example purposes now we have the class **TestConsumer** that implements the abstract class **Consume<>** with just only a console write of the payload as json.

That's it.

The complete example can be found in the **Examples** directory inside the project.

**Configuration**

Add this section to appsettings.json. 

```
"FelisMq": {
    "ClientId": "client-id",
    "Mqtt": {
       "Host": "host",
       "Port": 80
    },
    "Credentials": {
       "Username": "username",
       "Password": "password"
    },
    "Cache": {
      "SlidingExpiration": 3600,
      "AbsoluteExpiration": 3600,
      "MaxSizeBytes": 3000
     }
  }
```
The configuration is made of:

Property | Type | Context |
--- | --- | --- |
ClientId | string | The MQTT client id. |
Mqtt | object | The MQTT configuration. |
Mqtt.Host | string | The Host of the MQTT service to reach. |
Mqtt.Port | integer | The Port of the MQTT service to reach.|
Credentials | object | The object containing the MQTT service credentials. |
Credentials.Username | string | The Username of the MQTT credentials. |
Credentials.Password | string | The Password of the MQTT credentials.|
Cache | object | The cache configuration. The cache applies to all the client consumers. |
Cache.SlidingExpiration | double | The SlidingExpiration for IMemoryCacheOptions. |
Cache.AbsoluteExpiration | double | The AbsoluteExpiration for IMemoryCacheOptions.|
Cache.MaxSizeBytes | long | The MaxSizeBytes that can reach the cache, used for IMemoryCacheOptions. |


**Conclusion**

This is a simple project, i already have use it in some environments. 

  

