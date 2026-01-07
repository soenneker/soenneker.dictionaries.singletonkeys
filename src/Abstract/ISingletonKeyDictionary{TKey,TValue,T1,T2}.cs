using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

/// <summary>
/// A singleton dictionary keyed by <typeparamref name="TKey"/>, supporting initialization arguments of type
/// <typeparamref name="T1"/> and <typeparamref name="T2"/>.
/// </summary>
/// <typeparam name="TKey">The key type. Must be non-null.</typeparam>
/// <typeparam name="TValue">The cached value type.</typeparam>
/// <typeparam name="T1">The first initialization argument type.</typeparam>
/// <typeparam name="T2">The second initialization argument type.</typeparam>
/// <remarks>Be sure to dispose of this gracefully if using a disposable value type.</remarks>
public partial interface ISingletonKeyDictionary<TKey, TValue, T1, T2> : IDisposable, IAsyncDisposable
    where TKey : notnull
{
    /// <summary>
    /// Retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing, using <paramref name="arg1"/> and <paramref name="arg2"/> as initialization arguments.
    /// </summary>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="arg1">First initialization argument provided to the configured factory.</param>
    /// <param name="arg2">Second initialization argument provided to the configured factory.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>A task that completes with the cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<TValue> Get(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing.
    /// The <paramref name="argFactory"/> is invoked only if the value needs to be created.
    /// </summary>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="argFactory">Factory producing <c>(T1, T2)</c>. Invoked only on cache miss.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>A task that completes with the cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<TValue> Get(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default);

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
    /// Synchronously retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing, using <paramref name="arg1"/> and <paramref name="arg2"/> as initialization arguments.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="Get(TKey, T1, T2, CancellationToken)"/> when possible.
    /// If an async initialization delegate is configured, this call will block the calling thread.
    /// </remarks>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="arg1">First initialization argument provided to the configured factory.</param>
    /// <param name="arg2">Second initialization argument provided to the configured factory.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>The cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    TValue GetSync(TKey key, T1 arg1, T2 arg2, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing.
    /// The <paramref name="argFactory"/> is invoked only if the value needs to be created.
    /// </summary>
    /// <remarks>
    /// Prefer the async <c>Get</c> overload when possible. If an async initialization delegate is configured, this call will block the calling thread.
    /// </remarks>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="argFactory">Factory producing <c>(T1, T2)</c>. Invoked only on cache miss.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>The cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    TValue GetSync(TKey key, Func<(T1, T2)> argFactory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing, using a stateful <paramref name="argFactory"/>.
    /// This overload is designed to enable static lambdas and avoid closure allocations.
    /// </summary>
    /// <typeparam name="TState">The type of caller-provided state used to derive the arguments.</typeparam>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="state">Caller-provided state object.</param>
    /// <param name="argFactory">Factory producing <c>(T1, T2)</c> from <paramref name="state"/>.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>A task that completes with the cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<TValue> Get<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Synchronously retrieves the singleton value for <paramref name="key"/>, creating and caching it if missing, using a stateful <paramref name="argFactory"/>.
    /// This overload is designed to enable static lambdas and avoid closure allocations.
    /// </summary>
    /// <typeparam name="TState">The type of caller-provided state used to derive the arguments.</typeparam>
    /// <param name="key">The key identifying the singleton value.</param>
    /// <param name="state">Caller-provided state object.</param>
    /// <param name="argFactory">Factory producing <c>(T1, T2)</c> from <paramref name="state"/>.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the initialization lock.</param>
    /// <returns>The cached (or newly created) value.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    TValue GetSync<TState>(TKey key, TState state, Func<TState, (T1, T2)> argFactory, CancellationToken cancellationToken = default)
        where TState : notnull;

    /// <summary>
    /// Sets the async initialization function used to create values for a key, given initialization arguments.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, T1, T2, ValueTask<TValue>> func);

    /// <summary>
    /// Sets the async initialization function used to create values for a key, given initialization arguments, with cancellation support.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, T1, T2, CancellationToken, ValueTask<TValue>> func);

    /// <summary>
    /// Sets the async initialization function used to create values without a key, given initialization arguments.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<T1, T2, ValueTask<TValue>> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values without a key, given initialization arguments.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<T1, T2, TValue> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values for a key, given initialization arguments.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, T1, T2, TValue> func);

    /// <summary>
    /// Sets the synchronous initialization function used to create values for a key, given initialization arguments, with cancellation support.
    /// </summary>
    /// <param name="func">The factory invoked when a key is missing.</param>
    /// <exception cref="Exception">Thrown if initialization has already been configured.</exception>
    void SetInitialization(Func<TKey, T1, T2, CancellationToken, TValue> func);

    /// <summary>
    /// Removes the value associated with <paramref name="key"/> and disposes it if applicable.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the removal lock.</param>
    /// <returns>A task that completes when removal and disposal have finished.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    ValueTask Remove(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously removes the value associated with <paramref name="key"/> and disposes it if applicable.
    /// </summary>
    /// <remarks>Prefer <see cref="Remove(TKey, CancellationToken)"/> when possible.</remarks>
    /// <param name="key">The key to remove.</param>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the removal lock.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    void RemoveSync(TKey key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disposes the dictionary and disposes all cached values where applicable.
    /// </summary>
    new void Dispose();

    /// <summary>
    /// Asynchronously disposes the dictionary and disposes all cached values where applicable.
    /// </summary>
    new ValueTask DisposeAsync();
}


