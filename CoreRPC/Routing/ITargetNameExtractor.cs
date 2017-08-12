using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRPC.Routing
{
    public interface ITargetNameExtractor
    {
        string GetTargetName(Type interfaceType);
    }
}
