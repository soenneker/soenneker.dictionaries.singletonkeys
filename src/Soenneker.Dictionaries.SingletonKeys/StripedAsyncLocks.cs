using System;
using System.Threading;
using System.Threading.Tasks;
using Soenneker.Asyncs.Locks;
using Soenneker.Extensions.ValueTask;

namespace Soenneker.Dictionaries.SingletonKeys;

internal sealed class StripedAsyncLocks<TKey> where TKey : notnull
{
    private const int _stripeCount = 64;
    private readonly AsyncLock[] _locks = CreateLocks();

    internal AsyncLock For(TKey key)
    {
        int hash = key.GetHashCode() & int.MaxValue;
        return _locks[hash & (_stripeCount - 1)];
    }

    internal async ValueTask<AllReleaser> LockAll(CancellationToken cancellationToken = default)
    {
        var releasers = new Releaser[_locks.Length];
        var acquired = 0;

        try
        {
            for (; acquired < _locks.Length; acquired++)
                releasers[acquired] = await _locks[acquired].Lock(cancellationToken).NoSync();

            return new AllReleaser(releasers, acquired);
        }
        catch
        {
            DisposeReverse(releasers, acquired);
            throw;
        }
    }

    internal AllReleaser LockAllSync(CancellationToken cancellationToken = default)
    {
        var releasers = new Releaser[_locks.Length];
        var acquired = 0;

        try
        {
            for (; acquired < _locks.Length; acquired++)
                releasers[acquired] = _locks[acquired].LockSync(cancellationToken);

            return new AllReleaser(releasers, acquired);
        }
        catch
        {
            DisposeReverse(releasers, acquired);
            throw;
        }
    }

    private static AsyncLock[] CreateLocks()
    {
        var result = new AsyncLock[_stripeCount];
        for (var i = 0; i < result.Length; i++)
            result[i] = new AsyncLock();
        return result;
    }

    private static void DisposeReverse(Releaser[] releasers, int count)
    {
        for (int i = count - 1; i >= 0; i--)
            releasers[i].Dispose();
    }

    internal sealed class AllReleaser : IDisposable
    {
        private Releaser[]? _releasers;
        private readonly int _count;

        internal AllReleaser(Releaser[] releasers, int count)
        {
            _releasers = releasers;
            _count = count;
        }

        public void Dispose()
        {
            Releaser[]? releasers = Interlocked.Exchange(ref _releasers, null);
            if (releasers is not null)
                DisposeReverse(releasers, _count);
        }
    }
}
