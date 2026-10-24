using KSA;
using KSI.Core.Api.Services;

namespace KSI.Core.Mod.Services;

internal sealed class KsaAssetFacade : IKsiAssets
{
    public T Get<T>(string id) where T : IKeyed => ModLibrary.Get<T>(id);

    public void PrepareTexture(string id, Action<TextureReference> assign)
    {
        ArgumentNullException.ThrowIfNull(assign);
        var texture = Get<TextureReference>(id);
        texture.AddToImgui();
        // AddToImgui only sets a flag consumed by KSA's initial Bind pass.
        // Providers activated later must register already-bound textures now.
        if (texture.Texture is not null && texture.ImguiTextureRef is null)
            texture.BindToImgui(Program.GetRenderer().LinearSampler);
        assign(texture);
    }
}
