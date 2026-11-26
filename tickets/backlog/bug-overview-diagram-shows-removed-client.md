description: The architecture diagram at the front of the user guide draws a web browser client that this release does not ship, and the little mock windows inside it are filled with leftover nonsense text from whoever drew it.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/images/dataphor-overall-simplified.svg, docs/users-guide/architectural-overview.md
repro: verified
severity: cosmetic
likelihood: normal-use
tradeoffs: It is a cosmetic flaw in one picture on one page, and redrawing a hand-made vector diagram risks making it worse; a maintainer may reasonably leave it until someone redraws the architecture diagrams properly, or decide the whole diagram should be replaced rather than patched.
----
## What is wrong

`docs/images/dataphor-overall-simplified.svg` is the diagram at the top of `docs/users-guide/architectural-overview.md`, the first picture a reader of the user guide meets. It came across byte for byte in the documentation conversion (SVGs were copied, not regenerated), so it is the 3.x drawing. Two problems, both at the same file:

1. **It draws clients that 4.0 does not ship.** The diagram has a box labelled **Web Client** and a box labelled **Browser**, with a `<HTML></HTML><XML></XML>` label between them. 4.0 removes the ASP.NET web client and the Silverlight client; `docs/migration-notes.md` records the removal. `docs-retire-removed-client-text` removes those clients from the running prose of the guides and deliberately leaves this file alone, because editing a hand-drawn vector diagram is a different job with a different failure mode - the result can only be judged by looking at it. So after that ticket lands, the page's prose and its picture disagree.

2. **The mock windows in it contain placeholder text.** Inside the small drawn "screens" at roughly x 80-185, y 250-335 (all `class="st6 st7 st59"` text nodes) the labels are: `alright`, `alright cool things`, `fun fish`, `shellfish`, `more fun`, `food for me`, `me is hungry`, `fish fry`. This is leftover filler from whoever drew the diagram. It is small on the page but it is real text in a published document, and it reads exactly as badly as it sounds when a reader zooms in.

**How this was found:** verified by reading the SVG's text nodes at commit `e414e316`, while planning `docs-retire-removed-client-text`:

```
grep -o '>[^<>]\{2,30\}<' docs/images/dataphor-overall-simplified.svg | tr -d '<>' | sort -u
```

The file is 49 KB of plain SVG (`viewBox="0 0 612 792"`, Illustrator-style `st*` classes), so it is editable as text - the geometry, not the format, is what makes this fiddly.

## What done would look like

The diagram shows only what 4.0 ships, and contains no filler text. Beyond that, the shape of the fix is open and worth a moment's thought rather than a reflex:

- **Patch it** - remove the Web Client and Browser boxes, their labels and their connectors, and replace the filler strings with something real (or remove those text nodes; the mock screens still read as screens without words in them). Cheapest, and keeps a diagram the guide has always had. The risk is a hole where the removed boxes were, and the only way to know is to look at the rendered result.
- **Redraw it** - the diagram is one of two places the documentation explains the three-layer architecture, the other being `docs/architecture.md` and its per-subsystem pages, which are current. A diagram drawn for 4.0 could serve both.
- **Drop it** - the surrounding prose already names each component and the page works without the picture. The weakest option: a reader meeting this architecture for the first time is helped by a picture.

Whoever takes this should look at the rendered SVG before and after. That is not optional here, and it is the reason this is not folded into a prose-editing ticket.

## Scope notes

- **Other images are not known to have this problem.** `docs/images/` holds 97 files, 9 of them SVG; only this one was examined. A sweep of the other SVGs' text nodes is cheap and belongs with this work.
- **No link breaks either way.** The image is referenced from exactly one page (`docs/users-guide/architectural-overview.md` line 9). If the diagram is dropped rather than fixed, delete the file too: every file in `docs/images/` is currently referenced by some page, which is a property the conversion's review established and worth keeping. Note that the link checker reports broken links but *not* orphaned images, so that check is a grep, not a tool.
- **This is a published-documentation defect, not a conversion defect.** The conversion was meant to be faithful to the 3.x product and was. Both problems are in the 3.x original.
