using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    interface IChatPlugin
    {
        List<ChatFunctionDescriptor> GetFunctions();
        object ExecuteFunction(string function, string arguments);
    }
}
