using System;
using System.Threading.Tasks;
using CoreRPC.Transferable;

namespace CoreRPC
{
    public interface IMethodCallInterceptor
    {
        Task<object> Intercept(MethodCall call, object context,  Func<Task<object>> invoke);
    }
}