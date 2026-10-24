using KSI.Core.Api.Providers;
using KSI.Core.Api.Services;
using KSI.Core.Hosting;
using KSI.Core.Mod.Hosting;
using KSI.Core.Mod.Services;

var messages = new List<string>();
using var host = new KsiProviderHost(_ => new FakeContext(), messages.Add);
var first = new FakeProvider("first");
var second = new FakeProvider("second");
Check(host.Register(first) && host.Register(second), "registration failed");
Check(first.InitializeCount == 0 && second.InitializeCount == 0,
    "registration initialized an inactive provider");
Check(!host.Register(first) && !first.Disposed, "duplicate registration disposed its active instance");
Check(host.Providers.Count == 2, "provider list is incorrect");
Check(host.Activate("first") && first.ActivationCount == 1, "first activation failed");
Check(first.InitializeCount == 1 && second.InitializeCount == 0,
    "activation did not initialize only the selected provider");
host.CloseOptions();
Check(first.ConfigurationCloseCount == 1 && second.ConfigurationCloseCount == 0,
    "configuration close was not sent only to the active provider");
host.Draw(0.1);
Check(first.DrawCount == 1 && second.DrawCount == 0, "inactive provider drew");
Check(host.Activate("second") && first.DeactivationCount == 1 && second.ActivationCount == 1,
    "switch did not complete its lifecycle");
Check(first.InitializeCount == 1 && second.InitializeCount == 1,
    "switch initialized a provider more than once");
host.NotifyLayoutChanged(new KsiLayoutSelection("Custom", 42));
Check(first.LastLayout?.Key == "Custom|42" && second.LastLayout?.Key == "Custom|42",
    "layout change was not delivered");
first.FailActivation = true;
Check(!host.Activate("first") && host.ActiveId == "second", "failed switch lost the active UI");
second.FailDraw = true;
host.Draw(0.1);
Check(host.ActiveId is null && second.DeactivationCount == 2, "draw failure left a broken UI active");
Check(messages.Any(message => message.Contains("failed while drawing")), "failure was not reported");
Check(!host.Activate("missing"), "missing provider activated");
host.Dispose();
Check(first.Disposed && second.Disposed, "providers were not disposed");

using var unloadHost = new KsiProviderHost(_ => new FakeContext(), messages.Add);
var unloadable = new FakeProvider("unloadable");
Check(unloadHost.Register(unloadable) && unloadHost.Activate("unloadable"),
    "unload scenario setup failed");
unloadable.FailDeactivation = true;
Check(!unloadHost.UnloadActive() && unloadHost.ActiveId == "unloadable",
    "failed unload lost the active provider");
unloadable.FailDeactivation = false;
Check(unloadHost.UnloadActive() && unloadHost.ActiveId is null
    && !unloadable.Disposed && unloadHost.Providers.Contains(unloadable),
    "unload did not preserve the registered provider");
Check(unloadHost.Activate("unloadable") && unloadable.ActivationCount == 2,
    "unloaded provider could not be loaded again");

using var failureHost = new KsiProviderHost(_ => new FakeContext(), messages.Add);
var badRegistration = new FakeProvider("bad-registration") { FailLayoutChange = true };
Check(failureHost.Register(badRegistration) && !failureHost.Activate("bad-registration")
    && badRegistration.Disposed && failureHost.Providers.Count == 0,
    "failed first layout callback left a provider registered");
var stable = new FakeProvider("stable");
var unstable = new FakeProvider("unstable");
Check(failureHost.Register(stable) && failureHost.Register(unstable)
    && failureHost.Activate("stable"), "failure scenario setup failed");
stable.FailDeactivation = true;
Check(!failureHost.Activate("unstable") && failureHost.ActiveId == "stable"
    && unstable.ActivationCount == 0, "switch continued after deactivation failure");
stable.FailDeactivation = false;
unstable.FailActivationAfterStart = true;
Check(!failureHost.Activate("unstable") && failureHost.ActiveId == "stable"
    && unstable.DeactivationCount == 1, "partial activation was not cleaned up");
unstable.FailDeactivation = true;
Check(!failureHost.Activate("unstable") && failureHost.ActiveId is null
    && unstable.Disposed && !failureHost.Providers.Contains(unstable),
    "failed activation cleanup restored another provider");

var testDirectory = Path.Combine(Path.GetTempPath(), $"KSI-Core-Tests-{Guid.NewGuid():N}");
Directory.CreateDirectory(testDirectory);
try
{
    var store = new JsonSettingsFacade(testDirectory);
    var badSettings = Path.Combine(testDirectory, "sample", "settings.json");
    Directory.CreateDirectory(Path.GetDirectoryName(badSettings)!);
    File.WriteAllText(badSettings, "{ invalid json");
    try
    {
        store.Load("sample", "settings.json", () => new object());
        throw new Exception("invalid settings were accepted");
    }
    catch (KsiSettingsReadException) { }
    Check(File.ReadAllText(badSettings) == "{ invalid json",
        "invalid settings were changed while loading");

    var metadata = Path.Combine(testDirectory, "provider.json");
    File.WriteAllText(metadata, "{\"apiMinVersion\":\"0.2.1\",\"apiMaxVersionExclusive\":\"0.3.0\"}");
    Check(ProviderCompatibility.IsCompatible(typeof(FakeProvider).Assembly, metadata, out _),
        "compatible provider metadata was rejected");
    File.WriteAllText(metadata, "{\"apiMinVersion\":\"0.3.0\",\"apiMaxVersionExclusive\":\"0.4.0\"}");
    Check(!ProviderCompatibility.IsCompatible(typeof(FakeProvider).Assembly, metadata, out _),
        "incompatible provider metadata was accepted");
    File.WriteAllText(metadata, "{ invalid json");
    Check(!ProviderCompatibility.IsCompatible(typeof(FakeProvider).Assembly, metadata, out _),
        "malformed provider metadata was accepted");
}
finally { Directory.Delete(testDirectory, recursive: true); }
Console.WriteLine("KSI-Core host checks passed.");

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

internal sealed class FakeContext : IKsiUiContext
{
    public IKsiAssets Assets => null!;
    public IKsiCanvas Canvas => null!;
    public IKsiAudio Audio => null!;
    public IKsiLayouts Layouts { get; } = new FakeLayouts();
    public IKsiSettings Settings => null!;
    public IKsiModMenu ModMenu => null!;
}

internal sealed class FakeLayouts : IKsiLayouts
{
    public KsiLayoutSelection? Selected => null;
    public bool IsSelected(string id) => false;
    public (float X, float Y) ResolvePosition(float viewportX, float viewportY, float viewportWidth,
        float viewportHeight, float elementWidth, float elementHeight, float normalizedX, float normalizedY)
        => throw new NotSupportedException();
    public bool TryRegisterKsaProfile(string name, KSA.SerializedLayout layout, bool setDefault)
        => throw new NotSupportedException();
}

internal sealed class FakeProvider(string id) : IKsiUiProvider
{
    public string Id => id;
    public string DisplayName => id;
    public int ActivationCount { get; private set; }
    public int InitializeCount { get; private set; }
    public int DeactivationCount { get; private set; }
    public int DrawCount { get; private set; }
    public int ConfigurationCloseCount { get; private set; }
    public bool FailActivation { get; set; }
    public bool FailActivationAfterStart { get; set; }
    public bool FailDeactivation { get; set; }
    public bool FailLayoutChange { get; set; }
    public bool FailDraw { get; set; }
    public bool Disposed { get; private set; }
    public KsiLayoutSelection? LastLayout { get; private set; }
    public void Initialize(IKsiUiContext context) => InitializeCount++;
    public void Activate()
    {
        if (FailActivation) throw new InvalidOperationException("activation failure");
        ActivationCount++;
        if (FailActivationAfterStart) throw new InvalidOperationException("partial activation failure");
    }
    public void Deactivate()
    {
        if (FailDeactivation) throw new InvalidOperationException("deactivation failure");
        DeactivationCount++;
    }
    public void Draw(double deltaTime)
    {
        if (FailDraw) throw new InvalidOperationException("draw failure");
        DrawCount++;
    }
    public void DrawOptions() { }
    public void OnConfigurationClosed() => ConfigurationCloseCount++;
    public void OnKsaLayoutChanged(KsiLayoutSelection? selection)
    {
        if (FailLayoutChange) throw new InvalidOperationException("layout failure");
        LastLayout = selection;
    }
    public void Dispose() => Disposed = true;
}
