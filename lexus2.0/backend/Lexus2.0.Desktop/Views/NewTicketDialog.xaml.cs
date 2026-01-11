using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lexus2_0.Core.Models;
using Lexus2_0.Desktop.Models;

namespace Lexus2_0.Desktop.Views
{
    public partial class NewTicketDialog : Window
    {
        public Ticket? CreatedTicket { get; private set; }
        public BookingConfig? CreatedBookingConfig { get; private set; }

        private ObservableCollection<PassengerGridItem> _passengers;

        public NewTicketDialog()
        {
            InitializeComponent();
            _passengers = new ObservableCollection<PassengerGridItem>();
            PassengerDataGrid.ItemsSource = _passengers;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(FromStationTextBox.Text))
                {
                    MessageBox.Show("Please enter From Station.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ToStationTextBox.Text))
                {
                    MessageBox.Show("Please enter To Station.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (TravelDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Please select Travel Date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(TrainNoTextBox.Text))
                {
                    MessageBox.Show("Please enter Train No.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(MobileNoTextBox.Text))
                {
                    MessageBox.Show("Please enter Mobile No.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get passengers
                var passengers = new List<PassengerDetail>();
                foreach (var item in _passengers)
                {
                    if (!string.IsNullOrWhiteSpace(item.Name) && item.Age > 0)
                    {
                        passengers.Add(new PassengerDetail
                        {
                            Name = item.Name,
                            Age = item.Age,
                            Gender = item.Gender ?? "Male",
                            Seat = item.Seat ?? "No Preference",
                            Food = item.Food ?? "No Food"
                        });
                    }
                }

                if (passengers.Count == 0)
                {
                    MessageBox.Show("Please add at least one passenger.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Build BookingConfig
                var bookingConfig = new BookingConfig
                {
                    SourceStation = FromStationTextBox.Text.Trim(),
                    DestinationStation = ToStationTextBox.Text.Trim(),
                    TravelDate = TravelDatePicker.SelectedDate.Value.ToString("dd-MM-yyyy"),
                    TrainNo = TrainNoTextBox.Text.Trim(),
                    TrainCoach = (ClassComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "SL",
                    PassengerDetails = passengers
                };

                // Set Quota
                var quota = (QuotaComboBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "General";
                bookingConfig.Tatkal = quota == "Tatkal";
                bookingConfig.PremiumTatkal = quota == "Premium Tatkal";

                CreatedBookingConfig = bookingConfig;

                // Create Ticket for compatibility
                CreatedTicket = new Ticket
                {
                    Status = TicketStatus.Pending,
                    ConfigurationJson = System.Text.Json.JsonSerializer.Serialize(bookingConfig)
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    // Helper class for DataGrid binding
    public class PassengerGridItem
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Gender { get; set; } = "Male";
        public string? Seat { get; set; } = "No Preference";
        public string? Food { get; set; } = "No Food";

        public List<string> GenderOptions { get; } = new List<string> { "Male", "Female", "Other" };
        public List<string> SeatOptions { get; } = new List<string> 
        { 
            "No Preference", 
            "Lower", 
            "Middle", 
            "Upper", 
            "Side Lower", 
            "Side Upper" 
        };
        public List<string> FoodOptions { get; } = new List<string> 
        { 
            "No Food", 
            "Vegetarian", 
            "Non-Vegetarian" 
        };
    }
}
