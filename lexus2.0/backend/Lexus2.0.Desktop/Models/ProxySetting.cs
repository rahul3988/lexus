using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lexus2_0.Desktop.Models
{
    public class ProxySetting : INotifyPropertyChanged
    {
        private string _status = "Active";

        public int Id { get; set; }
        public string IpAddress { get; set; } = "";
        public string Port { get; set; } = "";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string Type { get; set; } = "HTTP"; // HTTP, HTTPS, SOCKS5
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
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastTestedDate { get; set; } = DateTime.UtcNow;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

