using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Easy.IO.Storages
{
    internal class GenericPreferencesStorage : AbstractPreferencesStorage
    {
        private static readonly SemaphoreSlim Sema = new SemaphoreSlim(1, 1);

        private static IsolatedStorageFile Store => IsolatedStorageFile.GetUserStoreForDomain();

        public override bool ContainsKey(string key)
        {
            return Store.FileExists(key);
        }

        public override async Task<bool> RemoveAsync(string key, CancellationToken? ct = null)
        {
            await Sema.WaitAsync(ct ?? CancellationToken.None);
            try
            {
                if (!ContainsKey(key) || AbstractPreferencesStorage.HasTokenBeenCancelled(ct))
                {
                    return false;
                }
                Store.DeleteFile(key);
                return true;
            }
            finally
            {
                Sema.Release();
            }
        }

        public override async Task<int> ClearAsync(CancellationToken? ct = null)
        {
            await Sema.WaitAsync(ct ?? CancellationToken.None);
            try
            {
                string[] fileNames = Store.GetFileNames();
                string[] array = fileNames;
                foreach (string file in array)
                {
                    if (AbstractPreferencesStorage.HasTokenBeenCancelled(ct))
                    {
                        return -1;
                    }
                    Store.DeleteFile(file);
                }
                return fileNames.Length;
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                Sema.Release();
            }
        }

        public override async Task<bool> TryPersistAsync<T>(string key, T value, CancellationToken ct)
        {
            await Sema.WaitAsync(ct);
            try
            {
                IsolatedStorageFileStream stream = Store.OpenFile(key, FileMode.Create, FileAccess.Write);
                bool result;
                try
                {
                    await JsonSerializer.SerializeAsync((Stream)stream, value, EasyIO.DefaultSerializerOptions, ct);
                    result = true;
                }
                finally
                {
                    if (stream != null)
                    {
                        await ((IAsyncDisposable)stream).DisposeAsync();
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                Sema.Release();
            }
        }

        public override async Task<T> LoadAsync<T>(string key, CancellationToken ct)
        {
            await Sema.WaitAsync(ct);
            try
            {
                IsolatedStorageFileStream stream = Store.OpenFile(key, FileMode.Open);
                T result;
                try
                {
                    result = await JsonSerializer.DeserializeAsync<T>((Stream)stream, EasyIO.DefaultSerializerOptions, ct);
                }
                finally
                {
                    if (stream != null)
                    {
                        await ((IAsyncDisposable)stream).DisposeAsync();
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return default(T);
            }
            finally
            {
                Sema.Release();
            }
        }
    }
}
