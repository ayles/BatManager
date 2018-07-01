using System.Runtime.InteropServices;
using System.Windows;

namespace BatManager {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        [DllImport("Kernel32.dll")]
        public static extern bool AttachConsole(int processId);

        protected override void OnStartup(StartupEventArgs e) {

            AttachConsole(-1);
        }
    }
}