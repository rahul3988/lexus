using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views
{
    public partial class BypassView : UserControl
    {
        private BypassManager? _bypassManager;

        public BypassView()
        {
            InitializeComponent();
            LoadSettings();
        }

        public void SetBypassManager(BypassManager bypassManager)
        {
            _bypassManager = bypassManager;
            LoadSettings();
        }

        private void LoadSettings()
        {
            if (_bypassManager == null) return;

            var settings = _bypassManager.GetSettings();
            CaptchaHandlingCheckBox.IsChecked = settings.CaptchaHandlingEnabled;
            RetryLimitTextBox.Text = settings.RetryLimit.ToString();
            HeadlessRadioButton.IsChecked = settings.BrowserMode == BrowserMode.Headless;
            VisibleRadioButton.IsChecked = settings.BrowserMode == BrowserMode.Visible;
            SessionResetCheckBox.IsChecked = settings.AutoSessionReset;
            CaptchaSolverComboBox.SelectedIndex = settings.CaptchaSolverType switch
            {
                CaptchaSolverType.EasyOCR => 0,
                CaptchaSolverType.Tesseract => 1,
                CaptchaSolverType.Manual => 2,
                _ => 0
            };
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            // Settings will be saved when user clicks Save
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_bypassManager == null)
            {
                MessageBox.Show("Bypass manager not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var settings = new BypassSettings
                {
                    CaptchaHandlingEnabled = CaptchaHandlingCheckBox.IsChecked ?? true,
                    RetryLimit = int.TryParse(RetryLimitTextBox.Text, out var limit) ? limit : 5,
                    BrowserMode = HeadlessRadioButton.IsChecked == true ? BrowserMode.Headless : BrowserMode.Visible,
                    AutoSessionReset = SessionResetCheckBox.IsChecked ?? false,
                    CaptchaSolverType = CaptchaSolverComboBox.SelectedIndex switch
                    {
                        0 => CaptchaSolverType.EasyOCR,
                        1 => CaptchaSolverType.Tesseract,
                        2 => CaptchaSolverType.Manual,
                        _ => CaptchaSolverType.EasyOCR
                    }
                };

                _bypassManager.UpdateSettings(settings);
                MessageBox.Show("Settings saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SbiOtpBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("SBI OTP By-Pass functionality - To be implemented", 
                "SBI OTP Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open SBI OTP Bypass Configuration
        }

        private void HdfcOtpBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("HDFC OTP By-Pass functionality - To be implemented", 
                "HDFC OTP Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open HDFC OTP Bypass Configuration
        }

        private void BhimSbiUpiBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Bhim SBI-Upi Bypass functionality - To be implemented", 
                "BHIM SBI UPI Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open BHIM SBI UPI Bypass Configuration
        }

        private void FreechargeUpiBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Freecharge Upi Bypass functionality - To be implemented", 
                "Freecharge UPI Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open Freecharge UPI Bypass Configuration
        }

        private void PaytmOtpBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PayTM OTP By-Pass functionality - To be implemented", 
                "PayTM OTP Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open PayTM OTP Bypass Configuration
        }

        private void NpciBhimBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NPCI BHIM Bypass functionality - To be implemented", 
                "NPCI BHIM Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open NPCI BHIM Bypass Configuration
        }

        private void AxisBhimUpiBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("AXIS Bhim UPI Add/By-Pass functionality - To be implemented", 
                "AXIS BHIM UPI Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open AXIS BHIM UPI Bypass Configuration
        }

        private void PhonePeBypassButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PhonePe Bypass functionality - To be implemented", 
                "PhonePe Bypass", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open PhonePe Bypass Configuration
        }

        private void CheckTokenButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Check Token functionality - To be implemented", 
                "Check Token", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Verify License Token
        }

        private void PyProxyManagerButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PyProxy manager functionality - To be implemented", 
                "PyProxy Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            // TODO: Open PyProxy Manager tool
        }

        public void HandleMenuItemAction(string action)
        {
            switch (action)
            {
                case "SbiOtpBypass":
                    SbiOtpBypassButton_Click(null, null);
                    break;
                case "HdfcOtpBypass":
                    HdfcOtpBypassButton_Click(null, null);
                    break;
                case "BhimSbiUpiBypass":
                    BhimSbiUpiBypassButton_Click(null, null);
                    break;
                case "FreechargeUpiBypass":
                    FreechargeUpiBypassButton_Click(null, null);
                    break;
                case "PaytmOtpBypass":
                    PaytmOtpBypassButton_Click(null, null);
                    break;
                case "NpciBhimBypass":
                    NpciBhimBypassButton_Click(null, null);
                    break;
                case "CheckToken":
                    CheckTokenButton_Click(null, null);
                    break;
                case "AxisBhimUpiBypass":
                    AxisBhimUpiBypassButton_Click(null, null);
                    break;
                case "PhonePeBypass":
                    PhonePeBypassButton_Click(null, null);
                    break;
                case "PyProxyManager":
                    PyProxyManagerButton_Click(null, null);
                    break;
            }
        }
    }
}
