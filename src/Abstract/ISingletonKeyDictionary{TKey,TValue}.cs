using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

/// <summary>
/// A singleton dictionary keyed by <typeparamref name="TKey"/>, using double-check async locking for initialization.
/// </summary>
/// <remarks>Be sure to dispose of this gracefully if using a Disposable type</remarks>
public partial interface ISingletonKeyDictionary<TKey, TValue> : IDisposable, IAsyncDisposable
    where TKey : notnull
{
    [Pure]
    ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default);

    [Pure]
    bool TryGet(TKey key, out TValue? value);

    [Pure]
    TValue GetSync(TKey key, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<TValue> Get<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    [Pure]
    TValue GetSync<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    SingletonKeyDictionary<TKey, TValue> Initialize<TState>(TState state, Func<TState, TKey, CancellationToken, ValueTask<TValue>> factory)
        where TState : notnull;

    void SetInitialization(Func<TKey, ValueTask<TValue>> func);

    void SetInitialization(Func<TKey, CancellationToken, ValueTask<TValue>> func);

    void SetInitialization(Func<ValueTask<TValue>> func);

    void SetInitialization(Func<TValue> func);

    void SetInitialization(Func<TKey, TValue> func);

    void SetInitialization(Func<TKey, CancellationToken, TValue> func);

    ValueTask Remove(TKey key, CancellationToken cancellationToken = default);

    void RemoveSync(TKey key, CancellationToken cancellationToken = default);

    new void Dispose();

    new ValueTask DisposeAsync();
}


