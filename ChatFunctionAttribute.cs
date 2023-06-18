using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class ChatFunctionAttribute : Attribute
    {
        private readonly string _description;

        public ChatFunctionAttribute(string description)
        {
            this._description = description;
        }

        public string Description
        {
            get { return _description; }
        }
    }
}
