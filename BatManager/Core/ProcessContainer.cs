using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using BatManager.Util;
using Newtonsoft.Json;

namespace BatManager.Core {
    public class ProcessContainer : IDisposable, INotifyPropertyChanged {
        public class ProcessLogHelper {
            public delegate void NewLogStringDelegate(LogString logString);

            public event NewLogStringDelegate NewLogStringEvent;

            public int LogStringsMaxLength { get; } = 200;
            
            private Queue<LogString> _logStrings = new Queue<LogString>();
            private int _newLogStrings;

            public void CopyLogStrings(Queue<LogString> copyTo, bool onlyNew = true) {
                lock (this) {
                    var enm = onlyNew
                        ? _logStrings.Skip(_logStrings.Count - _newLogStrings)
                        : _logStrings.AsEnumerable();
                    foreach (var logString in enm) {
                        copyTo.Enqueue(logString);
                    }

                    _newLogStrings = 0;
                }
            }

            public void ClearLog() {
                _logStrings.Clear();
                _newLogStrings = 0;
                NewLogStringEvent?.Invoke(null);
            }
            
            public void AddLogString(LogString logString) {
                lock (this) {
                    if (_newLogStrings < LogStringsMaxLength) _newLogStrings++;
                    _logStrings.Enqueue(logString);
                    while (_logStrings.Count > LogStringsMaxLength) _logStrings.Dequeue();
                }
                NewLogStringEvent?.Invoke(logString);
            }
        }
        
        public enum ProcessState {
            Dead,
            Alive,
            Restarting
        }
        
        private JobObject _jobObject;
        
        private ProcessStartInfo _processStartInfo;
        private ProcessState _processState = ProcessState.Dead;
        private ProcessLogHelper _logger = new ProcessLogHelper();
        
        private string _name = "New Process";
        private bool _autoRestart;
        private bool _startWithApp;
        private uint _autoRestartDelay = 5000;

        private bool _disposed;
        private Thread _restartThread;

        private ProcessManager _processManager;
        private DelegateCommand _startCommand;
        private DelegateCommand _stopCommand;
        private DelegateCommand _clearLogCommand;
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name {
            get => _name;
            set {
                _name = value;
                OnPropertyChanged("Name");
                _processManager?.Save();
            }
        }

        public string Executable {
            get => _processStartInfo.FileName;
            set {
                string fullPath;
                _processStartInfo.FileName = value;
                if (File.Exists(value)) {
                    _processStartInfo.WorkingDirectory = Path.GetDirectoryName(value);
                }

                OnPropertyChanged("Executable");
                _processManager?.Save();
            }
        }

        public string Arguments {
            get => _processStartInfo.Arguments;
            set {
                _processStartInfo.Arguments = value;
                OnPropertyChanged("Arguments");
                _processManager?.Save();
            }
        }

        public bool AutoRestart {
            get => _autoRestart;
            set {
                _autoRestart = value;
                OnPropertyChanged("AutoRestart");
                _processManager?.Save();
            }
        }

        public bool StartWithApp {
            get => _startWithApp;
            set {
                _startWithApp = value;
                OnPropertyChanged("StartWithApp");
                _processManager?.Save();
            }
        }

        public string AutoRestartDelay {
            get => _autoRestartDelay.ToString();
            set {
                try {
                    _autoRestartDelay = string.IsNullOrEmpty(value) ? 0 : uint.Parse(value);
                    OnPropertyChanged("AutoRestartDelay");
                    _processManager?.Save();
                } catch (Exception) {
                    OnPropertyChanged("AutoRestartDelay");
                }
            }
        }

        [JsonIgnore]
        public ProcessState State {
            get => _processState;
            private set {
                if (value == _processState) return;
                _processState = value;
                OnPropertyChanged("State");
                _startCommand?.RaiseCanExecuteChanged();
                _stopCommand?.RaiseCanExecuteChanged();
                AddLog(DateTime.Now.ToString("(hh:mm:ss)") + " Process is " + value, LogString.LogType.Debug);
            }
        }

        [JsonIgnore]
        public ProcessLogHelper Logger {
            get => _logger;
            private set {
                _logger = value;
                OnPropertyChanged("Logger");
            }
        }

        [JsonIgnore]
        public ICommand StartCommand => _startCommand;

        [JsonIgnore]
        public ICommand StopCommand => _stopCommand;
        
        [JsonIgnore]
        public ICommand ClearLogCommand => _clearLogCommand;

        public ProcessContainer() {
            _processStartInfo = new ProcessStartInfo {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = EncodingManager.EncodingToUse,
                StandardErrorEncoding = EncodingManager.EncodingToUse
            };

            _startCommand = new DelegateCommand(o => { StartProcess(); },
                o => State == ProcessState.Dead);

            _stopCommand = new DelegateCommand(o => { StopProcess(); },
                o => State == ProcessState.Alive || State == ProcessState.Restarting);
            
            _clearLogCommand = new DelegateCommand(o => {
                _logger?.ClearLog();
            });
        }

        public void StartProcess() {
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessContainer));
            if (State != ProcessState.Dead) throw new InvalidOperationException("Cannot start process that is not dead");
            
            Process process = null;
            try {
                process = Process.Start(_processStartInfo);
            } catch (FileNotFoundException e) {
                AddLog("File \"" + Executable + "\" not found", LogString.LogType.Debug);
            } catch (Win32Exception e) {
                AddLog("Cant open file \"" + Executable + "\"", LogString.LogType.Debug);
            } catch (Exception e) {
                AddLog(e + "");
                Console.WriteLine(e);
            }
            
            if (process == null || process.HasExited) {
                AddLog("Cannot start process \"" + Name + "\" (" + Executable + ")", LogString.LogType.Debug);
                return;
            }
            
            var jobObject = new JobObject();
            jobObject.AddProcess(process);

            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (sender, args) => {
                if (string.IsNullOrEmpty(args.Data)) return;
                AddLog(args.Data);
            };
            
            process.ErrorDataReceived += (sender, args) => {
                if (string.IsNullOrEmpty(args.Data)) return;
                AddLog(args.Data, LogString.LogType.Error);
            };

            process.Exited += (sender, args) => {
                jobObject.Dispose();
                State = ProcessState.Dead;
                if (_autoRestart) {
                    RestartProcessShedule();
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            _jobObject = jobObject;
            State = ProcessState.Alive;
        }

        public void StopProcess() {
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessContainer));
            if (State == ProcessState.Dead) return;
            StopRestart();
            _jobObject?.Dispose();
            _jobObject = null;
        }

        public void AttachProcessManager(ProcessManager processManager) {
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessContainer));
            _processManager = processManager;
            if (_startWithApp) StartProcess();
        }

        private void StopRestart() {
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessContainer));
            _restartThread?.Abort();
        }

        private void RestartProcessShedule() {
            if (_disposed) throw new ObjectDisposedException(nameof(ProcessContainer));
            if (State != ProcessState.Dead) return;
            _restartThread = new Thread(() => {
                State = ProcessState.Restarting;
                try {
                    var elapsed = 0;
                    while (elapsed < _autoRestartDelay) {
                        var sleepTime = _autoRestartDelay - elapsed;
                        AddLog(DateTime.Now.ToString("(hh:mm:ss)") + " Restarting in " + sleepTime + " ms", LogString.LogType.Debug);
                        Thread.Sleep((int)(1000 > sleepTime ? sleepTime : 1000));
                        elapsed += 1000;
                    }

                    State = ProcessState.Dead;
                    StartProcess();
                } catch (Exception e) {
                    Console.WriteLine(e);
                    State = ProcessState.Dead;
                } finally {
                    _restartThread = null;
                }
            });
            _restartThread.IsBackground = true;
            _restartThread.Start();
        }

        private void AddLog(string s, LogString.LogType logType = LogString.LogType.Info) {
            _logger.AddLogString(new LogString(s + Environment.NewLine, logType));
        }
        
        private void Save() {
            _processManager?.Save();
        }
        
        public void Dispose() {
            if (_disposed) return;
            StopProcess();
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        ~ProcessContainer() {
            Dispose();
        }
    }
}