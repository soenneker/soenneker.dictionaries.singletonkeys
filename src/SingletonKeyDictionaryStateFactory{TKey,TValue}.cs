using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Dictionaries.SingletonKeys.Abstract;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

internal sealed class SingletonKeyDictionaryStateFactory<TKey, TValue, TState> : ISingletonKeyDictionaryStateFactory<TKey, TValue>
    where TKey : notnull where TState : notnull
{
    private readonly TState _state;
    private readonly Func<TState, TKey, CancellationToken, ValueTask<TValue>> _factory;

    public SingletonKeyDictionaryStateFactory(TState state, Func<TState, TKey, CancellationToken, ValueTask<TValue>> factory)
    {
        _state = state;
        _factory = factory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Invoke(TKey key, CancellationToken cancellationToken) => _factory(_state, key, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue InvokeSync(TKey key, CancellationToken cancellationToken) => _factory(_state, key, cancellationToken)
        .AwaitSync();
}