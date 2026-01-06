using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

public partial class SingletonKeyDictionary<TKey, TValue>
    where TKey : notnull
{
    public async ValueTask<Dictionary<TKey, TValue>> GetAll(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken).NoSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(_dictionary);
        }
    }

    public async ValueTask<List<TKey>> GetKeys(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken).NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public async ValueTask<List<TValue>> GetValues(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using (await _lock.Lock(cancellationToken).NoSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }

    public Dictionary<TKey, TValue> GetAllSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary is null ? new Dictionary<TKey, TValue>() : new Dictionary<TKey, TValue>(_dictionary);
        }
    }

    public List<TKey> GetKeysSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Keys is { } keys ? [.. keys] : [];
        }
    }

    public List<TValue> GetValuesSync()
    {
        ThrowIfDisposed();

        using (_lock.LockSync())
        {
            ThrowIfDisposed();

            return _dictionary?.Values is { } values ? [.. values] : [];
        }
    }
}


