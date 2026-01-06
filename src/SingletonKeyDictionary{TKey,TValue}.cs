using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Locks;
using Soenneker.Atomics.ValueBools;
using Soenneker.Dictionaries.SingletonKeys.Abstract;
using Soenneker.Enums.InitializationModes;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

public partial class SingletonKeyDictionary<TKey, TValue> : ISingletonKeyDictionary<TKey, TValue> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue>? _dictionary;
    private readonly AsyncLock _lock;

    private Func<TKey, CancellationToken, ValueTask<TValue>>? _asyncKeyTokenFunc;
    private Func<TKey, CancellationToken, TValue>? _keyTokenFunc;

    private Func<TKey, ValueTask<TValue>>? _asyncKeyFunc;
    private Func<TKey, TValue>? _keyFunc;

    private Func<ValueTask<TValue>>? _asyncFunc;
    private Func<TValue>? _func;

    private ISingletonKeyDictionaryStateFactory<TKey, TValue>? _stateFactory;

    private ValueAtomicBool _disposed;
    private InitializationMode? _initializationMode;

    public SingletonKeyDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<TKey, TValue>();
    }

    public SingletonKeyDictionary(Func<TKey, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, CancellationToken, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, CancellationToken, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<TValue> func) : this()
    {
        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default) => GetCore(key, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TKey key, out TValue? value)
    {
        ThrowIfDisposed();

        ConcurrentDictionary<TKey, TValue>? dict = _dictionary;

        if (dict is null)
        {
            value = default;
            return false;
        }

        return dict.TryGetValue(key, out value);
    }

    public async ValueTask<TValue> GetCore(TKey key, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out TValue? instance))
            return instance;

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, cancellationToken)
                .NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public TValue GetSync(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = GetInternalSync(key, cancellationToken);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default) where TState : notnull =>
        GetCore(keyFactory(state), cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetSync<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default) where TState : notnull =>
        GetSync(keyFactory(state), cancellationToken);

    private ValueTask<TValue> GetInternal(TKey key, CancellationToken cancellationToken)
    {
        ISingletonKeyDictionaryStateFactory<TKey, TValue>? stateFactory = _stateFactory;
        if (stateFactory is not null)
            return stateFactory.Invoke(key, cancellationToken);

        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                return _asyncKeyFunc!(key);

            case InitializationMode.AsyncKeyTokenValue:
                return _asyncKeyTokenFunc!(key, cancellationToken);

            case InitializationMode.AsyncValue:
                return _asyncFunc!();

            case InitializationMode.SyncValue:
                return new ValueTask<TValue>(_func!());

            case InitializationMode.SyncKeyTokenValue:
                return new ValueTask<TValue>(_keyTokenFunc!(key, cancellationToken));

            case InitializationMode.SyncKeyValue:
                return new ValueTask<TValue>(_keyFunc!(key));

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private TValue GetInternalSync(TKey key, CancellationToken cancellationToken)
    {
        ISingletonKeyDictionaryStateFactory<TKey, TValue>? stateFactory = _stateFactory;
        if (stateFactory is not null)
            return stateFactory.InvokeSync(key, cancellationToken);

        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                return _asyncKeyFunc!(key)
                    .AwaitSync();

            case InitializationMode.AsyncKeyTokenValue:
                return _asyncKeyTokenFunc!(key, cancellationToken)
                    .AwaitSync();

            case InitializationMode.AsyncValue:
                return _asyncFunc!()
                    .AwaitSync();

            case InitializationMode.SyncKeyValue:
                return _keyFunc!(key);

            case InitializationMode.SyncKeyTokenValue:
                return _keyTokenFunc!(key, cancellationToken);

            case InitializationMode.SyncValue:
                return _func!();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SingletonKeyDictionary<TKey, TValue> Initialize<TState>(TState state, Func<TState, TKey, CancellationToken, ValueTask<TValue>> factory)
        where TState : notnull
    {
        SetInitialization(state, factory);
        return this;
    }

    public void SetInitialization(Func<TKey, ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public void SetInitialization(Func<TKey, CancellationToken, ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public void SetInitialization(Func<ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public void SetInitialization(Func<TValue> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    public void SetInitialization(Func<TKey, TValue> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public void SetInitialization(Func<TKey, CancellationToken, TValue> func)
    {
        EnsureInitializationNotSet();
        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public void SetInitialization<TState>(TState state, Func<TState, TKey, CancellationToken, ValueTask<TValue>> factory) where TState : notnull
    {
        EnsureInitializationNotSet();
        _stateFactory = new SingletonKeyDictionaryStateFactory<TKey, TValue, TState>(state, factory);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureInitializationNotSet()
    {
        if (_initializationMode is not null || _stateFactory is not null)
            throw new Exception("Setting the initialization of a SingletonKeyDictionary after it has already been set is not allowed");
    }

    public async ValueTask Remove(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryRemove(key, out TValue? instance))
        {
            await DisposeRemovedInstance(instance)
                .NoSync();
            return;
        }

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            ThrowIfDisposed();

            if (_dictionary is not null && _dictionary.TryRemove(key, out instance))
                await DisposeRemovedInstance(instance)
                    .NoSync();
        }
    }

    public void RemoveSync(TKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryRemove(key, out TValue? instance))
        {
            DisposeRemovedInstanceSync(instance);
            return;
        }

        using (_lock.LockSync(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary is not null && _dictionary.TryRemove(key, out instance))
                DisposeRemovedInstanceSync(instance);
        }
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        ConcurrentDictionary<TKey, TValue>? dict = _dictionary;
        _dictionary = null;

        if (dict is null || dict.IsEmpty)
            return;

        foreach (KeyValuePair<TKey, TValue> kvp in dict)
        {
            if (dict.TryRemove(kvp.Key, out TValue? instance))
                DisposeRemovedInstanceSync(instance);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed.TrySetTrue())
            return;

        ConcurrentDictionary<TKey, TValue>? dict = _dictionary;
        _dictionary = null;

        if (dict is null || dict.IsEmpty)
            return;

        foreach (KeyValuePair<TKey, TValue> kvp in dict)
        {
            if (dict.TryRemove(kvp.Key, out TValue? instance))
                await DisposeRemovedInstance(instance)
                    .NoSync();
        }
    }

    private static void DisposeRemovedInstanceSync(TValue instance)
    {
        switch (instance)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
            case IAsyncDisposable asyncDisposable:
                asyncDisposable.DisposeAsync()
                               .AwaitSync();
                break;
        }
    }

    private static async ValueTask DisposeRemovedInstance(TValue instance)
    {
        switch (instance)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync()
                                     .NoSync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed.Value)
            throw new ObjectDisposedException(nameof(SingletonKeyDictionary<TKey, TValue>));
    }
}