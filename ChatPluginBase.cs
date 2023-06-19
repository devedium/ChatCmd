using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    public abstract class ChatPluginBase : IChatPlugin
    {
        public virtual List<ChatFunctionDescriptor> GetFunctions()
        {
            List<ChatFunctionDescriptor> functions = new List<ChatFunctionDescriptor>();

            // Get methods of the derived class
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
            foreach (var method in methods)
            {
                // Check if the method has the ChatFunctionAttribute
                var attribute = method.GetCustomAttribute<ChatFunctionAttribute>();
                if (attribute != null)
                {
                    // Create a new function definition
                    var function = new ChatFunctionDescriptor
                    {
                        Name = $"{GetType().Name}-{method.Name}",
                        Description = attribute.Description,
                        Parameters = CreateJsonSchemaForParameters(method)
                    };

                    functions.Add(function);
                }
            }
            
            return functions;
        }

        public virtual object ExecuteFunction(string function, string arguments)
        {
            var args =  JObject.Parse(arguments);
            Dictionary<string, object> parameters = args.ToObject<Dictionary<string, object>>();

            Type type = this.GetType();
            MethodInfo methodInfo = type.GetMethod(function);
            ParameterInfo[] methodParameters = methodInfo.GetParameters();
            object[] parametersArray = methodParameters.Select(p => parameters[p.Name]).ToArray();

            object result = methodInfo?.Invoke(this, parametersArray);
            return result;
        }

        private JSchema CreateJsonSchemaForParameters(MethodInfo method)
        {
            var functionAttr = method.GetCustomAttribute<ChatFunctionAttribute>();
            var schema = new JSchema
            {
                Type = JSchemaType.Object,
                Description = functionAttr != null ? functionAttr.Description : null
        };

            foreach (var parameter in method.GetParameters())
            {
                var paramterAttr = parameter.GetCustomAttribute<ChatFunctionParameterAttribute>();
                var description = paramterAttr != null ? paramterAttr.Description : null;

                schema.Properties[parameter.Name] = new JSchema
                {
                    Type = GetJsonType(parameter.ParameterType),
                    Description = description,
                };
                if (!parameter.IsOptional)
                {
                    schema.Required.Add(parameter.Name);
                }
            }

            return schema;
        }

        private JSchemaType GetJsonType(Type type)
        {
            if (type == typeof(string))
            {
                return JSchemaType.String;
            }
            else if (type == typeof(int) || type == typeof(long) || type == typeof(short))
            {
                return JSchemaType.Integer;
            }
            else if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                return JSchemaType.Number;
            }
            else if (type == typeof(bool))
            {
                return JSchemaType.Boolean;
            }
            else if (type == typeof(DateTime))
            {
                return JSchemaType.String; // JSON Schema doesn't have a dedicated date-time type, usually represented as string
            }
            else if (type == typeof(object))
            {
                return JSchemaType.Object;
            }
            else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
            {
                return JSchemaType.Array;
            }
            // Add more types as needed

            throw new ArgumentException("Unsupported type: " + type.FullName);
        }


    }

}
