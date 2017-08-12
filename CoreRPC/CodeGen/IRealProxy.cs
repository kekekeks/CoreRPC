using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AsyncRpc.CodeGen
{
	public interface IRealProxy
	{
		object Invoke(MethodInfo method, IEnumerable args);
	}
}
