﻿using FelisMq.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FelisMq.Core;

public static class Extensions
{
    public static void AddFelisMq(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.Configure<FelisMqConfiguration>(context.Configuration.GetSection(FelisMqConfiguration.FelisMq));
            
            var serviceProvider = builder.Build().Services;

            var messageHandler = serviceProvider.GetService<MessageHandler>();

            messageHandler?.Subscribe(default).Wait();
        });
    }
}