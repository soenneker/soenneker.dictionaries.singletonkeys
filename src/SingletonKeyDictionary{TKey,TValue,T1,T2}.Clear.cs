using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

public partial class SingletonKeyDictionary<TKey, TValue, T1, T2> where TKey : notnull
{
    public void ClearSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
            {
                if (_dictionary.TryRemove(kvp.Key, out TValue? instance))
                    DisposeRemovedInstanceSync(instance);
            }
        }
    }

    public async ValueTask Clear(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            if (_dictionary is null || _dictionary.IsEmpty)
                return;

            foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
            {
                if (_dictionary.TryRemove(kvp.Key, out TValue? instance))
                    await DisposeRemovedInstance(instance)
                        .NoSync();
            }
        }
    }
}