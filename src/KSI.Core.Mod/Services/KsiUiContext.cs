using KSI.Core.Api.Providers;
using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class KsiUiContext(
    IKsiAssets assets, IKsiCanvas canvas, IKsiAudio audio, IKsiLayouts layouts,
    IKsiSettings settings, IKsiModMenu modMenu)
    : IKsiUiContext
{
    public IKsiAssets Assets { get; } = assets;
    public IKsiCanvas Canvas { get; } = canvas;
    public IKsiAudio Audio { get; } = audio;
    public IKsiLayouts Layouts { get; } = layouts;
    public IKsiSettings Settings { get; } = settings;
    public IKsiModMenu ModMenu { get; } = modMenu;
}
