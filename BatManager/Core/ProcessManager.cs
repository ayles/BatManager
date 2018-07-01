using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BatManager.Util;
using Microsoft.Win32;
using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace BatManager.Core {
    public class ProcessManager {
        private string _name;
        private ObservableCollection<ProcessContainer> _processWrappers = new ObservableCollection<ProcessContainer>();

        [JsonIgnore]
        public DelegateCommand CreateProcessWrapperCommand { get; private set; }
        [JsonIgnore]
        public DelegateCommand DeleteProcessWrapperCommand { get; private set; }
        [JsonIgnore]
        public DelegateCommand CloseApplicationCommand { get; private set; }
        [JsonIgnore]
        public DelegateCommand BrowseExecutableCommand { get; private set; }

        public IEnumerable<ProcessContainer> ProcessWrappers {
            get => _processWrappers;
            set => _processWrappers = new ObservableCollection<ProcessContainer>(value);
        }

        [JsonConstructor]
        private ProcessManager(string name) {
            _name = name;

            var tabsList = Application.Current?.MainWindow?.FindName("TabsList") as TabControl;
            
            CreateProcessWrapperCommand = new DelegateCommand(o => {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => tabsList.SelectedItem = CreateProcessWrapper()));
            });
            
            DeleteProcessWrapperCommand = new DelegateCommand(o => {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => {
                    if (new ConfirmWindow().ShowAndGetResult()) 
                        DeleteProcessWrapper(o as ProcessContainer);
                }));
            });
            
            CloseApplicationCommand = new DelegateCommand(o => {
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => {
                    if (new ConfirmWindow().ShowAndGetResult()) 
                        Application.Current?.MainWindow?.Close();
                }));
            });
            
            BrowseExecutableCommand = new DelegateCommand(o => {
                Console.WriteLine(123);
                Application.Current?.Dispatcher?.BeginInvoke(new Action(() => {
                    if (!(o is ProcessContainer processWrapper)) return;
                    
                    var dialog = new OpenFileDialog {
                        Filter = "Executables (*.exe;*.bat)|*.exe;*.bat|All files (*.*)|*.*",
                        RestoreDirectory = true
                    };
                    var result = dialog.ShowDialog();
                    if (result == true) {
                        processWrapper.Executable = dialog.FileName;
                    }
                }));
            });
        }

        public void Save() {
            try {
                File.WriteAllText(_name + ".json", JsonConvert.SerializeObject(this));
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public ProcessContainer CreateProcessWrapper() {
            var pw = new ProcessContainer();
            _processWrappers.Add(pw);
            pw.AttachProcessManager(this);
            Save();
            return pw;
        }

        public void DeleteProcessWrapper(ProcessContainer processContainer) {
            if (_processWrappers.Remove(processContainer)) {
                processContainer.StopProcess();
                Save();
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext streamingContext) {
            foreach (var processWrapper in _processWrappers) {
                processWrapper.AttachProcessManager(this);
            }
        }

        public static ProcessManager GetByName(string name) {
            try {
                var json = File.ReadAllText(name + ".json");
                var processManager = JsonConvert.DeserializeObject<ProcessManager>(json);
                processManager._name = name;
                return processManager;
            } catch (Exception e) {
                return new ProcessManager(name);
            }
        }
    }
}