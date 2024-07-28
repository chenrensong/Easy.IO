using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO.Preferences
{
    public interface IPreferences
    {
        bool Set<T>(string key, T value);

        T Get<T>(string key, T defaultValue);

        bool Remove(string key);

        int Clear();

        bool ContainsKey(string key);

        Task<bool> SetAsync<T>(string key, T value, CancellationToken? ct = null);

        Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken? ct = null);

        Task<bool> RemoveAsync(string key, CancellationToken? ct = null);

        Task<int> ClearAsync(CancellationToken? ct = null);
    }
}
