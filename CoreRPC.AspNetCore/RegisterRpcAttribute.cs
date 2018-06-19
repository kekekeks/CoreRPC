using System;

namespace CoreRPC.AspNetCore
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class RegisterRpcAttribute : Attribute
    {
        public Type Interface { get; set; }
        public string Name { get; set; }

        public RegisterRpcAttribute()
        {
            
        }
        
        public RegisterRpcAttribute(Type iface)
        {
            Interface = iface;
        }

        public RegisterRpcAttribute(string name)
        {
            Name = name;
        }

        public RegisterRpcAttribute(Type iface, string name)
        {
            Interface = iface;
            Name = name;
        }
    }
}