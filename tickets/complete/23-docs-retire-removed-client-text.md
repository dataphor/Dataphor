description: The user and developer guides no longer describe the web browser client that this release does not ship, and the one place that records it ever existed is a short historical note.
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/users-guide/frontend-clients.md, docs/users-guide/architectural-overview.md, docs/users-guide/dataphor-server.md, docs/users-guide/starting-dataphor-applications.md, docs/users-guide/using-dataphor-applications.md, docs/users-guide/tutorial-movie-rental.md, docs/developers-guide/dataphor-product-anatomy.md, docs/developers-guide/presentation-overview.md, docs/modernization-plan.md
difficulty: easy
----
## What landed

Documentation only; no code, no tests. The convention: running text in the converted user and developer guides loses the two browser clients release 3.x shipped (the ASP.NET Web Client and the Silverlight client), and the fact that they ever existed survives in exactly one place.

`ticket(implement): docs-retire-removed-client-text` applied 19 edit sites across seven guide pages plus a work-item row in the plan, deleted `docs/images/rental-tut027.png` (the screenshot of the removed client) with the tutorial block that was its only reference, and replaced the `## Web Client` section of `docs/users-guide/frontend-clients.md` with a `## Clients That Are No Longer Shipped` section naming both removed clients and linking `docs/migration-notes.md`.

The review pass fixed three further sites the implement pass missed, and annotated the plan row with the reason they were missed (see findings below).

## Deliberately left alone

- `docs/reference/error-codes.md` (prefix 204, *Web Client*) and `docs/reference/Error Message Prefix Codes.txt` (204, 206, 207): the error-prefix allocation record. Error codes are a compatibility surface, so a retired prefix stays listed and is never reused. Decided by `1-repo-delete-dead-trees`.
- `docs/users-guide/dataphor-frontend.md` lines 7 and 17: "the deployment and maintenance characteristics of a web-based solution" and "in much the same way that a web browser requests HTML documents" are similes about the Windows client, not claims that a web client ships.
- `docs/users-guide/dataphor-service.md` line 160: names `allowsilverlightclients` as a setting that no longer exists. Already past tense and already correct.
- `docs/images/dataphor-overall-simplified.svg`, the overview diagram, which still draws a *Web Client* box and a *Browser* box. Filed as `bug-overview-diagram-shows-removed-client` in `tickets/backlog/`: editing a hand-drawn vector diagram is judged by looking at the rendered result, which is a different job. Until it lands, a reader of `architectural-overview.md` meets prose with no web client and a picture with one.

## Review findings

**Read:** the full implement diff with no reference to the handoff first; then every file it touched, every other page under `docs/users-guide` and `docs/developers-guide` that a broader sweep flagged, and `docs/migration-notes.md` to confirm the new note's link lands on text that answers it.

**Found and fixed inline — three missed sites, one root cause.** The implement pass swept for the product's own names (*Web Client*, *Silverlight*, *WebClient*, *ASP.NET*, *browser client*, *web-based*), which is why it reported a clean three-hit residue. That sweep cannot see a paraphrase. Three sites described the removed client without naming it; all three are prose-only fixes:

- `docs/users-guide/using-dataphor-applications.md` line 11 — "Dataphor applications typically have at least a Microsoft Windows user interface, and a Web user interface", a flat claim that the product ships a web user interface, on the chapter page whose own child `starting-dataphor-applications.md` the implement pass did edit. Rewritten to name the Windows client as what 4.0 ships, keeping the point the following clause was making: every client consumes the same user interface descriptions, so the look-and-feel is the same whichever client presents it.
- `docs/developers-guide/presentation-overview.md` line 13 — "displaying the application in a different platform such as Microsoft Windows, or over the World-Wide Web", eighteen lines above the paragraph this ticket rewrote to say 4.0 ships only the Windows client. Now "on a particular platform; release 4.0 ships the Microsoft Windows client."
- `docs/developers-guide/presentation-overview.md` line 147 — "from a Microsoft Windows based machine, over the Web, or any other platform that has a Dataphor Frontend Client". The sentence is about client-independent form definitions, and its "any other platform" clause carries that point on its own; the "over the Web" item is the only part asserting that a web client exists. Dropped, clause kept.

The class, not the three instances, is the finding: a documentation sweep driven by product names under-reports. Parked as one clause on the `docs-retire-removed-client-text` row of the WS8 work-item table in `docs/modernization-plan.md`, where the remaining guide passes will read it — sweep for the capability as well as for the name. No `NOTE:` comment, because there is no code site, and a prose page is the wrong home for a note aimed at the next ticket rather than at a reader.

**Checked and accepted — the two judgment calls the handoff flagged.**

- *The n-tier deployment list* in `docs/users-guide/dataphor-server.md`. Deleting the Web Client Server bullet would have left the n-tier scenario with a server and a storage system and no presentation tier at all; the implement pass replaced it with a Windows Client bullet parallel to the one the client-server scenario already carries. Correct, and consistent with the roles list fifteen lines above it, which now reads Dataphor Server / Storage Device(s) / Windows Client: the architecture does support that deployment and the Windows client is its presentation tier. It is the one added product claim in the diff, and it is a true one. Kept as written.
- *The tutorial's surviving "With the Windows Client:" lead-in.* Now alone where it used to be one of a pair. It reads as a caption over the screenshot that follows, and the sentence after it still makes sense, so there is nothing to fix; left alone rather than churn the line.

**Checked and found nothing.**

- Residual sites: the name sweep over both guides returns exactly the three intended hits (the two similes above and the new historical note). A wider sweep for *over the Web*, *World-Wide Web*, *web user interface*, *web interface* and *web-hosted* over both guides plus `docs/architecture/`, `docs/glossary.md` and `docs/reference/` returns only the new note and the error-prefix row.
- Retired anchors: `frontend-clients.md#web-client` and `#running-the-rental-application-with-the-windows-or-web-client` are referenced nowhere, and the link checker reports broken fragments, so nothing points at either. The two pages listing the *Frontend Clients* chapter (`docs/README.md`, `docs/users-guide/product-tour.md`) link the page, not its sections.
- Orphaned images: every one of the 97 files in `docs/images/` is referenced by some page, so `rental-tut027.png` was the only casualty and it took its reference with it. The link checker does not report orphans, so this property is held by a grep, not a tool.
- Source hygiene: both files the review pass edited are still CRLF with no byte-order mark, verified by byte dump. The implement pass's correction to the ticket's hygiene note was right — every page under `docs/` is CRLF, not LF.
- Nothing to split: the diff touches nine files, each by a few lines, and no page approaches a size limit.

**Conditional concerns: none.** Nothing here is "fine now, breaks if X" — documentation prose either matches the product or does not.

## Tests

None, deliberately, and none were cut: the diff contains no `.cs`, `.d4`, `.d4l` or project file, so there is no behaviour to characterize. A test that grepped the guides for "Web Client" would fail on the legitimate historical note this ticket just wrote — it would pin the wrong thing. The gates are the link checker and the size checker, run by hand here and in CI once `docs-link-check-ci` lands.

## Validation run

No `dotnet build` and no test filters: documentation only.

```
python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
90 files scanned, 1079 links checked, 0 errors, 203 pending, 12 external

python tools/docs-size-check/check_size.py
13 files checked, 0 over the limit, 0 excepted
```

Both identical to the `e414e316` baseline. That is the expected result and not a sign the checker missed the change: the deleted tutorial block removed one image link and the new note added one `../migration-notes.md` link, so the totals net to zero, and no page was added or deleted. The size checker gates `docs/architecture/` only and is unaffected; it was run because the documentation workflow runs it.

## Known gaps

- The overview SVG still shows the removed client. Ticketed as `bug-overview-diagram-shows-removed-client`.
- `libraries/Frontend/Documents/Web.NodeTypes.dnt` and the `ClientTypes` rows in `libraries/Frontend/Documents/Register.d4` still ship, so the Edit Applications *Client Types* tab may still offer a web client type to a reader following the tutorial. `hygiene-retire-web-silverlight-client-types` owns that. The tutorial's "Add the Windows Client Type" is correct advice either way, which is why this ticket took no prereq on it.
- Conversion defects met nearby were left to the pass that owns them: `dataphor-server.md` line 87 still begins "ecause the Dataphor Server..." (a dropped capital inherited from the AsciiDoc source), and `d4-comparison-with-sql.md` has run-together headings.
- This is the first pass at making the converted guides describe 4.0 rather than 3.x, and it covers the removed browser clients only. Other 3.x-isms in the guides are untouched and unsurveyed.
