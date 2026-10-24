namespace KSI.Core.Api.Services;

public interface IKsiModMenu
{
    bool IsAvailable { get; }
    void RegisterToggle(string id, string label, Func<bool> get, Action<bool> set);
}
