namespace BatManager.Core {
    public class LogString {
        public enum LogType {
            Info,    // For process output
            Error,   // For error output
            Debug,   // For lifecycle output
        }

        public string Data { get; }
        public LogType Type { get; }

        public LogString(string data, LogType type = LogType.Info) {
            Data = data;
            Type = type;
        }

        public override string ToString() {
            return Data;
        }
    }
}