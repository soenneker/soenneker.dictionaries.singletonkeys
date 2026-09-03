using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;

namespace Soenneker.Dictionaries.SingletonKeys.Tests;

public sealed class SingletonKeyDictionaryTests
{
    [Test]
    public async Task Different_keys_initialize_concurrently(CancellationToken cancellationToken)
    {
        var started = 0;
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var dict = new SingletonKeyDictionary<int, string>(async key =>
        {
            if (Interlocked.Increment(ref started) == 2)
                bothStarted.TrySetResult();

            await release.Task.ConfigureAwait(false);
            return key.ToString();
        });

        ValueTask<string> first = dict.Get(1, cancellationToken: cancellationToken);
        ValueTask<string> second = dict.Get(2, cancellationToken: cancellationToken);

        await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.SetResult();

        (await first).Should().Be("1");
        (await second).Should().Be("2");
    }

    [Test]
    public async Task Keyed_initializes_once(CancellationToken cancellationToken)
    {
        var calls = 0;

        var dict = new SingletonKeyDictionary<int, string>(key =>
        {
            Interlocked.Increment(ref calls);
            return new ValueTask<string>($"v-{key}");
        });

        string a = await dict.Get(1, cancellationToken);
        string b = await dict.Get(1, cancellationToken);

        a.Should().Be("v-1");
        b.Should().Be("v-1");
        calls.Should().Be(1);
    }

    [Test]
    public async Task Comparer_equal_keys_share_initialization(CancellationToken cancellationToken)
    {
        var calls = 0;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var dict = new SingletonKeyDictionary<ComparerKey, string>(async key =>
        {
            Interlocked.Increment(ref calls);
            started.TrySetResult();
            await release.Task.ConfigureAwait(false);
            return key.Value;
        }, new EquivalentKeyComparer());

        ValueTask<string> first = dict.Get(new ComparerKey("first", 0), cancellationToken);
        await started.Task;

        ValueTask<string> second = dict.Get(new ComparerKey("second", 1), cancellationToken);
        calls.Should().Be(1);

        release.SetResult();

        (await first).Should().Be("first");
        (await second).Should().Be("first");
        calls.Should().Be(1);
    }

    [Test]
    public async Task T1_argFactory_only_runs_when_missing(CancellationToken cancellationToken)
    {
        var argFactoryCalls = 0;

        var dict = new SingletonKeyDictionary<string, string, int>((key, arg) =>
            new ValueTask<string>($"{key}-{arg}"));

        string first = await dict.Get("k", () =>
        {
            Interlocked.Increment(ref argFactoryCalls);
            return 123;
        }, cancellationToken);

        string second = await dict.Get("k", () =>
        {
            Interlocked.Increment(ref argFactoryCalls);
            return 999;
        }, cancellationToken);

        first.Should().Be("k-123");
        second.Should().Be("k-123");
        argFactoryCalls.Should().Be(1);
    }

    [Test]
    public async Task T1_TryGet_and_GetAll_work(CancellationToken cancellationToken)
    {
        var dict = new SingletonKeyDictionary<string, string, int>((key, arg) =>
            new ValueTask<string>($"{key}-{arg}"));

        dict.TryGet("k", out _).Should().BeFalse();

        _ = await dict.Get("k", 5, cancellationToken);

        dict.TryGet("k", out string? value).Should().BeTrue();
        value.Should().Be("k-5");

        var all = await dict.GetAll(cancellationToken);
        all.Should().HaveCount(1);
        all.Should().ContainKey("k").WhoseValue.Should().Be("k-5");
    }

    [Test]
    public async Task T1_clear_disposes_values(CancellationToken cancellationToken)
    {
        var disposed = 0;

        var dict = new SingletonKeyDictionary<string, DisposableValue, int>((key, arg) =>
            new ValueTask<DisposableValue>(new DisposableValue(() => Interlocked.Increment(ref disposed))));

        _ = await dict.Get("a", 1, cancellationToken);
        _ = await dict.Get("b", 2, cancellationToken);

        await dict.Clear(cancellationToken);

        disposed.Should().Be(2);
        (await dict.GetKeys(cancellationToken)).Should().BeEmpty();
    }

    [Test]
    public async Task T1T2_tuple_argFactory_only_runs_when_missing(CancellationToken cancellationToken)
    {
        var argFactoryCalls = 0;

        var dict = new SingletonKeyDictionary<string, string, int, int>((key, a1, a2) =>
            new ValueTask<string>($"{key}-{a1}-{a2}"));

        string first = await dict.Get("k", () =>
        {
            Interlocked.Increment(ref argFactoryCalls);
            return (1, 2);
        }, cancellationToken);

        string second = await dict.Get("k", () =>
        {
            Interlocked.Increment(ref argFactoryCalls);
            return (9, 9);
        }, cancellationToken);

        first.Should().Be("k-1-2");
        second.Should().Be("k-1-2");
        argFactoryCalls.Should().Be(1);
    }

    [Test]
    public async Task DisposeAsync_waits_for_and_disposes_inflight_creation(CancellationToken cancellationToken)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = 0;

        var dict = new SingletonKeyDictionary<string, DisposableValue>(async _ =>
        {
            started.SetResult();
            await release.Task.ConfigureAwait(false);
            return new DisposableValue(() => Interlocked.Increment(ref disposed));
        });

        ValueTask<DisposableValue> get = dict.Get("inflight", cancellationToken: cancellationToken);
        await started.Task;

        Task disposing = dict.DisposeAsync().AsTask();
        disposing.IsCompleted.Should().BeFalse();

        release.SetResult();
        _ = await get;
        await disposing;

        disposed.Should().Be(1);
        Func<Task> action = async () => _ = await dict.Get("after-dispose", cancellationToken: cancellationToken);
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    private sealed class DisposableValue : IDisposable
    {
        private readonly Action _onDispose;

        public DisposableValue(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose();
        }
    }

    private sealed class ComparerKey
    {
        public ComparerKey(string value, int hashCode)
        {
            Value = value;
            HashCode = hashCode;
        }

        public string Value { get; }
        private int HashCode { get; }

        public override int GetHashCode() => HashCode;
    }

    private sealed class EquivalentKeyComparer : IEqualityComparer<ComparerKey>
    {
        public bool Equals(ComparerKey? x, ComparerKey? y) => x is not null && y is not null;

        public int GetHashCode(ComparerKey obj) => 0;
    }
}
