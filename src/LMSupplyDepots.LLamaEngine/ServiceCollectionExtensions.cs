﻿using LMSupplyDepots.LLamaEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LMSupplyDepots.LLamaEngine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLLamaEngine(this IServiceCollection services)
    {
        services.AddSingleton<ILLamaBackendService, LLamaBackendService>();
        services.AddSingleton<ILocalModelManager, LocalModelManager>();
        services.AddSingleton<ILocalLLMService, LocalLLMService>();
        services.AddSystemMonitoring();

        return services;
    }
}
