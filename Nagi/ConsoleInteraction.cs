using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nagi
{
    public class ConsoleInteraction
    {
        //TODO: dynamic detecting of integration types
        private readonly Type[] Integrations = new[] { typeof(FileIntegration) };

        private WatchDog watchDog;

        public void Run(WatchDog watchDog)
        {
            this.watchDog = watchDog;
            watchDog.FileAdded += OnFileAdded;
            watchDog.IntegrationExecuted += OnIntegrationExecuted;
            PrintActiveConfigurations();
            while (true)
            {
                PrintPrompt();
                var line = Console.ReadLine();
                try
                {
                    HandleInput(line);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
        }

        public void OnFileAdded(string path)
        { 
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("File " + path);
            PrintPrompt();
        }

        public void OnIntegrationExecuted(IIntegration integration)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine("    " + integration.ActionMessage);
            PrintPrompt();
        }

        private void HandleInput(string line)
        {
            var parameters = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parameters.Length == 0)
                return;

            if (IsHelpInput(parameters))
                PrintHelp();

            if (IsAddInput(parameters))
            {
                AddIntegration(parameters.Skip(1).ToArray());   //remove the action
            }

            if (IsRemoveInput(parameters))
            {
                Remove(parameters.Skip(1).ToArray());   //remove the action
            }

            if (IsListInput(parameters))
            {
                List();
            }
        }

        private bool IsHelpInput(string[] parameters)
        {
            return parameters.First() == "?";
        }

        private bool IsAddInput(string[] parameters)
        {
            return parameters.First() == "add";
        }

        private bool IsRemoveInput(string[] parameters)
        {
            return parameters.First() == "remove";
        }

        private bool IsListInput(string[] parameters)
        {
            return parameters.First() == "list";
        }

        private void PrintPrompt()
        {
            Console.Write("> ");
        }

        private void PrintActiveConfigurations()
        {
            foreach (var configuration in watchDog.Configurations)
            {
                string integrationType = GetIntegrationType(configuration);
                Console.WriteLine($"Configuration {configuration.Id} ({integrationType}) watching {configuration.Folder}");
            }
        }

        private void PrintHelp()
        {
            var lines = new List<string>();
            lines.Add("Add a new watch configuration:");
            lines.Add("    add folder integrationType [integration parameters]");
            lines.Add("");
            lines.Add("List all active configurations:");
            lines.Add("    list");
            lines.Add("");
            lines.Add("Remove a configuration:");
            lines.Add("    remove id");
            Console.WriteLine(String.Join("\n", lines));
        }

        #region Add
        private void AddIntegration(string[] parameters)
        {
            var folder = parameters[0];
            if (!Directory.Exists(folder))
            {
                throw new Exception("The given directory does not exist");
            }

            var integration = CreateIntegration(parameters.Skip(1).ToArray());  //remove the source folder
            var configuration = watchDog.AddConfiguration(folder, integration);
            Console.WriteLine("Now watching " + configuration.Folder);
        }

        private IIntegration CreateIntegration(string[] parameters)
        {
            Type integration = GetIntegrationType(parameters[0]);

            var parameterPairs = parameters.Skip(1)
                .Select((v, i) => new { PairNum = i / 2, Value = v })
                .GroupBy(v => v.PairNum)
                .Select(group => new Tuple<string, string>(group.Select(g => g.Value).ElementAt(0), group.Select(g => g.Value).ElementAt(1))).ToArray();
            var constructorParameters = new List<object>();
            var integrationParameters = GetIntegrationParameters(integration);
            foreach (var integrationParameter in integrationParameters.Keys)
            {
                var value = parameterPairs.FirstOrDefault(pp => pp.Item1 == integrationParameter);
                if (value == null)
                    throw new Exception($"The parameter {integrationParameter} was not given");
                constructorParameters.Add(value.Item2);
            }

            var result = (IIntegration)Activator.CreateInstance(integration, constructorParameters.ToArray());
            return result;
        }

        private Type GetIntegrationType(string type)
        {
            Type integration = Integrations.FirstOrDefault(t =>
            {
                var typeProperty = t.GetProperty("Type", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                var integrationType = (string)typeProperty.GetValue(null);
                return integrationType == type;
            });

            if (integration == null)
                throw new Exception("The given integration does not exist");

            return integration;
        }

        private Dictionary<string, string> GetIntegrationParameters(Type integration)
        {
            var parametersProperty = integration.GetProperty("Parameters", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            return (Dictionary<string, string>)parametersProperty.GetValue(null);
        }
        #endregion

        private void List()
        {
            Console.WriteLine("Id Type        Folder");
            foreach (var configuration in watchDog.Configurations)
            {
                string integrationType = GetIntegrationType(configuration);
                Console.WriteLine(String.Format("{0,2} {1,-11} {2}", configuration.Id, integrationType, configuration.Folder));
            }
        }

        private void Remove(string[] parameters)
        {
            if (parameters.Length == 0)
            {
                Console.Error.WriteLine("You have to enter an id");
            }
            int id;
            if (int.TryParse(parameters[0], out id))
            {
                var configuration = watchDog.RemoveConfiguration(id);
                if(configuration != null)
                {
                    string integrationType = GetIntegrationType(configuration);
                    Console.WriteLine($"Configuration {configuration.Id} ({integrationType}) stopped watching {configuration.Folder}");
                } else
                {
                    Console.Error.WriteLine("There is no configuration with the given id");
                }
            } else
            {
                Console.Error.WriteLine("You have to enter a valid id");
            }
        }

        private string GetIntegrationType(WatchConfiguration configuration)
        {
            var type = configuration.Integration.GetType();
            var typeProperty = type.GetProperty("Type", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var integrationType = (string)typeProperty.GetValue(null);
            return integrationType;
        }
    }
}
