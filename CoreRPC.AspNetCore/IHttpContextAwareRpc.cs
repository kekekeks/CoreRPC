using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CoreRPC.AspNetCore
{
    public interface IHttpContextAwareRpc
    {
        Task<object> OnExecuteRpcCall(HttpContext context, Func<Task<object>> action);
    }
}