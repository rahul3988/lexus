using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Lexus2_0.Desktop.Views
{
    public partial class StatusBar : System.Windows.Controls.UserControl, INotifyPropertyChanged
    {
        private DispatcherTimer? _timer;
        private DateTime _currentDateTime;
        private int _validCount = 2;
        private bool _web2LoginEnabled = false;
        private bool _isPaidLicense = true; // TODO: Check actual license status

        public StatusBar()
        {
            InitializeComponent();
            DataContext = this;
            CurrentDateTime = DateTime.Now;
            StartTimer();
            Unloaded += StatusBar_Unloaded;
        }

        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set
            {
                _currentDateTime = value;
                OnPropertyChanged();
            }
        }

        public int ValidCount
        {
            get => _validCount;
            set
            {
                _validCount = value;
                OnPropertyChanged();
            }
        }

        public bool Web2LoginEnabled
        {
            get => _web2LoginEnabled;
            set
            {
                _web2LoginEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsPaidLicense
        {
            get => _isPaidLicense;
            set
            {
                _isPaidLicense = value;
                OnPropertyChanged();
            }
        }

        private void StartTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => CurrentDateTime = DateTime.Now;
            _timer.Start();
        }

        private void StatusBar_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _timer?.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

