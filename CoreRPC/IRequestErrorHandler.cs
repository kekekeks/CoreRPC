using System;

namespace CoreRPC
{
    public interface IRequestErrorHandler
    {
        string HandleError(Exception exception);
    }
}