﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Tweezers.Api.DataHolders;

namespace Tweezers.Api.Middleware
{
    public class TweezersMiddleware
    {
        public static void AddIgnoreNullService(IServiceCollection services)
        {
            services.AddMvc()
                .AddJsonOptions(options => {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
        }

        public static void AddErrorHandler(IApplicationBuilder app)
        {
#if DEBUG
#else
            app.UseExceptionHandler("/Error");
#endif
        }
    }
}