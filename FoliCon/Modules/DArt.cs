using FoliCon.Models;
using Newtonsoft.Json;
using Prism.Mvvm;
using System;
using System.Threading.Tasks;

namespace FoliCon.Modules
{
    public class DArt : BindableBase
    {
        private string _clientAccessToken;
        private string _clientSecret;
        private string _clientID;
        public string ClientID { get => _clientID; set => SetProperty(ref _clientID, value); }
        public string ClientSecret { get => _clientSecret; set => SetProperty(ref _clientSecret, value); }
        public string ClientAccessToken { get => _clientAccessToken; set => SetProperty(ref _clientAccessToken, value); }

        public DArt(string clientSecret, string clientId)
        {
            Services.Tracker.Configure<DArt>()
                .Property(p => p._clientAccessToken)
                .PersistOn(nameof(PropertyChanged));
            ClientSecret = clientSecret;
            ClientID = clientId;
            GetClientAccessTokenAsync();
        }

        public async void GetClientAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(_clientAccessToken))
            {
                if (!(await IsTokenValidAsync(_clientAccessToken)))
                {
                    _clientAccessToken = await GenerateNewAccessToken();
                }
            }
            else
            {
                _clientAccessToken = await GenerateNewAccessToken();
            }
        }

        public async Task<bool> IsTokenValidAsync(string clientAccessToken)
        {
            var url = "https://www.deviantart.com/api/v1/oauth2/placebo?access_token=" + clientAccessToken;
            DArtTokenResponse tokenResponse = null;
            using (var response = await Services.HttpC.GetAsync(new Uri(url)))
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);
            }
            if (tokenResponse.Status == "success")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<string> GenerateNewAccessToken()
        {
            var clientAccessToken = "";
            var url = "https://www.deviantart.com/oauth2/token?client_id=" + ClientID +
                      "&client_secret=" + ClientSecret +
                      "&grant_type=client_credentials";
            using (var response = await Services.HttpC.GetAsync(new Uri(url)))
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);
                clientAccessToken = tokenResponse.AccessToken;
            }
            return clientAccessToken;
        }

        public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
        {
            GetClientAccessTokenAsync();
            var url = "https://www.deviantart.com/api/v1/oauth2/browse/popular?timerange=alltime&offset=" + offset + "&category_path=customization%2ficons%2fos%2fwin&q=" + query + " folder icon" + "&limit=20&access_token=" + _clientAccessToken;
            DArtBrowseResult result = null;
            using (var response = await Services.HttpC.GetAsync(new Uri(url)))
            {
                var jsonData = await response.Content.ReadAsStringAsync();
                var serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                result = JsonConvert.DeserializeObject<DArtBrowseResult>(jsonData, serializerSettings);
            }
            return result;
        }
    }
}