using FoliCon.Modules;
using IGDB;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoliCon.Models
{
    public class IGDBjotTrackerStore : BindableBase, ITokenStore
    {
        private TwitchAccessToken _currentToken;
        private TwitchAccessToken CurrentToken { get => _currentToken; set => SetProperty(ref _currentToken, value); }
       public IGDBjotTrackerStore()
        {
            Services.Tracker.Configure<IGDBjotTrackerStore>()
                .Property(p => p._currentToken)
                .PersistOn(nameof(PropertyChanged));
            Services.Tracker.Track(this);

        }
        public Task<TwitchAccessToken> GetTokenAsync()
        {
            return Task.FromResult(CurrentToken);
        }

        public Task<TwitchAccessToken> StoreTokenAsync(TwitchAccessToken token)
        {
            CurrentToken = token;
            return Task.FromResult(token);
        }
    }
}
