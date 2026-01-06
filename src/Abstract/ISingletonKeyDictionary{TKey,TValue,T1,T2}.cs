using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

/// <summary>
/// A singleton dictionary keyed by <typeparamref name="TKey"/>, supporting initialization arguments of type
/// <typeparamref name="T1"/> and <typeparamref name="T2"/>.
/// </summary>
/// <remarks>Be sure to dispose of this gracefully if using a Disposable type</remarks>
public partial interface ISingletonKeyDictionary<TKey, TValue, T1, T2> : IDisposable, IAsyncDisposable
    where TKey : notnull
{
    [Pure]
    ValueTask<TValue> Get(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<TValue> Get(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default);

    [Pure]
    bool TryGet(TKey key, out TValue? value);

    [Pure]
    TValue GetSync(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default);

    [Pure]
    TValue GetSync(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<TValue> Get<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    [Pure]
    TValue GetSync<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    void SetInitialization(Func<TKey, T1, T2, ValueTask<TValue>> func);

    void SetInitialization(Func<TKey, CancellationToken, T1, T2, ValueTask<TValue>> func);

    void SetInitialization(Func<T1, T2, ValueTask<TValue>> func);

    void SetInitialization(Func<T1, T2, TValue> func);

    void SetInitialization(Func<TKey, T1, T2, TValue> func);

    void SetInitialization(Func<TKey, CancellationToken, T1, T2, TValue> func);

    ValueTask Remove(TKey key, CancellationToken cancellationToken = default);

    void RemoveSync(TKey key, CancellationToken cancellationToken = default);

    new void Dispose();

    new ValueTask DisposeAsync();
}


