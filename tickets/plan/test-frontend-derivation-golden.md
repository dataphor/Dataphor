description: Design the L5 Frontend tests — snapshot form XML that the derivation engine produces for the sample libraries so we notice when it changes, read every form file in the repository through the deserializer so a format change is caught at once, and prove that applying a customization and then subtracting it gets back to the original.
architecture: docs/modernization-plan.md#ws3-test-suite
files: libraries/Frontend/Source/Derivation/InterfaceBuilder.cs, libraries/Frontend/Source/Elaboration/Elaboration.cs, libraries/Frontend/Source/Production/DocumentBuilder.cs, libraries/Frontend/Source/Structuring/StructureBuilder.cs, libraries/Frontend/Documents/Register.d4, src/engine/Frontend.Server/, samples/
difficulty: medium
----
## What this is

L5 of the WS3 layer table: "Server side only: derive every page type for the sample libraries and compare the form XML to golden files; load every `.dfd`/`.dfdx` in the repository through the deserializer; customization merge/difference round trip". The plan slug is `test-frontend-derivation-golden`; it should become one or more implement tickets once the shape is settled.

140 `.dfd`/`.dfdx` files sit in the repository at filing (count from `find`). The sample libraries are at `samples/` (11 directories; `Sample.Shipping` is parked out of the current layout — confirm which samples belong in the corpus during planning).

## Open questions this plan must settle

The implement step cannot start until the planner picks one default for each and routes to `blocked/` any that need the owner's call:

1. **Which page types.** The derivation engine supports several page types (Browse, List, View, Edit, Add, Delete, OrderBrowse and related). Derive each page type for every sample surface, or only the subset the samples exercise today? Pick the exhaustive default (every page type supported by the derivation engine) and name the enumerating operator or the list of types as the implement ticket's starting point.
2. **Golden file layout.** One golden per `(library, surface, page type)` in `tests/goldens/frontend/<library>/<surface>.<pageType>.dfd`, UTF-8 without BOM, LF, formatted deterministically by whatever the derivation engine emits (not re-pretty-printed in the test). Decide whether to normalize the output (strip timestamps, GUIDs, machine names) and list every field the normalizer touches.
3. **Golden-file update workflow.** A test-only environment variable (e.g. `DATAPHOR_UPDATE_FRONTEND_GOLDENS=1`) that writes rather than compares, documented in AGENTS.md. Diffs reviewed by hand in the commit.
4. **Deserialization coverage.** Load every `.dfd` and `.dfdx` in the repository through the BOP deserializer. The test needs a `Frontend` library registered on the engine. Decide whether the test also re-serializes and round-trips (strong check, needs a stable serializer), or only parses (weaker but cheaper). The plan allows either — pick the stronger if the serializer is stable on the sample corpus, else only parse.
5. **Customization round trip.** A pair of operators the Frontend library already exposes (`CustomizeDocument`, `DifferenceDocument` or the `.dfdx` merge path) applies a diff to a base document and recovers the diff from the result. Design the inputs: a handful of synthetic base + customization pairs committed under `tests/goldens/frontend/customization/`, or derive both sides from the samples. Pick synthetic for now (the samples' customizations have no owner-committed oracle).
6. **Test host.** The three concerns share a fixture: a `TestServerInstance` with `Frontend` and the sample libraries registered once. Plan whether this reuses the coverage template or builds a sister fixture. The coverage template registers only `Frontend` and `TestFramework.Coverage.Base` (`test-harness-inprocess`), so the L5 fixture must be a sister. Confirm it does not cost more than ~30 s to build.
7. **Sample.Shipping.** The Shipping sample's C# project is parked (see `docs/modernization-plan.md` library table: it is not in the runtime layout because its assembly is not built). Exclude from the derivation corpus until `library-shipping-port` (or its successor) is cut; name the excluded sample in the implement ticket. `test-samples-corpus` carries the same exclusion.

## Expected output

One `plan/` ticket per sub-item if they split cleanly; otherwise prereq-chained implement tickets. Keep the slug `test-frontend-derivation-golden` on the ticket that finishes the last sub-item (so dependents, including `test-samples-corpus`, do not break). Candidate shape:

- `test-frontend-derivation-fixture` (implement) — the sister `TestServerInstance` that registers `Frontend` and the samples; no assertions yet.
- `test-frontend-document-deserialization` (implement, prereq above) — load every `.dfd`/`.dfdx`.
- `test-frontend-customization-roundtrip` (implement, prereq above) — synthetic base + customization corpus.
- `test-frontend-derivation-golden` (implement, prereq above, keeps the slug) — derive every page type, compare to goldens.

## Edge cases & interactions

- Sample libraries that need a device the harness does not configure — the Simple device is available; the SQL Server device is not (K14, `test-decouple-coverage-base`). Samples that require a SQL Server device have to be skipped visibly (Inconclusive, counted) or given a SimpleDevice variant where practical (plan says so). Enumerate during the plan; if any sample has no SimpleDevice variant and no viable skip, route the choice to `blocked/`.
- Culture and float formatting in the emitted XML can drift with the runtime (D2). The fixture must pin invariant culture and NLS globalization (which the runtime config already does for the test host).
- The emitter's output order for maps/dictionaries. Confirm it is deterministic in the derivation engine before golden files are usable; if not, normalize at known sites rather than re-sorting after the fact.
- `library-frontend-web-leftovers` (backlog, WS7) removes the `System.Web` classes from the Frontend server library. The derivation test must still build after that deletion; note the interaction in the implement ticket.
- Interaction with `compat-corpus-harness` (WS13): the customization round trip's synthetic corpus is small; `compat-corpus-harness` will later point its own corpus directory at this same test.

## TODO

- Investigate each open question above, note the finding and the default chosen, and move anything that still needs the owner's call to `blocked/` with proposed text.
- Emit the implement ticket(s) with files and anchors filled in.
- Delete this plan ticket on the way out.
