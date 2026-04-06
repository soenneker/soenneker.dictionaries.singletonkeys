using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Locks;
using Soenneker.Atomics.ValueBools;
using Soenneker.Dictionaries.SingletonKeys.Abstract;
using Soenneker.Enums.InitializationModes;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

/// <inheritdoc cref="ISingletonKeyDictionary{TKey,TValue,T1,T2}"/>
public partial class SingletonKeyDictionary<TKey, TValue, T1, T2> : ISingletonKeyDictionary<TKey, TValue, T1, T2> where TKey : notnull
{
    private ConcurrentDictionary<TKey, TValue>? _dictionary;
    private readonly AsyncLock _lock;

    private Func<TKey, T1, T2, CancellationToken, ValueTask<TValue>>? _asyncKeyTokenFunc;
    private Func<TKey, T1, T2, CancellationToken, TValue>? _keyTokenFunc;

    private Func<TKey, T1, T2, ValueTask<TValue>>? _asyncKeyFunc;
    private Func<TKey, T1, T2, TValue>? _keyFunc;

    private Func<T1, T2, ValueTask<TValue>>? _asyncFunc;
    private Func<T1, T2, TValue>? _func;

    private ValueAtomicBool _disposed;
    private InitializationMode? _initializationMode;

    public SingletonKeyDictionary()
    {
        _lock = new AsyncLock();
        _dictionary = new ConcurrentDictionary<TKey, TValue>();
    }

    public SingletonKeyDictionary(Func<TKey, T1, T2, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, T1, T2, CancellationToken, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<T1, T2, ValueTask<TValue>> func) : this()
    {
        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, T1, T2, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public SingletonKeyDictionary(Func<TKey, T1, T2, CancellationToken, TValue> func) : this()
    {
        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    public SingletonKeyDictionary(Func<T1, T2, TValue> func) : this()
    {
        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default) => GetCore(key, arg1, arg2, cancellationToken);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(TKey key, out TValue? value)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();
        return dict.TryGetValue(key, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default) =>
        GetCore(key, argFactory, cancellationToken);

    public async ValueTask<TValue> GetCore(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            (T1 arg1, T2 arg2) = argFactory();

            instance = await GetInternal(key, arg1, arg2, cancellationToken)
                .NoSync();
            return await TryAddOrGetExisting(key, instance, dict)
                .NoSync();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetSync(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default) =>
        GetCoreSync(key, argFactory, cancellationToken);

    public TValue GetCoreSync(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            (T1 arg1, T2 arg2) = argFactory();

            instance = GetInternalSync(key, arg1, arg2, cancellationToken);
            return TryAddOrGetExistingSync(key, instance, dict);
        }
    }

    public async ValueTask<TValue> GetCore(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            instance = await GetInternal(key, arg1, arg2, cancellationToken)
                .NoSync();
            return await TryAddOrGetExisting(key, instance, dict)
                .NoSync();
        }
    }

    public TValue GetSync(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            instance = GetInternalSync(key, arg1, arg2, cancellationToken);
            return TryAddOrGetExistingSync(key, instance, dict);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<TValue> Get<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull => GetCore(key, state, argFactory, cancellationToken);

    private async ValueTask<TValue> GetCore<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken)
        where TState : notnull
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            (T1 arg1, T2 arg2) = argFactory(state);

            instance = await GetInternal(key, arg1, arg2, cancellationToken)
                .NoSync();
            return await TryAddOrGetExisting(key, instance, dict)
                .NoSync();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue GetSync<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull => GetCoreSync(key, state, argFactory, cancellationToken);

    private TValue GetCoreSync<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken) where TState : notnull
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryGetValue(key, out TValue? instance))
            return instance;

        using (_lock.LockSync(cancellationToken))
        {
            dict = GetDictionaryOrThrow();

            if (dict.TryGetValue(key, out instance))
                return instance;

            (T1 arg1, T2 arg2) = argFactory(state);

            instance = GetInternalSync(key, arg1, arg2, cancellationToken);
            return TryAddOrGetExistingSync(key, instance, dict);
        }
    }

    private ValueTask<TValue> GetInternal(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken)
    {
        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyFunc(key, arg1, arg2);

            case InitializationMode.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyTokenFunc(key, arg1, arg2, cancellationToken);

            case InitializationMode.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncFunc(arg1, arg2);

            case InitializationMode.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_func(arg1, arg2));

            case InitializationMode.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_keyTokenFunc(key, arg1, arg2, cancellationToken));

            case InitializationMode.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return new ValueTask<TValue>(_keyFunc(key, arg1, arg2));

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private TValue GetInternalSync(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken)
    {
        if (_initializationMode is null)
            throw new InvalidOperationException("Initialization func for SingletonKeyDictionary cannot be null");

        switch (_initializationMode.Value)
        {
            case InitializationMode.AsyncKeyValue:
                if (_asyncKeyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyFunc(key, arg1, arg2)
                    .AwaitSync();

            case InitializationMode.AsyncKeyTokenValue:
                if (_asyncKeyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncKeyTokenFunc(key, arg1, arg2, cancellationToken)
                    .AwaitSync();

            case InitializationMode.AsyncValue:
                if (_asyncFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _asyncFunc(arg1, arg2)
                    .AwaitSync();

            case InitializationMode.SyncKeyValue:
                if (_keyFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _keyFunc(key, arg1, arg2);

            case InitializationMode.SyncKeyTokenValue:
                if (_keyTokenFunc is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _keyTokenFunc(key, arg1, arg2, cancellationToken);

            case InitializationMode.SyncValue:
                if (_func is null)
                    throw new NullReferenceException("Initialization func for SingletonKeyDictionary cannot be null");

                return _func(arg1, arg2);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void SetInitialization(Func<TKey, T1, T2, ValueTask<TValue>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.AsyncKey;
        _asyncKeyFunc = func;
    }

    public void SetInitialization(Func<TKey, T1, T2, CancellationToken, ValueTask<TValue>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.AsyncKeyToken;
        _asyncKeyTokenFunc = func;
    }

    public void SetInitialization(Func<T1, T2, ValueTask<TValue>> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.Async;
        _asyncFunc = func;
    }

    public void SetInitialization(Func<T1, T2, TValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.Sync;
        _func = func;
    }

    public void SetInitialization(Func<TKey, T1, T2, TValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.SyncKey;
        _keyFunc = func;
    }

    public void SetInitialization(Func<TKey, T1, T2, CancellationToken, TValue> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitializationNotSet();

        _initializationMode = InitializationMode.SyncKeyToken;
        _keyTokenFunc = func;
    }

    private void EnsureInitializationNotSet()
    {
        if (_initializationMode is not null)
            throw new InvalidOperationException("Setting the initialization of a SingletonKeyDictionary after it has already been set is not allowed");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask<bool> Remove(TKey key, CancellationToken cancellationToken = default) => TryRemoveAndDispose(key);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveSync(TKey key, CancellationToken cancellationToken = default) => TryRemoveAndDisposeSync(key);

    public async ValueTask<bool> Evict(TKey key, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryRemove(key, out TValue? instance))
        {
            await DisposeRemovedInstance(instance)
                .NoSync();
            return true;
        }

        using (await _lock.Lock(cancellationToken)
                          .NoSync())
        {
            dict = GetDictionaryOrThrow();

            if (!dict.TryRemove(key, out instance))
                return false;
        }

        await DisposeRemovedInstance(instance)
            .NoSync();

        return true;
    }

    public bool EvictSync(TKey key, CancellationToken cancellationToken = default)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryRemove(key, out TValue? instance))
        {
            DisposeRemovedInstanceSync(instance);
            return true;
        }

        using (_lock.LockSync(cancellationToken))
        {
            dict = GetDictionaryOrThrow();

            if (!dict.TryRemove(key, out instance))
                return false;
        }

        DisposeRemovedInstanceSync(instance);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRemove(TKey key, out TValue? value)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();
        return dict.TryRemove(key, out value);
    }

    public async ValueTask<bool> TryRemoveAndDispose(TKey key)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryRemove(key, out TValue? instance))
        {
            await DisposeRemovedInstance(instance)
                .NoSync();
            return true;
        }

        return false;
    }

    public bool TryRemoveAndDisposeSync(TKey key)
    {
        ConcurrentDictionary<TKey, TValue> dict = GetDictionaryOrThrow();

        if (dict.TryRemove(key, out TValue? instance))
        {
            DisposeRemovedInstanceSync(instance);
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (!_disposed.TrySetTrue())
            return;

        ConcurrentDictionary<TKey, TValue>? dict = _dictionary;
        _dictionary = null;

        if (dict is null || dict.IsEmpty)
            return;

        foreach (TValue value in dict.Values)
        {
            DisposeRemovedInstanceSync(value);
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

        foreach (TValue value in dict.Values)
        {
            await DisposeRemovedInstance(value)
                .NoSync();
        }
    }

    private async ValueTask<TValue> TryAddOrGetExisting(TKey key, TValue created, ConcurrentDictionary<TKey, TValue> dict)
    {
        if (dict.TryAdd(key, created))
            return created;

        if (dict.TryGetValue(key, out TValue? existing))
        {
            await DisposeRemovedInstance(created)
                .NoSync();
            return existing;
        }

        await DisposeRemovedInstance(created)
            .NoSync();

        throw new InvalidOperationException("Unable to add or retrieve an existing singleton value.");
    }

    private TValue TryAddOrGetExistingSync(TKey key, TValue created, ConcurrentDictionary<TKey, TValue> dict)
    {
        if (dict.TryAdd(key, created))
            return created;

        if (dict.TryGetValue(key, out TValue? existing))
        {
            DisposeRemovedInstanceSync(created);
            return existing;
        }

        DisposeRemovedInstanceSync(created);
        throw new InvalidOperationException("Unable to add or retrieve an existing singleton value.");
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
    private ConcurrentDictionary<TKey, TValue> GetDictionaryOrThrow()
    {
        ConcurrentDictionary<TKey, TValue>? dict = _dictionary;

        if (dict is null || _disposed.Value)
            throw new ObjectDisposedException(nameof(SingletonKeyDictionary<TKey, TValue, T1, T2>));

        return dict;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed.Value)
            throw new ObjectDisposedException(nameof(SingletonKeyDictionary<TKey, TValue, T1, T2>));
    }
}