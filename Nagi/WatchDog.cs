using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nagi
{
    public class WatchDog
    {
        private const string SavePath = "Configurations.json";

        public delegate void FileAddedDelegate(string path);
        public event FileAddedDelegate FileAdded;
        public delegate void IntegrationExecutedDelegate(IIntegration integration);
        public event IntegrationExecutedDelegate IntegrationExecuted;

        public IEnumerable<WatchConfiguration> Configurations { get { return configurations; } }
        private IList<WatchConfiguration> configurations = new List<WatchConfiguration>();
        private Dictionary<string, FileSystemWatcher> activeWatcher = new Dictionary<string, FileSystemWatcher>();

        public void Start()
        {
            var configurations = Load();
            foreach (var configuration in configurations)
                ActivateConfiguration(configuration);
        }

        public WatchConfiguration AddConfiguration(string sourceFolder, IIntegration integration)
        {
            var configuration = new WatchConfiguration(createId(), Path.GetFullPath(sourceFolder), integration);
            ActivateConfiguration(configuration);
            Save();
            return configuration;
        }

        public WatchConfiguration RemoveConfiguration(int id)
        {
            var configuration = Configurations.FirstOrDefault(c => c.Id == id);
            if (configuration != null)
                configurations.Remove(configuration);
            Save();
            return configuration;
        }

        private void Save()
        {
            var json = JsonConvert.SerializeObject(configurations, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            });
            File.WriteAllText(SavePath, json);
        }

        private List<WatchConfiguration> Load()
        {
            if (File.Exists(SavePath))
            {
                var json = File.ReadAllText(SavePath);
                var configurations = JsonConvert.DeserializeObject<List<WatchConfiguration>>(json, new IIntegrationJsonConverter());
                return configurations;
            }
            return new List<WatchConfiguration>();
        }

        private void ActivateConfiguration(WatchConfiguration configuration)
        {
            if (!activeWatcher.ContainsKey(configuration.Folder))
            {
                var watcher = new FileSystemWatcher(configuration.Folder);
                watcher.EnableRaisingEvents = true;
                watcher.Created += (s, e) => ExecuteIntegrations(e.FullPath);
            }
            //if there is already a watcher for the folder, the configuration will be automatically catched by ExecuteIntegrations

            configurations.Add(configuration);
        }

        private void ExecuteIntegrations(string addedFilePath)
        {
            FileAdded?.Invoke(addedFilePath);
            var folder = Path.GetDirectoryName(addedFilePath);
            foreach(var integration in Configurations.Where(c => c.Folder == folder).Select(c => c.Integration))
            {
                integration.Execute(addedFilePath);
                IntegrationExecuted?.Invoke(integration);
            }
        }

        private int createId()
        {
            var minId = 1;
            var ids = Configurations.Select(c => c.Id);
            var id = minId;
            while (ids.Contains(id))
            {
                id++;
            }
            return id;
        }
    }
}
