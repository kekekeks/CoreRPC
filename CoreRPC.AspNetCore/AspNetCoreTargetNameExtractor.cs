using System;
using System.Reflection;
using CoreRPC.Routing;

namespace CoreRPC.AspNetCore
{
    public class AspNetCoreTargetNameExtractor : ITargetNameExtractor
    {
        private DefaultTargetNameExtractor _default;

        public AspNetCoreTargetNameExtractor()
        {
            _default = new DefaultTargetNameExtractor();
        }
        
        public string GetTargetName(Type interfaceType)
        {
            var attr = interfaceType.GetTypeInfo().GetCustomAttribute<RegisterRpcAttribute>();
            return attr?.Name ?? _default.GetTargetName(attr?.Interface ?? interfaceType);
        }
    }
}