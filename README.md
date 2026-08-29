[![](https://img.shields.io/nuget/v/soenneker.dictionaries.singletonkeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.singletonkeys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.singletonkeys/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.singletonkeys/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.dictionaries.singletonkeys.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.dictionaries.singletonkeys/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.dictionaries.singletonkeys/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.dictionaries.singletonkeys/actions/workflows/codeql.yml)

# Soenneker.Dictionaries.SingletonKeys

Defines enumeration operations for singleton values addressed by composite keys.

## Install

```bash
dotnet add package Soenneker.Dictionaries.SingletonKeys
```

## Quick start

```csharp
using Soenneker.Dictionaries.SingletonKeys.Abstract;

ISingletonKeyDictionary<TKey, TValue, T1, T2> singletonKeyDictionary = /* resolve from DI */;
singletonKeyDictionary.ClearSync();
```

Clears all cached entries and disposes cached values where applicable (sync).

## What you get

- `ISingletonKeyDictionary<TKey, TValue, T1, T2>` — Defines enumeration operations for singleton values addressed by composite keys.
- `ISingletonKeyDictionary<TKey, TValue, T1>` — Defines enumeration operations for singleton values addressed by composite keys.
- `ISingletonKeyDictionary<TKey, TValue>` — Defines enumeration operations for singleton values addressed by composite keys.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.ClearSync()` | Clears all cached entries and disposes cached values where applicable (sync). | Returns no value; the requested change is complete when the method returns. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Clear(cancellationToken)` | Clears all cached entries and disposes cached values where applicable (async). | A task that completes when clearing and disposal have finished. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Get(key, arg1, arg2, cancellationToken)` | Retrieves the singleton value for `key`, creating and caching it if missing, using `arg1` and `arg2` as initialization arguments. If another concurrent creation wins the add race, the newly created instance is disposed and the existing cached value is returned. | A task that completes with the cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Get(key, argFactory, cancellationToken)` | Retrieves the singleton value for `key`, creating and caching it if missing. The `argFactory` is invoked only if the value needs to be created. If another concurrent creation wins the add race, the newly created instance is disposed and the existing cached value is returned. | A task that completes with the cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.TryGet(key, value)` | Attempts to retrieve a cached value for `key` without initializing it if missing. | `true` if a value exists for `key`; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, arg1, arg2, cancellationToken)` | Synchronously retrieves the singleton value for `key`, creating and caching it if missing, using `arg1` and `arg2` as initialization arguments. | The cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, argFactory, cancellationToken)` | Synchronously retrieves the singleton value for `key`, creating and caching it if missing. The `argFactory` is invoked only if the value needs to be created. | The cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Get(key, state, argFactory, cancellationToken)` | Retrieves the singleton value for `key`, creating and caching it if missing, using a stateful `argFactory`. This overload is designed to enable static lambdas and avoid closure allocations. If another concurrent creation wins the add race, the newly created instance is disposed and the existing cached value is returned. | A task that completes with the cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, state, argFactory, cancellationToken)` | Synchronously retrieves the singleton value for `key`, creating and caching it if missing, using a stateful `argFactory`. This overload is designed to enable static lambdas and avoid closure allocations. | The cached (or newly created) value. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.SetInitialization(func)` | Sets the async initialization function used to create values for a key, given initialization arguments. | Returns no value; the requested change is complete when the method returns. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.TryRemove(key, value)` | Attempts to remove the current value for `key` without disposing it. This is a direct pass-through to the underlying dictionary and does not coordinate with in-flight creation. | `true` if a value was removed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.TryRemoveAndDispose(key)` | Attempts to remove the current value for `key` and dispose it if applicable. This is the fast no-lock removal path and only affects the value currently stored at the time of removal. | `true` if a value was removed and disposed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.TryRemoveAndDisposeSync(key)` | Synchronously attempts to remove the current value for `key` and dispose it if applicable. This is the fast no-lock removal path and only affects the value currently stored at the time of removal. | `true` if a value was removed and disposed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Remove(key, cancellationToken)` | Removes and disposes the current value associated with `key`. This is the same fast no-lock behavior as `TryRemoveAndDispose(TKey)` and does not retry under the creation lock. | `true` if the current value was removed and disposed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.RemoveSync(key, cancellationToken)` | Synchronously removes and disposes the current value associated with `key`. This is the same fast no-lock behavior as `TryRemoveAndDisposeSync(TKey)` and does not retry under the creation lock. | `true` if the current value was removed and disposed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Evict(key, cancellationToken)` | Strongly removes the value associated with `key`, handling races with in-flight creation, and disposes it if applicable. Prefer this method when removal must account for a value being added between a fast remove attempt and lock acquisition. | `true` if a value was removed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.EvictSync(key, cancellationToken)` | Synchronously evicts the value associated with `key`, handling races with in-flight creation, and disposes it if applicable. Prefer this method when removal must account for a value being added between a fast remove attempt and lock acquisition. | `true` if a value was removed; otherwise, `false`. |
| `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Dispose()` | Disposes the dictionary and disposes all cached values where applicable. | Returns no value; the requested change is complete when the method returns. |

## Important behavior

- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.ClearSync()`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Clear(cancellationToken)`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Get(key, arg1, arg2, cancellationToken)`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.Get(key, argFactory, cancellationToken)`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.TryGet(key, value)`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, arg1, arg2, cancellationToken)`: Prefer `Get(TKey, T1, T2, CancellationToken)` when possible. If an async initialization delegate is configured, this call will block the calling thread.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, arg1, arg2, cancellationToken)`: Thrown if the dictionary has been disposed.
- `ISingletonKeyDictionary<TKey, TValue, T1, T2>.GetSync(key, argFactory, cancellationToken)`: Prefer the async `Get` overload when possible. If an async initialization delegate is configured, this call will block the calling thread.

## Practical notes

- Cancellation stops pending work; it does not undo work that has already completed.
- Calls that return a cached or singleton value reuse the same instance until the owning service is disposed.
- Dispose instances you own when their scope ends so held resources can be released.
