description: The JSON-handling library was upgraded and the new version refuses to read data nested more than sixty-four levels deep, where the old one had no limit; write a test that records exactly where that edge is and what crossing it looks like, so a customer whose data is deeper is not the one who finds out.
architecture: docs/modernization-plan.md#ws3-test-suite
files: tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4, tests/DAE.Server.Tests/coverage-manifest.json, libraries/System.Internet/Source/DynamicNodes.cs, libraries/System.Internet/Documents/Dynamic.d4, docs/migration-notes.md
difficulty: easy
----
## What this is about

Dataphor ships a library, `System.Internet`, that lets D4 code read JSON. The component it reads JSON with was upgraded from Newtonsoft.Json 9 to 13 as part of the move to .NET 10. Version 13 lowered a default as a security measure: reading JSON now fails once the data is nested more than sixty-four levels deep, where version 9 had no limit. Nothing in the test suite reads JSON, so the change is unexercised, and a document that used to be readable and now is not would surface only as a customer report.

The deliverable is one block of assertions in an existing D4 coverage script that pins where the edge is and what crossing it looks like, plus one line in the migration notes so that an application author can read the limit without reading the test. Nothing about the limit itself changes: version 13 lowered it deliberately, and a server reading JSON it did not produce wants it.

The planning pass measured the behaviour rather than guessing it, and the block below has been run green against the suite. So this is placement and documentation, not investigation.

## What was measured

All of this was measured on 2026-11-22 against Newtonsoft.Json 13.0.4 (the version `Directory.Packages.props` pins), partly by running the two reader calls directly and partly by running D4 through the `Internet` coverage case.

- The limit counts **containers, not characters**: a value nested in 64 objects or arrays reads; 65 is refused. `[1]` counts as one level.
- Both reader entry points carry the same limit. `JsonTextReader.MaxDepth` and `JsonSerializerSettings.MaxDepth` both default to 64 in 13.0.4, and nothing in Dataphor sets either.
- Crossing it raises `Newtonsoft.Json.JsonReaderException`, and that reaches D4 as an ordinary catchable Dataphor error. Its message, as a D4 `except on E : Error do` handler sees it, is:

  ```
  Runtime error: The reader's MaxDepth of 64 has been exceeded. Path 'a.a.a. … .a', line 1, position 321.
  ```

  The `Path` and `position` parts depend on the document's shape (an array of the same depth gives `'[0][0]…'` and `position 65`), so an assertion may use the leading sentence and must not use the path or the position.
- **Reading and writing are not symmetric, and that is the finding worth recording.** The `Set` operator nests one `Dynamic` value inside another without counting levels, and the `Content` read accessor serializes whatever it is given with no limit either. So D4 can build a 71-level value in memory, serialize it to text, and then be unable to read that same text back. Measured: 71 levels built by repeated `Set`, `Content` returned 427 characters, and `Dynamic(<those 427 characters>)` failed with the message above.
- A 71-level value **does** survive an in-process round trip through a session table (inserted and read back, 427 characters again). The memory device keeps the value as it is and never takes a physical representation of it, so no JSON reader runs. Do not write an assertion that expects a stored deep value to fail.

## Where the JSON readers are

In the whole live tree, exactly two files read JSON (`grep` for `JsonConvert.DeserializeObject`, `JToken.ReadFrom`, `JToken.Parse`, `JsonTextReader`, `JObject.Parse` across `src`, `libraries`, `samples`, `tests` and `tools` hits only these two):

- `libraries/System.Internet/Source/DynamicNodes.cs` — three reachable reader sites, all `JsonConvert.DeserializeObject`:
  - `DynamicSelectorNode` (line 68), the operator `Dynamic(const AContent : String)`;
  - `DynamicContentWriteAccessorNode` (line 96), the `Content` representation write accessor, reached from D4 as `LValue.Content := '…'`;
  - `DynamicContentSelectorNode` (line 54), the `Content` representation selector. Its signature is the same as the explicit `Dynamic(String)` operator declared in `Dynamic.d4`, so which of the two a `Dynamic('…')` call resolves to is not observable from D4 and the two do the same thing. One assertion covers whichever wins; do not try to reach both.
  - The fourth `Newtonsoft` call in that file, `JsonConvert.SerializeObject` in `DynamicContentReadAccessorNode` (line 82), is the write side and has no depth limit. The asymmetry assertion below is what covers it.
- `libraries/System.Internet/Source/JSONObjectConveyor.cs` — **unreachable, and not this ticket's business.** It implements only the streaming `Read(Stream)`/`Write(object, Stream)` pair but leaves `IsStreaming` returning the base class's `false`, and every call site in the engine that picks a conveyor method branches on `IsStreaming`. So its JSON reader is never called; the branch that *is* taken calls `GetSize`, which the base class throws `NotSupportedException` from. That is a separate latent defect, filed as `bug-json-conveyor-declares-itself-non-streaming` in `backlog/`. Write no conveyor case here.

Two paths the original concern named turned out not to read JSON at all, which is why this is a library test and not a client/server one: `src/engine/Server/REST/RESTMarshal.cs` has no reader and its only callers are in the parked tree, and `src/protocol/NativeCLI/NativeCLIHelper.cs` takes a `JObject` some caller has already built. The network protocol carries no JSON.

## Where the test goes

`tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`, which already exists, already carries a `// TODO: coverage for the System.Internet library` comment, and already has a manifest entry that registers the library:

```json
{ "script": "Internet", "status": "pass", "register": ["System.Internet"] }
```

So: append a block to that script and change nothing else about the suite. No new script, no manifest change, no new fixture. The coverage template registers `Frontend` before any script runs, which is what `System.Internet` registration needs today (`bug-system-internet-registration-needs-frontend`), so the case works as it stands; the new assertions must not depend on any other library being registered.

The case ran in 4 s with this block in it, against 4 s without — the `while` loops that build the strings cost nothing measurable, so this adds no meaningful time to suite part 1.

## The block to append

This text was appended to the script, built into the runtime layout and run (`dotnet test tests/DAE.Server.Tests --no-build --filter Name=Internet`), and the case reported `pass: 1 passed, 0 failed`. Place it as it is, or improve it, but re-run the case either way.

```
// JSON nesting depth. Newtonsoft.Json 13 refuses to read JSON nested more than 64 levels deep;
// version 9, which 3.1 shipped, had no limit. Measured against 13.0.4; see docs/migration-notes.md.
begin
	var LMaxDepthError : String := 'MaxDepth of 64 has been exceeded';
	var LOpen : String := '';
	var LClose : String := '';
	var LIndex : Integer := 0;
	while LIndex < 64 do
	begin
		LOpen := LOpen + '{"a":';
		LClose := LClose + '}';
		LIndex := LIndex + 1;
	end;
	var LAt64 : String := LOpen + '1' + LClose;
	var LAt65 : String := LOpen + '{"a":1}' + LClose;

	// 64 levels read
	if GetType(Dynamic(LAt64)) <> 'Object' then
		raise Error('Dynamic did not read 64 levels of nesting');

	// 65 levels are refused by the string selector
	var LCaught : String := '';
	try
		var LTooDeep : Dynamic := Dynamic(LAt65);
	except on E : Error do
		LCaught := E.Message;
	end;
	if IndexOf(LCaught, LMaxDepthError) < 0 then
		raise Error('Dynamic read 65 levels of nesting, or failed with an unexpected error: ' + LCaught);

	// and by the Content write accessor, the other reader D4 can reach
	LCaught := '';
	try
		var LValue : Dynamic := Dynamic('{"a":1}');
		LValue.Content := LAt65;
	except on E : Error do
		LCaught := E.Message;
	end;
	if IndexOf(LCaught, LMaxDepthError) < 0 then
		raise Error('The Content write accessor read 65 levels of nesting, or failed with an unexpected error: ' + LCaught);

	// Writing has no limit, so a value the engine builds itself can be deeper than it can read back.
	var LDeep : Dynamic := Dynamic('{"a":1}');
	LIndex := 0;
	while LIndex < 70 do
	begin
		LDeep := Set(Dynamic('{}'), 'a', LDeep);
		LIndex := LIndex + 1;
	end;
	var LDeepContent : String := LDeep.Content;
	LCaught := '';
	try
		var LRead : Dynamic := Dynamic(LDeepContent);
	except on E : Error do
		LCaught := E.Message;
	end;
	if IndexOf(LCaught, LMaxDepthError) < 0 then
		raise Error('A 71 level value serialized and read back, or failed with an unexpected error: ' + LCaught);
end;
```

Why the assertions are shaped that way:

- `IndexOf(AString, ASubString)` returns `-1` when the substring is absent (`StringUtility.IndexOf` in `src/engine/DAE/Runtime.Instructions/StringNodes.cs`), and `LCaught` is the empty string when nothing was raised. So one `IndexOf(…) < 0` test catches both "it did not fail" and "it failed for some other reason", and the message it raises carries whatever error did arrive.
- The expected substring is `MaxDepth of 64 has been exceeded` and not the whole message: it carries both the fact that this is a depth refusal and the number, and leaves out the `Path` and `position`, which vary with the document's shape. This is deliberately tied to the library's wording. That is the point — if a later upgrade moves the limit or rewords the message, this assertion is what says so, and `debt-library-upgrades-coverage` is the ticket that would meet it. The comment above the block names the version measured so that whoever meets the failure knows what to re-measure.
- The depths are 64 and 65 rather than "shallow" and "very deep", so the assertion pins the edge rather than the neighbourhood.

## Edge cases & interactions

| Case | How it is covered |
|---|---|
| The exact boundary: 64 reads, 65 does not | Both sides asserted in the block. |
| Nesting shape: objects versus arrays | The limit is identical for both (measured); only the message's `Path` and `position` differ, which is why the assertion uses neither. Covered by inspection — one shape in the test is enough, and the `Dynamic` type is declared `from class "Newtonsoft.Json.Linq.JObject"`, so nested objects are the shape a typed `Dynamic` variable certainly holds. Do not add an array case on a typed variable without first checking that a `JArray` can be assigned to one; that was not measured. |
| Both reachable reader sites | One assertion each: the string selector and the `Content` write accessor. The representation selector is behaviourally identical and is not reachable separately. |
| Write-then-read asymmetry | The `Set` / `Content` / re-read assertion. This is the half a reader of the test is most likely to be surprised by, so keep the comment that says why it is there. |
| A deep value stored in a table | Not asserted, deliberately: measured to succeed in process, because the memory device never takes a physical representation. An assertion either way here would be recording the memory device's storage strategy, not the JSON limit. |
| The conveyor's JSON reader | Not covered: unreachable, owned by `bug-json-conveyor-declares-itself-non-streaming`. Named in the comment block or the handoff so the next reader does not go looking for it. |
| The script's manifest entry | Unchanged (`pass`, `register: ["System.Internet"]`). Verify by running the one case; the suite fails a script whose entry does not match. |
| Registration order | `System.Internet` registration calls a `Frontend` operator (`bug-system-internet-registration-needs-frontend`); the coverage template registers `Frontend` first, so nothing is needed here. By inspection. |
| Suite time | Measured: the `Internet` case is 4 s with the block and 4 s without. |
| Source hygiene | `Internet.d4` is tabs, CRLF, no byte-order mark. Append in that form. |

## TODO

- Append the block above to `tests/libraries/TestFramework.Coverage.Scripts/Documents/Internet.d4`, keeping the file's tabs, CRLF line endings and absence of a byte-order mark.
- Build (`dotnet build Dataphor.slnx`, or `dotnet build tools/layout/Layout.csproj` if nothing else changed — the script has to reach the runtime layout before the test can run it) and run the one case: `dotnet test tests/DAE.Server.Tests --no-build --filter Name=Internet --logger "console;verbosity=normal"`. Expect `pass: 1 passed, 0 failed`.
- Add a row to the `## D4, catalog and libraries` table of `docs/migration-notes.md` (the existing Newtonsoft row is in `## Custom C# extensions` and is about building extensions, which is a different reader). Proposed text, to edit as you see fit:

  ```
  | done | Reading JSON through the `System.Internet` library fails if the document is nested more than 64 levels deep: the `Dynamic(String)` operator and the `Content` write accessor both raise "The reader's MaxDepth of 64 has been exceeded". Newtonsoft.Json 13 sets that limit by default; version 9, which 3.1 shipped, had none. Writing is not limited, so `Set` and the `Content` read accessor can still build and serialize a value deeper than 64 levels, which then cannot be read back. | Nothing, unless an application reads JSON nested deeper than 64 levels, or builds a value that deep with `Set`; there is no setting for the limit today, so such an application needs its documents or its nesting flattened. Report it if you hit this - the limit can be raised at the reader, and a report is what would justify doing so |
  ```

- Run the rest of suite part 1 once (`dotnet test Dataphor.slnx --no-build -- "NUnit.Where=class != Alphora.Dataphor.DAE.Server.Tests.CoverageScripts"`) only if you changed anything outside the script and the notes; the coverage parts 2 and 3 are not affected by a one-script change, and part 3 is the one that holds `Internet`, so re-running the single case covers it.
- In the handoff, say whether the limit now has a ticket behind it or not: this ticket deliberately does not propose raising it, and the migration note asks for a report rather than promising a setting.
