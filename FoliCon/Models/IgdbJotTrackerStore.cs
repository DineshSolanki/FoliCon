using IGDB;
using Prism.Mvvm;
using System.Threading.Tasks;

namespace FoliCon.Models
{
    public class IgdbJotTrackerStore : BindableBase, ITokenStore
    {
        private TwitchAccessToken _currentToken;
        private TwitchAccessToken CurrentToken { get => _currentToken; set => SetProperty(ref _currentToken, value); }
        public IgdbJotTrackerStore()
        {
            Services.Tracker.Configure<IgdbJotTrackerStore>()
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
