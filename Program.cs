using AI.Dev.OpenAI.GPT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Spectre.Console;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace ChatCmd
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("env") ?? "dev"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var apiKey = config.GetSection("apiKey").Value;

            var collection = new ServiceCollection();
            ConfigureServices(collection);
            var serviceProvider = collection.BuildServiceProvider();
            
            var functions = GetFunctions(serviceProvider);
            var functionsJsonSchema = JsonConvert.SerializeObject(functions, new JsonSerializerSettings()
            {
                 Formatting = Formatting.Indented,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://api.openai.com");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            string apiUrl = "/v1/chat/completions";

            JArray conversation = new JArray
            {
                new JObject
                {
                    ["role"] = "system",
                    ["content"] = "Assistant is a large language model trained by OpenAI."
                }
            };

            int maxResponseTokens = 1024;
            int tokenLimit = 1024 * 16;

            var functionsJArray = JArray.Parse(functionsJsonSchema);

            JObject input = new JObject
            {
                ["model"] = "gpt-3.5-turbo-0613",
                ["messages"] = conversation,
                ["max_tokens"] = maxResponseTokens,
                ["functions"] = functionsJArray,
            };

            tokenLimit -= GetFunctionsTokenNum(functionsJArray);

            bool functionCalling = false;

            while (true)
            {
                if (!functionCalling)
                {
                    string userInput = ReadCommand();

                    conversation.Add(new JObject
                    {
                        ["role"] = "user",
                        ["content"] = userInput
                    });
                }

                var tokenNum = GetConversationTokenNum(conversation);
                while (tokenNum + maxResponseTokens > tokenLimit)
                {
                    conversation.RemoveAt(1);
                    tokenNum = GetConversationTokenNum(conversation);
                }

                HttpContent content = new StringContent(input.ToString(), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API call failed with status code {response.StatusCode}: {response.ReasonPhrase}");
                    return;
                }

                string responseContent = await response.Content.ReadAsStringAsync();
                JObject jsonResponse = JObject.Parse(responseContent);

                if (jsonResponse["choices"][0]["finish_reason"].ToString() == "function_call")
                {
                    var functionCall = jsonResponse["choices"][0]["message"]["function_call"];
                    var result = CallFunction(serviceProvider, functionCall);
                    if (result != null)
                    {
                        conversation.Add(new JObject
                        {
                            ["role"] = "function",
                            ["name"] = functionCall["name"].ToString(),
                            ["content"] = JsonConvert.SerializeObject(result)
                        });
                        functionCalling = true;
                    }
                    else 
                    {
                        functionCalling = false;
                    }
                    var responseMessage = jsonResponse["choices"][0]["message"]["content"].ToString();
                    if (!string.IsNullOrEmpty(responseMessage))
                    {
                        Console.WriteLine(responseMessage);
                        Console.WriteLine();
                    }
                }
                else
                {
                    var responseMessage = jsonResponse["choices"][0]["message"]["content"].ToString();
                    Console.WriteLine(responseMessage);
                    Console.WriteLine();
                    conversation.Add(new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = responseMessage
                    });
                    if (jsonResponse["choices"][0]["finish_reason"].ToString() == "length")
                    {
                        Console.WriteLine("[...]");
                        Console.WriteLine();
                    }
                    functionCalling = false;
                }
            }
        }

        static int GetFunctionsTokenNum(JArray functions)
        {
            int num = 0;
            num = functions.Sum(m => GPT3Tokenizer.Encode(m["name"].ToString()).Count + GPT3Tokenizer.Encode(m["description"].ToString()).Count + GPT3Tokenizer.Encode(m["parameters"].ToString()).Count + 6);
            return num + 2;
        }

        static int GetConversationTokenNum(JArray conversation)
        {
            int num = 0;
            num = conversation.Sum(m => GPT3Tokenizer.Encode(m["role"].ToString()).Count + GPT3Tokenizer.Encode(m["content"].ToString()).Count + 4);
            return num + 2;
        }

        static string ReadCommand()
        {
            AnsiConsole.Markup($"[purple]{Directory.GetCurrentDirectory()}[/]{Environment.NewLine}$");
            var lines = new List<string>();
            string line;
            while ((line = System.Console.ReadLine()) != null)
            {
                lines.Add(line);
            }

            var text = string.Join(Environment.NewLine, lines);

            return text;
        }

        private static void ConfigureServices(IServiceCollection collection)
        {
            // Get the current assembly.
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Find all types that implement IChatPlug.
            var chatPlugTinypes = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IChatPlugin)));

            // Register them with the service collection.
            foreach (var type in chatPlugTinypes)
            {
                collection.AddScoped(typeof(IChatPlugin),type);                
            }
        }

        private static List<ChatFunctionDescriptor> GetFunctions(ServiceProvider serviceProvider)
        {
            List<ChatFunctionDescriptor> functions = new List<ChatFunctionDescriptor>();

            var chatPlugins = serviceProvider.GetServices<IChatPlugin>();

            foreach (var plugin in chatPlugins)
            {
                functions.AddRange(plugin.GetFunctions());
            }

            return functions;
        }

        private static object CallFunction(ServiceProvider serviceProvider, JToken functionCall)
        {            
            string[] parts = functionCall["name"].ToString().Split('-');

            var chatPlugins = serviceProvider.GetServices<IChatPlugin>();
            var chatPlugin = chatPlugins.Where(plugin => plugin.GetType().Name.Equals(parts[0])).FirstOrDefault();
            if (chatPlugin != null)
            {
                return chatPlugin.ExecuteFunction(parts[1], functionCall["arguments"].ToString());
            }
            else
            {
                return null;
            }            
        }
    }
}