namespace KSI.Core.Api.Services;

public interface IKsiSettings
{
    bool Exists(string fileName);
    T Load<T>(string fileName, Func<T> defaults) where T : class;
    void Save<T>(string fileName, T value) where T : class;
}
