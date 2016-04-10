using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nagi
{
    public class FileIntegration : IIntegration
    {
        public static string Type { get; } = "File";
        public static Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>
        {
            {"-t", "target folder" }
        };

        public string Target { get; }
        public string ActionMessage { get; }

        public FileIntegration(string target)
        {
            Target = Path.GetFullPath(target);
            ActionMessage = "copied to " + Target;
        }

        public void Execute(string filePath)
        {
            var destinationFileName = Path.Combine(Target, Path.GetFileName(filePath));
            File.Copy(filePath, destinationFileName);
        }
    }
}
