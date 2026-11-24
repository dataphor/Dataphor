description: Storing a new picture or document into a value the engine was already holding used to throw the old contents away without releasing the memory behind them; it now releases them, so a routine that does this in a loop no longer makes the server grow until it is restarted.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime/Stack.cs, src/engine/DAE/Runtime.Instructions/ImperativeNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNode.cs, src/engine/DAE/Compiling/Symbols.cs, src/engine/DAE/Compiling/Compiler.cs, src/engine/DAE/Runtime/Program.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, tests/DAE.Server.Tests/DAE.Server.Tests.csproj, docs/migration-notes.md
repro: verified
----
## What landed

An assignment now releases the value it replaces. `Diagnostics.StreamCount()` comes back to where it started after any number of assignments to a `Binary`, `Graphic` or row variable, and after calls to an operator that assigns to a parameter - including calls that raise.

**The mechanism.** `Alphora.Dataphor.DAE.Runtime.Stack.PokeOwned(int offset, object value, IValueManager manager)` stores a value in a slot and releases what it displaced, releasing nothing when the slot already held the value being stored (stream identifiers compared by value, because `StreamID` is a struct and two boxes can carry one identifier; everything else by reference). Its remarks are where the ownership rule for stack slots is written down. `AssignmentNode.InternalExecute` calls it.

**The `var` parameter write-back moved into a `finally`, and became unconditional.** A `var` parameter's slot is an alias of the caller's slot, so the body's assignment releases a value the caller's slot still points at, and only the write-back makes that safe; it used to run on the success path only. Writing *every* operand's slot value back - not just the `var` ones - is what keeps `arguments[index]` naming the value the callee's slot owns, which is what both arms of `InstructionNodeBase.CleanupOperand` need. One D4-visible behaviour changes with it, which the implement ticket authorised: **after an operator raises having assigned to a `var` parameter, the caller's variable holds the assigned value**, where it used to hold the value it had before the call. `docs/migration-notes.md` records it.

**The by-value-to-`const` promotion read the wrong symbol.** When the server compiles an operator body it decides, operand by operand, whether a by-value operand can be handed the caller's value instead of a copy - which it can whenever the body never assigns to it. The offset it used counted only the operands, ignoring the result symbol above them, so for every operator *with a result* it read a neighbouring operand's answer. An operand the body does assign to could therefore be promoted, and a `const` operand is passed by reference, so the body's assignment then released the caller's value out from under it - a hard `StreamID (n) not found` once `PokeOwned` existed. Found in review; the fix reads the offset from the top of the symbol stack. The decision is also printed as `const` in the declaration the server stores or scripts out, so a scripted operator can now carry that word on a different parameter than before; `docs/migration-notes.md` records that too.

**One variable passed twice by reference.** `F(var LB, var LB)` gave the callee two slots holding one value, and the second assignment released it twice. `CallNode.InternalExecute` now gives a repeat of a by-reference operand its own copy. Review narrowed this to collisions in which one of the two operands is `var`: a `const` operand cannot be assigned to, so two `const` operands sharing a slot can never release anything, and the copy made for one of them had no owner at all - which leaked one value per call of an ordinary `F(LB, LB)`.

**The `foreach` loop variable is the exception to the ownership rule.** The loop lends its slot the one row value it reads each row into and goes on reading into it, so releasing it left the next iteration selecting into a disposed row. `Symbol` gains a read-only `IsBorrowed` and `Symbols` a `SetIsBorrowed(int offset)`; both `foreach` compile entry points mark the loop variable in both of their branches, and `AssignmentNode` keeps a plain `Poke` for a borrowed target. The mark is never cleared, deliberately: a `foreach` over a pre-existing variable borrows that variable's slot for the loop and, in the list form, leaves the last element in it.

## Review findings

Reviewed `ticket(implement): bug-assigning-a-binary-variable-leaks-its-stream` and the review-stage work salvaged into `tess: agent error on bug-assigning-a-binary-variable-leaks-its-stream — added resume note` (a first review run was cut off by a usage limit after it had made its fixes and got the first suite part green; this run re-read the whole diff, re-ran everything, and carried on). Nothing was reverted.

### Found and filed

**An operator argument can be released by the wrong owner** - `tickets/fix/3-bug-an-operator-argument-is-released-by-the-wrong-owner.md`, two arms at one site, both verified against the pre-change commit in a worktree:

- A `const` operand given anything but a variable reference leaks one value per call. `InstructionNodeBase.CleanupOperand` has no `const` arm at all, and nothing else holds a value computed for the call. Pre-existing: a procedure in that shape leaks identically before this change. **This change widens it**, because correcting the promotion above promotes one more operand per function, and those operands then meet it; measured by reverting that one line and watching a function-with-a-literal case go from failing to passing.
- A by-value operand given a column of a row variable *destroys* the row's value, because the column extraction hands over the row's own value and the `in` arm releases it. Pre-existing and unchanged by this work: same failure before the change.

The obvious repair was written and measured here, and **it breaks the engine**: releasing a `const` operand's value unless the argument came by reference closes every leak case and then loses a row variable's column, because a scalar type's selector takes its `const` arguments over into the value it returns. So the modifier cannot carry the answer, which is why this is a `fix/` ticket for a representation change rather than an inline fix - and why this review did not ship the widening's repair. `debt-streams-coverage` now names it as a prerequisite: a stream-balance coverage script written while that leak stands would bake it into its expected counts.

### Checked and sound

- **`CallNode`'s new `finally`, which the handoff asked to be argued with.** The index arithmetic was derived independently and agrees: with `stackDepth` taken after the operands and the result symbol are pushed, operand `index` sits at offset `(Count - stackDepth) + (operands + result - 1 - index)` from the top, and the leading term is what lets a body that left the stack deeper than it started still find its own operands. The risk the handoff named - the indexer throwing while an exception unwinds, replacing it - needs the body to leave the stack *shallower* than its own operands, and nothing pops them before the window pops above this frame; every block and frame pop in between is itself in a `finally`.
- **The borrowed-slot surface is exactly the `foreach` loop variable.** All 18 `Stack.Poke` call sites were enumerated: three `foreach` runtime nodes (`ImperativeNodes.cs`, two in `ListNodes.cs`), the `VarNode` fill of a slot just pushed as null, the `var` write-back, the `AssignmentNode` borrowed arm, and unnamed iteration slots in `AggregateNodes`, `InNodes` and `TableNode`. Only a *named* slot is reachable from an assignment, because `AssignmentNode`'s target is always a `StackReferenceNode` resolved from an identifier; and the compiler has exactly two `foreach` entry points (`CompileForEachExpression`, `CompileForEachStatement`), both of which mark both of their branches. The handoff's "about seventeen sites" and its conclusion both hold.
- **The other three assignment-target shapes.** `EmitAssignmentNode` dispatches four ways. A table target is a DML path. A column target (`LR.B := …`) compiles to a temporary variable plus a row `update`, which this change does not touch. A property target (`LB.Value := …`) compiles through the write accessor back into an `AssignmentNode`, so it does go through `PokeOwned` - and its source expression *reads* the value the assignment is about to replace, which is the shape most likely to release something it still needs. Both were measured over a hundred iterations and balance.
- **A C# instruction node that replaced a `var` operand's value would leak the caller's old one**, which the handoff left as a non-exhaustive search. Closed: the only run-time assignment to `arguments[index]` anywhere in the engine or the libraries is `CallNode`'s write-back. The nodes behind the generated `var` event-handler signatures mutate their row in place.
- The generated `result := …` assignment nodes (three sites in `Compiler.cs`) are built without `DetermineDataType`, so `_targetIsBorrowed` stays false and they use `PokeOwned` - correct, a result slot owns its value and hands it to the caller before the window pops. `AssignmentNode.InternalClone` carries the flag; `Symbol`'s copy constructor carries `IsBorrowed`.
- Comment density: the long remarks on `Stack.PokeOwned`, `ReferencesAnEarlierOperandsSlot` and `Symbol.IsBorrowed` carry constraints, measurements and consequences rather than narrating statements, so none was extracted or cut. `ImperativeNodes.cs` grew to 2,112 lines (`wc -l`), which is not size debt this change created and is unremarkable beside the 15,730-line `Compiler.cs` next to it.

### Tripwires

Three were left by the implement and first-review runs and all three were re-read and kept as they stand: the residual leak of an assignment to a `foreach` loop variable (`AssignmentNode`), the residual leak of `F(var LB, LB)` (`ReferencesAnEarlierOperandsSlot`), and the `DEBUG`-only stream-count check at program teardown that cannot be added until `bug-an-error-inside-a-block-leaks-its-binary-values` lands (`Program.Stop`). This review added none of its own: its one finding is a real latent defect with a current anchor, which belongs in a ticket rather than a comment.

### Accepted tradeoffs

None at any site this change touches.

### Documentation

`docs/migration-notes.md` carries three entries from this work - the `var` parameter error path and the `const` printing change under *D4, catalog and libraries*, and `Stack.PokeOwned`, `Symbol.IsBorrowed` and `Symbols.SetIsBorrowed` under *Custom C# extensions*, including that an extension built against an earlier 4.0 build must be rebuilt because `Symbol` is a struct that grew a field. All three were read against the code and match.

Nothing under `docs/architecture/` was touched, and this review partly disagrees with the handoff's reasoning for that. The rule is not confined to one code site any more: it has three exceptions spread over four files, and no page under `docs/architecture/` mentions the stack, streams or who releases a value at all (`sessions-processes-plans.md`, the nearest, is within fifty words of the 1,500-word cap). But the rule is also not finished - the `const` arm above is still wrong - so writing the page now would mean writing it twice. It is named as deliverable work in the new ticket instead.

### Tests

No test was cut. All nine cases in `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` pin distinct behaviour through `Diagnostics.StreamCount()` on real scripts - nothing restates the implementation or verifies a mock - and under this project's characterization rule a port that changes a subsystem's behaviour owes exactly this. The three regression cases were each confirmed load-bearing by disabling their guard, rebuilding and watching them fail.

One was added by this review: `AssigningThroughAScalarPropertyReleasesTheValueItReplaces`, a hundred iterations of `LB.Value := '0002'`. It is the one assignment surface that reaches `PokeOwned` through a source expression that reads the slot it is replacing, which is the contract `PokeOwned`'s same-value test exists for, and nothing covered it.

Three temporary probe fixtures were used to measure the findings above - the const-operand shapes, the row-column shapes, and the two non-variable assignment targets - and removed again; `git status` shows only the one added test.

### Build and tests run

`dotnet build Dataphor.slnx` - 0 errors, 143 warnings, unchanged from before the work.

The change touches the compiler and the node every assignment and every operator call goes through, so the whole suite was run in Debug in the three parts the root `AGENTS.md` gives, rather than a targeted set:

- Part 1 (everything but the coverage scripts): 894 passed, 0 failed, 34 skipped, 10 m 45 s.
- Part 2 (coverage scripts A-R): 72 passed, 0 failed, 11 skipped, 3 m 57 s.
- Part 3 (the rest): 50 passed, 0 failed, 12 skipped, 3 m 26 s.

Parts 2 and 3 had never run against this code: the first review run reached part 1 and was cut off there. Before them, the ticket's named targeted set - `--filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~ServerStreamManagerTest|FullyQualifiedName~ConveyorTest|FullyQualifiedName~TableAssignmentEvaluationTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest|FullyQualifiedName~DebuggerSessionTest"` - 51 passed, 0 failed, 1 skipped.

The 23 skips across parts 2 and 3 are the manifest's known failures and are unchanged; no manifest entry was flipped. No pre-existing failure surfaced, so nothing was written to `tickets/.pre-existing-error.md`.

## Cost of the extra work

**Not separable from run-to-run noise**, measured during implement: a tight D4 loop of 4,000,000 integer assignments, Release, in-process, three timed runs per pass and three passes each way, discarding each pass's first run as warm-up - plain `Poke` 422-503 ms (mean 454 ms), `PokeOwned` 415-529 ms (mean 457 ms). The means differ by about 0.7% against a spread of ±50 ms within either arm, so the added work per assignment is bounded well under 25 ns on a roughly 110 ns operation. The extra work on a native value is the same-value test plus the type tests inside `DataValue.DisposeValue`, which falls through for anything it does not recognise.

## Compatibility

Nothing an application can see changes except the engine's memory use, the `var` parameter error path, and which parameter a scripted declaration prints `const` on - the two migration-notes entries above. No D4 operator signature, class name, assembly name, file format, error code, WCF contract or port changed, and the engine allocates no identifier differently, so no catalog rebuild is implied.

## Still open

- `bug-an-operator-argument-is-released-by-the-wrong-owner` - this review's finding, in `tickets/fix/`.
- `bug-an-error-inside-a-block-leaks-its-binary-values` - the sibling leak, untouched as the implement ticket directed: a block left by a raise still leaks the values of the variables it declared, through the frame pop rather than through assignment. No script in the new fixture leaves its declaring block by a raise, so nothing here depends on it.
- Assigning to a `foreach` loop variable still leaks what it displaces, and `F(var LB, var LB)` still leaks the one write such a call always loses. Both are recorded as `NOTE:` tripwires at their sites.
- `debt-streams-coverage` covers the same ground from a D4 coverage script and now waits on both this ticket and the new one.
