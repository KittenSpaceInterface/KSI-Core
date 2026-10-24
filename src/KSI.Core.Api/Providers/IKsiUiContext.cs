using KSI.Core.Api.Services;

namespace KSI.Core.Api.Providers;

public interface IKsiUiContext
{
    IKsiAssets Assets { get; }
    IKsiCanvas Canvas { get; }
    IKsiAudio Audio { get; }
    IKsiLayouts Layouts { get; }
    IKsiSettings Settings { get; }
    IKsiModMenu ModMenu { get; }
}
