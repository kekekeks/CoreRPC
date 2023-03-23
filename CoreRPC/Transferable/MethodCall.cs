using System;
using System.Reflection;

namespace CoreRPC.Transferable
{
    public class MethodCall : IDisposable
    {
        public object Target { get; set; }
        public MethodInfo Method { get; set; }
        public object[] Arguments { get; set; }
        public virtual void Dispose()
        {
        }
    }
}
