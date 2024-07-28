using Easy.IO.Extensions;
using Easy.IO.Preferences;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO.Storages
{

    internal abstract class AbstractPreferencesStorage : IPreferences
    {
        protected static bool HasTokenBeenCancelled(CancellationToken? ct)
        {
            if (!ct.HasValue)
            {
                return false;
            }
            if (ct.Value.CanBeCanceled)
            {
                return ct.Value.IsCancellationRequested;
            }
            return false;
        }

        public bool Set<T>(string key, T value)
        {
            return AsyncHelper.RunSync(() => SetAsync(key, value));
        }

        public async Task<bool> SetAsync<T>(string key, T value, CancellationToken? ct = null)
        {
            return await TryPersistAsync(key, value, ct ?? CancellationToken.None);
        }

        public T Get<T>(string key, T defaultValue)
        {
            return AsyncHelper.RunSync(() => GetAsync(key, defaultValue));
        }

        public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken? ct = null)
        {
            return ContainsKey(key) ? (await LoadAsync<T>(key, ct ?? CancellationToken.None)) : defaultValue;
        }

        public abstract Task<bool> RemoveAsync(string key, CancellationToken? ct = null);

        public abstract Task<int> ClearAsync(CancellationToken? ct = null);

        public bool Remove(string key)
        {
            return AsyncHelper.RunSync(() => RemoveAsync(key));
        }

        public int Clear()
        {
            return AsyncHelper.RunSync(() => ClearAsync());
        }

        public abstract bool ContainsKey(string key);

        public abstract Task<bool> TryPersistAsync<T>(string key, T value, CancellationToken ct);

        public abstract Task<T> LoadAsync<T>(string key, CancellationToken ct);
    }
}
