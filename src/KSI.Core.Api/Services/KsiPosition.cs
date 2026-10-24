namespace KSI.Core.Api.Services;

public static class KsiPosition
{
    public static (float X, float Y) Resolve(float viewportX, float viewportY,
        float viewportWidth, float viewportHeight, float elementWidth, float elementHeight,
        float normalizedX, float normalizedY)
    {
        var availableX = MathF.Max(0, viewportWidth - elementWidth);
        var availableY = MathF.Max(0, viewportHeight - elementHeight);
        return (viewportX + Math.Clamp(normalizedX, 0, 1) * availableX,
            viewportY + Math.Clamp(normalizedY, 0, 1) * availableY);
    }
}
