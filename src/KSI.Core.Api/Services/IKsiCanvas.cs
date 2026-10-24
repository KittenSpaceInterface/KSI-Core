using Brutal.ImGuiApi;
using Brutal.Numerics;

namespace KSI.Core.Api.Services;

public interface IKsiCanvas
{
    bool BeginArtworkWindow(ImGuiViewportPtr viewport, string id);
    void EndArtworkWindow();
    bool BeginControlCanvas(string id, float2 position, float2 size, bool acceptsInput);
    void EndControlCanvas();
}
