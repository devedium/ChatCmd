using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    public class ChatFunctionDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JSchema Parameters { get; set; }
    }   

}
