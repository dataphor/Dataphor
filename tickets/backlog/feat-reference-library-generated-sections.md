description: The reference page for each shipped library lists only some of the operators that library provides, and three libraries have no operator documentation at all, because the old publishing tool filled those sections from a running server and the text never reached this repository. Generate the missing parts with the same machinery the core library's reference generator uses.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/reference/libraries/, tests/DAE.Server.Tests/FrontendFixture.cs, tests/goldens/reference/, libraries/
difficulty: medium
tradeoffs: These pages are reference material for libraries most applications never load, the work needs a test server carrying thirteen libraries rather than the bare engine the system-library generator uses, and a maintainer could reasonably say that a library's own D4 scripts are the honest answer for anyone who needs this detail.
----
## What this is

`docs/reference/libraries/` has one page per shipped library, converted by `docs-reference-libraries` from the 2007 documentation. Two sections of those chapters were filled by the old publishing tool from a running server at publication time rather than written by hand, and neither the tool's output nor any source for it is in this repository, so both are missing or partial on every page.

**The operator declarations are incomplete.** Measured on 2026-11-26 by counting the declarations blocks each page carries: 49 blocks across six pages, and eight pages with none at all. Measured during the review of `docs-reference-libraries` by counting the distinct operator names each library's own `Documents/*.d4` declares against the operator sections its page carries: `Frontend` declares 36 and documents 29; `SQLDevice` declares 19 and documents 6, the thirteen missing being mostly the `SQLText` type's own comparison and string operators plus `DeviceReconciliationScript`; `SimpleDevice` and `System.Integration` document all of theirs. Three more libraries - `Connection`, `Security` and `System.Internet`, 77 declarations between them - have no operator documentation at all, because the 2007 chapter pulled it in from a file this repository does not contain.

**The per-library schema section is missing entirely.** `docs/reference/libraries/README.md` says so under *What these pages do not say*: each library chapter of the 2007 documentation ended in a *Schema* section listing the forms, roles, devices, types and tables the library defines, and it too was generated at publication time. It was left out rather than published empty or stale, and that README names generating it from a live catalog as a later piece of work. This is that work.

## Why it is parked rather than filed into the pipeline

`docs-reference-signatures` and `docs-reference-generator` (the two tickets of the WS8 `docs-reference-generator` work item) scope themselves to the System library, which is what the plan row and the plan's gave-up list name. The library pages were weighed while planning those and left out deliberately: they are a different and larger job, and they do not block the work item from closing.

**Promote this once the two tickets above have landed.** The generator they build - a test fixture driving the shipped `System.Script` operator over the `Operators` and `TableVars` system tables, with a goldens tree and an update environment variable - is most of what this needs, and re-deciding any of that before it exists would be guesswork.

## What a plan would have to settle

- **Which server.** The System library's generator uses `InProcessTestFixture` with no libraries at all, which is what makes its output a function of the engine alone. This needs thirteen libraries registered, which means the set has to be pinned and stated, and the output then depends on the library versions in the tree. `FrontendFixture` already stands up ten; the reference documents thirteen plus `General`. Whether to extend that fixture or build a second one, and whether one server carrying all thirteen is honest or each library wants its own, is the first question.
- **Whether a generated section can be mixed into a hand-written page at all.** The System library's operator pages have one generated block inside hand-written prose and are compared block by block; the generated system tables pages are generated whole and compared whole. A library page would need the first treatment for the operator declarations and would gain a wholly generated schema section at the end - two mechanisms on one page. That may be fine or may argue for the schema section living on a page of its own per library.
- **What to do about the 77 operators with no prose.** A declarations block with no remarks around it is better than nothing for looking a signature up, and worse than nothing if it reads as finished documentation. Say which, and mark the pages accordingly.
- **Whether `General` gets anything.** It is a library with no contents - no files, no D4, nothing but a descriptor - so its schema section would be empty, which is a fact worth stating once rather than a page to generate.

## Worth knowing before planning it

- The declarations the pages already carry include full D4 operator *bodies*, not just signatures, because the emitter emits the body for a D4-implemented operator. `docs/reference/libraries/Frontend.md` shows this. So a generated library page is considerably longer than a system-library operator page, which bears on the page-shape question above.
- Library facts other than operators - version, requisites, file list, registered assemblies - are already on the pages and come from each library's own `.d4l` descriptor, not from the catalog. That was settled by `docs-reference-tooling` because the help file's published library facts are known-stale (its `SimpleDevice` says version 2.1.0.0 where the shipped descriptor says 3.0.0.0). Do not regenerate those from a catalog; the descriptor is the source.
- The five device and connection libraries this release does not ship (ADO, Microsoft Oracle, Oracle, SAS, DB2) have no pages and should not get any - the plan records that as a deliberate decision, with the reason on `docs/reference/libraries/README.md`.
