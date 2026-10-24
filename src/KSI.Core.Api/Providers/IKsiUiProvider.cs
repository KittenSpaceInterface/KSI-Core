namespace KSI.Core.Api.Providers;

public interface IKsiUiProvider : IDisposable
{
    string Id { get; }
    string DisplayName { get; }
    void Initialize(IKsiUiContext context);
    void Activate();
    void Deactivate();
    void Draw(double deltaTime);
    void DrawOptions();
    void OnConfigurationClosed() { }
    void OnKsaLayoutChanged(KsiLayoutSelection? selection);
}
