using Brutal.ImGuiApi;
using Brutal.Numerics;
using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class KsaCanvasFacade : IKsiCanvas
{
    public bool BeginArtworkWindow(ImGuiViewportPtr viewport, string id)
    {
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.SetNextWindowPos(viewport.Pos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(viewport.Size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, float2.Zero);
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus;
        return ImGui.Begin($"##KSI-Artwork-{id}", flags);
    }

    public void EndArtworkWindow()
    {
        ImGui.End();
        ImGui.PopStyleVar();
    }

    public bool BeginControlCanvas(string id, float2 position, float2 size, bool acceptsInput)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, float2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, float2.Zero);
        ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        var flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar |
                    ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing |
                    ImGuiWindowFlags.NoNavInputs;
        if (!acceptsInput) flags |= ImGuiWindowFlags.NoInputs;
        return ImGui.Begin(id, flags);
    }

    public void EndControlCanvas()
    {
        ImGui.End();
        ImGui.PopStyleVar(3);
    }
}
