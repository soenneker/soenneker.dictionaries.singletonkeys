[![](https://img.shields.io/nuget/v/soenneker.dictionaries.singletonkeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.singletonkeys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.singletonkeys/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.singletonkeys/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.dictionaries.singletonkeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.singletonkeys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.singletonkeys/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.singletonkeys/actions/workflows/codeql.yml)

# Soenneker.Dictionaries.SingletonKeys

Creates, caches, and owns one value per key, with asynchronous and synchronous factories plus coordinated eviction and disposal.

## Installation

```bash
dotnet add package Soenneker.Dictionaries.SingletonKeys
```

## Basic usage

```csharp
using Soenneker.Dictionaries.SingletonKeys;

await using var clients = new SingletonKeyDictionary<string, ApiClient>(
    async (tenantId, cancellationToken) =>
        await ApiClient.Connect(tenantId, cancellationToken));

ApiClient client = await clients.Get("tenant-42", cancellationToken);
```

The first caller for a missing key runs its factory while concurrent callers for that key wait. A successful value is cached and returned until removal, clear, or disposal. Factories for different lock stripes can run concurrently; keys that hash to the same stripe temporarily serialize.

If a factory faults or is canceled, no value is cached and a later call can retry. The cancellation token of the creating call is passed to its factory; waiting callers can cancel while waiting for the key lock.

## Initialization arguments

Use the `T1` or `T1, T2` variants when creation needs arguments supplied by `Get`:

```csharp
var clients = new SingletonKeyDictionary<string, ApiClient, Uri, string>(
    (tenantId, endpoint, apiKey, cancellationToken) =>
        ApiClient.Connect(endpoint, apiKey, cancellationToken));

ApiClient client = await clients.Get(
    "tenant-42",
    endpoint,
    apiKey,
    cancellationToken);
```

Arguments are creation-only. Later calls for the same key receive the cached value even if they pass different arguments. The `Func<T1>` and `Func<(T1, T2)>` overloads defer argument construction until the key is known to be missing.

Factories can also be assigned once with `SetInitialization`, or with `Initialize(state, static ...)` to avoid capturing a closure. Configure initialization before concurrent use; changing the factory after one is set is rejected.

## Removal choices

```csharp
// Fast removal of an already cached value, including disposal:
bool removed = await clients.Remove("tenant-42", cancellationToken);

// Strong eviction that also waits behind an in-flight creation for this key:
bool evicted = await clients.Evict("tenant-42", cancellationToken);
```

- `Remove` is the same fast path as `TryRemoveAndDispose`. It can return `false` while a factory is still creating the key.
- `Evict` coordinates with creation and is the appropriate choice when the key must be absent after the call.
- `TryRemove(key, out value)` does not dispose the value; ownership transfers to the caller.
- `Clear` coordinates across all stripes, removes every cached value, and disposes them.

Synchronous counterparts are available, but they block when the configured factory or value disposal is asynchronous. Prefer the async APIs in request and worker code.

## Snapshots and ownership

`TryGet` never initializes. `GetAll`, `GetKeys`, and `GetValues` acquire all stripes and return new collections representing a coordinated snapshot.

Cached values are dictionary-owned. Removal-with-disposal, clear, and dictionary disposal prefer `IAsyncDisposable` over `IDisposable`. Do not cache the same disposable instance under multiple keys unless repeated disposal is safe, and do not use a value after its key is evicted.

Disposal is terminal and waits for factories already running under a key stripe before disposing their results. Do not call dictionary disposal from inside one of its own factories, because the factory holds a stripe that disposal must acquire.
