using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

/// <summary>
/// A singleton dictionary keyed by <typeparamref name="TKey"/>, supporting an initialization argument of type <typeparamref name="T1"/>.
/// </summary>
/// <remarks>Be sure to dispose of this gracefully if using a Disposable type</remarks>
public partial interface ISingletonKeyDictionary<TKey, TValue, T1> : IDisposable, IAsyncDisposable
    where TKey : notnull
{
    [Pure]
    ValueTask<TValue> Get(TKey key, T1 arg, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<TValue> Get(TKey key, Func<T1> argFactory, CancellationToken cancellationToken = default);

    [Pure]
    bool TryGet(TKey key, out TValue? value);

    [Pure]
    TValue GetSync(TKey key, T1 arg, CancellationToken cancellationToken = default);

    [Pure]
    TValue GetSync(TKey key, Func<T1> argFactory, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<TValue> Get<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    [Pure]
    TValue GetSync<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    void SetInitialization(Func<TKey, T1, ValueTask<TValue>> func);

    void SetInitialization(Func<TKey, CancellationToken, T1, ValueTask<TValue>> func);

    void SetInitialization(Func<T1, ValueTask<TValue>> func);

    void SetInitialization(Func<T1, TValue> func);

    void SetInitialization(Func<TKey, T1, TValue> func);

    void SetInitialization(Func<TKey, CancellationToken, T1, TValue> func);

    ValueTask Remove(TKey key, CancellationToken cancellationToken = default);

    void RemoveSync(TKey key, CancellationToken cancellationToken = default);

    new void Dispose();

    new ValueTask DisposeAsync();
}


