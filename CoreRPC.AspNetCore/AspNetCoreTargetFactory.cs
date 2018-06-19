using System;
using System.Collections.Generic;
using CoreRPC.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CoreRPC.AspNetCore
{
    public class AspNetCoreTargetFactory : ITargetFactory
    {
        public object CreateInstance(Type type, object callContext)
        {
            var ctx = (HttpContext) callContext;
            var resolved = ctx.RequestServices.GetService(type);
            if (resolved != null)
                return resolved;
            return ActivatorUtilities.CreateInstance(ctx.RequestServices, type);
        }
    }
}