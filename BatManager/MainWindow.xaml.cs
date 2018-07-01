using BatManager.Core;

namespace BatManager {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        
        public MainWindow() {
            InitializeComponent();
            var pm = ProcessManager.GetByName("processes");
            DataContext = pm;
            TabsList.ItemsSource = pm.ProcessWrappers;
            Closing += (sender, args) => { SettingsManager.Instance.Save(); };
        }
    }
}