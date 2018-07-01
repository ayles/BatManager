using System;
using System.IO;
using Newtonsoft.Json;

namespace BatManager.Core {
    public class SettingsManager {
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowX { get; set; }
        public double WindowY { get; set; }
        public bool ProcessSettingsExpanded { get; set; }

        public void Save() {
            try {
                File.WriteAllText(_saveFileName, JsonConvert.SerializeObject(this));
            } catch (Exception e) { }
        }

        private const string _saveFileName = "settings.json";
        private static SettingsManager _instance;

        public static SettingsManager Instance => _instance ?? (_instance = File.Exists(_saveFileName)
                                                      ? JsonConvert.DeserializeObject<SettingsManager>(
                                                          File.ReadAllText(_saveFileName))
                                                      : new SettingsManager());
    }
}