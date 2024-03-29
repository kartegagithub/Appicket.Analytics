﻿using Appicket.Analytics.WebAPI.Extensions.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Appicket.Analytics.WebAPI.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection UseAppicket(this IServiceCollection services)
        {
            return services.AddHostedService<AnalyzerService>();
        }
        public static IApplicationBuilder UseAppicket(this IApplicationBuilder builder)
        {
            return builder.UseAppicket(null);
        }
        public static IApplicationBuilder UseAppicket(this IApplicationBuilder builder, Func<AnalyzerOptions> optionBuilder)
        {
            builder.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        try
                        {
                            Analyzer.LogException(contextFeature.Error);
                        }
                        catch (Exception)
                        {

                        }
                        await new Task<int>(() => 1);
                    }
                });
            });
            if (optionBuilder != null)
                return builder.UseMiddleware<AnalyzerMiddleware>(optionBuilder());
            return builder.UseMiddleware<AnalyzerMiddleware>();
        }
    }
}