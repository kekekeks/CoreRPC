using CoreRPC.Binding;
using CoreRPC.Binding.Default;
using CoreRPC.CodeGen;
using CoreRPC.Routing;
using CoreRPC.Serialization;
using CoreRPC.Transport;

namespace CoreRPC
{
    public class Engine
    {
        private readonly IMethodBinder _binder;
        private readonly IMethodCallSerializer _serializer;

        public Engine (IMethodCallSerializer serializer, IMethodBinder binder)
        {
            _binder = binder;
            _serializer = serializer;
        }

        public Engine()
            : this(new JsonMethodCallSerializer(false), new DefaultMethodBinder())
        {

        }


        public IRequestHandler CreateRequestHandler(ITargetSelector selector, IRequestErrorHandler errors = null)
        {
            return new RequestHandler(selector, _binder, _serializer, null, errors);
        }
        
        public IRequestHandler CreateRequestHandler(
            ITargetSelector selector,
            IMethodCallInterceptor interceptor,
            IRequestErrorHandler errors = null)
        {
            return new RequestHandler(selector, _binder, _serializer, interceptor, errors);
        }

        public TInterface CreateProxy<TInterface>(IClientTransport transport, ITargetNameExtractor nameExtractor = null)
        {
            if (nameExtractor == null)
                nameExtractor = new DefaultTargetNameExtractor();
            return ProxyGen.CreateInstance<TInterface>(new CallProxy(transport, _serializer,
                                                                     _binder, nameExtractor.GetTargetName(typeof (TInterface))));
        }
    }
}
