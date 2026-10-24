using KSI.Core.Api.Providers;
using KSI.Core.Api.Services;
using KSA;

namespace KSI.Core.Mod.Services;

internal sealed class KsaLayoutFacade : IKsiLayouts
{
    public KsiLayoutSelection? Selected { get; private set; }
    internal event Action<KsiLayoutSelection?>? SelectionChanged;
    public bool IsSelected(string id) => Selected?.Id == id;

    public (float X, float Y) ResolvePosition(float viewportX, float viewportY,
        float viewportWidth, float viewportHeight, float elementWidth, float elementHeight,
        float normalizedX, float normalizedY)
    {
        return KsiPosition.Resolve(viewportX, viewportY, viewportWidth, viewportHeight,
            elementWidth, elementHeight, normalizedX, normalizedY);
    }

    public bool TryRegisterKsaProfile(string name, SerializedLayout layout, bool setDefault)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || name.Contains('/') || name.Contains('\\') || name is "." or "..")
            throw new ArgumentException("KSA layout name must be a simple directory name.", nameof(name));
        ArgumentNullException.ThrowIfNull(layout);
        var parent = LayoutSaves.SaveFolderPath;
        var destination = Path.Combine(parent, name);
        if (Directory.Exists(destination)) return false;
        var temporary = Path.Combine(parent, $".{name}.{Guid.NewGuid():N}.tmp");
        try
        {
            Directory.CreateDirectory(parent);
            Directory.CreateDirectory(temporary);
            layout.WriteTo(new DirectoryInfo(temporary));
            var metadata = new HudLayoutMetaData(name, temporary)
            {
                IsDefault = setDefault,
                HudKey = InputAction.None.ToString()
            };
            metadata.Write();
            if (Directory.Exists(destination)) return false;
            Directory.Move(temporary, destination);
            if (LayoutSave.FromDirectory(destination) is not { } registered) return false;
            if (setDefault) LayoutSaves.SetDefault(registered);
            registered.Apply();
            Refresh();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            Console.Error.WriteLine($"KSI-Core: could not create KSA layout '{name}': {ex.Message}");
            return false;
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                try { Directory.Delete(temporary, true); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Console.Error.WriteLine($"KSI-Core: could not remove temporary layout: {ex.Message}");
                }
            }
        }
    }

    internal bool Refresh()
    {
        var selected = KSA.LayoutSaves.Selected;
        var next = selected is null ? null : new KsiLayoutSelection(selected.Id, selected.MetaData.Created.Ticks);
        if (next == Selected) return false;
        Selected = next;
        SelectionChanged?.Invoke(next);
        return true;
    }
}
