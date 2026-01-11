using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lexus2_0.Desktop.Models
{
    /// <summary>
    /// Represents an automation job/ticket
    /// </summary>
    public class Ticket : INotifyPropertyChanged
    {
        private TicketStatus _status;
        private int _attemptCount;
        private int _successCount;
        private int _failureCount;
        private int _captchaFailureCount;
        private DateTime _lastUpdatedTimestamp;
        private string? _errorMessage;

        public int Id { get; set; }
        
        public string TicketId { get; set; } = Guid.NewGuid().ToString();
        
        public TicketStatus Status
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
        
        public int AttemptCount
        {
            get => _attemptCount;
            set
            {
                if (_attemptCount != value)
                {
                    _attemptCount = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int SuccessCount
        {
            get => _successCount;
            set
            {
                if (_successCount != value)
                {
                    _successCount = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int FailureCount
        {
            get => _failureCount;
            set
            {
                if (_failureCount != value)
                {
                    _failureCount = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int CaptchaFailureCount
        {
            get => _captchaFailureCount;
            set
            {
                if (_captchaFailureCount != value)
                {
                    _captchaFailureCount = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public DateTime CreatedTimestamp { get; set; } = DateTime.UtcNow;
        
        public DateTime LastUpdatedTimestamp
        {
            get => _lastUpdatedTimestamp;
            set
            {
                if (_lastUpdatedTimestamp != value)
                {
                    _lastUpdatedTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string? ConfigurationJson { get; set; }

        // Additional ticket display fields
        public string Name { get; set; } = string.Empty;
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string QT { get; set; } = string.Empty; // Quota
        public string GN { get; set; } = string.Empty; // General
        public string CLS { get; set; } = string.Empty; // Class
        public string SL { get; set; } = string.Empty; // Service Level
        public string SLOT { get; set; } = string.Empty; // Slot
        public string Pair { get; set; } = string.Empty;
        public string TrainNo { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PaymentGateway { get; set; } = string.Empty;
        public string UpiId { get; set; } = string.Empty;
        public bool EnableOtpReader { get; set; } = false;
        public int TotalTicket { get; set; } = 0;
        public int WebCount { get; set; } = 0;
        public int AppCount { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum TicketStatus
    {
        Pending,
        Running,
        Completed,
        Failed,
        Cancelled,
        CaptchaFailed
    }
}
