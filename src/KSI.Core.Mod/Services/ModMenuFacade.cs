using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class ModMenuFacade
{
    private readonly Dictionary<(string ProviderId, string Id), Toggle> _toggles = new();
    private sealed record Toggle(string Label, Func<bool> Get, Action<bool> Set);

    public bool IsAvailable => AppDomain.CurrentDomain.GetAssemblies()
        .Any(assembly => assembly.GetName().Name == "ModMenu");

    internal IKsiModMenu ForProvider(string providerId) => new ProviderModMenu(this, providerId);

    private void RegisterToggle(string providerId, string id, string label, Func<bool> get, Action<bool> set)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(label)) throw new ArgumentException("ModMenu option needs an ID and label.");
        _toggles[(providerId, id)] = new Toggle(label, get, set);
    }

    internal void Draw(string? activeProviderId)
    {
        if (activeProviderId is null) return;
        foreach (var (key, toggle) in _toggles)
        {
            if (key.ProviderId != activeProviderId) continue;
            var value = toggle.Get();
            if (Brutal.ImGuiApi.ImGui.MenuItem(toggle.Label, default, ref value)) toggle.Set(value);
        }
    }

    private sealed class ProviderModMenu(ModMenuFacade menu, string providerId) : IKsiModMenu
    {
        public bool IsAvailable => menu.IsAvailable;
        public void RegisterToggle(string id, string label, Func<bool> get, Action<bool> set)
            => menu.RegisterToggle(providerId, id, label, get, set);
    }
}
