using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Routing
{
	public class DefaultTargetNameExtractor : ITargetNameExtractor
	{
		public string GetTargetName(Type interfaceType)
		{
			return interfaceType.Name;
		}
	}
}
