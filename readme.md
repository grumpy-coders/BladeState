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
* 🛡 Avoids reliance on `HttpContext.Session`
* 🔧 Extensible design for custom providers (SQL, Redis, EF Core, InMemory)

---

## 🚀 Installation

Install via NuGet:

```bash
dotnet add package BladeState
```

---

## 🚀 Quick Start

### Define Your State

```csharp
public class MyState
{
    public string SomeValue { get; set; } = "Hello";
    public int Counter { get; set; } = 0;
}
```

### Register BladeState in DI

You can register any provider using the built-in extension method:

```csharp
using BladeState;
using BladeState.Providers;

builder.Services.AddBladeState<MyState, RedisBladeStateProvider<MyState>>();
```

This pattern works with any provider you add (`SqlBladeStateProvider<T>`, `RedisBladeStateProvider<T>`, `EfCoreBladeStateProvider<T>`).

---

## 🗄️ SQL Provider Setup

You can configure `SqlBladeStateProvider<T>` to work as a singleton in your DI container while still creating a **new `DbConnection` per operation**. This avoids connection pooling issues while keeping state accessible throughout your app.

### 1️⃣ Configure `appsettings.json`

```json
{
  "ConnectionStrings": {
    "BladeState": "Server=localhost;Database=BladeStateDb;User Id=sa;Password=yourStrong(!)Password;TrustServerCertificate=True;"
  }
}
```

### 2️⃣ Register in `Program.cs`

```csharp
using Microsoft.Data.SqlClient;
using BladeState.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddBladeState<MyState, SqlBladeStateProvider<MyState>>();

builder.Services.AddSingleton<SqlBladeStateProvider<MyState>>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connString = config.GetConnectionString("BladeState");
    return new SqlBladeStateProvider<MyState>(() => new SqlConnection(connString));
});
```

### 3️⃣ Inject and use

```csharp
public class MyService
{
    private readonly SqlBladeStateProvider<MyState> _stateProvider;

    public MyService(SqlBladeStateProvider<MyState> stateProvider)
    {
        _stateProvider = stateProvider;
    }

    public async Task DoWorkAsync()
    {
        await _stateProvider.LoadStateAsync();
        _stateProvider.State.SomeValue = "Updated!";
        _stateProvider.State.Counter++;
        await _stateProvider.SaveStateAsync();
    }
}
```

---

## 🧩 Redis Provider Setup

`RedisBladeStateProvider<T>` persists state to Redis using [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/).

### Register

```csharp
using StackExchange.Redis;
using BladeState.Providers;

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost"));

builder.Services.AddBladeState<MyState, RedisBladeStateProvider<MyState>>();
```

### Usage

```csharp
public class MyService
{
    private readonly RedisBladeStateProvider<MyState> _stateProvider;

    public MyService(RedisBladeStateProvider<MyState> stateProvider)
    {
        _stateProvider = stateProvider;
    }

    public async Task DoWorkAsync()
    {
        await _stateProvider.LoadStateAsync();
        _stateProvider.State.SomeValue = "From Redis";
        await _stateProvider.SaveStateAsync();
    }
}
```

---

## 🗃 EF Core Provider Setup

`EfCoreBladeStateProvider<T>` persists state directly into an EF Core-managed database.

### Register

```csharp
using BladeState.Providers;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BladeState")));

builder.Services.AddBladeState<MyState, EfCoreBladeStateProvider<MyState>>();
```

### Usage

```csharp
public class MyService
{
    private readonly EfCoreBladeStateProvider<MyState> _stateProvider;

    public MyService(EfCoreBladeStateProvider<MyState> stateProvider)
    {
        _stateProvider = stateProvider;
    }

    public async Task DoWorkAsync()
    {
        await _stateProvider.LoadStateAsync();
        _stateProvider.State.SomeValue = "From EF Core";
        await _stateProvider.SaveStateAsync();
    }
}
```

---

## 🛠️ Custom Providers

If SQL, Redis, or EF Core isn’t your persistence layer, you can build your own by extending `BladeStateProvider<T>`:

```csharp
public class CustomBladeStateProvider<T> : BladeStateProvider<T> where T : class, new()
{
    public override Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        // Implement persistence
        return Task.CompletedTask;
    }

    public override Task<T> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        // Implement load
        return Task.FromResult(new T());
    }

    public override Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        // Implement clearing
        return Task.CompletedTask;
    }
}
```

---

## 📖 License

MIT License.
