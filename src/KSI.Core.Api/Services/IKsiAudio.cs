namespace KSI.Core.Api.Services;

public interface IKsiAudio
{
    void PlayUiSound(string id);
    (float X, float Y) NormalizeScreenPosition(float x, float y, float screenWidth, float screenHeight);
}
