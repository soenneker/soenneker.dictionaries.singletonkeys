using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// Retrieves a snapshot of all cached key/value pairs (sync).
    /// </summary>
    /// <returns>A new dictionary containing all cached entries.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    Dictionary<TKey, TValue> GetAllSync();

    /// <summary>
    /// Retrieves a snapshot of all cached key/value pairs (async).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the snapshot lock.</param>
    /// <returns>A new dictionary containing all cached entries.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<Dictionary<TKey, TValue>> GetAll(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a snapshot of all cached keys (sync).
    /// </summary>
    /// <returns>A list containing all cached keys.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    List<TKey> GetKeysSync();

    /// <summary>
    /// Retrieves a snapshot of all cached keys (async).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the snapshot lock.</param>
    /// <returns>A list containing all cached keys.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<List<TKey>> GetKeys(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a snapshot of all cached values (sync).
    /// </summary>
    /// <returns>A list containing all cached values.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    List<TValue> GetValuesSync();

    /// <summary>
    /// Retrieves a snapshot of all cached values (async).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token used while waiting to acquire the snapshot lock.</param>
    /// <returns>A list containing all cached values.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the dictionary has been disposed.</exception>
    [Pure]
    ValueTask<List<TValue>> GetValues(CancellationToken cancellationToken = default);
}