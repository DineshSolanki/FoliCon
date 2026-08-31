using System.Security.Cryptography;
using STJ = System.Text.Json.Serialization;

#nullable enable

namespace FoliCon.Modules.Configuration;

/// <summary>
/// JSON converter that encrypts string values at rest using Windows DPAPI (CurrentUser scope).
/// On read: decrypts Base64-encoded DPAPI ciphertext back to plaintext.
///   If the value cannot be decrypted (e.g. plaintext from a pre-encryption config),
///   returns the raw value so the app doesn't crash. It will be encrypted on next save.
/// On write: encrypts plaintext via DPAPI and writes as Base64 — but only for
/// properties listed in <see cref="DpapiEncryptingConverterFactory.SecretProperties"/>.
/// The property name is set via <see cref="DpapiPropertyContext.CurrentPropertyName"/>
/// by <see cref="DpapiPolymorphicJsonConverter{T}"/> before each value is serialized.
/// </summary>
internal static class DpapiPropertyContext
{
    internal static readonly AsyncLocal<string?> CurrentPropertyName = new();
}

/// <summary>
/// Returns <see cref="DpapiEncryptingConverter"/> for string properties whose names appear
/// in the <see cref="SecretProperties"/> set. Before each property value is serialized,
/// the factory sets <see cref="DpapiPropertyContext.CurrentPropertyName"/> so the shared
/// converter instance can decide whether to encrypt.
/// </summary>
public sealed class DpapiEncryptingConverterFactory : STJ.JsonConverterFactory
{
    private static readonly DpapiEncryptingConverter Instance = new();

    // NOTE: AppConfig is not directly referenced to avoid a circular assembly reference.
    // Property names are listed explicitly so the converter only encrypts intended fields.
    private static readonly HashSet<string> SecretProperties =
    [
        "DeviantArtAccessToken",
        "DeviantArtRefreshToken",
        "DeviantArtClientId",
        "DeviantArtClientSecret",
        "TmdbKey",
        "IgdbClientId",
        "IgdbClientSecret",
    ];

    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(string);

    public override STJ.JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Instance;
    }

    /// <summary>
    /// Called by the custom <see cref="DpapiPolymorphicJsonConverter"/> before serializing
    /// each property value, so the converter knows whether to encrypt.
    /// </summary>
    public static void SetCurrentProperty(string propertyName) => DpapiPropertyContext.CurrentPropertyName.Value = propertyName;

    public static bool IsSecretProperty(string propertyName) => SecretProperties.Contains(propertyName);
}

/// <summary>
/// Encrypts (on write) and decrypts (on read) secret string values using Windows DPAPI.
/// Write is selective: only encrypts when <see cref="DpapiPropertyContext.CurrentPropertyName"/>
/// is a known secret property name set by <see cref="DpapiPolymorphicJsonConverter{T}"/>.
/// Read always attempts decryption; if it fails, returns the raw value (plaintext) so the
/// app continues working even with pre-encryption configs.
/// </summary>
public sealed class DpapiEncryptingConverter : STJ.JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(value);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return System.Text.Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (FormatException)
        {
            // Not valid Base64 — treat as plaintext. Will be encrypted on next save.
            return value;
        }
        catch (CryptographicException)
        {
            // Valid Base64 but not DPAPI-encrypted — treat as plaintext. Will be encrypted on next save.
            return value;
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.WriteStringValue(value);
            return;
        }

        // Only encrypt if the current property being serialized is a known secret
        if (DpapiPropertyContext.CurrentPropertyName.Value is not null &&
            DpapiEncryptingConverterFactory.IsSecretProperty(DpapiPropertyContext.CurrentPropertyName.Value))
        {
            var plainBytes = System.Text.Encoding.UTF8.GetBytes(value);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            writer.WriteStringValue(Convert.ToBase64String(encryptedBytes));
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

/// <summary>
/// Replacement for HandyControls' <c>PolymorphicJsonConverter</c> that also tracks
/// the current property name via <see cref="DpapiEncryptingConverterFactory"/>
/// before serializing each value, enabling selective encryption.
/// </summary>
internal sealed class DpapiPolymorphicJsonConverter<T> : STJ.JsonConverter<T>
{
    public override bool CanConvert(Type typeToConvert) => typeof(T).IsAssignableFrom(typeToConvert);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var property in value.GetType().GetProperties())
        {
            if (!property.CanRead || property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() != null)
                continue;

            var propertyValue = property.GetValue(value);
            writer.WritePropertyName(property.Name);

            // Signal which property is about to be serialized so DpapiEncryptingConverter
            // can decide whether to encrypt based on DpapiPropertyContext.CurrentPropertyName.
            DpapiEncryptingConverterFactory.SetCurrentProperty(property.Name);
            System.Text.Json.JsonSerializer.Serialize(writer, propertyValue, options);
        }
        writer.WriteEndObject();
    }
}
