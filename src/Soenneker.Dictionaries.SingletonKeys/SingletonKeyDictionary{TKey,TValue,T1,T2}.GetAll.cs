using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

public partial class SingletonKeyDictionary<TKey, TValue, T1, T2> where TKey : notnull
{
    /// <summary>
    /// Gets all.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    public async ValueTask<Dictionary<TKey, TValue>> GetAll(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _locks.LockAll(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(_dictionary);
        }
    }

    /// <summary>
    /// Gets keys.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    public async ValueTask<List<TKey>> GetKeys(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _locks.LockAll(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    /// <summary>
    /// Gets values.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    public async ValueTask<List<TValue>> GetValues(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _locks.LockAll(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    /// <summary>
    /// Gets all sync.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public Dictionary<TKey, TValue> GetAllSync()
    {
        ThrowIfDisposed();

        using (_locks.LockAllSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(_dictionary);
        }
    }

    /// <summary>
    /// Gets keys sync.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<TKey> GetKeysSync()
    {
        ThrowIfDisposed();

        using (_locks.LockAllSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    /// <summary>
    /// Gets values sync.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public List<TValue> GetValuesSync()
    {
        ThrowIfDisposed();

        using (_locks.LockAllSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }
}
