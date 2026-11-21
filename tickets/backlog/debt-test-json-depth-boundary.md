description: The JSON-handling library was upgraded and the new version refuses to read data nested more than sixty-four levels deep, where the old one had no limit; nothing in the tests ever reads JSON, so if that limit breaks a customer's data nobody would find out until they reported it.
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/System.Internet/Source/DynamicNodes.cs, libraries/System.Internet/Source/JSONObjectConveyor.cs, libraries/System.Internet/Documents/Dynamic.d4, tests/libraries/TestFramework.Coverage.Scripts/Documents, tests/DAE.Server.Tests
tradeoffs: It needs deeply nested JSON to matter at all, the library it concerns is one of the less-used shipped libraries, and a maintainer may prefer to wait for a report rather than spend a test on a limit that may never be reached in practice.
----
## What this is about

Dataphor ships a library, `System.Internet`, that lets D4 code fetch and read JSON. The component it uses for that was upgraded from version 9 to version 13 as part of the move to .NET 10 (recorded in `docs/migration-notes.md`). Version 13 changed a default as a security measure: reading JSON now fails once the data is nested more than sixty-four levels deep, where version 9 had no limit at all. Nothing in the test suite reads JSON, so the change is unexercised, and a document that used to be readable and now is not would surface only as a customer report.

What is wanted is one characterization test at the lowest layer that shows the behaviour: feed the library JSON nested far enough to cross the boundary and record what actually happens — the error, its text, and whether it reaches the caller as a D4 error or as something less useful. Then the behaviour is written down and a later decision to raise the limit, or to document it, has something to stand on.

## Where the JSON actually is, and where it is not

This was handed to the client/server test ticket by `build-server-project`, which wrote: *"Newtonsoft.Json 13 (was 9) changed defaults, such as `MaxDepth` 64 when reading. Neither `REST/RESTMarshal.cs` nor NativeCLI serialization is exercised by anything that runs today; noted in the WS5 umbrella for `test-client-server`."* Planning `test-client-server` established that both of those sites are the wrong place to look, so the concern is filed here instead of being bent into a network test:

- `src/engine/Server/REST/RESTMarshal.cs` — its only callers are in `legacy/Dataphoria/Dataphoria.Processing/Processor.cs`, which is parked and excluded from the build. It is unreachable, not merely unexercised.
- `src/protocol/NativeCLI/NativeCLIHelper.cs` — its JSON branch takes a `JObject` that some caller has already built; nothing in the live tree calls it. The network protocol serializes with a binary message encoder and a data contract, not with JSON, so there is no JSON on the wire at all.
- Neither of those **reads** JSON, and the sixty-four-level default applies to reading.

The reader that does exist is in the `System.Internet` library, reachable from D4:

- `libraries/System.Internet/Source/DynamicNodes.cs` calls `JsonConvert.DeserializeObject` in four places — the operators that turn a fetched response, or a string, into a value.
- `libraries/System.Internet/Source/JSONObjectConveyor.cs` reads with a `JsonTextReader`; it is the physical representation of the scalar type `Dynamic`, declared in `libraries/System.Internet/Documents/Dynamic.d4` as `create type Dynamic from class "Newtonsoft.Json.Linq.JObject"`. So a `Dynamic` value read back out of a device goes through a JSON reader too, which is the quieter half of the problem: a value that was stored successfully could fail to read.

So this is an in-process library test, not a client/server test: a D4 coverage script in `tests/libraries/TestFramework.Coverage.Scripts/Documents/` with its manifest entry, or a case in `tests/DAE.Server.Tests` against the registered library — whichever the person doing it judges cheaper. The project's testing rule prefers the lowest layer that shows the behaviour, and prefers a D4 script run through the in-process harness over anything above it.

## What a reader should know before starting

- Both halves are worth covering, and they may behave differently: the operator path (`JsonConvert.DeserializeObject`) and the stored-value path (`JSONObjectConveyor`) construct their readers separately, so a depth limit may be in force on one and not the other.
- `System.Internet`'s C# is converted and in `Dataphor.slnx`, and the library is in the runtime layout, so nothing is blocked.
- There is no suggestion here that the limit should be *changed*. Version 13 lowered it deliberately, and a server that reads attacker-supplied JSON wants it. The point is to know where the edge is and what crossing it looks like, so that if an application turns out to depend on deeper nesting the decision is informed rather than a surprise.
