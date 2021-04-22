using System;

namespace CoreRPC
{
    public interface IRequestErrorHandler
    {
        void HandleError(Exception exception);
    }
}