description: The product has no reference describing the elements an application author can put in a user-interface description - that reference used to be produced automatically from the client and no copy of it survives, so authors currently have to read the source.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/reference/frontend-nodes/README.md, src/frontend/, docs/developers-guide/presentation-layer.md, docs/developers-guide/form-automation.md
tradeoffs: Writing it by hand duplicates what the source already states and goes stale the first time a node changes, and generating it means a second documentation generator beside the one WS8 already plans for the operator reference - a maintainer may reasonably decide that application authors reading the node classes is good enough.
----
## What this is

Applications in this product are built from user-interface descriptions - a document naming elements (a form, a grid, a frame, a notebook page) and their properties. There has never been a hand-written reference for those elements. The published documentation had one, but it was produced automatically from the client's own interfaces at publishing time, and no copy of the output survives: the two source files that would hold it (`legacy/Documentation/Docs2.0/DilReference.xml` and `legacy/Documentation/DocBookManuals/DilReference.xml`) are 300-byte stubs that say "This sect1 is generated through automation", and the published help file has no such pages at all.

`docs-convert-legacy-reference` found this while converting the rest of the reference and left `docs/reference/frontend-nodes/README.md` as a short honest page: it says the element reference is generated, points at the guide chapters that cover forms and frames, and points at the source. The one documentation link that wanted an element page now goes there. So nothing is broken - there is simply no element reference.

## Why someone would want it

An application author writing or customising a form document chooses elements and sets their properties. Today the way to find out what elements exist and what properties each takes is to read the node classes under `src/frontend/`. That is fine for the people porting the product and poor for the people the product is for, and it is the only part of the reference that has no pages at all.

## What it would have to cover

One entry per element an author can name in a user-interface description: what it is for, what it may contain, each of its properties with its type and default, and which of them the derivation engine sets. The authority is the client's node types, which carry this in their declarations and attributes - which is exactly why it was generated rather than written.

## Open questions a plan would have to settle

- **Generated or written.** Generated keeps it true and costs a second generator; written costs a reader every time a node changes. `docs-reference-generator` is already building a Markdown emitter for the operator reference from a running server - whether that machinery extends to the client's node types, which are C# classes rather than catalog objects, is the first thing to find out, and the answer decides the size of the work.
- **Where it belongs.** The documentation set, or the IDE's own component help, or both. The designer in the IDE already shows properties; a reference that duplicates it badly would be worse than none.
- **Which elements.** The Windows client's node set is the one that ships; the removed browser clients had their own. Only what ships should appear.
- **Whether the derivation engine's own tags belong here or in `docs/reference/tags.md`**, which already documents the metadata tags.

## Worth knowing before planning it

- `docs/developers-guide/presentation-layer.md` and `docs/developers-guide/form-automation.md` describe how user interfaces are derived and customised, in prose. A new reference complements them; it must not repeat them.
- The Windows client is still being brought to .NET 10 by WS6 work items. A node reference written before the client runs would describe something nobody has exercised, so this is naturally downstream of `ui-windows-client-runs`.
- The stub files named above are deleted by `docs-remove-old-sources`. Nothing is lost - they contain no content - but anyone looking for a source will not find one, which is the point of this ticket.
