using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ChatFunctionParameterAttribute : Attribute
    {
        private string _description;

        public ChatFunctionParameterAttribute(string description)
        {
            _description = description;
        }

        public string Description
        {
            get
            {
                return _description;
            }
        }
    }
}
