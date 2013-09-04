using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.Routing
{
	public interface ITargetNameExtractor
	{
		string GetTargetName(Type interfaceType);
	}
}
