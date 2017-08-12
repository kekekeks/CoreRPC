using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CoreRPC.Binding.Default
{
	public class DefaultMethodBinder : IMethodBinder
	{
		private readonly Dictionary<Type, Implementation> _cache = new Dictionary<Type, Implementation>();

		private Implementation GetFor(object obj)
		{
			lock (_cache)
			{
				var type = obj.GetType();
				Implementation rv;
				if (!_cache.TryGetValue(type, out rv))
					_cache[type] = rv = new Implementation(type);
				return rv;
			}
		}

		public IMethodInfoProvider GetInfoProviderFor(object obj)
		{
			return GetFor(obj);
		}

		byte[] IMethodBinder.GetMethodSignature(MethodInfo nfo)
		{
			return GetMethodSignature(nfo);
		}

		private static byte[] GetMethodSignature(MethodInfo nfo)
		{
			var ms = new MemoryStream();
			using (var bw = new BinaryWriter(ms, new UTF8Encoding(false)))
			{
				bw.Write(nfo.Name);
				foreach (var arg in nfo.GetParameters())
					bw.Write(arg.ParameterType.FullName);
			}
			return ms.ToArray();
		}

		private class Implementation : IMethodInfoProvider
		{
			private readonly List<KeyValuePair<byte[], MethodInfo>> _signatures = new List<KeyValuePair<byte[], MethodInfo>>();

			public Implementation(Type type)
			{
				_signatures = type.GetMethods().Select(m => new KeyValuePair<byte[], MethodInfo>(GetMethodSignature(m), m)).ToList();
			}

			public MethodInfo GetMethod(byte[] signature)
			{
				return _signatures.First(s => s.Key.SequenceEqual(signature)).Value;
			}

		}
	}
}