using System.Reflection;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSI.Core.Api.Providers;
using KSI.Core.Hosting;
using KSI.Core.Mod.Integration;
using KSI.Core.Mod.Services;
using KSA;
using StarMap.API;

namespace KSI.Core.Mod.Hosting;

[StarMapMod]
public sealed class KsiCoreMod
{
    private static KsiCoreMod? _current;
    private readonly KsaLayoutFacade _layouts = new();
    private readonly ModMenuFacade _modMenu = new();
    private KsiProviderHost? _host;
    private JsonSettingsFacade? _settingsStore;
    private HostSettings _settings = new();
    private string? _candidateId;
    private bool _discovered;
    private bool _settingsWritable = true;
    private string? _settingsError;
    private string? _providerError;

    [StarMapAllModsLoaded]
    public void OnAllModsLoaded()
    {
        _current = this;
        var directory = Path.GetDirectoryName(typeof(KsiCoreMod).Assembly.Location) ?? AppContext.BaseDirectory;
        _settingsStore = new JsonSettingsFacade(Path.Combine(directory, "settings"));
        try { _settings = _settingsStore.Load("core", "host.json", () => new HostSettings()); }
        catch (KsiSettingsReadException ex)
        {
            _settingsWritable = false;
            _settingsError = ex.Message;
            Console.Error.WriteLine($"KSI-Core: {ex}");
        }
        _layouts.Refresh();
        var assets = new KsaAssetFacade();
        var canvas = new KsaCanvasFacade();
        var audio = new KsaAudioFacade();
        _host = new KsiProviderHost(id => new KsiUiContext(assets, canvas, audio, _layouts,
            _settingsStore.ForProvider(id), _modMenu.ForProvider(id)),
            message =>
            {
                _providerError = message;
                Console.Error.WriteLine($"KSI-Core: {message}");
            });
        _layouts.SelectionChanged += OnLayoutChanged;
        DiscoverProviders();
        var savedId = _settings.SelectedProviderId;
        if (savedId is not null && !_settings.HasEverLoadedInterface)
        {
            // Existing host settings predate the explicit history flag.
            _settings.HasEverLoadedInterface = true;
            Save();
        }
        if (savedId is not null && !_host.Activate(savedId))
        {
            _settings.SelectedProviderId = null;
            Save();
        }
        _candidateId = _host.ActiveId ?? (!_settings.HasEverLoadedInterface
            ? _host.Providers.FirstOrDefault()?.Id : null);
        if (!_modMenu.IsAvailable || _host.ActiveId is null) _settings.ConfigurationVisible = true;
    }

    [StarMapAfterGui]
    public void OnAfterGui(double deltaTime)
    {
        if (_host is null || ImGui.GetCurrentContext().IsNull()) return;
        _layouts.Refresh();
        var activeBeforeDraw = _host.ActiveId;
        _host.Draw(deltaTime);
        if (activeBeforeDraw is not null && _host.ActiveId is null)
        {
            _settings.SelectedProviderId = null;
            _settings.ConfigurationVisible = true;
            _candidateId = null;
            Save();
        }
        DrawConfiguration();
    }

    [ModMenuEntry("KSI")]
    public static void DrawModMenu()
    {
        if (_current is not { _host: not null } current) return;
        var visible = current._settings.ConfigurationVisible;
        if (ImGui.MenuItem("Configuration", default, ref visible))
            current.SetConfigurationVisible(visible);
        current._modMenu.Draw(current._host.ActiveId);
    }

    private void DiscoverProviders()
    {
        if (_discovered || _host is null) return;
        _discovered = true;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly == typeof(KsiCoreMod).Assembly || assembly == typeof(KsiProviderHost).Assembly
                || assembly == typeof(IKsiUiProvider).Assembly) continue;
            Type[] types;
            try
            {
                if (!ProviderCompatibility.ReferencesApi(assembly)) continue;
                if (!ProviderCompatibility.IsCompatible(assembly, out var error))
                {
                    _providerError = error;
                    Console.Error.WriteLine($"KSI-Core: {error}");
                    continue;
                }
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.OfType<Type>().ToArray(); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"KSI-Core: could not inspect '{assembly.GetName().Name}': {ex.Message}");
                continue;
            }
            foreach (var type in types)
            {
                if (!type.IsClass || type.IsAbstract || !type.IsPublic
                    || !type.IsDefined(typeof(KsiUiProviderAttribute), false)) continue;
                if (!typeof(IKsiUiProvider).IsAssignableFrom(type))
                {
                    Console.Error.WriteLine($"KSI-Core: '{type.FullName}' has a provider attribute but does not implement IKsiUiProvider.");
                    continue;
                }
                try
                {
                    if (Activator.CreateInstance(type) is IKsiUiProvider provider) _host.Register(provider);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"KSI-Core: could not create '{type.FullName}': {ex}");
                }
            }
        }
    }

    private void DrawConfiguration()
    {
        if (_host is null || !_settings.ConfigurationVisible) return;
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos + new float2(24f, 72f), ImGuiCond.FirstUseEver);
        var initialSize = new float2(470f, 390f) * ImGuiHelper.InterfaceScale;
        var visible = _settings.ConfigurationVisible;
        var expanded = _modMenu.IsAvailable
            ? ConsoleStyle.BeginWindow("ksi-configuration", "KSI CONFIGURATION", "KSI-HUD",
                ref visible, initialSize)
            : ConsoleStyle.BeginWindow("ksi-configuration", "KSI CONFIGURATION", "KSI-HUD",
                initialSize);
        if (visible != _settings.ConfigurationVisible)
            SetConfigurationVisible(visible);
        if (!expanded || !visible)
        {
            if (expanded) ImGui.Dummy(float2.Zero);
            ConsoleStyle.EndWindow();
            return;
        }
        try
        {
            ConsoleStyle.BeginBody();
            ConsoleStyle.PushWidgetStyle();
            try
            {
                if (_settingsError is not null)
                    ImGui.TextWrapped($"Core settings could not be read. The file was preserved and changes will not be saved. {_settingsError}");
                if (_providerError is not null)
                    ImGui.TextWrapped($"Interface error: {_providerError}");
                DrawInterfaceSelector();
                if (_host.ActiveId is not null)
                {
                    ConsoleWidgets.Rule();
                    _host.DrawOptions();
                }
            }
            finally
            {
                ConsoleStyle.PopWidgetStyle();
                ConsoleStyle.EndBody();
            }
        }
        finally { ConsoleStyle.EndWindow(); }
    }

    private void DrawInterfaceSelector()
    {
        if (_host is null) return;
        ConsoleWidgets.RegionHeader("INTERFACE");
        ConsoleWidgets.BeginRow("SELECT INTERFACE");
        var selected = _host.Providers.FirstOrDefault(provider => provider.Id == _candidateId);
        var label = selected?.DisplayName ?? "None";
        if (ConsoleWidgets.BeginComboControl("ksi-interface", label,
            pending: false))
        {
            if (ImGui.Selectable("None", _candidateId is null))
                _candidateId = null;
            foreach (var provider in _host.Providers)
            {
                if (ImGui.Selectable($"{provider.DisplayName}##{provider.Id}", _candidateId == provider.Id))
                    _candidateId = provider.Id;
            }
            ConsoleWidgets.EndComboControl();
        }
        ConsoleWidgets.EndRow();
        if (_candidateId is null && _host.ActiveId is not null
            && ConsoleWidgets.PrimaryButton("UNLOAD INTERFACE") && _host.UnloadActive())
        {
            _settings.SelectedProviderId = null;
            Save();
        }
        if (_candidateId is not null && ConsoleWidgets.PrimaryButton("LOAD SELECTED INTERFACE"))
        {
            if (_host.Activate(_candidateId))
            {
                _providerError = null;
                _settings.SelectedProviderId = _candidateId;
                _settings.HasEverLoadedInterface = true;
                Save();
            }
            else if (_host.ActiveId is null)
            {
                _settings.SelectedProviderId = null;
                _candidateId = _settings.HasEverLoadedInterface
                    ? null : _host.Providers.FirstOrDefault()?.Id;
                Save();
            }
        }
    }

    private void Save()
    {
        if (!_settingsWritable) return;
        try { _settingsStore?.Save("core", "host.json", _settings); }
        catch (Exception ex) { Console.Error.WriteLine($"KSI-Core: could not save selection: {ex.Message}"); }
    }

    private void SetConfigurationVisible(bool visible)
    {
        if (_settings.ConfigurationVisible == visible) return;
        _settings.ConfigurationVisible = visible;
        if (!visible) _host?.CloseOptions();
        Save();
    }

    private void OnLayoutChanged(KsiLayoutSelection? selection) => _host?.NotifyLayoutChanged(selection);

    [StarMapUnload]
    public void Unload()
    {
        _layouts.SelectionChanged -= OnLayoutChanged;
        _host?.Dispose();
        _host = null;
        if (ReferenceEquals(_current, this)) _current = null;
    }

    public sealed class HostSettings
    {
        public HostSettings() { }
        public string? SelectedProviderId { get; set; }
        public bool HasEverLoadedInterface { get; set; }
        public bool ConfigurationVisible { get; set; }
    }
}
