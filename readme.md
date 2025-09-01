# BladeState

[![NuGet Version](https://img.shields.io/nuget/v/BladeState.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BladeState.svg?style=flat&logo=nuget)](https://www.nuget.org/packages/BladeState/)
[![License](https://img.shields.io/github/license/doomfaller/BladeState.svg?style=flat)](LICENSE)

**BladeState** is a lightweight server-side state/session persistence library for Razor and Blazor applications.  
It provides **dependency-injected storage** for persisting state across requests without relying on `HttpContext.Session`.

---

## ✨ Features
- 🗂 Server-side storage abstraction
- ⚡ Easy integration with **Dependency Injection**
- 🔄 Works across Razor & Blazor server applications
- 🛡 Avoids reliance on `HttpContext.Session`
- 🔧 Extensible design for custom providers (e.g. Redis, SQL, InMemory)

---

## 🚀 Installation

Install via NuGet:

```bash
dotnet add package BladeState
