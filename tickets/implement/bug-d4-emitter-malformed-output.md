description: The D4 text emitter writes text the D4 parser cannot read for eight documents in the repository, so anything that turns a parsed script back into text — including the server when it stores or scripts a catalog object — can produce a script that no longer runs.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, src/engine/DAE/Language/Emitter.cs, src/engine/DAE/Language.D4/Parser.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md, tickets/plan/bug-d4-emitter-reshapes-parse-tree.md
repro: verified
----
## What this is

`D4TextEmitter` turns a parsed D4 statement back into D4 text. The server uses it in the places that matter: `ServerCatalogDeviceSession.ScriptObject`/`ScriptCatalog` emit a catalog object's definition through it before storing it, and `Engine.ScriptCatalog`/`ScriptDropCatalog` produce the rebuild scripts through it. For eight `.d4` documents in the repository the text that comes out does not parse, so those paths produce a script nobody can run back.

`ParserRoundTripCorpusTest` (`tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs`) quarantines those eight files in its `AllowList` under the category `emitter-malformed`. This ticket fixes the emitter at the sites that produce the bad text.

Seven distinct defects were isolated, each reduced to a one-line D4 snippet. Every snippet below was run against the current emitter (malformed) and against a prototype of the fixes (parses, and the re-parsed tree is equal to the first under the fixture's own `TreesEqual`).

| # | Site | What goes wrong | Snippet | Emitted today |
|---|---|---|---|---|
| A | `D4Emitter.cs:1787` `EmitForEachExpression` | writes `do` with no separator after it, so the keyword runs into the body; the statement form at `:1768` is fine because it starts a new line | `var x := ToTable(foreach var r in Y do row { r Value });` | `… in Y dorow { r Value }` |
| B | `D4Emitter.cs:4785` `EmitScalarTypeSpecifier` | writes `generic scalar` for the generic scalar type; the parser only accepts the bare keyword `scalar`, and on reading `generic` it takes the "any type" branch and leaves `scalar` unconsumed | `create operator Get(const AKey : scalar) : Integer class "X.Y";` | `const AKey : generic scalar` |
| C | `Emitter.cs:185` `EmitValueExpression`, `TokenType.Hex` | the case formats the value and then throws the string away — nothing is appended, so a hex literal vanishes from the output | `var a : Integer := 0x1F;` | `var a : Integer := ;` |
| D | `Emitter.cs:183` `EmitValueExpression`, `TokenType.Money` | writes the `$` before the sign; the lexer reads `$` as the start of a number and rejects the `-` that follows | `var a : Money := -$10.50;` | `$-10.50` |
| E | `D4Emitter.cs:4491` `EmitSecuritySpecifier` | appends an `i` suffix to the user and group name. That is case-insensitive-string literal syntax from the `USEISTRING` build, which is off and whose lexer support does not exist; the parser reads a plain string here | `grant all on T to user 'U';` | `to user "U"i` |
| F | `D4Emitter.cs:329` `EmitQualifierExpression` | a qualified column extractor writes the qualifier outside the extractor's parentheses, so a `(` lands where the parser wants an identifier. `C.Name from X` parses as a qualifier whose right side *is* the extractor (`Parser.cs:1440` `QualifierExpression`, `from` branch), so the qualifier has to come out attached to the column name | `var x := (C.Name from row from (V where C.ID = '1'));` | `C.(Name from (row from (…)))` |
| G | `D4Emitter.cs:329` `EmitQualifierExpression` | a numeric literal on the left of a `.` is written bare, and `1.` lexes as a malformed number | `var x := (1).Max(50);` | `1.Max(50)` |

Defect A clears two files for two reasons that look different but share the one missing separator: a row selector as the body (`FHIR.LoadConformanceCache`) and a call as the body (`ModelInfo.Operators`). Those seven causes are the whole set, and together they clear all eight quarantined files.

### Why this is worth fixing rather than tolerating

B and E are not confined to the eight test documents. Any operator with a `scalar` parameter — `System.Internet`'s `Get` and `Set`, and the system operators that take the generic scalar type — and any `grant`, `revoke` or `revert` on a user or group, produces unparseable text the moment the server scripts or stores it. The eight corpus files are how the defect was found, not its extent.

## Scope

Fix the emitter. Do not change the parser or the lexer: the compatibility rule keeps D4 semantics and the accepted grammar as they are, and in every one of these cases the parser is right and the emitter is wrong.

C and D are in the shared base `BasicTextEmitter.EmitValueExpression` (`src/engine/DAE/Language/Emitter.cs`), which `SQLTextEmitter` also inherits. Fixing them there is safe: `SQLTextEmitter` overrides the `Nil` and `Decimal` cases itself, and nothing in the SQL device path ever builds a `ValueExpression` with `TokenType.Hex` or `TokenType.Money` — `SQLDevice.cs:868` builds literals through the single-argument `ValueExpression` constructor, which only ever assigns `Decimal`, `Integer`, `Float`, `Boolean` or `String`. `TokenType.Hex` appears nowhere outside `Compiler.cs`, `Lexer.cs`, `Parser.cs` and this emitter. So put C and D in the base class, where the defect is, not in a D4-only override.

Out of scope: the tree-reshape class of emitter defect, which `tickets/plan/bug-d4-emitter-reshapes-parse-tree.md` owns. Two of the eight files land in that class once these fixes go in — see *AllowList changes*.

## Proposed implementation

This is the prototype that was measured, written as overrides; fold each piece into the method it replaces, matching the surrounding style (tabs, CRLF, `AppendFormat` with `Keywords.*` rather than literal punctuation).

`src/engine/DAE/Language/Emitter.cs`, in `EmitValueExpression`:

```csharp
case TokenType.Money:
    {
        // The sign goes before the "$": the lexer reads "$" as the start of a number and rejects a "-" after it.
        decimal money = (decimal)expression.Value;
        Append(String.Format(System.Globalization.CultureInfo.InvariantCulture, money < 0 ? "-${0}" : "${0}", money < 0 ? -money : money));
    }
    break;
case TokenType.Hex:
    Append("0x" + ((long)expression.Value).ToString("X"));
    break;
```

`D4Emitter.cs`, `EmitForEachExpression` — the one changed line:

```csharp
AppendFormat(" {0} ", Keywords.Do);
```

`D4Emitter.cs`, `EmitScalarTypeSpecifier`:

```csharp
if (statement.IsGeneric)
    Append(Keywords.Scalar);
else
    Append(statement.ScalarTypeName);
```

`D4Emitter.cs`, `EmitSecuritySpecifier` — both the `User` and the `Group` case, keeping the rest of the `Group` case (the `inherited`, `apply recursively` and `include users` clauses) as it is:

```csharp
AppendFormat("{0} '{1}'", Keywords.User, statement.Grantee.Replace("'", "''"));
```

`D4Emitter.cs`, `EmitQualifierExpression` — F and G:

```csharp
protected virtual void EmitQualifierExpression(QualifierExpression expression)
{
    // The parser builds "C.Name from X" as a qualifier whose right side is the column extractor, so the
    // qualifier has to be emitted as part of the extractor's column name: "(C.Name from X)". Emitting it
    // outside the extractor's parentheses - "C.(Name from X)" - puts a "(" where the parser wants an identifier.
    ColumnExtractorExpression extractor = TrailingColumnExtractor(expression);
    if (extractor != null)
    {
        Append(Keywords.BeginGroup);
        EmitQualifierPrefix(expression);
        EmitColumnExtractorBody(extractor);
        Append(Keywords.EndGroup);
        return;
    }

    EmitQualifierOperand(expression.LeftExpression);
    Append(Keywords.Qualifier);
    EmitExpression(expression.RightExpression);
}

// Walks the right spine; a qualifier's right side is itself a qualifier for each extra dot.
private static ColumnExtractorExpression TrailingColumnExtractor(QualifierExpression expression)
{
    Expression right = expression.RightExpression;
    while (right is QualifierExpression)
        right = ((QualifierExpression)right).RightExpression;
    ColumnExtractorExpression extractor = right as ColumnExtractorExpression;
    return (extractor != null) && (extractor.Columns.Count == 1) ? extractor : null;
}

private void EmitQualifierPrefix(QualifierExpression expression)
{
    EmitQualifierOperand(expression.LeftExpression);
    Append(Keywords.Qualifier);
    if (expression.RightExpression is QualifierExpression)
        EmitQualifierPrefix((QualifierExpression)expression.RightExpression);
}

// A numeric literal left of a "." has to be parenthesized: "1." lexes as a malformed number.
private void EmitQualifierOperand(Expression expression)
{
    ValueExpression value = expression as ValueExpression;
    bool needsGroup =
        (value != null)
            && ((value.Token == TokenType.Integer) || (value.Token == TokenType.Hex) || (value.Token == TokenType.Float)
                || (value.Token == TokenType.Decimal) || (value.Token == TokenType.Money));
    if (needsGroup)
        Append(Keywords.BeginGroup);
    EmitExpression(expression);
    if (needsGroup)
        Append(Keywords.EndGroup);
}
```

`EmitColumnExtractorBody` is the inside of today's `EmitColumnExtractorExpression` (`D4Emitter.cs:788`) — the column list, `from`, the expression and the optional `order by` clause, without the surrounding parentheses. Split it out and have `EmitColumnExtractorExpression` emit `(`, call it, and emit `)`, so the `order by` emission is written once. Keep `EmitColumnExtractorExpression(ColumnExtractorExpression)` as the `protected virtual` entry point it is today; a subclass may be overriding it.

## AllowList changes in `ParserRoundTripCorpusTest.cs`

Measured with the prototype over all 387 `.d4` files in the repository (the same walk and the same `TreesEqual` the fixture uses). Before: 290 pass, 51 reshape, 8 malformed, 38 unparseable. After: 296 pass, 53 reshape, 0 malformed, 38 unparseable. No file that passes today regressed.

Delete these six entries — each file passes outright:

- `legacy/libraries/FHIR.Server/Documents/FHIR.LoadConformanceCache.d4`
- `legacy/libraries/FHIR.Server/Documents/ModelInfo.Operators.d4`
- `libraries/System.Internet/Documents/Dynamic.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/Lexer.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SecurityLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/ViewUpdatability.d4`

Two of those are under `legacy/`, which is not to be fixed — but nothing in them is being fixed. The emitter is, and the corpus walk reads them, so their entries go.

Change these two from `emitter-malformed` to `emitter-reshape` and move them into that block of the list, keeping it sorted as it is now. Their malformed output is gone; what remains is the multi-variable-declaration flattening that the reshape ticket already names as its first sub-pattern (`var a : X, b : Y` parses as a nested `Block` and re-parses as two siblings):

- `tests/libraries/TestFramework.Coverage.Scripts/Documents/IntegerLibrary.d4`
- `tests/libraries/TestFramework.Coverage.Scripts/Documents/SerializationOperators.d4`

Then update the comment block above the `AllowList`: the `(emitter-malformed)` category is gone, so drop its bullet and reword the "two classes of defect and one class of file" sentence to match what is left.

## Reproduction test

The six files above are the integration-level confirmation, but two of the causes keep no corpus coverage once the dust settles: G (`(1).Max(50)`) is only exercised by `IntegerLibrary.d4`, which stays quarantined for its reshape. So the reproduction goes in a focused fixture at the lowest layer that shows the defect — parser and emitter, no server.

Add `tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs` with one case per cause. Parse the snippet, emit it, parse the emitted text asserting no `ParserMessages` entries, then assert `ParserRoundTripCorpusTest.TreesEqual` (it is `internal static`, so it is reachable from the same assembly — reuse it rather than writing a second comparator). The nine snippets, each verified to fail today and pass with the prototype:

```
var x := ToTable(foreach var r in Y do row { r Value });            -- A
create operator Get(const AKey : scalar) : Integer class "X.Y";     -- B
var a : Integer := 0x1F;                                            -- C
var a : Integer := -0x1;                                            -- C, through a negate the parser does not fold
var a : Money := -$10.50;                                           -- D
grant all on T to user 'U';                                         -- E
revoke all on T from group 'G' inherited;                           -- E, group arm with a trailing clause
var x := (C.Name from row from (V where C.ID = '1'));               -- F
var x := (1).Max(50);                                               -- G
```

Add the file to the `<Compile Include=…>` list in `tests/DAE.Server.Tests/DAE.Server.Tests.csproj` — that project lists its sources explicitly.

## Also in this commit

**A migration-notes entry.** `D4TextEmitter` output changes, and the server stores and scripts catalog DDL through it, so an extension or a deployment script that reads emitted text sees the difference. Add one row to the `## D4, catalog and libraries` table of `docs/migration-notes.md`, status `done`: emitted D4 now re-parses where it did not — the generic scalar type is written `scalar` rather than `generic scalar`, a grantee is written as a plain string rather than with a trailing `i`, hexadecimal literals are written rather than dropped, a negative money literal is written `-$1` rather than `$-1`, a qualified column extractor is written `(C.Name from X)` rather than `C.(Name from X)`, and a numeric literal before a `.` is parenthesized. What to do: catalog scripts produced by an earlier 4.0 preview may need regenerating; code that pattern-matches emitted D4 text should be checked against the new spellings.

**A `NOTE:` at `Parser.cs:5458` (`ScalarTypeSpecifier`).** `docs/reference/d4-grammar.md:770` gives the production as `[generic] scalar`, but this parser accepts only the bare keyword: `TypeSpecifier` (`Parser.cs:5414`) has no `Keywords.Scalar` branch under `generic`, its `case Keywords.Type` branch is unreachable, and `ScalarTypeSpecifier`'s own `isGeneric` parameter is never read. Nothing in the repository writes `generic scalar`, and this ticket makes the emitter agree with the parser rather than the document. Record the divergence at the site so the next reader meets it instead of rediscovering it; the fix, if one is ever wanted, belongs to `bug-d4-parser-rejects-active-sources`.

**Board bookkeeping.** `tickets/plan/bug-d4-emitter-reshapes-parse-tree.md` says "fifty-one" in its `description:` and its body; with the two recategorized files it is fifty-three. Update both, and add the two file names to its multi-variable sub-pattern bullet. Skip this if that ticket is no longer on the board.

## Source hygiene

`D4Emitter.cs`, `Emitter.cs` and `Parser.cs` are tabs, CRLF, no byte-order mark. `ParserRoundTripCorpusTest.cs` is tabs but **LF** — do not convert it. `DAE.Server.Tests.csproj` is spaces, CRLF, with a byte-order mark. `docs/migration-notes.md` is CRLF, and one paragraph per line with no hard wrapping.

## Verification

```
dotnet build Dataphor.slnx
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~ParserRoundTripCorpusTest"
dotnet test tests/DAE.Server.Tests/DAE.Server.Tests.csproj --no-build --filter "FullyQualifiedName~D4EmitterRoundTripTest"
```

Then the full suite in the three parts the root `AGENTS.md` gives, each as its own foreground command. The emitter change reaches the catalog store (DDL is stored as emitted text), so the engine tests and the coverage scripts both need to run; `coverage-manifest.json` should need no change, and if a `known-failure` entry now passes, that is a manifest update to make in the same commit with a line in the handoff saying which.

## TODO

- Fix `TokenType.Money` and `TokenType.Hex` in `BasicTextEmitter.EmitValueExpression` (`src/engine/DAE/Language/Emitter.cs:183`, `:185`).
- Add the separator after `do` in `D4TextEmitter.EmitForEachExpression`.
- Emit the bare `scalar` keyword in `D4TextEmitter.EmitScalarTypeSpecifier`.
- Drop the `i` suffix and quote the grantee as a plain string in `D4TextEmitter.EmitSecuritySpecifier`, for both the user and the group arm.
- Split the body of `D4TextEmitter.EmitColumnExtractorExpression` into a parenthesis-free helper, keeping the existing virtual method as the entry point.
- Handle the qualified column extractor and the numeric-literal left operand in `D4TextEmitter.EmitQualifierExpression`.
- Add `tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs` with the nine snippets, reusing `ParserRoundTripCorpusTest.TreesEqual`, and list it in `DAE.Server.Tests.csproj`.
- Delete the six `AllowList` entries, recategorize the two, and update the comment block above the list.
- Add the `NOTE:` at `Parser.cs` `ScalarTypeSpecifier` about the grammar-document divergence.
- Add the migration-notes row.
- Update the file and count in `tickets/plan/bug-d4-emitter-reshapes-parse-tree.md`.
- Build, run the two filtered test runs, then the full suite in three parts.
