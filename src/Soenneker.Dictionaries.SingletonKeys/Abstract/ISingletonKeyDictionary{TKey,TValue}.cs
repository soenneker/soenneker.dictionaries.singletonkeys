using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

/// <summary>
/// A keyed singleton cache that creates at most one logical value per <typeparamref name="TKey"/> and reuses it for subsequent lookups.
/// </summary>
/// <typeparam name="TKey">The key type. Must be non-null.</typeparam>
/// <typeparam name="TValue">The cached value type.</typeparam>
/// <remarks>
/// Value creation is coordinated with double-check locking.
/// Removal APIs intentionally have different semantics:
/// <see cref="TryRemove(TKey, out TValue?)"/> does not dispose,
/// <see cref="TryRemoveAndDispose(TKey)"/> is the fast no-lock remove path,
/// <see cref="Remove(TKey, CancellationToken)"/> is an alias for that fast path,
/// and <see cref="Evict(TKey, CancellationToken)"/> is the stronger option for races with in-flight creation.
/// </remarks>
public partial interface ISingletonKeyDictionary<TKey, TValue> : IDisposable, IAsyncDisposable
    where TKey : notnull
{
    /// <summary>
    /// Retrieves the singleton value associated with <paramref name="key"/>, creating and caching it if it does not already exist.
    /// If another concurrent creation wins the add race, the newly created instance is disposed and the existing cached value is returned.
    /// </summary>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>A task that completes with the cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the dictionary has not been configured with an initialization function.</exception>
    /// <exception cref="NullReferenceException">Thrown if a configured initialization delegate is unexpectedly null.</exception>
    [Pure]
    ValueTask<TValue> Get(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to retrieve a cached value for <paramref name="key"/> without initializing it if missing.
    /// </summary>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="value">When this method returns, contains the cached value if found; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if a value exists for <paramref name="key"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    bool TryGet(TKey key, out TValue? value);

    /// <summary>
    /// Synchronously retrieves the singleton value associated with <paramref name="key"/>, creating and caching it if it does not already exist.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="Get(TKey, CancellationToken)"/> when possible.
    /// If an async initialization delegate is configured, this call will block the calling thread.
    /// </remarks>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>The cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the dictionary has not been configured with an initialization function.</exception>
    /// <exception cref="NullReferenceException">Thrown if a configured initialization delegate is unexpectedly null.</exception>
    [Pure]
    TValue GetSync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the singleton value associated with a key derived from <paramref name="state"/> via <paramref name="keyFactory"/>.
    /// This overload is designed to enable static lambdas and avoid closure allocations.
    /// </summary>
    /// <typeparam name="TState">The type of caller-provided state used to derive the key.</typeparam>
    /// <param name="state">Caller-provided state object.</param>
    /// <param name="keyFactory">Factory that produces the key from <paramref name="state"/>.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>A task that completes with the cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<TValue> Get<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Synchronously retrieves the singleton value associated with a key derived from <paramref name="state"/> via <paramref name="keyFactory"/>.
    /// This overload is designed to enable static lambdas and avoid closure allocations.
    /// </summary>
    /// <typeparam name="TState">The type of caller-provided state used to derive the key.</typeparam>
    /// <param name="state">Caller-provided state object.</param>
    /// <param name="keyFactory">Factory that produces the key from <paramref name="state"/>.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>The cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    TValue GetSync<TState>(TState state, Func<TState, TKey> keyFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Configures the dictionary to initialize values via a stateful factory and returns this instance for fluent usage.
    /// </summary>
    /// <typeparam name="TState">The type of the state object passed to the factory.</typeparam>
    /// <param name="state">The state object to pass to the factory when creating values.</param>
    /// <param name="factory">Factory invoked to create values for a given key.</param>
    /// <returns>This instance, configured to use the provided stateful factory.</returns>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    SingletonKeyDictionary<TKey, TValue> Initialize<TState>(TState state, Func<TState, TKey, CancellationToken, ValueTask<TValue>> factory)
        where TState : notnull;

    /// <summary>
    /// Sets the async initialization function used to create values for a key.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, ValueTask<TValue>> func);

    /// <summary>
    /// Sets the async initialization function used to create values for a key, with cancellation support.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, CancellationToken, ValueTask<TValue>> func);

    /// <summary>
    /// Sets the async initialization function used to create values without a key.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<ValueTask<TValue>> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values without a key.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TValue> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values for a key.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, TValue> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values for a key, with cancellation support.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, CancellationToken, TValue> func);

    /// <summary>
    /// Attempts to remove the current value for <paramref name="key"/> without disposing it.
    /// This is a direct pass-through to the underlying dictionary and does not coordinate with in-flight creation.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="value">When this method returns, contains the removed value if one was present; otherwise, the default value.</param>
    /// <returns><see langword="true"/> if a value was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    bool TryRemove(TKey key, out TValue? value);

    /// <summary>
    /// Attempts to remove the current value for <paramref name="key"/> and dispose it if applicable.
    /// This is the fast no-lock removal path and only affects the value currently stored at the time of removal.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if a value was removed and disposed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    ValueTask<bool> TryRemoveAndDispose(TKey key);

    /// <summary>
    /// Synchronously attempts to remove the current value for <paramref name="key"/> and dispose it if applicable.
    /// This is the fast no-lock removal path and only affects the value currently stored at the time of removal.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns><see langword="true"/> if a value was removed and disposed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    bool TryRemoveAndDisposeSync(TKey key);

    /// <summary>
    /// Removes and disposes the current value associated with <paramref name="key"/>.
    /// This is the same fast no-lock behavior as <see cref="TryRemoveAndDispose(TKey)"/> and does not retry under the creation lock.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="cancellationToken">Unused for the fast path. Included for API consistency.</param>
    /// <returns><see langword="true"/> if the current value was removed and disposed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    ValueTask<bool> Remove(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously removes and disposes the current value associated with <paramref name="key"/>.
    /// This is the same fast no-lock behavior as <see cref="TryRemoveAndDisposeSync(TKey)"/> and does not retry under the creation lock.
    /// </summary>
    /// <remarks>Prefer <see cref="Remove(TKey, CancellationToken)"/> when possible.</remarks>
    /// <param name="key">The key to remove.</param>
    /// <param name="cancellationToken">Unused for the fast path. Included for API consistency.</param>
    /// <returns><see langword="true"/> if the current value was removed and disposed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    bool RemoveSync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Strongly removes the value associated with <paramref name="key"/>, handling races with in-flight creation, and disposes it if applicable.
    /// Prefer this method when removal must account for a value being added between a fast remove attempt and lock acquisition.
    /// </summary>
    /// <param name="key">The key to evict.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the eviction lock.</param>
    /// <returns><see langword="true"/> if a value was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    ValueTask<bool> Evict(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously evicts the value associated with <paramref name="key"/>, handling races with in-flight creation, and disposes it if applicable.
    /// Prefer this method when removal must account for a value being added between a fast remove attempt and lock acquisition.
    /// </summary>
    /// <remarks>Prefer <see cref="Evict(TKey, CancellationToken)"/> when possible.</remarks>
    /// <param name="key">The key to evict.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the eviction lock.</param>
    /// <returns><see langword="true"/> if a value was removed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    bool EvictSync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes the dictionary and disposes all cached values where applicable.
    /// </summary>
    /// <remarks>
    /// If a cached value implements <see cref="IDisposable"/>, <see cref="IDisposable.Dispose"/> is used.
    /// If a cached value only implements <see cref="IAsyncDisposable"/>, this will block while disposing it.
    /// Prefer <see cref="DisposeAsync"/> when possible.
    /// </remarks>
    new void Dispose();

    /// <summary>
    /// Asynchronously disposes the dictionary and disposes all cached values where applicable.
    /// </summary>
    new ValueTask DisposeAsync();
}