using System;
using System.Collections.Generic;
using System.Reflection;

namespace CoreRPC.Routing
{
    public class DefaultTargetSelector : ITargetSelector
    {
        private readonly ITargetFactory _factory;
        private readonly ITargetNameExtractor _extractor;
        private readonly Dictionary<string, Type> _handlers = new Dictionary<string, Type>();
        private readonly Dictionary<string, object> _instanceHandlers = new Dictionary<string, object>();

        public DefaultTargetSelector() : this(new DefaultTargetFactory(), new DefaultTargetNameExtractor())
        {
            
        }

        public DefaultTargetSelector(ITargetFactory factory, ITargetNameExtractor extractor)
        {
            _factory = factory;
            _extractor = extractor;
        }

        public void Register(string name, Type handler)
        {
            _handlers.Add(name, handler);
        }

        public void Register<THandler>(string name)
        {
            Register(name, typeof (THandler));
        }

        public void Register<TInterface, THandler>(THandler instance)
        {
            _instanceHandlers.Add(_extractor.GetTargetName(typeof(TInterface)), instance);
        }

        public void Register(Type iface, Type handler)
        {
            if (!iface.GetTypeInfo().IsInterface)
                throw new ArgumentException("iface should be interface");
            if (!iface.IsAssignableFrom(handler))
                throw new ArgumentException("handler should implement iface");
            Register(_extractor.GetTargetName(iface), handler);
        }

        public void Register<TInterface, THandler>() where THandler : TInterface
        {
            Register(typeof (TInterface), typeof (THandler));
        }


        object ITargetSelector.GetTarget (string target, object callContext)
        {
            if (_instanceHandlers.TryGetValue(target, out var instance))
                return instance;
            if (_handlers.TryGetValue(target, out var type))
                return _factory.CreateInstance(type, callContext);
            throw new ArgumentException($"Handler for {target} is not registered");
        }
    }
}
