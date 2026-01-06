using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue, T1, T2>
    where TKey : notnull
{
    void ClearSync();

    ValueTask Clear(CancellationToken cancellationToken = default);
}


