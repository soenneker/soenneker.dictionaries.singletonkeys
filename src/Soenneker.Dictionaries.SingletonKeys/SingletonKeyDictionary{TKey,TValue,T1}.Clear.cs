using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

/// <summary>
/// Represents the singleton key dictionary.
/// </summary>
/// <typeparam name="TKey">The TKey type.</typeparam>
/// <typeparam name="TValue">The TValue type.</typeparam>
/// <typeparam name="T1">The T1 type.</typeparam>
public partial class SingletonKeyDictionary<TKey, TValue, T1> where TKey : notnull
{
    /// <summary>
    /// Executes the clear sync operation.
    /// </summary>
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

    /// <summary>
    /// Executes the clear operation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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