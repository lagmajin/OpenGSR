using Newtonsoft.Json;

namespace OpenGS
{
    public interface ISaveStorage
    {
        bool Save<T>(string fileName, T data, Formatting formatting = Formatting.Indented);
        T Load<T>(string fileName, T defaultValue = default);
        bool TryLoad<T>(string fileName, out T data);
        bool Delete(string fileName);
        bool Exists(string fileName);
        string GetPath(string fileName);
    }
}
