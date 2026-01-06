using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

public partial interface ISingletonKeyDictionary<TKey, TValue>
    where TKey : notnull
{
    void ClearSync();

    ValueTask Clear(CancellationToken cancellationToken = default);
}


