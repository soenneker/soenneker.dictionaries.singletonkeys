using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Dictionaries.SingletonKeys;

namespace Audit;

public class CacheHitTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    [Test]
    public async Task SingletonCacheInitializesOnceAndHitsDoNotInvokeFactories()
    {
        int calls = 0;
        await using var cache = new SingletonKeyDictionary<string, object>(async (_, _) => { Interlocked.Increment(ref calls); await Task.Delay(10); return new object(); });
        var values = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => cache.Get("key").AsTask()));
        Check(calls == 1 && values.All(v => ReferenceEquals(v, values[0])), "Concurrent initialization was duplicated");
        Check(cache.Get("key").IsCompletedSuccessfully, "Cache hit not synchronous");
        await using var args = new SingletonKeyDictionary<string, string, int>(static (_, n) => n.ToString());
        Check(await args.Get("x", 42) == "42", "Argument was not passed to factory");
        Check(await args.Get("x", () => throw new Exception("must not evaluate")) == "42", "Hit evaluated arguments");
    }
}
