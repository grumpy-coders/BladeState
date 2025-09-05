# BladeState

[![NuGet Version](https://img.shields.io/nuget/v/BladeState.svg?style=flat\&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BladeState.svg?style=flat\&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![License](https://img.shields.io/github/license/doomfaller/BladeState.svg?style=flat)](LICENSE)

**BladeState** is a lightweight server-side state/session persistence library for Razor and Blazor applications.
It provides **dependency-injected storage** for persisting state across requests without relying on `HttpContext.Session`.

---

## ✨ Features

* 🗂 Server-side storage abstraction
* ⚡ Easy integration with **Dependency Injection**
* 🔄 Works across Razor & Blazor server applications
* 🔧 Extensible design for custom providers (e.g. Redis, SQL, Memory Cache)

---

## 🚀 Installation

Install via NuGet:

```bash
dotnet add package BladeState
```

---

## 🛠 Providers

BladeState includes multiple built-in providers for persisting state:

### 1. Memory Cache Provider (`MemoryCacheBladeStateProvider<T>`) ⚡

### 2. SQL Provider (`SqlBladeStateProvider<T>`) 📃

The SQL provider stores state in a relational database table using JSON serialization.

#### Example schema

```sql
CREATE TABLE BladeState (
    Id NVARCHAR(256) PRIMARY KEY,
    StateJson NVARCHAR(MAX) NOT NULL
);
```

#### Setup via `appsettings.json`

```json
{
  "ConnectionStrings": {
    "BladeState": "Server=localhost;Database=BladeStateDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;"
  }
}
```

#### Registration

```csharp
using Microsoft.Data.SqlClient;
using BladeState.Providers;

builder.Services.AddBladeState<MyState, SqlBladeStateProvider<MyState>>();

builder.Services.AddSingleton(sp => new SqlBladeStateProvider<MyState>(
    () => new SqlConnection(builder.Configuration.GetConnectionString("BladeState")),
    tableName: "BladeState",   // optional, defaults to "BladeState"
    stateId: "CustomStateId"   // optional, defaults to typeof(T).Name
));
```

#### How it works

* Uses a simple key/value table (`Id`, `StateJson`).
* JSON serialization handled automatically.

---

### 3. Redis Provider (`RedisBladeStateProvider<T>`) 🔥

Stores state in Redis using `StackExchange.Redis`.

#### Registration

```csharp
using BladeState.Providers;
using StackExchange.Redis;

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost")
);

builder.Services.AddBladeState<MyState, RedisBladeStateProvider<MyState>>();
```

#### Notes

* Stores JSON under a Redis key like `BladeState-{Profile.Id}`.
* Fast, distributed, great for scale-out.

---

### 4. EF Core Provider (`EfCoreBladeStateProvider<T>`) 🟢

Uses an Entity Framework `DbContext` to persist state directly in your model.

#### Registration

```csharp
using BladeState.Providers;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BladeState")));

builder.Services.AddBladeState<MyState, EfCoreBladeStateProvider<MyState>>();
```

#### Notes

* Assumes `T` maps directly to a table via EF Core.
* Uses normal `DbContext.SaveChangesAsync()` semantics.
* Best for when you want strongly typed schema vs JSON.

---

## ⚖️ Provider Comparison

| Provider    | Best For                            | Pros                                               | Cons                                           |
| ----------- | ----------------------------------- | -------------------------------------------------- | ---------------------------------------------- |
| **Memory Cache** | Performance and application level processing | Simple, next to no overhead, fast | Requires custom handling for persistence if necessary |
| **SQL**     | Simple persistence in relational DB | Works out of the box, JSON storage | Tied to SQL dialect, less efficient than Redis |
| **Redis**   | High-performance distributed cache  | Fast, scalable, great for web farms                | Requires Redis infrastructure, persistence optional     |
| **EF Core** | Strongly-typed relational models    | Uses your existing EF models, schema-first         | More overhead, requires migrations             |

---

## 🧩 Simple Service Collection Wire-up

Usage:

```csharp
builder.Services.AddBladeState<MyState, SqlBladeStateProvider<MyState>>();
```

---

## 📖 Example: Consuming State

```csharp
public class MyService
{
    private readonly BladeStateProvider<MyState> _stateProvider;

    public MyService(BladeStateProvider<MyState> stateProvider)
    {
        _stateProvider = stateProvider;
    }

    public async Task DoWorkAsync()
    {
        var state = await _stateProvider.LoadStateAsync();
        state.Counter++;
        await _stateProvider.SaveStateAsync(state);
    }
}
```

---

## Built-in Encryption ❔🪽

BladeState automatically encrypts persisted state data using AES encryption.

Enabled by default – you don’t need to do anything.

Encryption key – if not provided, BladeState will generate one automatically, simplifying encryption and decryption without explicit wire-up.
You may also supply your own key, example below

``` csharp

builder.Services.AddBladeState<MyAppState, SqlBladeStateProvider<MyAppState>>(
    useEncryption: true,
    encryptionKey: "my-custom-key" // Your key supplied as a string value
);

```

Optionally (and highly NOT recommended for Production 😁):
You may turn off encryption – you can explicitly disable encryption if needed for testing purposes.

``` csharp

builder.Services.AddBladeState<MyAppState, RedisBladeStateProvider<MyAppState>>(
    useEncryption: false
);

```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
