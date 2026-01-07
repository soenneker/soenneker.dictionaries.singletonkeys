using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// Clears all cached entries and disposes cached values where applicable (sync).
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    void ClearSync();

    /// <summary>
    /// Clears all cached entries and disposes cached values where applicable (async).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the clear lock.</param>
    /// <returns>A task that completes when clearing and disposal have finished.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    ValueTask Clear(CancellationToken cancellationToken = default);
}


