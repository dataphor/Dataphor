# Writing documentation

The documentation is GitHub-flavoured Markdown under `docs/`, and it is checked. This page says what the conventions are, where a new page goes, and how to run the two checks before you push.

It replaces the old *Authoring Documentation* appendix, which described editing AsciiDoc sources through GitBook. Those sources were converted to Markdown and the editing workflow with them: there is no build step, no generator and no toolchain to install. A page is a file; you edit it and commit it.

## File conventions

- **GitHub-flavoured Markdown.** It renders on GitHub, which is where it is read. Tables, fenced code blocks and task lists are all available; nothing else is.
- **UTF-8, with no byte-order mark.** Every Markdown file under `docs/` is that way today.
- **LF line endings.** They are LF in the repository. On Windows with `core.autocrlf=true` your working copy shows CRLF and commits back as LF, which is correct and needs nothing from you — and `.gitattributes` deliberately declares no `text` rule for Markdown, so that committing a page cannot renormalize anything else.
- **One paragraph per line; no hard wrapping.** Editors soft-wrap, and diffs stay readable: a reworded sentence shows as one changed line rather than as a reflowed block. Do not wrap a paragraph at 80 or 100 columns.
- **Links are relative, and the letter case has to match.** `../reference/error-codes.md`, not an absolute path and not a GitHub URL. The link check runs on a case-sensitive file system even though a Windows checkout is not, so `Error-Codes.md` is an error there and silently works for you.
- **Images are PNG or SVG, under `docs/images/`.**

## Where a page goes

| Folder | What belongs there |
|---|---|
| `users-guide/`, `developers-guide/`, `reference/` | The three converted books. A page here is part of the product documentation: what Dataphor does and how to use it. `reference/` is the per-subject material — the D4 grammar, tags, error codes, the library references. |
| `architecture/` | How this codebase is built: one page per subsystem. See the rule below. |
| `admin/` | Installing, configuring and running a deployment. Not written yet; the plan's `docs-admin-guide` work item adds it. |
| `contributing/` | This folder: how to build, test and document the project. |
| `images/` | Every image, for every page. |

Some documents sit at the top of `docs/` because they belong to no book: the [modernization plan](../modernization-plan.md), [architecture.md](../architecture.md), the [migration notes](../migration-notes.md), the measured [coverage](../coverage-report.md) and [performance](../perf-baseline.md) records, and the books' shared [glossary](../glossary.md) and [bibliography](../bibliography.md). [docs/README.md](../README.md) indexes everything; add a new page to it, and to its folder's own `README.md` where there is one.

## The architecture pages have a shape, and it is enforced

`docs/architecture.md` is a short overview. The detail lives in one page per subsystem under `docs/architecture/`, and **new architecture text goes on the child page that owns the subject, never in the root.**

Every page of that set is capped at 1,500 words. A page that outgrows the cap is **split** — into a parent page beside a folder of the same name holding its children, the shape `architecture.md` already has beside `architecture/`. The cap is not raised, and the page is not added to the checker's exception list, which is empty and meant to stay that way. The number lives in one place, `LIMIT` in `tools/docs-size-check/check_size.py`, and three documents state the rule, so changing it is a deliberate act in four files rather than an edit in one.

Only the architecture set is capped. That is not licence to write a long page anywhere else: two short pages are easier to find things in than one long one.

## The two checks

Both are standard-library Python 3 — no packages, no network — and both run in about a second:

```
python tools/docs-link-check/check_links.py docs --pending tools/docs-convert/legacy-links.tsv
python tools/docs-size-check/check_size.py
```

The first checks every link and image reference under `docs/` and reports the ones that point at nothing: a missing file, a file name with the wrong letter case, a heading anchor that no heading produces, a path that leaves the repository. The expected result today is **0 errors** and a couple of hundred *pending*; `--pending` exempts the links into the reference pages that have not been converted yet, and that argument goes away with the list when they are. Its [README](../../tools/docs-link-check/README.md) documents the options and exactly what counts as a link.

The second is the word-count cap above. The expected result is **0 over the limit**.

[`.github/workflows/docs.yml`](../../.github/workflows/docs.yml) runs both on every push and pull request that touches `docs/` or either checker. It is a separate workflow from the main build, which ignores `docs/**` on purpose.

Run the link check after moving, renaming or linking a page. It is also the completeness test for a documentation change: a page you added but linked from nowhere is not reported, but a link you repointed at the wrong place is.

## Two conventions that reach outside `docs/`

**`NOTE:` comments.** Two kinds of knowledge belong at the code site rather than in a document, and both are tagged `NOTE:` so that the whole set is one `grep` away:

- *A conditional concern* — something that is fine now and only becomes work if a condition is met later: `// NOTE: re-counts every entity per save; if scenarios get large, keep a running count.`
- *An accepted tradeoff* — a finding that was weighed and declined on purpose. State what was declined, why, and what would make it worth revisiting: `// NOTE: accepted tradeoff — config reload re-parses the whole file on every change; simplicity weighed over incremental parsing; revisit if reload shows up in a profile.`

Both exist so that the next reader meets the decision where they would otherwise re-discover the problem. Where the concern is architectural and has no single code site, it goes as a bullet on the relevant page under `docs/architecture/` instead.

**Migration notes.** A change that an application, a deployment script or a custom C# extension could notice — including any public type or member that an extension builds against — gets an entry in [docs/migration-notes.md](../migration-notes.md) **in the same commit** as the change. That document is what an existing deployment reads to find out what will break; an entry added later is an entry someone has already been surprised by.
