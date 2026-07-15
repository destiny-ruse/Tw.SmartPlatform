# Tw.AspNetCore.TestBase

`Tw.AspNetCore.TestBase` provides ASP.NET Core test-only helpers. Production projects must not reference this package.

## Stability

The package is currently `experimental`. Promotion to `stable` requires integration tests that prove authenticated and anonymous request isolation, host override behavior, deterministic disposal, and compatibility with the supported `Microsoft.AspNetCore.Mvc.Testing` baseline.
