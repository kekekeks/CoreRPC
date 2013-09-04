using AsyncRpc.Binding;
using AsyncRpc.Binding.Default;
using AsyncRpc.CodeGen;
using AsyncRpc.Routing;
using AsyncRpc.Serialization;
using AsyncRpc.Transport;

namespace AsyncRpc
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
			: this(new XmlMethodCallSerializer(), new DefaultMethodBinder())
		{

		}


		public IRequestHandler CreateRequestHandler(ITargetSelector selector)
		{
			return new RequestHandler(selector, _binder, _serializer);
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
