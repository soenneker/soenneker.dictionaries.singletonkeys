using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;

namespace Soenneker.Dictionaries.SingletonKeys.Tests;

public sealed class SingletonKeyDictionaryTests
{
    [Test]
    public async Task Keyed_initializes_once()
    {
        CancellationToken cancellationToken = CancellationToken.None;
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
    public async Task T1_argFactory_only_runs_when_missing()
    {
        CancellationToken cancellationToken = CancellationToken.None;
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
    public async Task T1_TryGet_and_GetAll_work()
    {
        CancellationToken cancellationToken = CancellationToken.None;
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
    public async Task T1_clear_disposes_values()
    {
        CancellationToken cancellationToken = CancellationToken.None;
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
    public async Task T1T2_tuple_argFactory_only_runs_when_missing()
    {
        CancellationToken cancellationToken = CancellationToken.None;
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
}
