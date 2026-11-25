description: A routine call that failed while working out one of its arguments used to abandon the picture or document values it had already worked out for earlier arguments, so the server held that memory for the rest of the session. It now gives them back, with tests pinning both halves of the rule for each of the three shapes a call can take.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/InstructionNode.cs, tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs, docs/architecture/value-lifetime.md, docs/migration-notes.md
----
## What landed

A call evaluates its arguments, runs the operator, then cleans each argument up in a `finally` - releasing the value where the call owns it, writing it back where the operand was declared `var`. The arguments were evaluated *before* the `try` that `finally` belongs to, so when argument 2 raised, nothing cleaned up argument 1: a `Binary` or a `Graphic` lives in a stream the server owns, and an orphaned stream is given back only when the process ends.

The implement stage is commit `ticket(implement): bug-an-argument-is-orphaned-when-a-later-one-raises`. In `src/engine/DAE/Runtime.Instructions/InstructionNode.cs` it added:

- **`InstructionNodeBase.ReleaseUncalledOperand`**, a `protected` method beside `CleanupOperand` carrying the failure-path rule once instead of in each shape.
- **A `catch` around the evaluation of every argument after the first** in the three shapes that can have one - `BinaryInstructionNode`, `TernaryInstructionNode` and the general `InstructionNode` - releasing the arguments already evaluated and rethrowing with a bare `throw;`, so the original error and its stack trace reach the caller unchanged. Each guards on `Operator != null`, as the existing `finally` does.
- **`UnaryInstructionNode` and `NilaryInstructionNode` untouched**, with a comment on the unary shape saying why one argument needs no failure path.

Review changed three things in that work and found one defect elsewhere; all four are below.

`src/engine/DAE/Runtime.Instructions/InstructionNode.cs` is 549 lines after review (`wc -l`), down from the implement stage's 557.

## Review findings

### Read first, then the handoff

The implement diff was read before the handoff summary, as the stage asks. Checked: all five execute shapes; `ReleaseUncalledOperand`'s branches against `CleanupOperand`'s; the `evaluated` counter's arithmetic on the raising index; the general shape's second bound; `DataValue.DisposeValue(manager, null)` being a no-op, which the ternary's null sentinel depends on; and that `Modifier` has a fourth value, `Out`, that neither switch handled and neither handles now.

**The `var` branch is correct, and now confirmed rather than argued.** The handoff called this the part worth a reviewer's attention. `InstructionNodeBase.DetermineDataType`'s `Modifier.Var` branch sets `StackReferenceNode.ByReference = true` unconditionally, and `StackReferenceNode.InternalExecute` returns the slot's own value when `ByReference` and a copy otherwise - so a var operand's argument never produces a value of its own, there is nothing to release, and doing nothing is right whether the var operand was evaluated before the raise or not.

**The class is retired for this family.** Every other plan node deriving from `InstructionNodeBase` directly rather than through an arity shape was read - `ExistsNode`, `TableToListNode`, the `TableNode` family and `IfNilNode` - and each takes one argument or disposes what it opens, so none can orphan an earlier argument. `AggregateCallNode` derives from `PlanNode`, contains no `CleanupOperand` call at all, and evaluates nothing through these shapes; out of scope, correctly.

### Fixed in this pass (minor)

**The new test did not guard the `var` branch - the reason the method exists.** Measured rather than reasoned: replacing `ReleaseUncalledOperand` with `CleanupOperand` in the general shape's `catch` and running the shipped suite left it **green**. With the shipped `index < evaluated` bound the var operand is never reached on the failure path, so the stream count cannot see the difference, and the handoff's claim that its var-last case "fails on the value under that fix" holds only for the variant the implementer happened to try - cleanup run over *every* operand regardless of how many were evaluated.

Added `AFailedCallLeavesAVarOperandEvaluatedBeforeTheRaiseAlone` to `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs`: a scalar type with a validate handler that counts its own runs in a table, a var operand of that type evaluated *before* the argument that raises, and an assertion that the handler ran no more times after the failed call than after the assignment that preceded it. The handler counts instead of changing its value because a validate handler that assigns to its operand is validated again, and only one that stops changing the value terminates - an earlier draft that appended a marker recursed until the run aborted. The count is compared with itself rather than a literal, so the case does not also encode how many times assigning a variable validates it (twice, as it happens).

**Both directions measured, after the refactor below:**

| What was changed | `ACallWhoseLaterArgumentRaises…` | `AFailedCallLeavesAVarOperand…` |
|---|---|---|
| nothing | pass | pass |
| whole method neutered (early `return`) | **fails 3×**, one stream per shape - `0/1`, `1/2`, `2/3` | pass |
| the `var` guard removed | pass | **fails**, one extra validate run |

So the two cases are complements, neither is redundant, and each fails for the reason it exists. Both temporary edits were removed; `grep -rn TEMPORARY` over both files is empty.

**`ReleaseUncalledOperand` duplicated `CleanupOperand`'s `in` and `const` branches.** The remark said the two "should stay readable as the same rule", which is a comment load-bearing against a divergence the code itself predicts: the `NOTE:` on `CleanupOperand`'s `in` branch anticipates that branch being gated on `ReturnsByReference`, and that change would have had to be made twice. `var` is the only modifier whose rule differs, so the method now states that one and calls `CleanupOperand` for the rest - eight lines shorter, and the "same rule" claim is structural instead of asserted. Behaviour is identical, `Modifier.Out` included: it was unhandled in both switches and is now unhandled in one.

**`docs/migration-notes.md` had no entry.** The handoff left it out and asked for a second opinion; the second opinion is that it needs one. The project rule's bar is "a change that an application, a deployment script or a custom C# extension could notice - including a public type or member that extensions use", and both sibling ownership fixes carry entries for effects that are behaviour-only for extensions (`InstructionNodeBase.CleanupOperand`'s const rule, and `ListValue` honouring `ValuesOwned`). Added an entry under *Custom C# extensions* naming the new `protected` member, the three changed shapes, the two left alone, and why the failure path is not `CleanupOperand`. The "what to do" column is honest that there is nothing to change in a custom node that already follows the const rule.

### Filed (major, past the filing bar)

**`IfNil` leaks one stream per call.** Found while establishing that this ticket's class was retired. `IfNilNode.InternalExecute` returns `DataValue.CopyValue(...)` of whichever argument it chose and never releases the value it copied, so a computed `Binary` or `Graphic` argument is orphaned every call. **Verified, not inferred**: a temporary case calling `IfNil(<an operator returning Binary>, Binary('0002'))` twenty times finished 20 streams higher than it started - exactly one per call. The probe was removed.

Filed as `tickets/fix/4.9-bug-ifnil-leaks-the-value-it-copies.md`, alongside the two other value-ownership fix tickets, on the same `#ws2-engine-and-server-port` anchor. Not fixed in this pass, although the local fix is small, because the choice it forces is not: releasing the original after copying is three lines, while returning an owned value as it stands saves an allocation and a copy on the common case but makes `IfNilNode` hand back a value it does not own - and its ownership is then per-execution, where `PlanNode.ReturnsByReference` is settled at compile time. That mismatch is worth settling deliberately rather than inside a review.

The ladder was climbed before filing. A generalisation does exist - one test calling every system operator whose signature admits a `Binary` and asserting the stream count comes back would cover the family rather than this node - but whether the class is larger than one node was not established here, so the ticket names the instance it can prove and records the generalisation for whoever plans it. Nothing open on the board touched `SystemNodes.cs` or mentioned `IfNil`, and there is no accepted-tradeoff `NOTE:` at the site.

### Tripwire (recorded, not ticketed)

**`docs/architecture/value-lifetime.md` has three words of headroom.** 1,497 of the 1,500-word cap, measured with `python tools/docs-size-check/check_size.py --limit 1497` (over at 1,496, clean at 1,497). The gate passes, so this is not work yet, but the next sentence anyone adds forces the split. Parked as an HTML-comment `NOTE:` at the top of the page naming the limit, the command that shows it and the page's natural seams. The page's own gate ignores HTML comment text - `prose_words` blanks comments before counting - so the note costs nothing against the budget it is about, which was checked: the count is still 1,497 with it in place.

### Evidence added to an existing ticket

**Suite part 1 is now 9 m 48 s against the runner's 10-minute idle limit** - twelve seconds of allowance. Appended as a row and a paragraph to `tickets/backlog/debt-test-part-one-near-the-runner-limit.md`, which already tracks this, rather than filed again: it is the fourth measurement of a class that ticket owns. Run twice in this review, 9 m 54 s and 9 m 48 s, so not one slow run; part 1's case count is unchanged from the previous measurement bar this ticket's one new case, so the jump of over a minute from 8 m 43 s is not new tests. Its `description:` was updated from "nearly nine minutes", which now understates it.

### Checked and deliberately left alone

- **A release inside a `catch` can replace the original error.** If `DataValue.DisposeValue` threw while unwinding, the caller would see that instead of the argument's error. The handoff flagged it as a judgement call; the existing `CleanupOperand` in each `finally` has had the identical exposure since the beginning, so this adds no new class of risk, and wrapping it would swallow a genuine double-release - which is how most defects in this family were found. Left as the handoff decided.
- **The general shape's second bound**, `(index < evaluated) && (index < Operator.Operands.Count)`. Whether `Operands.Count` and `NodeCount` can differ is still not established either way, as the handoff said - but it does not need to be: if they can, the existing `finally` already indexes out of range or leaks on the *normal* path, so the new bound adds no exposure and is strictly defensive. No finding.
- **The ternary's null sentinel.** `argument2` is `null` when argument 2 was the one that raised, and the `in` and `const` branches reach `DisposeValue(manager, null)`, which returns without doing anything (`as IDataValue` is null, and null is not a `StreamID`). Correct, and the asymmetry with the general shape's counter is three lines rather than a pattern worth unifying.
- **The `in` branch's by-inspection invariant.** The handoff noted the new method inherited it without re-verifying. After the refactor there is nothing separate to inherit: the branch *is* `CleanupOperand`'s, `NOTE:` included.
- **Performance.** Not re-measured. The structural argument holds - the normal path executes the instructions it executed before, entering a `try` region costs nothing at run time when nothing is thrown, and the refactor only shortens the failure path. The handoff's own reading of `PerfBaselineTest` was that the instrument's run-to-run spread exceeds anything this change could cause, which is the right reading; a second recording run would have added noise, not evidence.
- **Source hygiene.** Tabs, CRLF and byte-order-mark state preserved in all four files (`InstructionNode.cs` and `migration-notes.md` no BOM, `AssignmentValueOwnershipTest.cs` BOM, `value-lifetime.md` no BOM); no file reformatted that was not otherwise changed. Comments in the diff state why code is absent or why a value is null, not what a statement does.

### Nothing found in

The `evaluated` counter's arithmetic (on a raise at index *k* it holds *k*, and the catch releases 0..*k*-1 - correct), the `Operator != null` guards, the bare `throw;` rethrows, and the new paragraph in `value-lifetime.md`, which states the rule accurately and still does after the refactor.

## What was run

Build first each time: `dotnet build Dataphor.slnx` - 0 errors, 143 warnings, all pre-existing `CA1416`/`CS0618`/`CA2022` from `Connection`, `SimpleDevice` and the test project.

The whole suite, in the three parts, because the change sits in the path every compiled operator call takes and review also refactored the method the normal path uses:

- part 1 (everything but the coverage scripts) - **927 passed, 33 skipped, 9 m 48 s**, plus `Common.Tests` 18 passed;
- part 2, coverage scripts `[A-Ra-r]` - **72 passed, 11 skipped, 3 m 29 s**;
- part 3, coverage scripts `[^A-Ra-r]` - **51 passed, 12 skipped, 3 m 11 s**.

Zero failures across all three. Also, repeatedly while iterating: `dotnet test tests/DAE.Server.Tests --no-build --filter "FullyQualifiedName~AssignmentValueOwnershipTest|FullyQualifiedName~BufferedOperatorColumnOwnershipTest|FullyQualifiedName~ContainerValueOwnershipTest"` - 28 passed, 6 s. And `python tools/docs-size-check/check_size.py` - 12 files checked, 0 over the limit.

**One thing worth passing on.** A `dotnet test --no-build` run early in this review reported this ticket's own test failing in all three shapes, with exactly the original bug's signature. The cause was the binary, not the code: the source had been restored after a deliberate experiment but not rebuilt, and `--no-build` ran the experiment's assembly. The fix is mechanical - build between restoring a file and running `--no-build` - but the failure is indistinguishable from a real regression, which is what makes it worth writing down.

## Not done

- `tests/DAE.Server.Tests/AssignmentValueOwnershipTest.cs` is now 895 lines and 22 cases, which is large for one fixture. Not split here: the cases are one subject - who owns a value and what releases it - and the fixture's `<summary>` is the written form of that rule, so splitting it would divide the explanation rather than the file. Worth a look when the next ticket in this family adds to it.
- No case was added for a var operand of a *non*-scalar type (a row or a list) on the failure path. `CleanupOperand`'s write-back skips `ValidateValue` for those, so the wrong fix is unobservable there and a case would pass under both the right and the wrong implementation - a test that cannot fail is documentation, and the `<summary>` already carries it.
