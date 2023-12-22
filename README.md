# FelisMq
A library that implements a way to consume MQTT message by topic avoiding to write every time the listening part.

**How can i use it?**

You should reference the project and just the following line in your startup part (Program.cs or whatever you prefer):

```
builder.AddFelisClient();
```

An example of message consumer is the following:

Let's define a class with the metadata of the message that we want to consume:
```
public class TestMessage
{
    public string? Header { get; set; }
    public string? Message { get; set; }
}
```
Now let's implement the abstract class **Consume<>** as below:

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
For the sake of clarity now we have the class **TestConsumer** that implements the abstract class **Consume<>** with just only a console write of the payload as json.

That's it.

The complete example can be found in the **Examples** directory inside the project.

**Configuration**

Add this part in appsettings.json. 

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
The configuration is composed of:

Property | Type | Context |
--- | --- | --- |
ClientId | string | The MQTT client id that identifies the client. |
Mqtt | object | The object containing the MQTT service configuration part. |
Mqtt.Host | string | The Host of the MQTT service to reach. |
Mqtt.Port | integer | The Port of the MQTT service to reach.|
Credentials | object | The object containing the MQTT service credentials part. |
Credentials.Username | string | The Username of the MQTT credentials. |
Credentials.Password | string | The Password of the MQTT credentials.|
Cache | object | The object containing the cache configuration part for Felis client, used to cache the consumers, to avoid the reflection part everytime. |
Cache.SlidingExpiration | double | The SlidingExpiration for IMemoryCacheOptions used. |
Cache.AbsoluteExpiration | double | The AbsoluteExpiration for IMemoryCacheOptions used.|
Cache.MaxSizeBytes | long | The MaxSizeBytes that can reach the cache, used for IMemoryCacheOptions used. |


**Conclusion**

This is a simple project, i already have use it in some environments. 

  

