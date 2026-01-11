using System;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Automation;
using Lexus2_0.Core.Logging;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Services;
using Lexus2_0.Desktop.ViewModels;
using Lexus2_0.Desktop.Views;
using Lexus2_0.Desktop.Views.Pages;
using Lexus2_0.Desktop.Views.Pages.Data;
using Lexus2_0.Desktop.Views.Pages.Options;
using Lexus2_0.Desktop.Views.Pages.Bypass;

namespace Lexus2_0.Desktop
{
    public partial class MainWindow : Window
    {
        private DataView? _dataView;
        private TicketView? _ticketView;
        private BypassView? _bypassView;
        private ControlView? _optionsView;

        private readonly DatabaseContext _dbContext;
        private readonly TicketService _ticketService;
        private readonly BypassManager _bypassManager;
        private readonly AutomationService _automationService;
        private readonly DataViewModel _dataViewModel;
        private readonly TicketViewModel _ticketViewModel;
        private readonly IrctcAccountService _irctcAccountService;
        private readonly PaymentOptionService _paymentOptionService;
        private readonly ProxyService _proxyService;
        private readonly ApiClientService _apiClientService;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            var logger = new Lexus2_0.Core.Logging.FileLogger();
            var automationEngine = new AutomationEngine(logger);
            
            _dbContext = new DatabaseContext();
            _ticketService = new TicketService(_dbContext);
            
            var bypassSettingsService = new BypassSettingsService();
            _bypassManager = new BypassManager(bypassSettingsService);
            
            _automationService = new AutomationService(automationEngine, _ticketService, _bypassManager, logger);

            // Initialize ViewModels
            _dataViewModel = new DataViewModel(_ticketService);
            _ticketViewModel = new TicketViewModel(_ticketService);

            // Initialize Services
            _irctcAccountService = new IrctcAccountService(_dbContext);
            _paymentOptionService = new PaymentOptionService(_dbContext);
            _proxyService = new ProxyService(_dbContext);
            _apiClientService = new ApiClientService();

            // Wire up AutomationService events to ViewModels
            _automationService.TicketStatusChanged += OnTicketStatusChanged;

            InitializeViews();
            NavigateToView("Data");
        }

        private void InitializeViews()
        {
            _dataView = new DataView();
            _dataView.ViewModel = _dataViewModel;

            _ticketView = new TicketView();
            _ticketView.ViewModel = _ticketViewModel;

            _bypassView = new BypassView();
            _bypassView.SetBypassManager(_bypassManager);

            _optionsView = new ControlView();
            _optionsView.SetAutomationService(_automationService);
        }

        private void OnTicketStatusChanged(object? sender, TicketStatusChangedEventArgs e)
        {
            // Update both ViewModels on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                _dataViewModel.UpdateTicket(e.Ticket);
                _ticketViewModel.UpdateTicket(e.Ticket);
            });
        }

        private void MenuExpander_Expanded(object sender, RoutedEventArgs e)
        {
            // Only one menu expanded at a time
            if (sender is Expander expandedExpander)
            {
                if (expandedExpander != TicketExpander) TicketExpander.IsExpanded = false;
                if (expandedExpander != DataExpander) DataExpander.IsExpanded = false;
                if (expandedExpander != OptionsExpander) OptionsExpander.IsExpanded = false;
                if (expandedExpander != BypassExpander) BypassExpander.IsExpanded = false;
                
                // Navigate to the corresponding view when menu is expanded
                if (expandedExpander == TicketExpander)
                {
                    NavigateToView("Ticket");
                }
                else if (expandedExpander == DataExpander)
                {
                    NavigateToView("Data");
                }
                else if (expandedExpander == OptionsExpander)
                {
                    NavigateToView("Options");
                }
                else if (expandedExpander == BypassExpander)
                {
                    NavigateToView("Bypass");
                }
            }
        }

        private void MenuExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            // Keep the view visible even when menu is collapsed
        }

        private void OpenTicketMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("OpenTicket");
        }

        private void NewTicketMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage("NewTicket");
        }

        private void DataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string action)
            {
                NavigateToPage($"Data_{action}");
            }
        }

        private void OptionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string action)
            {
                NavigateToPage($"Options_{action}");
            }
        }

        private void BypassMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string action)
            {
                NavigateToPage($"Bypass_{action}");
            }
        }

        private void OptionsSettingChanged(object sender, RoutedEventArgs e)
        {
            // This method is no longer used as checkboxes are now buttons that navigate to pages
        }

        private void NavigateToView(string viewName)
        {
            UserControl? view = viewName switch
            {
                "Data" => _dataView,
                "Ticket" => _ticketView,
                "Bypass" => _bypassView,
                "Options" => _optionsView,
                _ => _dataView
            };

            if (view != null)
            {
                ContentArea.Content = view;
            }
        }

        private void NavigateToPage(string pageName)
        {
            UserControl? page = pageName switch
            {
                // Ticket pages
                "OpenTicket" => new OpenTicketPage 
                { 
                    TicketService = _ticketService,
                    AutomationService = _automationService
                },
                "NewTicket" => new NewTicketPage 
                { 
                    TicketService = _ticketService,
                    AccountService = _irctcAccountService,
                    PaymentService = _paymentOptionService
                },
                
                // Data pages
                "Data_AddIrctcId" => new AddIrctcIdPage { AccountService = _irctcAccountService },
                "Data_ProxyIpSetting" => new ProxyIpSettingPage { ProxyService = _proxyService },
                "Data_AddPaymentOption" => new AddPaymentOptionPage { PaymentService = _paymentOptionService },
                "Data_AutoLoginNotAllowed" => CreateDataPage("Auto Login on Not Allowed", "Configure auto-login behavior"),
                "Data_StationSwitchOn" => CreateDataPage("Station Switch On", "Enable/disable station switching"),
                "Data_ShowStnSwitchForm" => CreateDataPage("Show Station Switch Form", "Station switching configuration"),
                "Data_SubmitOnePairPerForm" => CreateDataPage("Submit One Pair Per Form", "Form submission settings"),
                "Data_ProcessHdfcJio" => CreateDataPage("Process APP HDFC_DC From JIO", "Process HDFC payment from JIO app"),
                "Data_InstallWeb2Driver" => CreateDataPage("Install Web2 Driver", "Install Web2 browser driver"),
                "Data_InstallRailOneDriver" => CreateDataPage("Install RailOne Driver", "Install RailOne app driver"),
                "Data_ProcessHdfcIpay" => CreateDataPage("Process HDFC_DC From IPAY", "Process HDFC payment from IPAY"),
                "Data_DownloadWebDriver" => CreateDataPage("Download Web Driver", "Download and update web driver"),
                "Data_IpLoginLimit" => CreateDataPage("IP Login Limit Set", "Configure IP-based login limits"),
                
                // Options pages
                "Options_BackupRestore" => new BackupRestorePage(),
                "Options_KeyInfoVersionCheck" => new KeyInfoVersionCheckPage(),
                "Options_ShowHistory" => new ShowHistoryPage { TicketService = _ticketService },
                "Options_AutoFillCaptcha" => CreateOptionsPage("Auto Fill Captcha", "Configure automatic captcha filling"),
                "Options_AutoSubmitCaptcha" => CreateOptionsPage("AutoSubmit Captcha After Fill", "Configure automatic captcha submission"),
                "Options_StepsOfAdvPayment" => CreateOptionsPage("Steps Of Adv Payment", "Advanced payment configuration steps"),
                "Options_AutoCloseAfterBooking" => CreateOptionsPage("Auto Close After Booking", "Configure auto-close behavior after booking"),
                "Options_VideoMode" => CreateOptionsPage("Video Mode", "Enable/disable video recording mode"),
                "Options_SystemRefresh" => new SystemRefreshPage(),
                "Options_OptimizeSystem" => CreateOptionsPage("Optimize System", "System optimization tools"),
                "Options_CreateIrctcId" => CreateOptionsPage("Create IRCTC ID", "Create new IRCTC account"),
                "Options_BhimUpiDirectPay" => CreateOptionsPage("BHIMUPI Direct Pay", "Configure BHIM UPI direct payment"),
                "Options_SmallWindow" => CreateOptionsPage("Small Window", "Configure small window mode"),
                "Options_PaxCountdownBypass" => CreateOptionsPage("Pax Countdown Bypass", "Configure passenger countdown bypass"),
                "Options_DishaLogin" => CreateOptionsPage("Disha Login On / Off", "Enable/disable Disha login method"),
                "Options_FailIssueFix" => CreateOptionsPage("Fail Issue Fix", "Diagnostic and troubleshooting tools"),
                "Options_ResetLexusId" => new ResetLexusIdPage(),
                
                // Bypass pages
                "Bypass_SbiOtpBypass" => CreateBypassPage("SBI OTP By-Pass", "Configure SBI OTP bypass settings"),
                "Bypass_HdfcOtpBypass" => CreateBypassPage("HDFC OTP By-Pass", "Configure HDFC OTP bypass settings"),
                "Bypass_BhimSbiUpiBypass" => CreateBypassPage("Bhim SBI-UPI Bypass", "Configure BHIM SBI UPI bypass"),
                "Bypass_FreechargeUpiBypass" => CreateBypassPage("Freecharge UPI Bypass", "Configure Freecharge UPI bypass"),
                "Bypass_PaytmOtpBypass" => CreateBypassPage("PayTM OTP By-Pass", "Configure PayTM OTP bypass"),
                "Bypass_NpciBhimBypass" => CreateBypassPage("NPCI BHIM Bypass", "Configure NPCI BHIM bypass"),
                "Bypass_CheckToken" => new CheckTokenPage { ApiClient = _apiClientService },
                "Bypass_AxisBhimUpiBypass" => CreateBypassPage("AXIS Bhim UPI Add / By-Pass", "Configure AXIS BHIM UPI bypass"),
                "Bypass_PhonePeBypass" => CreateBypassPage("PhonePe Bypass", "Configure PhonePe bypass"),
                "Bypass_PyProxyManager" => CreateBypassPage("PyProxy Manager", "Manage PyProxy settings"),
                
                _ => null
            };

            if (page != null)
            {
                ContentArea.Content = page;
            }
            else
            {
                // Fallback to default view
                NavigateToView("Data");
            }
        }

        private UserControl CreateDataPage(string title, string description)
        {
            return new GenericPage(title, description, "Data");
        }

        private UserControl CreateOptionsPage(string title, string description)
        {
            return new GenericPage(title, description, "Options");
        }

        private UserControl CreateBypassPage(string title, string description)
        {
            return new GenericPage(title, description, "Bypass");
        }

        protected override void OnClosed(EventArgs e)
        {
            _automationService?.Dispose();
            _dbContext?.Dispose();
            base.OnClosed(e);
        }
    }
}
