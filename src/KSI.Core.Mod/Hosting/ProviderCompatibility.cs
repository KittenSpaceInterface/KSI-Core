using System.Reflection;
using System.Text.Json;
using KSI.Core.Api.Providers;

namespace KSI.Core.Mod.Hosting;

internal static class ProviderCompatibility
{
    internal static bool ReferencesApi(Assembly assembly) => assembly.GetReferencedAssemblies()
        .Any(reference => reference.Name == typeof(IKsiUiProvider).Assembly.GetName().Name);

    internal static bool IsCompatible(Assembly assembly, out string error)
    {
        var name = assembly.GetName().Name!;
        var metadataPath = Path.Combine(Path.GetDirectoryName(assembly.Location)!,
            $"{name}.ksi-provider.json");
        return IsCompatible(assembly, metadataPath, out error);
    }

    internal static bool IsCompatible(Assembly assembly, string metadataPath, out string error)
    {
        var name = assembly.GetName().Name!;
        if (!File.Exists(metadataPath))
        {
            error = $"'{name}' has no API compatibility file '{Path.GetFileName(metadataPath)}'.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(metadataPath));
            var root = document.RootElement;
            var minimum = Version.Parse(root.GetProperty("apiMinVersion").GetString()!);
            var maximum = Version.Parse(root.GetProperty("apiMaxVersionExclusive").GetString()!);
            var installed = typeof(IKsiUiProvider).Assembly.GetName().Version!;
            var referenced = assembly.GetReferencedAssemblies()
                .First(reference => reference.Name == typeof(IKsiUiProvider).Assembly.GetName().Name).Version!;
            if (minimum >= maximum || installed < minimum || installed >= maximum || referenced > installed)
            {
                error = $"'{name}' requires KSI.Core.Api [{minimum}, {maximum}) and references {referenced}; installed API is {installed}.";
                return false;
            }
            error = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException
            or ArgumentException or InvalidOperationException or FormatException)
        {
            error = $"'{name}' has invalid API compatibility metadata: {ex.Message}";
            return false;
        }
    }
}
