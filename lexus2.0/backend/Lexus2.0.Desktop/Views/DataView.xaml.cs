using System;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.ViewModels;

namespace Lexus2_0.Desktop.Views
{
    public partial class DataView : UserControl
    {
        private DataViewModel? _viewModel;

        public DataViewModel ViewModel
        {
            get => _viewModel ?? throw new InvalidOperationException("ViewModel not initialized");
            set
            {
                _viewModel = value;
                DataContext = _viewModel;
            }
        }

        public DataView()
        {
            InitializeComponent();
        }

        private void AddIrctcIdButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new IrctcIdManagerWindow();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void AddPaymentOptionButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new BankOptionWindow();
            window.Owner = Window.GetWindow(this);
            window.ShowDialog();
        }

        private void ProcessHdfcJioButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Process APP HDFC_DC From JIO functionality - To be implemented", 
                "Process HDFC JIO", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Implement HDFC JIO processing
        }

        private void ProcessHdfcIpayButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Process HDFC_DC From IPAY functionality - To be implemented", 
                "Process HDFC IPAY", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Implement HDFC IPAY processing
        }

        private void ProxyIpSettingButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Proxy IP Setting functionality - To be implemented", 
                "Proxy IP Setting", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Proxy Configuration Window
        }

        private void IpLoginLimitButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("IP Login Limit Set functionality - To be implemented", 
                "IP Login Limit", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open IP Login Limit Configuration
        }

        private void AutoLoginNotAllowedButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Auto login on not allowed functionality - To be implemented", 
                "Auto Login Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Auto Login Configuration
        }

        private void StationSwitchOnButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Station Switch On/Off functionality - To be implemented", 
                "Station Switch", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Toggle Station Switch feature
        }

        private void ShowStnSwitchFormButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Show Station Switch Form functionality - To be implemented", 
                "Station Switch Form", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Station Switch Configuration Form
        }

        private void SubmitOnePairPerFormButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Submit One Pair Per Form functionality - To be implemented", 
                "Submit One Pair Per Form", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Configure form submission logic
        }

        private void InstallWeb2DriverButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Install Web2 Driver functionality - To be implemented", 
                "Install Web2 Driver", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Install Web2 browser driver
        }

        private void InstallRailOneDriverButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Install RailOne Driver functionality - To be implemented", 
                "Install RailOne Driver", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Install RailOne app driver
        }

        private void DownloadWebDriverButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Download Web Driver functionality - To be implemented", 
                "Download Web Driver", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Download/Update web driver
        }

        public void HandleMenuItemAction(string action)
        {
            switch (action)
            {
                case "AddIrctcId":
                    AddIrctcIdButton_Click(null, null);
                    break;
                case "AddPaymentOption":
                    AddPaymentOptionButton_Click(null, null);
                    break;
                case "ProxyIpSetting":
                    ProxyIpSettingButton_Click(null, null);
                    break;
                case "AutoLoginNotAllowed":
                    AutoLoginNotAllowedButton_Click(null, null);
                    break;
                case "StationSwitchOn":
                    StationSwitchOnButton_Click(null, null);
                    break;
                case "ShowStnSwitchForm":
                    ShowStnSwitchFormButton_Click(null, null);
                    break;
                case "SubmitOnePairPerForm":
                    SubmitOnePairPerFormButton_Click(null, null);
                    break;
                case "ProcessHdfcJio":
                    ProcessHdfcJioButton_Click(null, null);
                    break;
                case "InstallWeb2Driver":
                    InstallWeb2DriverButton_Click(null, null);
                    break;
                case "InstallRailOneDriver":
                    InstallRailOneDriverButton_Click(null, null);
                    break;
                case "ProcessHdfcIpay":
                    ProcessHdfcIpayButton_Click(null, null);
                    break;
                case "DownloadWebDriver":
                    DownloadWebDriverButton_Click(null, null);
                    break;
                case "IpLoginLimit":
                    IpLoginLimitButton_Click(null, null);
                    break;
            }
        }
    }
}
