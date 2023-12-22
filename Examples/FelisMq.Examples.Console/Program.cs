// See https://aka.ms/new-console-template for more information

using FelisMq.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging(configure => configure.AddConsole());
    });

builder.AddFelisMq();

Console.WriteLine("Test Console for FelisMq up and Running :)");

var host = builder.Build();

host.Run();


