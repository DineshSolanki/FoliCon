namespace FoliCon.Modules
{
    public class DArt : BindableBase
    {
        private string _clientAccessToken;
        private string _clientSecret;
        private string _clientId;

        public string ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        public string ClientSecret
        {
            get => _clientSecret;
            set => SetProperty(ref _clientSecret, value);
        }

        public string ClientAccessToken
        {
            get => _clientAccessToken;
            set => SetProperty(ref _clientAccessToken, value);
        }

        public DArt(string clientSecret, string clientId)
        {
            Services.Tracker.Configure<DArt>()
                .Property(p => p.ClientAccessToken)
                .PersistOn(nameof(PropertyChanged));
            ClientSecret = clientSecret;
            ClientId = clientId;
            Services.Tracker.Track(this);
            GetClientAccessTokenAsync();
        }

        public async void GetClientAccessTokenAsync()
        {
            if (!string.IsNullOrEmpty(ClientAccessToken))
            {
                if (!await IsTokenValidAsync(ClientAccessToken))
                {
                    ClientAccessToken = await GenerateNewAccessToken();
                }
            }
            else
            {
                ClientAccessToken = await GenerateNewAccessToken();
            }
        }

        public static async Task<bool> IsTokenValidAsync(string clientAccessToken)
        {
            var url = "https://www.deviantart.com/api/v1/oauth2/placebo?access_token=" + clientAccessToken;
            using var response = await Services.HttpC.GetAsync(new Uri(url));
            var jsonData = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);

            return tokenResponse.Status == "success";
        }

        private async Task<string> GenerateNewAccessToken()
        {
            var url = "https://www.deviantart.com/oauth2/token?client_id=" + ClientId +
                      "&client_secret=" + ClientSecret +
                      "&grant_type=client_credentials";
            using var response = await Services.HttpC.GetAsync(new Uri(url));
            var jsonData = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<DArtTokenResponse>(jsonData);
            var clientAccessToken = tokenResponse.AccessToken;
            return clientAccessToken;
        }

        public async Task<DArtBrowseResult> Browse(string query, int offset = 0)
        {
            GetClientAccessTokenAsync();
            var url = "https://www.deviantart.com/api/v1/oauth2/browse/popular?timerange=alltime&offset=" + offset +
                      "&q=" + query + " folder icon" +
                      "&limit=20&access_token=" + ClientAccessToken;
            using var response = await Services.HttpC.GetAsync(new Uri(url));
            var jsonData = await response.Content.ReadAsStringAsync();
            var serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var result = JsonConvert.DeserializeObject<DArtBrowseResult>(jsonData, serializerSettings);
            return result;
        }
    }
}