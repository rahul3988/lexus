using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages
{
    public partial class NewTicketPage : UserControl
    {
        public class Passenger
        {
            public int SNo { get; set; }
            public string Name { get; set; } = "";
            public int Age { get; set; }
            public string Gender { get; set; } = "";
            public string Seat { get; set; } = "";
            public string Food { get; set; } = "";
            public string Nationality { get; set; } = "India-IN";
            public string Passport { get; set; } = "";
            public bool ChildSeniorBed { get; set; } = false;
        }

        private TicketService? _ticketService;
        private IrctcAccountService? _accountService;
        private PaymentOptionService? _paymentService;
        private StationSearchService? _stationSearchService;
        private ObservableCollection<Passenger> _passengers = new();
        private ObservableCollection<StationInfo> _fromStationSuggestions = new();
        private ObservableCollection<StationInfo> _toStationSuggestions = new();
        private ObservableCollection<StationInfo> _boardingPointSuggestions = new();
        private bool _isSelectingFromStation = false;
        private bool _isSelectingToStation = false;
        private bool _isSelectingBoardingPoint = false;

        public TicketService? TicketService
        {
            get => _ticketService;
            set => _ticketService = value;
        }

        public IrctcAccountService? AccountService
        {
            get => _accountService;
            set
            {
                _accountService = value;
                LoadIrctcAccounts();
            }
        }

        public PaymentOptionService? PaymentService
        {
            get => _paymentService;
            set
            {
                _paymentService = value;
                LoadPaymentOptions();
            }
        }

        public NewTicketPage()
        {
            InitializeComponent();
            // Initialize station search service
            // You can get API key from environment variable
            // To use Indian Rail API, get API key from http://indianrailapi.com/api-collection
            // Set environment variable: INDIAN_RAIL_API_KEY=your_api_key
            var apiKey = Environment.GetEnvironmentVariable("INDIAN_RAIL_API_KEY");
            _stationSearchService = new StationSearchService(apiKey);
            InitializeData();
            
            // Attach keyboard handlers to editable ComboBoxes after they're loaded
            this.Loaded += NewTicketPage_Loaded;
        }

        private void NewTicketPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Attach PreviewKeyDown to TextBox inside editable ComboBoxes
            AttachTextBoxKeyboardHandlers(FromStationComboBox);
            AttachTextBoxKeyboardHandlers(ToStationComboBox);
            AttachTextBoxKeyboardHandlers(BoardingPointComboBox);
        }

        private void AttachTextBoxKeyboardHandlers(ComboBox comboBox)
        {
            if (comboBox.IsEditable)
            {
                // Find the TextBox inside the ComboBox
                comboBox.ApplyTemplate();
                var textBox = comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBoxBase;
                
                if (textBox != null)
                {
                    textBox.PreviewKeyDown += (sender, e) =>
                    {
                        // Handle arrow keys to navigate dropdown
                        if (e.Key == Key.Down || e.Key == Key.Up)
                        {
                            if (!comboBox.IsDropDownOpen)
                            {
                                // Open dropdown and set initial selection
                                if (comboBox.Items.Count > 0)
                                {
                                    comboBox.IsDropDownOpen = true;
                                    if (comboBox.SelectedIndex < 0)
                                    {
                                        comboBox.SelectedIndex = e.Key == Key.Down ? 0 : comboBox.Items.Count - 1;
                                    }
                                    e.Handled = true;
                                }
                            }
                            // If dropdown is already open, don't handle - let ComboBox navigate naturally
                        }
                        // Handle Enter to select
                        else if (e.Key == Key.Enter)
                        {
                            if (comboBox.IsDropDownOpen)
                            {
                                // Select the highlighted/first matching item
                                if (comboBox.Items.Count > 0)
                                {
                                    var searchText = comboBox.Text?.Trim() ?? "";
                                    object? itemToSelect = null;
                                    
                                    // Try to find matching item based on text
                                    foreach (var item in comboBox.Items)
                                    {
                                        if (item is StationInfo station)
                                        {
                                            if (station.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
                                                station.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                            {
                                                itemToSelect = item;
                                                break;
                                            }
                                        }
                                    }
                                    
                                    // If no text match, use selected index or first item
                                    if (itemToSelect == null)
                                    {
                                        if (comboBox.SelectedIndex >= 0 && comboBox.SelectedIndex < comboBox.Items.Count)
                                        {
                                            itemToSelect = comboBox.Items[comboBox.SelectedIndex];
                                        }
                                        else if (comboBox.Items.Count > 0)
                                        {
                                            itemToSelect = comboBox.Items[0];
                                        }
                                    }
                                    
                                    if (itemToSelect != null)
                                    {
                                        // Set appropriate flag based on which ComboBox
                                        if (comboBox == FromStationComboBox)
                                            _isSelectingFromStation = true;
                                        else if (comboBox == ToStationComboBox)
                                            _isSelectingToStation = true;
                                        else if (comboBox == BoardingPointComboBox)
                                            _isSelectingBoardingPoint = true;
                                        
                                        try
                                        {
                                            comboBox.SelectedItem = itemToSelect;
                                            if (itemToSelect is StationInfo station)
                                            {
                                                comboBox.Text = station.Code;
                                            }
                                            comboBox.IsDropDownOpen = false;
                                            e.Handled = true;
                                        }
                                        finally
                                        {
                                            if (comboBox == FromStationComboBox)
                                                _isSelectingFromStation = false;
                                            else if (comboBox == ToStationComboBox)
                                                _isSelectingToStation = false;
                                            else if (comboBox == BoardingPointComboBox)
                                                _isSelectingBoardingPoint = false;
                                        }
                                    }
                                }
                            }
                        }
                        // Handle Escape to close dropdown
                        else if (e.Key == Key.Escape)
                        {
                            if (comboBox.IsDropDownOpen)
                            {
                                comboBox.IsDropDownOpen = false;
                                e.Handled = true;
                            }
                        }
                    };
                }
            }
        }

        private async void LoadIrctcAccounts()
        {
            if (_accountService == null) return;

            try
            {
                var accounts = await _accountService.GetAllAccountsAsync();
                IrctcIdComboBox.ItemsSource = accounts;
                IrctcIdComboBox.DisplayMemberPath = "IrctcId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading IRCTC accounts: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPaymentOptions()
        {
            if (_paymentService == null) return;

            try
            {
                var options = await _paymentService.GetAllPaymentOptionsAsync();
                PriorBankComboBox.ItemsSource = options.Where(o => o.IsPriorBank).ToList();
                BackupBankComboBox.ItemsSource = options.Where(o => o.IsBackupBank).ToList();
                PriorBankComboBox.DisplayMemberPath = "Name";
                BackupBankComboBox.DisplayMemberPath = "Name";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payment options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeData()
        {
            // Initialize passenger grid with 6 empty passengers
            for (int i = 1; i <= 6; i++)
            {
                _passengers.Add(new Passenger { SNo = i, Nationality = "India-IN" });
            }
            PassengerDataGrid.ItemsSource = _passengers;
            
            // Set default date to tomorrow
            JourneyDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            
            // Format date display
            JourneyDatePicker.SelectedDateFormat = DatePickerFormat.Short;
            
            // Initialize station combo boxes
            FromStationComboBox.ItemsSource = _fromStationSuggestions;
            ToStationComboBox.ItemsSource = _toStationSuggestions;
            BoardingPointComboBox.ItemsSource = _boardingPointSuggestions;
        }
        
        private async void FromStation_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't process if we're programmatically setting the selection
            if (_isSelectingFromStation) return;
            
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                var query = comboBox.Text;
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    _fromStationSuggestions.Clear();
                    FromStationComboBox.ItemsSource = null;
                    return;
                }
                
                var results = await _stationSearchService.SearchStationsAsync(query);
                _fromStationSuggestions.Clear();
                foreach (var station in results)
                {
                    _fromStationSuggestions.Add(station);
                }
                
                // Set ItemsSource to show suggestions
                FromStationComboBox.ItemsSource = _fromStationSuggestions;
                comboBox.IsDropDownOpen = _fromStationSuggestions.Count > 0;
            }
        }
        
        private void FromStation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is StationInfo station)
            {
                _isSelectingFromStation = true;
                try
                {
                    // Set the text to show station code
                    comboBox.Text = station.Code;
                    // Keep ItemsSource set so the selected item displays properly
                    FromStationComboBox.ItemsSource = _fromStationSuggestions;
                    // Close dropdown after selection
                    comboBox.IsDropDownOpen = false;
                }
                finally
                {
                    _isSelectingFromStation = false;
                }
            }
        }
        
        private async void ToStation_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't process if we're programmatically setting the selection
            if (_isSelectingToStation) return;
            
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                var query = comboBox.Text;
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    _toStationSuggestions.Clear();
                    ToStationComboBox.ItemsSource = null;
                    return;
                }
                
                var results = await _stationSearchService.SearchStationsAsync(query);
                _toStationSuggestions.Clear();
                foreach (var station in results)
                {
                    _toStationSuggestions.Add(station);
                }
                
                // Set ItemsSource to show suggestions
                ToStationComboBox.ItemsSource = _toStationSuggestions;
                comboBox.IsDropDownOpen = _toStationSuggestions.Count > 0;
            }
        }
        
        private void ToStation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is StationInfo station)
            {
                _isSelectingToStation = true;
                try
                {
                    // Set the text to show station code
                    comboBox.Text = station.Code;
                    // Keep ItemsSource set so the selected item displays properly
                    ToStationComboBox.ItemsSource = _toStationSuggestions;
                    // Close dropdown after selection
                    comboBox.IsDropDownOpen = false;
                }
                finally
                {
                    _isSelectingToStation = false;
                }
            }
        }
        
        private async void BoardingPoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Don't process if we're programmatically setting the selection
            if (_isSelectingBoardingPoint) return;
            
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                var query = comboBox.Text;
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    _boardingPointSuggestions.Clear();
                    BoardingPointComboBox.ItemsSource = null;
                    return;
                }
                
                var results = await _stationSearchService.SearchStationsAsync(query);
                _boardingPointSuggestions.Clear();
                foreach (var station in results)
                {
                    _boardingPointSuggestions.Add(station);
                }
                
                // Set ItemsSource to show suggestions
                BoardingPointComboBox.ItemsSource = _boardingPointSuggestions;
                comboBox.IsDropDownOpen = _boardingPointSuggestions.Count > 0;
            }
        }
        
        private void BoardingPoint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is StationInfo station)
            {
                _isSelectingBoardingPoint = true;
                try
                {
                    // Set the text to show station code
                    comboBox.Text = station.Code;
                    // Keep ItemsSource set so the selected item displays properly
                    BoardingPointComboBox.ItemsSource = _boardingPointSuggestions;
                    // Close dropdown after selection
                    comboBox.IsDropDownOpen = false;
                }
                finally
                {
                    _isSelectingBoardingPoint = false;
                }
            }
        }

        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Handle Enter key to select highlighted item
                if (e.Key == Key.Enter)
                {
                    if (comboBox.IsDropDownOpen)
                    {
                        // For editable ComboBox, we need to handle selection differently
                        if (comboBox.IsEditable)
                        {
                            // When dropdown is open and Enter is pressed, select the highlighted item
                            // The ComboBox internally tracks the highlighted index
                            if (comboBox.Items.Count > 0)
                            {
                                // Try to find matching item based on text
                                var searchText = comboBox.Text?.Trim() ?? "";
                                object? itemToSelect = null;
                                
                                // First, try to find exact match
                                foreach (var item in comboBox.Items)
                                {
                                    if (item is StationInfo station)
                                    {
                                        if (station.Code.Equals(searchText, StringComparison.OrdinalIgnoreCase) ||
                                            station.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                                        {
                                            itemToSelect = item;
                                            break;
                                        }
                                    }
                                }
                                
                                // If no match found, use selected index or first item
                                if (itemToSelect == null)
                                {
                                    if (comboBox.SelectedIndex >= 0 && comboBox.SelectedIndex < comboBox.Items.Count)
                                    {
                                        itemToSelect = comboBox.Items[comboBox.SelectedIndex];
                                    }
                                    else if (comboBox.Items.Count > 0)
                                    {
                                        itemToSelect = comboBox.Items[0];
                                    }
                                }
                                
                                if (itemToSelect != null)
                                {
                                    // Set appropriate flag based on which ComboBox
                                    if (comboBox == FromStationComboBox)
                                        _isSelectingFromStation = true;
                                    else if (comboBox == ToStationComboBox)
                                        _isSelectingToStation = true;
                                    else if (comboBox == BoardingPointComboBox)
                                        _isSelectingBoardingPoint = true;
                                    
                                    try
                                    {
                                        comboBox.SelectedItem = itemToSelect;
                                        
                                        // Update text based on selected item
                                        if (itemToSelect is StationInfo station)
                                        {
                                            comboBox.Text = station.Code;
                                        }
                                        
                                        comboBox.IsDropDownOpen = false;
                                        e.Handled = true;
                                    }
                                    finally
                                    {
                                        if (comboBox == FromStationComboBox)
                                            _isSelectingFromStation = false;
                                        else if (comboBox == ToStationComboBox)
                                            _isSelectingToStation = false;
                                        else if (comboBox == BoardingPointComboBox)
                                            _isSelectingBoardingPoint = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // For non-editable ComboBox, standard behavior
                            comboBox.IsDropDownOpen = false;
                            e.Handled = true;
                        }
                    }
                }
                // Handle Escape key to close dropdown
                else if (e.Key == Key.Escape)
                {
                    if (comboBox.IsDropDownOpen)
                    {
                        comboBox.IsDropDownOpen = false;
                        e.Handled = true;
                    }
                }
                // Handle Arrow keys - ensure dropdown opens and navigation works
                else if (e.Key == Key.Down || e.Key == Key.Up)
                {
                    if (comboBox.IsEditable)
                    {
                        // For editable ComboBox, open dropdown if not open
                        if (!comboBox.IsDropDownOpen && comboBox.Items.Count > 0)
                        {
                            comboBox.IsDropDownOpen = true;
                            // Set initial selection
                            if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
                            {
                                comboBox.SelectedIndex = e.Key == Key.Down ? 0 : comboBox.Items.Count - 1;
                            }
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        // For non-editable, ensure dropdown opens
                        if (!comboBox.IsDropDownOpen && comboBox.Items.Count > 0)
                        {
                            comboBox.IsDropDownOpen = true;
                            if (comboBox.SelectedIndex < 0 && comboBox.Items.Count > 0)
                            {
                                comboBox.SelectedIndex = e.Key == Key.Down ? 0 : comboBox.Items.Count - 1;
                            }
                            e.Handled = true;
                        }
                    }
                }
            }
        }

        private async void FromStation_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                // If no text entered, show some common stations
                if (string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    var commonStations = await _stationSearchService.SearchStationsAsync("A");
                    _fromStationSuggestions.Clear();
                    foreach (var station in commonStations.Take(20))
                    {
                        _fromStationSuggestions.Add(station);
                    }
                    FromStationComboBox.ItemsSource = _fromStationSuggestions;
                }
                else if (_fromStationSuggestions.Count > 0)
                {
                    // Ensure ItemsSource is set when dropdown opens
                    FromStationComboBox.ItemsSource = _fromStationSuggestions;
                }
            }
        }

        private async void ToStation_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                // If no text entered, show some common stations
                if (string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    var commonStations = await _stationSearchService.SearchStationsAsync("A");
                    _toStationSuggestions.Clear();
                    foreach (var station in commonStations.Take(20))
                    {
                        _toStationSuggestions.Add(station);
                    }
                    ToStationComboBox.ItemsSource = _toStationSuggestions;
                }
                else if (_toStationSuggestions.Count > 0)
                {
                    // Ensure ItemsSource is set when dropdown opens
                    ToStationComboBox.ItemsSource = _toStationSuggestions;
                }
            }
        }

        private async void BoardingPoint_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is ComboBox comboBox && _stationSearchService != null)
            {
                // If no text entered, show some common stations
                if (string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    var commonStations = await _stationSearchService.SearchStationsAsync("A");
                    _boardingPointSuggestions.Clear();
                    foreach (var station in commonStations.Take(20))
                    {
                        _boardingPointSuggestions.Add(station);
                    }
                    BoardingPointComboBox.ItemsSource = _boardingPointSuggestions;
                }
                else if (_boardingPointSuggestions.Count > 0)
                {
                    // Ensure ItemsSource is set when dropdown opens
                    BoardingPointComboBox.ItemsSource = _boardingPointSuggestions;
                }
            }
        }
        
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate search criteria
            if (string.IsNullOrWhiteSpace(FromStationComboBox.Text))
            {
                MessageBox.Show("Please enter From Station", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ToStationComboBox.Text))
            {
                MessageBox.Show("Please enter To Station", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (JourneyDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select Journey Date", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Get station codes
                var fromStation = FromStationComboBox.SelectedItem as StationInfo;
                var toStation = ToStationComboBox.SelectedItem as StationInfo;
                
                var fromCode = fromStation?.Code ?? FromStationComboBox.Text;
                var toCode = toStation?.Code ?? ToStationComboBox.Text;
                var date = JourneyDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");

                // Search trains using GraphQL API
                if (_stationSearchService != null)
                {
                    var trains = await _stationSearchService.SearchTrainsAsync(fromCode, toCode, date);
                    
                    if (trains.Count > 0)
                    {
                        var trainList = string.Join("\n", trains.Take(10).Select(t => $"{t.Number} - {t.Name}"));
                        MessageBox.Show($"Found {trains.Count} train(s):\n\n{trainList}", 
                            "Trains Found", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // TODO: Show trains in a dialog or grid for selection
                    }
                    else
                    {
                        MessageBox.Show("No trains found for the selected route and date.", 
                            "No Trains", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching trains: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void AvailabilityLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: Open availability check window/page
            MessageBox.Show("Availability check - To be implemented", "Availability", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GetFareButton_Click(object sender, RoutedEventArgs e)
        {
            // Calculate fare based on journey details
            var from = FromStationComboBox.Text;
            var to = ToStationComboBox.Text;
            var trainNo = TrainNoTextBox.Text;
            var selectedClass = (ClassComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
            {
                MessageBox.Show("Please enter From and To stations", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (selectedClass == "Select Class" || string.IsNullOrEmpty(selectedClass))
            {
                MessageBox.Show("Please select a Class", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Simulate fare calculation
            var baseFare = 500.0;
            var fare = baseFare * (selectedClass == "1A" ? 3.0 : selectedClass == "2A" ? 2.0 : selectedClass == "3A" ? 1.5 : 1.0);
            
            FareTextBlock.Text = $"Fare: Base ₹{fare:F2}";
            
            // Set ticket slot if not already set
            if (TicketSlotComboBox.SelectedIndex == 0)
            {
                TicketSlotComboBox.SelectedIndex = 1; // Select first slot
            }
            
            MessageBox.Show($"Estimated Fare: ₹{fare:F2}\n\nNote: Actual fare may vary.", "Fare Calculation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketService == null)
            {
                MessageBox.Show("Service not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Validate form
            if (string.IsNullOrWhiteSpace(FromStationComboBox.Text))
            {
                MessageBox.Show("Please enter From Station", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ToStationComboBox.Text))
            {
                MessageBox.Show("Please enter To Station", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (JourneyDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select Journey Date", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrWhiteSpace(MobileNoTextBox.Text))
            {
                MessageBox.Show("Please enter Mobile Number", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Validate passengers
                var validPassengers = _passengers.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();
                if (validPassengers.Count == 0)
                {
                    MessageBox.Show("Please enter at least one passenger name", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Create ticket
                var selectedClass = (ClassComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (selectedClass == "Select Class") selectedClass = "SL";
                
                var selectedQuota = (QuotaComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General";
                var journeyDate = JourneyDatePicker.SelectedDate.Value;
                var ticketName = !string.IsNullOrWhiteSpace(SaveNameTextBox.Text) 
                    ? SaveNameTextBox.Text 
                    : $"{FromStationComboBox.Text}_{ToStationComboBox.Text}_{journeyDate:ddMMyyyy}";
                
                var selectedSlot = (TicketSlotComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Select_Auto Slot";
                var irctcId = (IrctcIdComboBox.SelectedItem as IrctcAccount)?.IrctcId ?? IrctcIdTextBox.Text;
                var paymentMethod = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "IRCTC";
                
                var ticket = new Ticket
                {
                    TicketId = Guid.NewGuid().ToString(),
                    Status = TicketStatus.Pending,
                    Name = ticketName,
                    From = FromStationComboBox.Text,
                    To = ToStationComboBox.Text,
                    Date = journeyDate.ToString("dd-MMM-yyyy"),
                    TrainNo = TrainNoTextBox.Text,
                    CLS = selectedClass ?? "SL",
                    QT = selectedQuota,
                    GN = selectedQuota == "General" ? "GN" : "",
                    SL = selectedClass ?? "SL",
                    SLOT = selectedSlot,
                    Username = irctcId,
                    PaymentGateway = paymentMethod,
                    TotalTicket = validPassengers.Count,
                    ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        FromStation = FromStationComboBox.Text,
                        BoardingPoint = BoardingPointComboBox.Text,
                        ToStation = ToStationComboBox.Text,
                        JourneyDate = journeyDate.ToString("yyyy-MM-dd"),
                        TrainNo = TrainNoTextBox.Text,
                        TrainType = (TrainTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        Class = selectedClass,
                        Quota = selectedQuota,
                        MobileNo = MobileNoTextBox.Text,
                        TicketSlot = selectedSlot,
                        TravelInsurance = TravelInsuranceCheckBox.IsChecked ?? false,
                        ConfirmBerths = ConfirmBerthsCheckBox.IsChecked ?? false,
                        Passengers = validPassengers.Select(p => new
                        {
                            p.Name,
                            p.Age,
                            p.Gender,
                            p.Seat,
                            p.Food,
                            p.Nationality,
                            p.Passport,
                            p.ChildSeniorBed
                        }).ToList()
                    })
                };

                await _ticketService.CreateTicketAsync(ticket);
                MessageBox.Show($"Ticket saved successfully!\nTicket Name: {ticket.Name}\n\nPlease Save Data Carefully ... Avoid Mistake in Names and Age", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Clear form
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ResetForm()
        {
            FromStationComboBox.Text = "";
            FromStationComboBox.SelectedItem = null;
            BoardingPointComboBox.Text = "";
            BoardingPointComboBox.SelectedItem = null;
            ToStationComboBox.Text = "";
            ToStationComboBox.SelectedItem = null;
            TrainNoTextBox.Clear();
            MobileNoTextBox.Clear();
            SaveNameTextBox.Clear();
            IrctcIdTextBox.Clear();
            FareTextBlock.Text = "Fare: Base";
            JourneyDatePicker.SelectedDate = DateTime.Now.AddDays(1);
            ClassComboBox.SelectedIndex = 0;
            QuotaComboBox.SelectedIndex = 0;
            TrainTypeComboBox.SelectedIndex = 0;
            TicketSlotComboBox.SelectedIndex = 0;
            PaymentMethodComboBox.SelectedIndex = 0;
            TravelInsuranceCheckBox.IsChecked = false;
            ConfirmBerthsCheckBox.IsChecked = false;
            _passengers.Clear();
            for (int i = 1; i <= 6; i++)
            {
                _passengers.Add(new Passenger { SNo = i, Nationality = "India-IN" });
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear form or navigate back
            var result = MessageBox.Show("Are you sure you want to cancel? All entered data will be lost.", 
                "Cancel Booking", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                ResetForm();
            }
        }
    }
}

