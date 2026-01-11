using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Lexus2_0.Desktop.Models;
using Lexus2_0.Desktop.Services;

namespace Lexus2_0.Desktop.Views.Pages.Data
{
    public partial class ProxyIpSettingPage : UserControl
    {
        private ProxyService? _proxyService;
        private ObservableCollection<ProxySetting> _proxies = new();
        private ProxySetting? _editingProxy;

        public ProxyService? ProxyService
        {
            get => _proxyService;
            set
            {
                _proxyService = value;
                LoadProxies();
            }
        }

        public ProxyIpSettingPage()
        {
            InitializeComponent();
            ProxyGrid.ItemsSource = _proxies;
        }

        private async void LoadProxies()
        {
            if (_proxyService == null) return;

            try
            {
                var proxies = await _proxyService.GetAllProxySettingsAsync();
                _proxies.Clear();
                foreach (var proxy in proxies)
                {
                    _proxies.Add(proxy);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading proxies: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_proxyService == null)
            {
                MessageBox.Show("Service not initialized", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ip = ProxyIpTextBox.Text;
            var port = ProxyPortTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port))
            {
                MessageBox.Show("Please enter IP and Port", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var setting = new ProxySetting
                {
                    IpAddress = ip,
                    Port = port,
                    Type = "HTTP",
                    Status = "Active"
                };

                if (_editingProxy != null)
                {
                    setting.Id = _editingProxy.Id;
                    await _proxyService.UpdateProxySettingAsync(setting);
                    MessageBox.Show("Proxy updated successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    _editingProxy = null;
                }
                else
                {
                    await _proxyService.CreateProxySettingAsync(setting);
                    MessageBox.Show("Proxy added successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ProxyIpTextBox.Clear();
                ProxyPortTextBox.Clear();
                LoadProxies();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving proxy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditProxy_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ProxySetting proxy)
            {
                _editingProxy = proxy;
                ProxyIpTextBox.Text = proxy.IpAddress;
                ProxyPortTextBox.Text = proxy.Port;
            }
        }

        private async void DeleteProxy_Click(object sender, RoutedEventArgs e)
        {
            if (_proxyService == null) return;

            if (sender is Button btn && btn.Tag is ProxySetting proxy)
            {
                var result = MessageBox.Show($"Are you sure you want to delete proxy {proxy.IpAddress}:{proxy.Port}?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _proxyService.DeleteProxySettingAsync(proxy.Id);
                        LoadProxies();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting proxy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void TestProxy_Click(object sender, RoutedEventArgs e)
        {
            if (_proxyService == null) return;

            if (sender is Button btn && btn.Tag is ProxySetting proxy)
            {
                try
                {
                    var result = await _proxyService.TestProxyAsync(proxy);
                    if (result)
                    {
                        MessageBox.Show("Proxy test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Proxy test failed. Check your proxy settings.", "Test Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    LoadProxies();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error testing proxy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

