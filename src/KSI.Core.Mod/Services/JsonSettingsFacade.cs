using System.Text.Json;
using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class JsonSettingsFacade(string rootDirectory)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    internal IKsiSettings ForProvider(string providerId) => new ProviderSettings(this, providerId);

    public bool Exists(string providerId, string fileName) => File.Exists(Resolve(providerId, fileName));

    public T Load<T>(string providerId, string fileName, Func<T> defaults) where T : class
    {
        var path = Resolve(providerId, fileName);
        if (!File.Exists(path)) return defaults();
        try { return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? defaults(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or JsonException or NotSupportedException)
        {
            throw new KsiSettingsReadException(path, ex);
        }
    }

    public void Save<T>(string providerId, string fileName, T value) where T : class
    {
        var path = Resolve(providerId, fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, Options));
        File.Move(temporaryPath, path, true);
    }

    private string Resolve(string providerId, string fileName)
    {
        if (string.IsNullOrWhiteSpace(providerId) || providerId is "." or ".."
            || providerId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || providerId.Contains('/') || providerId.Contains('\\'))
            throw new ArgumentException("Provider ID must be a simple name.", nameof(providerId));
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or ".."
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || Path.GetFileName(fileName) != fileName)
            throw new ArgumentException("Settings file must be a simple file name.", nameof(fileName));
        return Path.Combine(rootDirectory, providerId, fileName);
    }

    private sealed class ProviderSettings(JsonSettingsFacade store, string providerId) : IKsiSettings
    {
        public bool Exists(string fileName) => store.Exists(providerId, fileName);
        public T Load<T>(string fileName, Func<T> defaults) where T : class
            => store.Load(providerId, fileName, defaults);
        public void Save<T>(string fileName, T value) where T : class
            => store.Save(providerId, fileName, value);
    }
}

internal sealed class KsiSettingsReadException(string path, Exception cause)
    : Exception($"Could not read settings at '{path}'. The file was preserved.", cause);
