#nullable enable
using System.Threading.Tasks;
using FoliCon.Models.Protocol;
using Prism.Dialogs;

namespace FoliCon.Modules.Protocol;

/// <summary>
/// Service responsible for parsing, validating, and routing folicon:// universal deeplinks.
/// </summary>
public interface IDeepLinkDispatcher
{
    /// <summary>
    /// Parses a raw folicon:// URI string into a strongly-typed and validated <see cref="DeepLinkRequest"/>.
    /// </summary>
    DeepLinkRequest Parse(string uriString);

    /// <summary>
    /// Dispatches a validated deeplink request to the appropriate UI modal or navigation tab.
    /// </summary>
    Task<bool> DispatchAsync(DeepLinkRequest request, IDialogService dialogService);
}
