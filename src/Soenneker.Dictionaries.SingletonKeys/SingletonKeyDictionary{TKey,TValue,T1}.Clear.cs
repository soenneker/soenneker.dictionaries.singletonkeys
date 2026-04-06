using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

public partial class SingletonKeyDictionary<TKey, TValue, T1> where TKey : notnull
{
    public void ClearSync()
    {
        using (_lock.LockSync())
        {
            ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

            if (dict.IsEmpty)
                return;

            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                if (dict.TryRemove(kvp.Key, out TValue? instance))
                    DisposeRemovedInstanceSync(instance);
            }
        }
    }

    public async ValueTask Clear(CancellationToken cancellationToken = default)
    {
        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

            if (dict.IsEmpty)
                return;

            foreach (KeyValuePair<TKey, TValue> kvp in dict)
            {
                if (dict.TryRemove(kvp.Key, out TValue? instance))
                    await DisposeRemovedInstance(instance)
                        .NoSync();
            }
        }
    }
}