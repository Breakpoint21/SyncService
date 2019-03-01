using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Newtonsoft.Json;
using SyncService.ObjectModel.Folder;

namespace SyncService.Services.Folder
{
    public class FolderConfigurationService
    {
        public const string FileName = "folders.json";
        private readonly Subject<FolderConfiguration> _configurationUpdatedSubject = new Subject<FolderConfiguration>();
        private readonly Subject<FolderConfiguration> _configurationDeletedSubject = new Subject<FolderConfiguration>();

        public FolderConfigurationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _filePath = Path.Combine(appDataPath, "SyncService", FileName);
            Load();
        }

        private readonly IList<FolderConfiguration> _configs = new List<FolderConfiguration>();
        private readonly string _filePath;

        public IList<FolderConfiguration> GetAllConfigs()
        {
            return _configs;
        }

        public void AddFolderConfig(FolderConfiguration folder)
        {
            _configs.Add(folder);
            Save();
            _configurationUpdatedSubject.OnNext(folder);
        }

        public IObservable<FolderConfiguration> ConfigurationUpdated => _configurationUpdatedSubject;
        public IObservable<FolderConfiguration> ConfigurationDeleted => _configurationDeletedSubject;

        private void Load()
        {
            _configs.Clear();
            if (File.Exists(_filePath))
            {
                using (var file = File.OpenText(_filePath))
                {
                    var content = file.ReadToEnd();
                    var configs = JsonConvert.DeserializeObject<IEnumerable<FolderConfiguration>>(content);
                    foreach (var folderConfig in configs)
                    {
                        _configs.Add(folderConfig);
                    }
                }
            }
        }

        public void Save()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));

            using (var file = File.CreateText(_filePath))
            {
                file.WriteLine(JsonConvert.SerializeObject(_configs.ToArray(), Formatting.Indented));
            }
        }

        public void DeleteConfig(FolderConfiguration folderConfiguration)
        {
            if (_configs.Remove(folderConfiguration))
            {
                Save();
                _configurationDeletedSubject.OnNext(folderConfiguration);
            }
        }

        public void Save(FolderConfiguration folderConfiguration)
        {
            if (_configs.Contains(folderConfiguration))
            {
                Save();
                _configurationUpdatedSubject.OnNext(folderConfiguration);
            }
        }
    }
}