using KSI.Core.Api.Providers;

namespace KSI.Core.Hosting;

public sealed class KsiProviderHost(Func<string, IKsiUiContext> contextForProvider, Action<string> log) : IDisposable
{
    private readonly SortedDictionary<string, IKsiUiProvider> _providers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _initialized = new(StringComparer.Ordinal);
    private IKsiUiProvider? _active;
    private bool _disposed;

    public IReadOnlyCollection<IKsiUiProvider> Providers => _providers.Values;
    public string? ActiveId => _active?.Id;

    public bool Register(IKsiUiProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (_disposed) throw new ObjectDisposedException(nameof(KsiProviderHost));
        if (string.IsNullOrWhiteSpace(provider.Id) || string.IsNullOrWhiteSpace(provider.DisplayName))
        {
            log("Ignored a UI provider with no ID or display name.");
            DisposeRejected(provider);
            return false;
        }
        if (_providers.TryGetValue(provider.Id, out var registered))
        {
            log($"Ignored duplicate UI provider '{provider.Id}'.");
            if (!ReferenceEquals(registered, provider)) DisposeRejected(provider);
            return false;
        }
        try
        {
            _providers.Add(provider.Id, provider);
            return true;
        }
        catch (Exception ex)
        {
            log($"UI provider '{provider.Id}' failed to initialize: {ex}");
            _providers.Remove(provider.Id);
            DisposeRejected(provider);
            return false;
        }
    }

    public bool Activate(string id)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KsiProviderHost));
        if (!_providers.TryGetValue(id, out var next)) return false;
        if (ReferenceEquals(_active, next)) return true;
        var previous = _active;
        if (previous is not null)
        {
            try { previous.Deactivate(); }
            catch (Exception ex)
            {
                log($"UI provider '{previous.Id}' failed to deactivate: {ex}");
                return false;
            }
        }
        _active = null;
        var initializationComplete = _initialized.Contains(id);
        try
        {
            if (!initializationComplete)
            {
                var context = contextForProvider(id);
                next.Initialize(context);
                next.OnKsaLayoutChanged(context.Layouts.Selected);
                _initialized.Add(id);
                initializationComplete = true;
            }
            next.Activate();
            _active = next;
            return true;
        }
        catch (Exception ex)
        {
            log($"UI provider '{id}' failed to activate: {ex}");
            var stopped = true;
            try { next.Deactivate(); }
            catch (Exception stopError)
            {
                stopped = false;
                log($"UI provider '{id}' failed to clean up after activation: {stopError}");
                _providers.Remove(id);
                _initialized.Remove(id);
                DisposeRejected(next);
            }
            if (!initializationComplete && stopped)
            {
                _providers.Remove(id);
                DisposeRejected(next);
            }
            if (stopped && previous is not null)
            {
                try { previous.Activate(); _active = previous; }
                catch (Exception restoreError)
                {
                    log($"Could not restore UI provider '{previous.Id}': {restoreError}");
                    try { previous.Deactivate(); }
                    catch (Exception stopError)
                    {
                        log($"UI provider '{previous.Id}' failed to clean up after restore: {stopError}");
                        _providers.Remove(previous.Id);
                        _initialized.Remove(previous.Id);
                        DisposeRejected(previous);
                    }
                }
            }
            return false;
        }
    }

    public bool UnloadActive()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KsiProviderHost));
        if (_active is not { } active) return true;
        try
        {
            active.Deactivate();
            _active = null;
            return true;
        }
        catch (Exception ex)
        {
            log($"UI provider '{active.Id}' failed to deactivate: {ex}");
            return false;
        }
    }

    public void Draw(double deltaTime)
    {
        if (_active is not { } active) return;
        try { active.Draw(deltaTime); }
        catch (Exception ex)
        {
            log($"UI provider '{active.Id}' failed while drawing: {ex}");
            try { active.Deactivate(); }
            catch (Exception stopError)
            {
                log($"UI provider '{active.Id}' failed to deactivate: {stopError}");
                _providers.Remove(active.Id);
                _initialized.Remove(active.Id);
                DisposeRejected(active);
            }
            _active = null;
        }
    }

    public void DrawOptions()
    {
        if (_active is not { } active) return;
        try { active.DrawOptions(); }
        catch (Exception ex) { log($"UI provider '{active.Id}' failed while drawing options: {ex}"); }
    }

    public void CloseOptions()
    {
        if (_active is not { } active) return;
        try { active.OnConfigurationClosed(); }
        catch (Exception ex) { log($"UI provider '{active.Id}' failed to close configuration: {ex}"); }
    }

    public void NotifyLayoutChanged(KsiLayoutSelection? selection)
    {
        foreach (var provider in _providers.Values)
        {
            if (!_initialized.Contains(provider.Id)) continue;
            try { provider.OnKsaLayoutChanged(selection); }
            catch (Exception ex) { log($"UI provider '{provider.Id}' failed on layout change: {ex}"); }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_active is not null)
        {
            try { _active.Deactivate(); }
            catch (Exception ex) { log($"UI provider '{_active.Id}' failed to deactivate: {ex}"); }
        }
        _active = null;
        foreach (var provider in _providers.Values)
        {
            try { provider.Dispose(); }
            catch (Exception ex) { log($"UI provider '{provider.Id}' failed to dispose: {ex}"); }
        }
        _providers.Clear();
        _initialized.Clear();
    }

    private void DisposeRejected(IKsiUiProvider provider)
    {
        try { provider.Dispose(); }
        catch (Exception ex) { log($"UI provider '{provider.Id}' failed to dispose: {ex}"); }
    }
}
