using KSA;
using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class KsaAudioFacade : IKsiAudio
{
    public void PlayUiSound(string id)
    {
        GameAudio.Play2DSound(ModLibrary.Get<SoundBehavior>(id), out _, 1f);
    }

    public (float X, float Y) NormalizeScreenPosition(float x, float y, float screenWidth, float screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0) return (0.5f, 0.5f);
        return (Math.Clamp(x / screenWidth, 0f, 1f), Math.Clamp(1f - y / screenHeight, 0f, 1f));
    }
}
