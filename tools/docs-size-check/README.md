# docs-size-check — word-count gate for the architecture pages

Counts the words of each architecture documentation page and fails when one has grown past a limit, so that the architecture documentation stays hierarchical — `docs/architecture.md` a short overview, one page per subsystem under `docs/architecture/` — instead of growing back into one long page. It needs nothing beyond Python 3: one file, standard library only, no network.

## Run it

```
python tools/docs-size-check/check_size.py                    # the gate: the configured set, the default limit
python tools/docs-size-check/check_size.py --limit 0          # print every page's word count
python tools/docs-size-check/check_size.py --root C:\temp\tree
python -m unittest discover -s tools/docs-size-check          # its tests
```

Output is one line per finding, paths relative to the repository root with forward slashes as the link checker prints them, then a summary line (files checked, how many over the limit, how many excepted):

```
docs/architecture/x.md: 1623 words (limit 1500)              an error
docs/architecture/x.md: 1623 words (exception: <reason>)     reported, not an error
docs/architecture/x.md: missing                              an error: a literal path in the set that does not exist
checked set matched no files                                 an error
```

Exit code 0 when there is no error, 1 when there is, 2 for bad arguments (a `--root` that is not a folder, a negative `--limit`).

- No arguments is what the gate runs: the configured set, resolved against the repository root. The root is derived from the script's own path, so the tool runs from any working directory and needs no `.git`.
- `--root DIR` resolves the set against another folder instead. This is what lets the tests exercise the real configuration without writing into `docs/`.
- `--limit N` applies another limit for a what-if; the gate always uses the default. `--limit 0` reports every page with its count, which is the way to see how much of its budget each page has left.

`check(root, limit, checked, exceptions)` returns a report of the findings, so the tests do not have to parse the output.

## What is checked

The set is `CHECKED` in `check_size.py`: `docs/architecture.md` and `docs/architecture/**/*.md`, as repository-relative glob patterns. A pattern with no glob character (the root page) must exist — a page renamed without updating the list would otherwise drop out of the gate unnoticed — and a set that matches no files at all is an error. A glob that matches nothing is not.

The limit is `LIMIT`, 1,500 words today. The number is written down in three other places — the hierarchy rule in `AGENTS.md`, which names this symbol and file as well; the architecture-documentation rule in `tickets/rules/10-dataphor.md`; and WS8 of `docs/modernization-plan.md` — so a change to the limit has to be made in all of them. Each of the three names this folder beside the number, so `grep -rn docs-size-check` finds them all.

A **word** is a whitespace-separated token outside fenced code blocks, counted as `wc -w` counts it. Markdown punctuation counts too: a `##` marker is a word, so is a table row's leading `|`. This is deliberately a *budget* for how long a page may get, not a measurement of its prose — a page a few words either side of the limit is at the limit, and the number is not worth arguing with. Fenced code is excluded so that a diagram or a long code sample cannot force a page to split.

Fenced code blocks follow the same rule as the link checker (`tools/docs-link-check/check_links.py`), so that the two tools agree on what is prose: a line indented at most three spaces whose content starts with three or more backticks or tildes opens a block; a line of the same character repeated at least as many times and nothing else closes it (backticks do not close a tilde fence); an unclosed fence runs to the end of the file; and a backtick fence whose info string contains a backtick is inline code rather than a fence. That last guard matters, because mistaking inline code for a fence would silently *undercount* a page and let an oversized one pass. Unlike the link checker, a fence indented four spaces or more inside a list item is counted as prose: over-counting can only fail a page early, never let one through. HTML comment text is blanked before a line is tested, in the same order as the link checker does it, so a line of backticks inside an HTML comment is not a fence — were it taken for one, everything after it would drop out of the count — and a comment's own words are not prose.

Files are read as UTF-8 with or without a byte-order mark, and CRLF is handled. A file that is not valid UTF-8 is an error (`cannot read as UTF-8`), not a crash.

## When the check fails

**Split the page. Do not raise the limit, and do not add an exception.** The shape to split into is the one `docs/architecture.md` already has: a parent page beside a folder of the same name holding its children, with the parent keeping the overview and linking to each child. So a `docs/architecture/catalog-and-store.md` that outgrows the limit becomes a shorter `docs/architecture/catalog-and-store.md` beside `docs/architecture/catalog-and-store/` holding one page per part. The point of the limit is that the hierarchy keeps forming on its own; a raised limit or a new exception is the one change that stops it.

```
NOTE: catalog-and-store.md (~1,025 words) and hosting-protocol-discovery.md (~985) are the pages nearest
the limit and the two that grow; when one trips the check, split that page into a parent page beside a
folder of the same name rather than raising the limit.
```

`EXCEPTIONS` maps a page to the reason it is allowed over the limit. **It is empty, and is meant to stay empty.** It exists for a page whose length is not a choice — generated content, say — and an entry in it is a decision for a human to make, not a way past a red gate.

## What is deliberately outside the set

These documents are long and are not in `CHECKED`. Do not add them: the reasons are not about their size.

| Document | Words | Why it is outside |
|---|---|---|
| `docs/modernization-plan.md` | 36,895 | It is the anchor every ticket names; splitting it moves those anchors, which is a decision of its own. Its own split is ticket `debt-split-the-modernization-plan`. |
| `docs/migration-notes.md` | 18,573 | A running append-only record, not a hierarchy. Its own split is ticket `debt-split-the-migration-notes`. |
| `docs/perf-baseline.md` | 3,565 | A record of measurements with the procedure that produced them, not architecture prose. |
| `docs/coverage-report.md` | 3,270 | The same: the output of one measured run. |
| the converted user, developer and reference documentation | — | Its structure comes from the books it was converted from. |

Counts measured 2026-11-23 with this tool's own rule (`--limit 0` against a set holding them).

## CI

`.github/workflows/docs.yml` runs both commands — the check and these tests — on every push and pull request that touches `docs/`, this tool or the workflow itself. Nothing else gates: `.github/workflows/build.yml` ignores `docs/**` on purpose.
