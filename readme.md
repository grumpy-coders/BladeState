# BladeState

## This project is being developed and provided 'AS IS'

DOING OUR BEST TO GET FEATURES AND FIXES (IF NEEDED) OUT ASAP

## I AM ACTIVELY UPDATING ON NUGET AND README, HMU FOR FEATURE REQUESTS -> doomfaller@gmail.com

THANKS FOR YOUR PATIENCE - YOUR FAVORITE SCHIZO

## What's on the road map? 
- FileBladeStateProvider
- File State viewer app
- Performance and Enhancements with Change Event
- Enterprise and Large Team Licensing

## What's new in version 1.0.6?

- Performance and stability enhancements

[![NuGet Version](https://img.shields.io/nuget/v/BladeState.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BladeState.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![License](https://img.shields.io/github/license/doomfaller/BladeState.svg?style=flat)](LICENSE)

**BladeState** is a lightweight server-side dependency injection state persistence library for .NET applications.
It provides **dependency-injected storage** for persisting state across requests without relying on `HttpContext.Session`.

---

## ✨ Features

- 🗂 Server-side storage abstraction
- ⚡ Easy integration with **Dependency Injection**
- 🔄 Works across Razor & Blazor server applications
- 🔧 Extensible design for custom providers (e.g. Redis, SQL, Memory Cache)

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

Stores state in in-memory cache for the lifetime of the application.

```csharp
using BladeState;
using BladeState.Models;
using BladeState.Providers;

builder.Services.AddMemoryCacheBladeState<MyState>();
```

### 2. SQL Provider (`SqlBladeStateProvider<T>`) 📃

The SQL provider stores state in a relational database table using JSON serialization.

#### Example schema

```sql
CREATE TABLE BladeState (
    InstanceId NVARCHAR(256) PRIMARY KEY,
    [Data] NVARCHAR(MAX) NOT NULL
);
```

#### Registration

```csharp
using Microsoft.Data.SqlClient;
using BladeState;
using BladeState.Models;
using BladeState.Providers;

var profile = new BladeStateProfile();

builder.Services.AddSqlBladeState<MyState>(
    () => new SqlConnection("Server=localhost;Database=BladeStateDb;User Id=yourUserId;Password=YourStrong(!)Password;TrustServerCertificate=True;"),
    profile,
    SqlType.MySql
);

```

#### How it works

- Uses a simple key/value table (`InstanceId`, `Data`).
- JSON serialization handled automatically.

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

builder.Services.AddRedisBladeState<MyState>(
    new BladeStateProfile
    {
        InstanceName = "MyAppUsingRedis" //If InstanceName is not provided the value will be 'BladeState'
    }
);

```

#### Notes

- Stores JSON under a Redis key formatted like `{BladeStateProfile.InstanceName}:{BladeStateProfile.InstanceId}`.
- Fast, distributed, great for scale-out.

---

### 4. EF Core Provider (`EfCoreBladeStateProvider<T>`) 🟢

Uses an Entity Framework `DbContext` to persist state directly in your model.

#### Registration

````csharp

## 1. Create your EF Core `DbContext`

Define a `DbContext` that includes your `BladeStateEntity` set. This is where BladeState will persist state.
- Optionally you may inherit from the IBladeStateEntity to extend for your organization

```csharp
using BladeState.Models;
using Microsoft.EntityFrameworkCore;

namespace MyApp.Data;

public class MyDbContext : DbContext
{
    public LicenseDbContext(DbContextOptions<MyDbContext> options)
        : base(options) { }

    // Required for BladeState
    public DbSet<BladeStateEntity> BladeStates { get; set; }
}
````

---

## 2. Configure `DbContext` in `Program.cs`

Register your `DbContext` with EF Core.

```csharp
// --- EF Core ---
builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("MyConnection")
    )
);
```

---

## 3. Add a BladeState Profile

In your `appsettings.json`, define the BladeState profile:

```json
{
  "BladeState": {
    "Profile": {
      "AutoEncrypt": true,
      "EncryptionKey": "your-crypto-key"
    }
  }
}
```

Then load it inside `Program.cs`:

```csharp
// --- BladeState ---
BladeStateProfile profile = builder.Configuration
    .GetSection("BladeState:Profile")
    .Get<BladeStateProfile>();
```

---

## 4. Register the EF Core Provider

Now wire up the EF Core provider for your state type.

```csharp
// Register EfCoreBladeStateProvider with your types
builder.Services.AddEfCoreBladeState<MyState>(profile);
```
---

## ✅ Summary

- `DbContext` must include `DbSet<BladeStateEntity>`.
- `AddDbContext` should be **Scoped**.
- Use `AddEfCoreBladeState<TState, TEntity, TDbContext>(profile)` for wiring.

---

## ⚖️ Provider Comparison

| Provider         | Best For                                     | Pros                                       | Cons                                                  |
| ---------------- | -------------------------------------------- | ------------------------------------------ | ----------------------------------------------------- |
| **Memory Cache** | Performance and application level processing | Simple, next to no overhead, fast          | Requires custom handling for persistence if necessary |
| **SQL**          | Simple persistence in relational DB          | Works out of the box, JSON storage         | Tied to SQL dialect, less efficient than Redis        |
| **Redis**        | High-performance distributed cache           | Fast, scalable, great for web farms        | Requires Redis infrastructure, persistence optional   |
| **EF Core**      | Strongly-typed relational models             | Uses your existing EF models, schema-first | More overhead, requires migrations                    |

---

## 🧩 Simple Service Collection Wire-up

-- this syntax is included primarily to extend BladeState with your own providers

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

## 💿 Drive BladeState with BladeStateProfile!

```csharp
var profile = new BladeStateProfile
{
    InstanceId = string.Empty,
    InstanceName = "MyApplicationState",
    InstanceTimeout = TimeSpan.FromMinutes(120),
    SaveOnInstanceTimeout = true,
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key"
}
```

## ⚙️ Example: Binding Profile from appsettings.json

You can configure profiles from appsettings.json and register them directly with a couple simple steps:

1. Add the following structure to your appsettings.json file

```json
{
  "BladeState": {
    "Profile": {
      "InstanceId": "MyApplicationState",
      "EncryptionKey": "my-crypto-key"
    }
  }
}
```

2. Get the section and pass the BladeStateProfile to the 'AddBladeState();' extension method in your Program.cs

```csharp
using BladeState;
using BladeState.Models;
using BladeState.Providers;

var profile = builder.Configuration.GetSection("BladeState:Profile").Get<BladeStateProfile>();

builder.Services.AddBladeState<MyAppState, SqlBladeStateProvider<MyAppState>>(profile);
```

---

## ❔🪽 Built-in Encryption

BladeState automatically encrypts persisted state data using AES encryption.

Enabled by default – you don’t need to do anything.

Encryption key – if not provided, BladeState will generate one automatically, simplifying encryption and decryption without explicit wire-up.  
You may also supply your own key by configuring a `BladeStateProfile`, example below:

```csharp
var profile = new BladeStateProfile
{
    AutoEncrypt = true,              // enable encryption
    EncryptionKey = "my-crypto-key"  // optional custom key
};

builder.Services.AddBladeState<MyAppState, SqlBladeStateProvider<MyAppState>>(profile);
```

Optionally (and NOT to be used for Production Environments - the universe frowns heavily upon that action 😔):
You may turn off encryption – you can explicitly disable it via **AutoEncrypt** in your profile:

This is not necessary even when wiring up your own BladeStateProvider.
The Decrypt/Encrypt State methods should be explicitly used.

```csharp
var profile = new BladeStateProfile
{
    AutoEncrypt = false  // disables automatic crypto transforms in the 'out of the box' providers
};

builder.Services.AddBladeState<MyAppState, RedisBladeStateProvider<MyAppState>>(profile);
```

## ❗Built-In Events

When a provider method is called an event will be raised to be handled by consuming components and services.
This is useful for reliable UI updates.

```csharp

// inject the provider
[Inject]
required public MemoryCacheBladeStateProvider<MyState> Provider { get; set; }

// add an event handler (anonymously)
Provider.OnStateChange += (sender, args) =>
{
    // get updated state if need be
    var state = args.State;

    // do something
    Console.WriteLine($"State changed: {args.EventType} for {args.InstanceId}! There are now {state.Items.Count} items!");
};

// add a custom handler
Provider.OnStateChange += MyCustomEventHandler;
```

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
