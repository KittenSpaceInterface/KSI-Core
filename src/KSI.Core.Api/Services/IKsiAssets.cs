using KSA;

namespace KSI.Core.Api.Services;

public interface IKsiAssets
{
    T Get<T>(string id) where T : IKeyed;
    void PrepareTexture(string id, Action<TextureReference> assign);
}
