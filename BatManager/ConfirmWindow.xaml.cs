using System.Windows;
using BatManager.Core;

namespace BatManager {
    /// <summary>
    /// Interaction logic for ConfirmWindow.xaml
    /// </summary>
    public partial class ConfirmWindow {
        public bool Result { get; private set; }
        
        public ConfirmWindow() {
            InitializeComponent();
            Owner = Application.Current?.MainWindow;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        public bool ShowAndGetResult() {
            ShowDialog();
            return Result;
        }
        
        private void ConfirmClick(object sender, RoutedEventArgs e) {
            Result = true;
            Close();
        }
        
        private void RefuseClick(object sender, RoutedEventArgs e) {
            Result = false;
            Close();
        }
    }
}