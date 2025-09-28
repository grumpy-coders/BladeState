# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [2.0.0] - 2025-09-28
### Added
- This change log file.
- Licensing support.
- File Provider initial support ([#35](https://github.com/grumpy-coders/BladeState/issues/35)).
- `FileProviderOptions` added to `BladeStateProfile` to handle provider-specific configuration.
- `SqlProviderOptions` added to `BladeStateProfile` for provider-specific configuration.

### Changed
- Namespace updates (**breaking change**: now prefixed with `GrumpyCoders`).
- File and naming consistency
- Improved README clarity.
- The parameter for SqlType has been moved to Profile.SqlProviderOptions
- SqlType is no longer a required parameter and is defaulted to use ANSI syntax for agnostic support of data backends, this simplifies swaps with no code changes but may not be as performant (dependent on data engine)
- Use of .AddEfCoreBladeState() and .AddSqlBladeState() service collection extensions have been drastically simplified (see readme)

### Licensing
- License changes for Large Teams and Enterprise users.

---
