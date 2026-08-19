namespace FoliconTest;

/// <summary>
/// Serializes every test class that loads compiled XAML.
///
/// <para>
/// <c>Application.LoadComponent</c> reads BAML through a <c>PackagePart</c> whose internal list
/// of open streams is not thread-safe. Two threads loading compiled XAML out of the same
/// assembly at the same time can corrupt that list, which surfaces as an unrelated-looking
/// <c>ArgumentOutOfRangeException</c> from <c>PackagePart.CleanUpRequestedStreamsList</c>.
/// </para>
///
/// <para>
/// The suite hits this because loading happens on two threads: the shared
/// <see cref="WpfTestHost"/> dispatcher, and the process-wide STA render thread used by
/// <c>StaRenderer.Default</c>. xUnit runs test classes in parallel, so without a shared
/// collection those two can overlap. The defect is in WPF, not in the code under test, and the
/// only fix available here is not to load from two threads at once.
/// </para>
///
/// <para>
/// Membership is by behaviour, not by subject: a class belongs here if it constructs a View,
/// renders a poster icon, or exports a package (which renders a preview).
/// </para>
/// </summary>
[CollectionDefinition(name)]
#pragma warning disable S1118, S2339 // Required by xUnit: non-static class with const for CollectionDefinition attribute
public class XamlLoadingCollection
{
    public const string name = "XAML loading";
}
#pragma warning restore S1118, S2339
