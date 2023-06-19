using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatCmd
{
    public class ChatFile : ChatPluginBase
    {
        [ChatFunction(description:"Read all the text in the file")]
        public string ReadTextFile([ChatFunctionParameter(description: "The file to open for reading")]string path)
        {        
            return File.ReadAllText(path);
        }

        [ChatFunction(description: "Write the content to the file")]
        public void WriteTextFile([ChatFunctionParameter(description: "The file to write to")] string path, [ChatFunctionParameter(description: "The string to write to the file")] string contents)
        {
            File.WriteAllText(path, contents);
            return;
        }

        [ChatFunction(description: "Delete the file, return true if it succeeded.")]
        public void DeleteFile([ChatFunctionParameter(description: "The name of the file to be deleted")] string path)
        {
            File.Delete(path);
            return;
        }
    }
}
