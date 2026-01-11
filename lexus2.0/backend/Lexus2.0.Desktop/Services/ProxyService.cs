using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Lexus2_0.Desktop.DataAccess;
using Lexus2_0.Desktop.Models;

namespace Lexus2_0.Desktop.Services
{
    public class ProxyService
    {
        private readonly DatabaseContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ProxyService(DatabaseContext dbContext, string apiBaseUrl = "http://localhost:5000")
        {
            _dbContext = dbContext;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _apiBaseUrl = apiBaseUrl;
        }

        public async Task<ProxySetting> CreateProxySettingAsync(ProxySetting setting)
        {
            return await _dbContext.CreateProxySettingAsync(setting);
        }

        public async Task<List<ProxySetting>> GetAllProxySettingsAsync()
        {
            return await _dbContext.GetAllProxySettingsAsync();
        }

        public async Task UpdateProxySettingAsync(ProxySetting setting)
        {
            await _dbContext.UpdateProxySettingAsync(setting);
        }

        public async Task DeleteProxySettingAsync(int id)
        {
            await _dbContext.DeleteProxySettingAsync(id);
        }

        public async Task<bool> TestProxyAsync(ProxySetting setting)
        {
            try
            {
                var proxyConfig = new
                {
                    enabled = true,
                    host = setting.IpAddress,
                    port = int.Parse(setting.Port),
                    username = setting.Username,
                    password = setting.Password,
                    type = setting.Type
                };

                var json = JsonSerializer.Serialize(proxyConfig);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/proxy/test", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    setting.Status = "Active";
                    setting.LastTestedDate = DateTime.UtcNow;
                    await UpdateProxySettingAsync(setting);
                    return true;
                }
                else
                {
                    setting.Status = "Failed";
                    setting.LastTestedDate = DateTime.UtcNow;
                    await UpdateProxySettingAsync(setting);
                    return false;
                }
            }
            catch
            {
                setting.Status = "Error";
                setting.LastTestedDate = DateTime.UtcNow;
                await UpdateProxySettingAsync(setting);
                return false;
            }
        }
    }
}

