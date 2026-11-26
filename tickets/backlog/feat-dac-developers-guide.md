description: A 70,000-character guide for developers writing .NET applications against this product's data-access components exists in the oldest documentation tree, was dropped from the product's documentation twenty years ago, and is about to be deleted along with that tree - decide whether it is worth reviving before it goes.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/reference/data-access-components.md, docs/developers-guide/, legacy/Documentation/DocBookManuals/DACDevP1UsingtheDAC.xml, legacy/Documentation/DocBookManuals/DACDevGuide.xml
tradeoffs: The guide is from 2002, describes a Visual Studio .NET designer workflow and an ADO.NET comparison that have both moved on, and whoever dropped it from the 2006 documentation presumably had a reason - reviving it could cost more editing than writing a fresh page, and a maintainer may reasonably prefer to leave it in history.
----
## What this is

This product ships a set of .NET components for building Windows applications against it - a data source, a data view, data-bound controls - as an alternative to the standard .NET data-access libraries. The current documentation for them is one page, `docs/reference/data-access-components.md`, which `docs-convert-legacy-reference` wrote as a short signpost: what the components are, which types a reader starts from, where they live in the source, and which guide chapters touch them.

There used to be a whole manual. `legacy/Documentation/DocBookManuals/DACDevP1UsingtheDAC.xml` is 70 KB of hand-written prose from 2002 - an architectural overview, how the components buffer and navigate data, how that differs from the standard .NET approach and why, and how to use them from the Visual Studio designer. It is assembled by `DACDevGuide.xml` into a manual of its own.

**It was dropped.** The 2006-2007 documentation set (`legacy/Documentation/Docs2.0/`) does not include it, and the published help file does not either; what the help file has under this heading is generated API reference, which is what the page's stub stood in for. So this is not material the conversion missed - it is material the product stopped shipping, found again twenty years later.

## The decision this ticket asks for

Is a developer's guide for these components worth having in the 4.0 documentation, and if so, is the 2002 text the place to start?

Arguments for reviving it: it is the only sustained explanation of *why* these components work the way they do, it is hand-written prose rather than generated reference, and nothing in the converted Developer's Guide replaces it. Arguments against: it is 24 years old, its worked examples are Visual Studio .NET designer walkthroughs, its central comparison is with a version of ADO.NET nobody uses, and someone already decided once that it was not worth shipping.

A middle answer is also available and may be the right one: take only the architectural overview - the first chapter, which explains the buffering and navigation model - and fold it into `docs/developers-guide/` or expand `docs/reference/data-access-components.md` with it, and leave the designer walkthroughs in history.

## Recovering the source after the tree is deleted

`docs-remove-old-sources` deletes `legacy/Documentation/` from the working tree. Nothing is lost from history, but nobody will go looking, so the paths are recorded here:

    git show <commit before docs-remove-old-sources>:legacy/Documentation/DocBookManuals/DACDevP1UsingtheDAC.xml
    git show <commit before docs-remove-old-sources>:legacy/Documentation/DocBookManuals/DACDevGuide.xml

`git log --diff-filter=D -- legacy/Documentation/DocBookManuals/DACDevGuide.xml` finds that commit. The other files of the set (`DACDevIntroduction.xml`, `DACDevP2ExtendingTheDAC.xml`, `DACDevGuideStandAlone.xml`) are all under 3 KB and are assembly and stub files.

## Worth knowing before planning it

- The conversion tooling under `tools/docs-convert/` is deleted by `docs-remove-old-sources` too, so a later revival converts the DocBook by hand or with a fresh pandoc invocation. The file is a single `<part>` with ordinary DocBook chapters and sections and no generated-content placeholders, so it is a plain pandoc job - far simpler than the reference material was.
- Check what the converted Developer's Guide already says first. `docs/developers-guide/dataphor-product-anatomy.md` and `docs/developers-guide/presentation-overview.md` both mention these components, and a second explanation that disagrees with them would be worse than the signpost page that exists now.
- These components are built for Windows Forms, which WS6 keeps. If that ever changes, this ticket goes with it.
