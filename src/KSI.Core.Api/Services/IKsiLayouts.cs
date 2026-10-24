using KSI.Core.Api.Providers;
using KSA;

namespace KSI.Core.Api.Services;

public interface IKsiLayouts
{
    KsiLayoutSelection? Selected { get; }
    bool IsSelected(string id);
    (float X, float Y) ResolvePosition(float viewportX, float viewportY,
        float viewportWidth, float viewportHeight, float elementWidth, float elementHeight,
        float normalizedX, float normalizedY);
    bool TryRegisterKsaProfile(string name, SerializedLayout layout, bool setDefault);
}
