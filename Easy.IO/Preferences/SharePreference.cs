using Easy.IO.Storages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO.Preferences
{
    public class SharePreference : IPreferences
    {
        private readonly IPreferences _storage;

        public static IPreferences PlatformStorage { get; set; }

        public SharePreference(IPreferences storage = null)
        {
            _storage = PlatformStorage ?? storage ?? new GenericPreferencesStorage();
        }

        public bool Set<T>(string key, T value)
        {
            return _storage.Set(key, value);
        }

        public T Get<T>(string key, T defaultValue)
        {
            return _storage.Get(key, defaultValue);
        }

        public bool Remove(string key)
        {
            return _storage.Remove(key);
        }

        public int Clear()
        {
            return _storage.Clear();
        }

        public bool ContainsKey(string key)
        {
            return _storage.ContainsKey(key);
        }

        public async Task<bool> SetAsync<T>(string key, T value, CancellationToken? ct = null)
        {
            return await _storage.SetAsync(key, value, ct);
        }

        public async Task<T> GetAsync<T>(string key, T defaultValue, CancellationToken? ct = null)
        {
            return await _storage.GetAsync(key, defaultValue, ct);
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken? ct = null)
        {
            return await _storage.RemoveAsync(key, ct);
        }

        public async Task<int> ClearAsync(CancellationToken? ct = null)
        {
            return await _storage.ClearAsync(ct);
        }
    }
}
