using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lexus2_0.Desktop.Models
{
    public class PaymentOption : INotifyPropertyChanged
    {
        private string _status = "Active";

        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = ""; // Credit Card, Debit Card, UPI, Net Banking
        public string Gateway { get; set; } = ""; // IRCTC, UPI, etc.
        public string BankName { get; set; } = "";
        public string CardNumber { get; set; } = "";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsPriorBank { get; set; }
        public bool IsBackupBank { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

