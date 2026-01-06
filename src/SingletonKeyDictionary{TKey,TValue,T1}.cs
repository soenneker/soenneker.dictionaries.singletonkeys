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

/// <inheritdoc cref="ISingletonKeyDictionary{TKey,TValue,T1}"/>
public partial class SingletonKeyDictionary<TKey, TValue, T1> : ISingletonKeyDictionary<TKey, TValue, T1> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue>? _dictionary;
    private readonly AsyncLock _lock;

    private Func<TKey, CancellationToken, T1, ValueTask<TValue>>? _asyncKeyTokenFunc;
    private Func<TKey, CancellationToken, T1, TValue>? _keyTokenFunc;

    private Func<TKey, T1, ValueTask<TValue>>? _asyncKeyFunc;
    private Func<TKey, T1, TValue>? _keyFunc;

    private Func<T1, ValueTask<TValue>>? _asyncFunc;
    private Func<T1, TValue>? _func;

    private ValueAtomicBool _disposed;
    private InitializationMode? _initializationMode;

    public SingletonKeyDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<TKey, TValue>();
    }

    public SingletonKeyDictionary(Func<TKey, T1, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, CancellationToken, T1, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<T1, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, T1, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, CancellationToken, T1, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<T1, TValue> func) : this()
    {
        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull =>
        GetCore(key, state, argFactory, cancellationToken);

    private async ValueTask<TValue> GetCore<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken)
        where TState : notnull
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

            // Arg created from state via a static-friendly delegate
            T1 arg = argFactory(state);

            instance = await GetInternal(key, arg, cancellationToken)
                .NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetSync<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken = default) where TState : notnull =>
        GetCoreSync(key, state, argFactory, cancellationToken);

    private TValue GetCoreSync<TState>(TKey key, TState state, Func<TState, T1> argFactory, CancellationToken cancellationToken) where TState : notnull
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            T1 arg = argFactory(state);

            instance = GetInternalSync(key, arg, cancellationToken);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get(TKey key, T1 arg, CancellationToken cancellationToken = default) => GetCore(key, arg, cancellationToken);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get(TKey key, Func<T1> argFactory, CancellationToken cancellationToken = default) => GetCore(key, argFactory, cancellationToken);

    public async ValueTask<TValue> GetCore(TKey key, Func<T1> argFactory, CancellationToken cancellationToken)
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

            T1 arg = argFactory();

            instance = await GetInternal(key, arg, cancellationToken)
                .NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetSync(TKey key, Func<T1> argFactory, CancellationToken cancellationToken = default) => GetCoreSync(key, argFactory, cancellationToken);

    public TValue GetCoreSync(TKey key, Func<T1> argFactory, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            T1 arg = argFactory();

            instance = GetInternalSync(key, arg, cancellationToken);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public async ValueTask<TValue> GetCore(TKey key, T1 arg, CancellationToken cancellationToken)
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

            instance = await GetInternal(key, arg, cancellationToken)
                .NoSync();
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    public TValue GetSync(TKey key, T1 arg, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_dictionary!.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            ThrowIfDisposed();

            if (_dictionary.TryGetValue(key, out instance))
                return instance;

            instance = GetInternalSync(key, arg, cancellationToken);
            _dictionary.TryAdd(key, instance);
        }

        return instance;
    }

    private ValueTask<TValue> GetInternal(TKey key, T1 arg, CancellationToken cancellationToken)
    {
        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyFunc(key, arg);

            case InitializationMode.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyTokenFunc(key, cancellationToken, arg);

            case InitializationMode.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncFunc(arg);

            case InitializationMode.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_func(arg));

            case InitializationMode.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_keyTokenFunc(key, cancellationToken, arg));

            case InitializationMode.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_keyFunc(key, arg));

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private TValue GetInternalSync(TKey key, T1 arg, CancellationToken cancellationToken)
    {
        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyFunc(key, arg)
                    .AwaitSync();

            case InitializationMode.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyTokenFunc(key, cancellationToken, arg)
                    .AwaitSync();

            case InitializationMode.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncFunc(arg)
                    .AwaitSync();

            case InitializationMode.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _keyFunc(key, arg);

            case InitializationMode.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _keyTokenFunc(key, cancellationToken, arg);

            case InitializationMode.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _func(arg);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetInitialization(Func<TKey, T1, ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public void SetInitialization(Func<TKey, CancellationToken, T1, ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public void SetInitialization(Func<T1, ValueTask<TValue>> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public void SetInitialization(Func<T1, TValue> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    public void SetInitialization(Func<TKey, T1, TValue> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public void SetInitialization(Func<TKey, CancellationToken, T1, TValue> func)
    {
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    private void EnsureInitializationNotSet()
    {
        if (_initializationMode is not null)
            throw new Exception("Setting the initialization of an SingletonKeyDictionary after it's already has been set is not allowed");
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
            throw new ObjectDisposedException(nameof(SingletonKeyDictionary<TKey, TValue, T1>));
    }
}