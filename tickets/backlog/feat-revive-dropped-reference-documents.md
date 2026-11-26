description: Four technical documents in the oldest documentation tree describe things the product still does - how it builds forms from the database schema, which files it reads and writes, which requests its form server answers, and how its visual form designer is used - and none of it is in the current documentation; decide what is worth keeping before that tree is deleted.
architecture: docs/modernization-plan.md#ws8-documentation
files: legacy/Documentation/DocBookManuals/DerivationReference.xml, legacy/Documentation/DocBookManuals/DataphorFileStructures.xml, legacy/Documentation/DocBookManuals/FrontendServerRequestTypes.xml, legacy/Documentation/DocBookManuals/FrontendDevGuidePartII.xml, legacy/Documentation/Docs2.0/DevGuidePart3.xml, docs/reference/tags.md, docs/architecture/on-disk.md, docs/developers-guide/form-automation.md
tradeoffs: All four documents are twenty years old and parts of each are certainly stale - one is half a Visual Studio-era walkthrough and another documents a client that has been removed - so a maintainer may reasonably decide that the subsystems' own source code is a better reference than 2004 prose, and that reviving any of it costs more editing than writing fresh pages when someone actually needs them.
----
## What this is

The documentation conversion (`docs-convert-asciidoc`, then the `docs-convert-legacy-reference` chain) brought the product's documentation into Markdown under `docs/`. It worked from two inputs: a 2016 edition of the three manuals, and the 2007 edition of the reference material. There was an earlier edition too - the 2004 manuals - and the later editions dropped chapters from it. Those chapters survive nowhere else.

`docs-remove-old-sources` deletes both old trees. Four documents go with them that describe behaviour this release still has, and which nothing under `docs/` replaces. This ticket asks a maintainer to decide, document by document, whether to bring the material across - not to do it. Each arm below says what the document is, what evidence there is that nothing covers it, and the argument for leaving it in history.

Nothing here blocks the deletion: the *Recovering a source* section at the end records how to get any of these files back afterwards.

## Arm 1 - how forms are derived from the schema

`legacy/Documentation/DocBookManuals/DerivationReference.xml`, 29.5 KB, titled *Dataphor Derivation Reference*.

The product's distinguishing feature is that it builds user interfaces from the database schema rather than from hand-drawn forms, guided by metadata tags. This document is the reference for how that works: how a tag's value is chosen when several could apply and in what order of precedence; the naming scheme that lets a tag apply to one kind of page or one kind of element; which tags each kind of schema object accepts; how the four kinds of relationship between tables are classified; and - the part nothing else covers - a catalog of the kinds of page the engine can build, and exactly what it does for each one.

**The evidence that this is a gap, and that the subject is live.** The engine still has that catalog: `PageTypes` in the source declares Browse, List, Report, Add, Edit, Delete, View, Preview, Filter, Manage and OrderBrowse, and the test suite derives eight of them for 52 table variables and compares 416 forms against golden files. So this is a tested, shipping concept. Under `docs/`, the string "PageType" occurs twice: once as a parameter name in an operator signature, once in a plan row. `docs/reference/tags.md` documents the tags themselves, which is the other half; `docs/developers-guide/form-automation.md` describes derivation in prose. Neither says what a page type is or what the engine builds for each. Searching `docs/` for this document's distinctive phrases - the tag-precedence rule, the unknown-tag handling - returns nothing, and so does searching the 2007 edition, so it was dropped between 2004 and 2007 and never came back.

**Why a maintainer might decline it:** tag precedence and page-type behaviour are the kind of thing that drifts silently, and a wrong reference is worse than none. The golden tests are an executable specification of the same behaviour and cannot go stale.

## Arm 2 - the files the product reads and writes

`legacy/Documentation/DocBookManuals/DataphorFileStructures.xml`, 28.7 KB, titled *File Structures*.

A reference for every file extension the product uses, what each file is for, the settings each configuration file accepts, and the structure of each document format with worked examples.

**The evidence.** Three of the extensions it documents (`.dfi`, `.dft`, `.d4c`) do not occur anywhere under `docs/`. Two more occur only as single-sentence asides in an architecture page and in the migration notes. `docs/architecture/on-disk.md` is a short table of where a deployment's files live, not a format reference.

**Why this one is the most defensible to revive.** The project's own working rules name several of these file formats as things that must not change without a decision, because existing applications are made of them. A document that says what is in each of them is directly load-bearing for that rule, and today no such document exists. Against that: part of this file is settings tables for configuration files that have since been replaced, and anything revived has to be checked setting by setting against the code - which is most of the work.

## Arm 3 - the requests the form server answers

`legacy/Documentation/DocBookManuals/FrontendServerRequestTypes.xml`, 8.8 KB.

The form server answers three kinds of request - derive a form, fetch an image for a derived form, fetch a customization. This document gives the exact request grammar: the names, the arguments, what each argument means.

**The evidence.** None of its argument or request names occur under `docs/`.

**Why a maintainer might decline it:** this is an internal protocol between the product's own client and server, not a published interface, and the smallest of the four. If the client and server are always deployed together, nobody outside the project needs it written down.

## Arm 4 - using the visual form designer

`legacy/Documentation/DocBookManuals/FrontendDevGuidePartII.xml`, 25.9 KB.

Four subjects in one file: deploying and configuring a form application; a walkthrough of the visual form designer in the IDE (adding, moving and deleting elements, saving a customization rather than a new document, extending the designer with templates); an overview of how to extend the client with new element types; and a short architecture overview of how a form document becomes a window.

**The evidence.** None of its walkthrough's distinctive phrases occur under `docs/`.

**This arm is the one most likely to be worth only in part.** Its deployment half is written for a web-server setup this release does not ship, and one of its sections documents a client that has been removed - `docs-retire-removed-client-text` set the precedent for how such text is handled. The designer walkthrough is the valuable half, and it describes a designer that WS6 is currently bringing to the current .NET, so it cannot be checked until that work lands. Naturally downstream of `ui-dataphoria-runs` or whatever work item ends up proving the IDE works.

## Arm 5 - the catalog of user-interface patterns

Unlike the four above, this one is in the *2007* edition as well - `legacy/Documentation/Docs2.0/DevGuidePart3.xml` - and was dropped by the 2016 edition, which is why it is absent from `docs/`.

It is a catalog of eight shapes a pair of related tables can take and the interface each one should get: parent/extension, lookup/detail, recursive, generalized recursive (a tree), association, specialized association (a network), detail lookup, and generalized detail lookup. Each has an illustration, and the illustrations are in `legacy/Documentation/DocBookManuals/images/` under names like `Association Graphic.jpg` and `Tree Graphic.jpg`; none of them is under `docs/images/`.

`docs/developers-guide/form-automation.md` and `presentation-overview.md` carry the first two patterns - they define parent, extension, lookup and detail references - and stop there. So this arm is a four-pattern gap in a page that already exists, not a new page: the cheapest of the five to settle, and the one where a decision to decline is easiest to live with.

## Also noticed, and deliberately not an arm

- **`legacy/Documentation/DocBookManuals/DAEDevGuidePartII.xml`** has a *Server Architecture* chapter naming internal components (session manager, stream manager, lock manager, catalog manager) that does not literal-text match anything under `docs/architecture/`. It is not an arm because `docs/architecture/` was written fresh against the current code rather than converted, and a 2004 description of the same subsystems is more likely to mislead than to help. Someone writing a new architecture page may still want to read it.
- **`legacy/Documentation/DocBookManuals/CatalogReference.xml`** declares 29 scalar domains as one-line aliases, of which 17 (`System.Alpha`, `HostError`, `Huge`, `ULong`, `UInteger`, `UShort`, `SByte`, `Puny`, `Image`, `Comment`, `ObjectType`, `DataTypeType`, `ColumnType`, `Modifier`, `ReferenceAction`, `Catalog`, `PlanTree`) have no page under `docs/`. They are declarations, not prose, and whether the engine still has them is a question for the catalog, not for a 2004 document.
- **`legacy/Documentation/DocBookManuals/RealSQLBNF.xml`** is **not** a loss and needs no arm: all 49 of its grammar productions are present, name for name, in the comments of `src/engine/DAE/Language.RealSQL/RealSQLParser.cs`, which is also the only copy that cannot go stale. A reader who wants the full RealSQL grammar as a documentation page beside `docs/reference/d4-grammar.md` can transcribe it from there at any time; `docs/developers-guide/realsql.md`, which `docs-reference-chapters` landed, already carries a simplified form of it.
- The **two tutorials** for the .NET data-access components (`ADO.NETTutorial.xml`, `VS.NETComponentsTutorial.xml`) are arms on `feat-dac-developers-guide`, which already asks the same question about the same subject.

## Recovering a source after the tree is deleted

`docs-remove-old-sources` deletes `legacy/Documentation/` from the working tree. Nothing leaves history, but nobody will go looking, so:

    git show <commit before docs-remove-old-sources>:legacy/Documentation/DocBookManuals/DerivationReference.xml

`git log --diff-filter=D -- legacy/Documentation/DocBookManuals/DerivationReference.xml` finds that commit. The same recipe works for every path in the `files:` header, and for the illustration images named in arm 5.

The conversion tooling under `tools/docs-convert/` is deleted by the same ticket, so a revival converts the DocBook by hand or with a fresh pandoc invocation. All five sources are ordinary DocBook with no generated-content placeholders - the thing that made the reference material hard - so each is a plain pandoc job followed by an editorial pass, and the editorial pass is where the work is.
