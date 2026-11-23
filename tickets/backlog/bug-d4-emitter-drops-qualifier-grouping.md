description: When the server writes stored D4 back out as text, it drops the brackets from an expression like (a.b).c, so the text it writes is a.(b.c) - a differently grouped expression. Both forms have meant the same thing everywhere it has been tried, but the written text no longer says what the server was asked to write.
architecture: docs/modernization-plan.md#ws3-test-suite
files: src/engine/DAE/Language.D4/D4Emitter.cs, tests/DAE.Server.Tests/D4EmitterRoundTripTest.cs, tests/DAE.Server.Tests/ParserRoundTripCorpusTest.cs
repro: verified
severity: edge-case
likelihood: unusual
tradeoffs: Nothing has been shown to behave differently because of it - the two groupings compiled and evaluated identically in the one case measured - so the whole cost of a fix is paid against a divergence that is so far only visible to the round-trip test; and a fix that adds brackets changes the text the server stores for every object whose definition contains the construct, which is a diff a maintainer has to read.
----
## What is wrong

`D4TextEmitter` prints a qualifier chain without the grouping the parse tree carries. The parser is right-associative over `.`, so `a.b.c` parses as `a.(b.c)`; a source that writes `(a.b).c` gets the left-associative tree instead. The emitter prints that tree as `a.b.c`, which reads back as the right-associative one. The printed text is therefore not a printing of the tree it was given.

The site is one method, `EmitQualifierOperand` (`src/engine/DAE/Language.D4/D4Emitter.cs`, around line 384). It parenthesises its operand for exactly one case today - a numeric literal, because `1.` lexes as a malformed number - and a qualifier operand needs the same treatment for the same kind of reason.

This is the third defect of the class that `bug-d4-emitter-malformed-output` and `bug-d4-emitter-reshapes-parse-tree` retired: the printed text did not say what the parse tree said. Those two drained 38 entries from `ParserRoundTripCorpusTest`'s allow list and are held fixed by cases in `D4EmitterRoundTripTest`; the generalized guard for this class already exists and is what caught this, so the fix needs no new mechanism, only a case of its own in that fixture.

## Reproduction

Verified on 2026-11-23 at `b85ac069`, by putting one-line `.d4` files in the repository and running `ParserRoundTripCorpusTest`:

| File content | Result |
|---|---|
| `var P := (LRow.Line).ToString();` | **fails**: `expression.leftExpression: type differs (QualifierExpression vs IdentifierExpression)` |
| `var P := ((Operators where ID = 1)[].Line).ToString();` | **fails**: `type differs (QualifierExpression vs D4IndexerExpression)` |
| `var P := (Operators[1 by { ID }].Line).ToString();` | **fails**, the same way |
| `var P := (Operators where ID = 1)[].Line;` | passes - no second `.` |
| `var P := (Operators where ID = 1)[].Line.ToString();` | passes - the right-associative spelling the emitter produces |
| `var P := ((1 + 2)).ToString();` | passes - redundant brackets round a non-qualifier operand are not the trigger |

The first line is the whole defect: `(a.b).c`, with nothing else in it.

## What has not been shown

**That anything behaves differently.** Run through the engine, both spellings gave the same answer:

```
var LA := ((Operators where OperatorName like '%X')[].Line).ToString();
var LB := (Operators where OperatorName like '%X')[].Line.ToString();
// LA = LB = '40'
```

So the compiler resolved the two shapes identically there, and no wrong result has been observed. The reason to think the shapes *can* differ is that D4's `x.Op(args)` is sugar for `Op(x, args)`, which makes the grouping the thing that decides which part of a dotted chain is the operator's name and which part is its first argument: under `(a.b).c(1)` the operator is `c` and its first argument is `a.b`, under `a.(b.c(1))` the operator name could be read as `b.c` with `a` as its first argument. Whether any pair of catalog names makes the compiler take the second reading is the question this ticket has not answered, and answering it is the first thing to do - it decides whether the fix is a correctness fix or tidiness.

## Where the server's own text goes through this

`D4TextEmitter` is what prints D4 whenever the engine stores catalog DDL, scripts a catalog out, or shows a user the D4 behind an object. So a stored operator whose body contains `(a.b).c` is stored with the brackets gone, `Debug.GetSource` of its locator returns the regrouped text, and a catalog dump that is run back runs the regrouped text. An operator created from a document keeps its original source text separately (`Schema.Operator.DeclarationText`), so the regrouped form is what the emission path produces rather than what a user reads back out of their own file.

## What it already costs

It keeps a construct out of every new `.d4` file in the repository. `debt-debugger-breakpoint-script` met it while writing `tests/libraries/TestFramework.Coverage.Scripts/Documents/DebugBreakpoints.d4`: three assertions printed a line number with `((table where c)[].Line).ToString()`, the file's corpus case failed, and the assertions were rewritten to read the value into a local first. A comment at that site names this slug. The alternative - an allow-list entry for the new file - would have gone backwards on a list two tickets emptied.

## What a fix has to decide

- **Which operands need brackets.** A qualifier whose own right side is not a plain identifier is the narrow reading; every qualifier operand is the wide one. The narrow reading leaves `System.Integer`-shaped text alone, which matters because a dotted *name* must not start printing as `(System).Integer`. Check first how the parser represents a dotted identifier - if `a.b` as a name is a single `IdentifierExpression` rather than a `QualifierExpression`, the blast radius is much smaller than it looks.
- **What the new text looks like in the places that compare it.** The scripts that print and compare D4 (`OperatorSourceText`, `OperatorText`, `ParserEmitter`, the `Serialization*` set) and the derived-form goldens are what would show churn.
- **Whether it is a migration note.** Emitted D4 is text an application or a deployment script can read; a change to its shape belongs in `docs/migration-notes.md` if any of those comparisons move.
