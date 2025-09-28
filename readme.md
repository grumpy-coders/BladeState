# 🗡️ BladeState

## This project is being developed and provided 'AS IS'

DOING OUR BEST TO GET FEATURES AND FIXES (IF NEEDED) OUT ASAP

## I AM ACTIVELY UPDATING ON NUGET AND README, HMU FOR FEATURE REQUESTS -> [doomfaller@gmail.com](mailto:doomfaller@gmail.com)

THANKS FOR YOUR PATIENCE - YOUR FAVORITE SCHIZO

## What's on the road map?

* FileBladeStateProvider
* File State viewer app
* Performance and Enhancements with Change Event
* Enterprise and Large Team Licensing

## What's new in version 1.0.6?

* Performance and stability enhancements

[![NuGet Version](https://img.shields.io/nuget/v/BladeState.svg?style=flat\&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BladeState.svg?style=flat\&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![License](https://img.shields.io/github/license/doomfaller/BladeState.svg?style=flat)](LICENSE)

**BladeState** is a lightweight server-side dependency injection state persistence library for .NET applications.
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

---

### 1. Memory Cache Provider (`MemoryCacheBladeStateProvider<T>`) ⚡

Stores state in in-memory cache for the lifetime of the application.

```csharp
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState.Providers;

var profile = new BladeStateProfile
{
    InstanceName = "MyMemoryCacheApp",
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key"
};

builder.Services.AddMemoryCacheBladeState<MyState>(profile);
```

---

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
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState.Providers;

var profile = new BladeStateProfile
{
    InstanceName = "MySqlApp",
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key"
};

builder.Services.AddSqlBladeState<MyState>(
    () => new SqlConnection("Server=localhost;Database=BladeStateDb;User Id=yourUserId;Password=YourStrong(!)Password;TrustServerCertificate=True;"),
    profile
);
```

#### How it works

* Uses a simple key/value table (`InstanceId`, `Data`).
* JSON serialization handled automatically.
* Encryption honored from the `BladeStateProfile`.

---

### 3. Redis Provider (`RedisBladeStateProvider<T>`) 🔥

Stores state in Redis using `StackExchange.Redis`.

#### Registration

```csharp
using GrumpyCoders.BladeState.Providers;
using StackExchange.Redis;

var profile = new BladeStateProfile
{
    InstanceName = "MyAppUsingRedis",
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key"
};

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost")
);

builder.Services.AddRedisBladeState<MyState>(profile);
```

#### Notes

* Stores JSON under a Redis key formatted like `{InstanceName}:{InstanceId}`.
* Fast, distributed, great for scale-out.

---

### 4. EF Core Provider (`EfCoreBladeStateProvider<T>`) 🟢

Uses an Entity Framework `DbContext` to persist state directly in your model.

#### 1. Define your profile

```csharp
BladeStateProfile profile = builder.Configuration
    .GetSection("BladeState:Profile")
    .Get<BladeStateProfile>();
```

#### 2. Register the EF Core Provider

```csharp
using Microsoft.EntityFrameworkCore;
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState.Providers;

builder.Services.AddEfCoreBladeState<MyState>(
    options => options.UseSqlServer("Server=localhost;Database=BladeStateDb;User Id=yourUserId;Password=YourStrong(!)Password;TrustServerCertificate=True;"),
    profile
);
```

#### ✅ Summary

* `DbContext` must include `DbSet<BladeStateEntity>`.
* Uses `IDbContextFactory<BladeStateDbContext>` for safe, scoped creation.
* Profile and crypto are handled automatically as singletons.
* Encryption is optional but strongly recommended.

---

### 5. File System Provider (`FileSystemBladeStateProvider<T>`) 📁

Stores state as JSON files on the local or networked file system.
Great for simple persistence without a database or cache layer.

#### Registration

```csharp
using GrumpyCoders.BladeState.Providers;

builder.Services.AddFileSystemBladeState<MyState>(
    new BladeStateProfile
    {
        InstanceName = "MyAppFileSystem",
        InstanceId = "MyInstance",
        AutoEncrypt = true,
        EncryptionKey = "my-crypto-key",
        FileSystemProviderOptions = new()
        {
            BasePath = "C:\\BladeState" // default is system temp folder (tmp environment variable). This can be a network share
        }
    }
);
```

#### How it works

* Persists state as `.json` files under the configured `BasePath`.
* File names use the format `{InstanceName}\{InstanceId}.json`.
* Honors encryption settings via `BladeStateProfile`.

---

## ⚖️ Provider Comparison (updated)

| Provider         | Best For                                     | Pros                                                    | Cons                                                  |
| ---------------- | -------------------------------------------- | ------------------------------------------------------- | ----------------------------------------------------- |
| **Memory Cache** | Performance and application level processing | Simple, next to no overhead, fast                       | Requires custom handling for persistence if necessary |
| **SQL**          | Simple persistence in relational DB          | Works out of the box, JSON storage, encryption included | Tied to SQL dialect, less efficient than Redis        |
| **Redis**        | High-performance distributed cache           | Fast, scalable, great for web farms                     | Requires Redis infrastructure, persistence optional   |
| **EF Core**      | Strongly-typed relational models             | Uses your existing EF models, schema-first, crypto safe | More overhead, requires migrations                    |
| **File System**  | Lightweight persistence without DB/cache     | Easy to set up, portable, works offline, human-readable | File I/O overhead, not ideal for high concurrency     |

---

## 🧩 Simple Service Collection Wire-up

This syntax is included primarily to extend BladeState with your own providers.

```csharp
builder.Services.AddBladeState<MyState, SqlBladeStateProvider<MyState>>(profile);
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

## 💿 Drive BladeState with BladeStateProfile!

```csharp
var profile = new BladeStateProfile
{
    InstanceId = string.Empty,
    InstanceName = "MyApplicationState",
    InstanceTimeout = TimeSpan.FromMinutes(120),
    SaveOnInstanceTimeout = true,
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key",
    AutoClearOnDispose = true,
    FileSystemProviderOptions = new(),
    SqlProviderOptions = new()
};
```

---

## ⚙️ Example: Binding Profile from appsettings.json

1. Add the following structure to your **appsettings.json**:

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

2. Get the section and register BladeState in **Program.cs**:

```csharp
using GrumpyCoders.BladeState;
using GrumpyCoders.BladeState.Models;
using GrumpyCoders.BladeState.Providers;

var profile = builder.Configuration.GetSection("BladeState:Profile").Get<BladeStateProfile>();

builder.Services.AddBladeState<MyAppState, SqlBladeStateProvider<MyAppState>>(profile);
```

---

## ❔🪽 Built-in Encryption

BladeState automatically encrypts persisted state data using AES encryption.

* Enabled by default – you don’t need to do anything.
* Encryption key – if not provided, BladeState will generate one automatically.

```csharp
var profile = new BladeStateProfile
{
    AutoEncrypt = true,
    EncryptionKey = "my-crypto-key"
};

builder.Services.AddBladeState<MyAppState, SqlBladeStateProvider<MyAppState>>(profile);
```

If you want to disable encryption (not recommended):

```csharp
var profile = new BladeStateProfile
{
    AutoEncrypt = false
};

builder.Services.AddBladeState<MyAppState, RedisBladeStateProvider<MyAppState>>(profile);
```

---

## ❗Built-In Events

When a provider method is called, an event is raised for consuming components and services.
This enables reliable UI updates.

```csharp
[Inject]
required public MemoryCacheBladeStateProvider<MyState> Provider { get; set; }

// anonymous handler
Provider.OnStateChange += (sender, args) =>
{
    var state = args.State;
    Console.WriteLine($"State changed: {args.EventType} for {args.InstanceId}! Count: {state.Items.Count}");
};

// custom handler
Provider.OnStateChange += MyCustomEventHandler;
```

---

## 📝 License

For enterprise users and large teams licensing is required.

* Add license key to **appsettings.json**:

```json
{
  "BladeState": {
    "LicenseKey": "YOUR-LICENSE-KEY-HERE",
    "Profile": {
      "InstanceName": "MyApp",
      "AutoEncrypt": true,
      "EncryptionKey": "my-crypto-key"
    }
  }
}
```

* Register it in **Program.cs**:

```csharp
builder.Services.AddBladeStateLicense(configuration.GetSection("BladeState")["LicenseKey"]);
```

That’s it!

This project is licensed - see the [LICENSE](LICENSE) file for details.
Previously obtained versions (before v1.0.7 and before October 1st 2025) may continue to be used via MIT Licensing.