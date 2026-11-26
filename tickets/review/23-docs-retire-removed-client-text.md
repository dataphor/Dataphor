description: The user and developer guides no longer describe the web browser client that this release does not ship; review the cuts for readability and for anything missed.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/users-guide/frontend-clients.md, docs/users-guide/architectural-overview.md, docs/users-guide/dataphor-server.md, docs/users-guide/starting-dataphor-applications.md, docs/users-guide/tutorial-movie-rental.md, docs/developers-guide/dataphor-product-anatomy.md, docs/developers-guide/presentation-overview.md, docs/modernization-plan.md
difficulty: easy
----
## What landed

All 19 edit sites from the implement ticket's table, applied as the ticket specified. Documentation only; no code, no tests. The convention is the one the ticket decided: running text loses the removed browser clients, and the fact that they existed survives in exactly one place.

**The one surviving note** is a new `## Clients That Are No Longer Shipped` section at the end of `docs/users-guide/frontend-clients.md`, replacing the `## Web Client` section. It names both removed clients (Web and Silverlight), says 4.0 ships neither, and links `../migration-notes.md`.

**The rewritten example.** `docs/developers-guide/presentation-overview.md` line 31 kept the mechanism and dropped the Windows/Web comparison: the Source entry is client-neutral, the TextBox entry is a Windows Forms edit control and therefore client-specific, 4.0 loads only the Windows client's entries, and the table keeps its per-client shape because another platform's client supplies its own component type definition documents. Wording is mine, not the ticket's suggested text; check it reads cleanly in context and that a reader still learns that an entry *can* be client-specific.

**Deleted:** `docs/images/rental-tut027.png`, the screenshot of the removed client, together with the tutorial block that was its only reference.

**Plan table:** a `docs-retire-removed-client-text` row added to the WS8 work-item table in `docs/modernization-plan.md`, marked **Done**, no prereq, difficulty easy.

## Two judgment calls to look at first

**The n-tier deployment list.** `docs/users-guide/dataphor-server.md` listed three roles for the n-tier scenario; deleting the Web Client Server bullet left two, both server-side, so the scenario had no client tier at all and was no longer recognisably n-tier. Rather than leave that, I replaced the bullet with a Windows Client bullet parallel to the one the client-server scenario already carries ("installed on each Client machine accessing the Dataphor Server"). That is one sentence more than a pure deletion. The alternative readings are: leave two bullets and accept that the n-tier scenario reads as server-and-storage only, or delete the whole n-tier paragraph. I think the added bullet is right - the architecture still supports that deployment and the Windows client is still its presentation tier - but it is the one place where I wrote new product claims rather than removing old ones, so it is worth a second opinion.

**The tutorial's surviving "With the Windows Client:" lead-in.** It used to be one of a pair ("With the Windows Client:" / "With the Web Client:"); now it is alone and reads slightly oddly as a lead-in over a single screenshot. The ticket did not ask to touch it and I left it. Minor prose nit if you want it.

## What was deliberately left alone, and why

These are the ticket's "what stays" list; a reviewer re-running the grep will see them and should *not* treat them as misses:

- `docs/reference/error-codes.md` (prefix 204, *Web Client*) and `docs/reference/Error Message Prefix Codes.txt` (204, 206, 207) - the error-prefix allocation record. Error codes are a compatibility surface; a retired prefix stays listed so it is never reused. Decided by `1-repo-delete-dead-trees`.
- `docs/users-guide/dataphor-frontend.md` line 7 - "all the deployment and maintenance characteristics of a web-based solution" is a simile about the Windows client, not a mention of the Web client.
- `docs/users-guide/dataphor-service.md` line 160 - names `allowsilverlightclients` as a setting that no longer exists; already past tense and already correct.
- `docs/images/dataphor-overall-simplified.svg`, the overview diagram, which still draws a *Web Client* box and a *Browser* box. Filed separately as `bug-overview-diagram-shows-removed-client` in `tickets/backlog/`; editing a hand-drawn vector diagram is a different job, judged by looking at it. No caption was added about it, per the ticket.

## One factual correction to the ticket

The ticket's *Source hygiene* note said every file under `docs/` is "UTF-8 with no byte-order mark and LF line endings". The no-byte-order-mark half is right; the line endings are **CRLF**, not LF (`file docs/users-guide/*.md` reports CRLF line terminators on all of them, and `.gitattributes` deliberately sets no `text=auto`, so nothing normalizes them). I preserved CRLF and no byte-order mark in every file I touched, per the AGENTS.md rule to preserve whatever each file has - verified after editing with `file` and a three-byte head dump on each. No durable note was added for this: the AGENTS.md *Formatting* rule already says "preserve whatever each file has", which is the correct instruction either way.

## How to validate

Documentation only, so there is nothing to build or run. No `dotnet build` and no test filters: the diff contains no `.cs`, `.d4`, `.d4l` or project file.

**Re-measure the sites.** From the repository root:

```
grep -rIn -iE "silverlight|web client|webclient|web-based|asp\.net|browser client" docs/users-guide docs/developers-guide
```

Expect exactly three hits, all intended: `dataphor-frontend.md:7` and `dataphor-service.md:160` (the two "what stays" sites above), and the new historical note on `frontend-clients.md`. Any fourth hit is a miss. The baseline before this work was 21 hits across 9 files.

**Orphaned-image property.** `grep -rIn "rental-tut027" docs/` must find nothing - the file is deleted and so is its only reference. The link checker does **not** report orphaned images, so this property is held only by that grep. Worth a wider spot check if you want it: every other file in `docs/images/` should still be referenced by some page.

**Link check** - ran clean:

```
python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
90 files scanned, 1079 links checked, 0 errors, 203 pending, 12 external
```

Identical to the `e414e316` baseline, which is the expected result and not a sign the checker missed the change: the deleted tutorial block removed one image link and the new note added one `../migration-notes.md` link, so the totals net to zero. No page was added or deleted, hence the same 90 files.

**Size check** - `python tools/docs-size-check/check_size.py` reports `13 files checked, 0 over the limit, 0 excepted`. It gates `docs/architecture/` only and is unaffected; run because the docs workflow runs it.

**Retired anchors.** Deleting `## Web Client` retires `frontend-clients.md#web-client`, and renaming the tutorial heading retires `#running-the-rental-application-with-the-windows-or-web-client`. `grep -rIn "running-the-rental-application\|#web-client" docs/` finds nothing, and the link checker reports broken fragments, so both are confirmed unreferenced.

## Tests

None, deliberately. There is no behaviour here; the gate is the link checker, which runs by hand now and in CI once `docs-link-check-ci` lands. A test that grepped the documentation for "Web Client" would fail the moment someone wrote the legitimate historical note that this ticket just added, so it would pin the wrong thing.

## Known gaps

- The overview SVG still shows the removed client. Known, deliberate, ticketed in `backlog/` as described above. A reader of `architectural-overview.md` therefore meets prose with no Web client and a diagram with one, until that ticket lands.
- `libraries/Frontend/Documents/Web.NodeTypes.dnt` and the `ClientTypes` rows in `libraries/Frontend/Documents/Register.d4` still ship, so the Edit Applications *Client Types* tab may still offer a Web client type when a reader follows the tutorial. `hygiene-retire-web-silverlight-client-types` owns that. "Add the Windows Client Type" is correct advice either way, which is why this ticket took no prereq.
- Faithful-conversion defects nearby were left as-is, per the ticket: `dataphor-server.md` line 87 still begins "ecause the Dataphor Server..." (a dropped capital inherited from the AsciiDoc source), and `d4-comparison-with-sql.md` has run-together headings. A separate conversion-defect pass owns these; do not file them from here.
- This is the first pass at making the converted guides describe 4.0 rather than 3.x. It covers the removed browser clients only. Other 3.x-isms in the guides are untouched and unsurveyed.
