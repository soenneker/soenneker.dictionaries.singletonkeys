using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue, T1, T2>
    where TKey : notnull
{
    [Pure]
    Dictionary<TKey, TValue> GetAllSync();

    [Pure]
    ValueTask<Dictionary<TKey, TValue>> GetAll(CancellationToken cancellationToken = default);

    [Pure]
    List<TKey> GetKeysSync();

    [Pure]
    ValueTask<List<TKey>> GetKeys(CancellationToken cancellationToken = default);

    [Pure]
    List<TValue> GetValuesSync();

    [Pure]
    ValueTask<List<TValue>> GetValues(CancellationToken cancellationToken = default);
}


