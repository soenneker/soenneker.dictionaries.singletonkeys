using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Dictionaries.SingletonKeys.Abstract;

internal interface ISingletonKeyDictionaryStateFactory<TKey, TValue> where TKey : notnull
{
    ValueTask<TValue> Invoke(TKey key, CancellationToken cancellationToken);

    TValue InvokeSync(TKey key, CancellationToken cancellationToken);
}