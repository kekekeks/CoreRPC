using System;
using System.Collections.Generic;
using CoreRPC.Routing;
using CoreRPC.Typescript;
using Microsoft.AspNetCore.Hosting;

namespace CoreRPC.AspNetCore
{
    public static class AspNetCoreRpcTypescriptGenerator
    {
        public static string GenerateCode(IHostingEnvironment env, Action<TypescriptGenerationOptions> configure = null)
        {
            return GenerateCode(RpcTypesResolver.GetRpcTypes(env), configure);
        }
        
        public static string GenerateCode(IEnumerable<Type> types, Action<TypescriptGenerationOptions> configure = null)
        {
            var cfg = new TypescriptGenerationOptions();
            cfg.TargetNameExtractor = new AspNetCoreTargetNameExtractor();
            cfg.ApiFieldNamingPolicy = t =>
                TypescriptGenerationOptions.ToCamelCase(cfg.TargetNameExtractor.GetTargetName(t));
            configure?.Invoke(cfg);
            return TypescriptClientGenerator.GenerateGlueCode(cfg, types);
        }
    }
    
    
    
    
}