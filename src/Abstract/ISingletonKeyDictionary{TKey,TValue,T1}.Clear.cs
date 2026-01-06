using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue, T1>
    where TKey : notnull
{
    void ClearSync();

    ValueTask Clear(CancellationToken cancellationToken = default);
}


