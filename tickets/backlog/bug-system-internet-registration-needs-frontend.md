description: Installing the library that provides internet features fails unless the user interface library happens to have been installed first, because its setup script quietly uses something that library provides.
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: libraries/System.Internet/System.Internet.d4l, libraries/System.Internet/Documents/Register.d4
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: Nobody has reported it in fifteen years, every deployment that matters registers the Frontend library anyway, and the obvious fix adds a library dependency that makes the internet library unusable on a server that deliberately has no user interface layer.
----
## What happens

Registering the `System.Internet` library on a server where the `Frontend` library is not yet registered fails:

```
Exceptions occurred while attempting to run the registration script for library "System.Internet".
  -> Unable to resolve operator reference "ExecuteScript" with signature "(System.String, System.String)".
```

Verified on 2026-11-21 on a fresh in-process server with only `System.Internet`, `Connection` and `SQLDevice` copied in. Registering `Frontend` first makes it succeed.

## Why

`libraries/System.Internet/Documents/Register.d4` ends (line 281) with

```
ExecuteScript("System.Internet", "Dynamic");
```

`ExecuteScript(Name, Name)` is an operator of the **Frontend** library (`libraries/Frontend/Documents/Register.d4`), not of `System`. But `libraries/System.Internet/System.Internet.d4l` declares only one library reference, to `System`. So the library's declared dependencies do not include the library whose operator its own registration script calls, and registration order decides whether it works.

## Options, for whoever picks this up

1. Add a `Frontend` library reference to `System.Internet.d4l`. Smallest change, and it makes the dependency honest — but it means a server cannot have the internet library without the Frontend library, which is a real narrowing for a headless deployment.
2. Inline the `Dynamic` script's contents at the end of `Register.d4` and delete the `ExecuteScript` call, leaving the declared dependency as it is. More text moved, no new dependency.
3. Add a `System`-library equivalent of `ExecuteScript` and use that. Largest change, and it adds a new operator to a compatibility surface.

Option 2 looks best on the face of it — it keeps the library standalone — but check first what `Documents/Dynamic.d4` actually does and whether anything else runs it.

Whichever is chosen, it changes what an application sees when it registers this library, so it needs an entry in `docs/migration-notes.md` in the same commit.

## How it was found

`test-frontend-document-corpus` (implement) needs `System.Internet` registered for its fixture, and hit this when it registered the libraries in the order its list happened to be written. That fixture works around it by registering `Frontend` first, with a comment naming this slug; the workaround can go when this lands.
