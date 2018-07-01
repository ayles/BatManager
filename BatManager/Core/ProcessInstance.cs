/*using System;
using System.Diagnostics;

namespace BatManager.Core {
    public class ProcessInstance : IDisposable {
        public bool Alive {
            get => _alive;
            private set => _alive = value;
        }

        public event EventHandler Exited;
        public event OutputReceivedHandler OutputReceived;

        public delegate void OutputReceivedHandler(object sender, LogString output);
        
        private Process _process;
        private ProcessStartInfo _processStartInfo;
        private JobObject _jobObject;

        private bool _disposed;
        private volatile bool _alive;
        
        public ProcessInstance(ProcessStartInfo processStartInfo) {
            _processStartInfo = processStartInfo ?? throw new ArgumentNullException(nameof(processStartInfo));
        }

        public void Start() {
            if (_disposed || _alive) return;

            _process = Process.Start(_processStartInfo);
            if (_process == null || _process.HasExited) {
                Dispose();
                return;
            }

            _alive = true;
            
            _jobObject = new JobObject();
            _jobObject.AddProcess(_process);

            _process.EnableRaisingEvents = true;

            _process.OutputDataReceived += (sender, args) => {
                if (string.IsNullOrEmpty(args.Data)) return;
                OutputReceived?.Invoke(this, new LogString(args.Data + Environment.NewLine));
            };

            _process.ErrorDataReceived += (sender, args) => {
                if (string.IsNullOrEmpty(args.Data)) return;
                OutputReceived?.Invoke(this, new LogString(args.Data + Environment.NewLine, LogString.LogType.Error));
            };

            _process.Exited += (sender, args) => {
                _alive = false;
                Dispose();
            };
            
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        public void Dispose() {
            if (_disposed) return;
            _alive = false;
            _jobObject?.Dispose();
            _process?.Dispose();
            Exited?.Invoke(this, EventArgs.Empty);
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        ~ProcessInstance() {
            Dispose();
        }
    }
}*/