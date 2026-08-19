#nullable enable
using Newtonsoft.Json.Serialization;
using JsonProperty = Newtonsoft.Json.Serialization.JsonProperty;

namespace FoliCon.Modules.Overlays.Designer;

/// <summary>
/// Serializes overlay packages in the exact shape the store contract expects.
///
/// This is deliberately explicit rather than relying on <see cref="JsonConvert"/> defaults:
///
/// - The schema and the catalog-generation CI both read <b>camelCase</b> keys
///   (<c>manifest["id"]</c>). Default Newtonsoft settings emit PascalCase, which would
///   produce a package the CI silently skips.
/// - <c>overlay-schema.json</c> sets <c>additionalProperties: false</c>, so runtime-only
///   fields such as <c>IsBuiltIn</c> must never reach the file.
/// - Output must be byte-identical for identical input so re-exporting does not churn
///   manifest hashes. Fixed indentation and invariant culture guarantee that.
/// </summary>
public static class OverlayPackageSerializer
{
    /// <summary>
    /// Runtime-only members of <see cref="PosterOverlayDefinition"/> that the schema rejects.
    /// <c>OverlayFolderPath</c> already carries <c>[JsonIgnore]</c>; <c>IsBuiltIn</c> does not,
    /// because the provider relies on it in memory.
    /// </summary>
    private static readonly HashSet<string> RuntimeOnlyDefinitionFields =
    [
        nameof(PosterOverlayDefinition.IsBuiltIn),
        nameof(PosterOverlayDefinition.OverlayFolderPath)
    ];

    private static readonly JsonSerializerSettings DefinitionSettings = new()
    {
        ContractResolver = new ExportContractResolver(RuntimeOnlyDefinitionFields),
        Formatting = Formatting.Indented,
        Culture = CultureInfo.InvariantCulture,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    private static readonly JsonSerializerSettings ManifestSettings = new()
    {
        ContractResolver = new ExportContractResolver([]),
        Formatting = Formatting.Indented,
        Culture = CultureInfo.InvariantCulture,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    /// <summary>Serializes <c>overlay.json</c> exactly as the schema defines it.</summary>
    public static string SerializeDefinition(PosterOverlayDefinition definition) =>
        JsonConvert.SerializeObject(definition, DefinitionSettings);

    /// <summary>Serializes <c>manifest.json</c> exactly as the catalog generator reads it.</summary>
    public static string SerializeManifest(OverlayManifest manifest) =>
        JsonConvert.SerializeObject(manifest, ManifestSettings);

    /// <summary>
    /// camelCase naming plus removal of runtime-only members.
    /// </summary>
    private sealed class ExportContractResolver(HashSet<string> excludedMembers) : DefaultContractResolver
    {
        public ExportContractResolver() : this([])
        {
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (excludedMembers.Contains(member.Name))
            {
                property.ShouldSerialize = _ => false;
                property.Ignored = true;
            }

            return property;
        }

        protected override string ResolvePropertyName(string propertyName) =>
            string.IsNullOrEmpty(propertyName)
                ? propertyName
                : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
