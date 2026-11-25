description: The row-equality leak fix broke the ImplicitConversions coverage script in both Debug and Release - every run since it landed fails with a null reference while the server works out where in the script it is. CI has been red on every commit since.
architecture: docs/modernization-plan.md#ws2-engine-and-server-port
files: src/engine/DAE/Runtime.Instructions/RowComparisonNodes.cs, src/engine/DAE/Runtime.Instructions/InstructionNodeBase.cs, tests/libraries/TestFramework.Coverage.Scripts/Documents/ImplicitConversions.d4
----
## What failed

CI runs 45579893996 (`e321e388`, `ticket(implement): bug-row-equality-leaks-the-rows-it-compares`) and 45582470814 (`e3764acc`, its review), **both jobs, Debug and Release**. One failing test of 1,119 each time:

- `Alphora.Dataphor.DAE.Server.Tests.CoverageScripts.Script(ImplicitConversions)` (1 s), manifest entry `pass`:

  > `RuntimeException: Runtime error: Could not determine the current location for program <guid>. (doc:Coverage.Scripts:ImplicitConversions@1:0) <= ServerException: Could not determine the current location for program <guid>. <= NullReferenceException: Object reference not set to an instance of an object.`

  The NullReferenceException is the real error; "could not determine the current location" is the server failing to place it in the script, so first get the inner exception's stack (run the case locally with `--logger "console;verbosity=normal"`, or with `DATAPHOR_TEST_LOGGING=1`).

Reproduce: `dotnet test tests/DAE.Server.Tests --filter Name=ImplicitConversions --logger "console;verbosity=normal"`.

## Commit range

- Last green: `cdb56b30` (`ticket(review): bug-a-var-parameter-written-in-the-body-can-release-a-callers-loop-value`, run 45574822579).
- First red: `e321e388`. Its only engine change is `RowComparisonNodes.cs` (row `=` / `iEqual`). The prime suspect is the switch from `DetermineModifiers(plan)` to `base.DetermineDataType(plan)` in `RowEqualNode.DetermineDataType` (or whichever row-comparison node that is): the base does more than ask its const operands to lend, and the row node then replaces argument nodes afterwards (upcasts and conversions - which is exactly what ImplicitConversions exercises). The new release logic in `InternalExecute` reading `Nodes[i].ReturnsByReference` on a replaced node is the second suspect.
- The commits after it (`20bd0362`..`21ac205b`, the store-lock ticket) touch other code and are expected to carry the same red.

## Note for the fix

The implement and review stages of the row-equality ticket both ran targeted tests and neither ran ImplicitConversions. Keep the leak fix (its tests in `AssignmentValueOwnershipTest` and `OperandOwnershipShapeTest` must still pass); find the narrower change that keeps both. When done, also run the coverage scripts whose names suggest row comparison or conversions (`--filter` on a few names), since the ring the stages chose last time was too narrow.

## Done when

ImplicitConversions passes again in Debug and Release, the row-equality leak tests still pass, and the cause is stated in the ticket.
