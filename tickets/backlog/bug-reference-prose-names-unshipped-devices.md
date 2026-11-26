description: Two published reference pages still talk about Oracle and DB2 database support that this release does not ship, and the page that lists the settings for each database device never mentions PostgreSQL, which is one of the two databases it does ship.
prereq: docs-remove-old-sources
architecture: docs/modernization-plan.md#ws8-documentation
files: docs/reference/libraries/SQLDevice.md, docs/reference/libraries/SimpleDevice.md, docs/reference/libraries/README.md, docs/reference/, docs/developers-guide/realsql.md, docs/developers-guide/d4-programming-topics.md
repro: verified
severity: wrong-result
likelihood: normal-use
tradeoffs: These are three sentences of 2007 prose on two pages, and a maintainer may reasonably decide that a reader who sees "Oracle device settings" as plain grey text rather than a link already understands it is gone - in which case the only change worth making is adding the missing PostgreSQL entry.
----
## What is wrong

The library reference pages were converted from the 2007 documentation, which described a product that shipped database support for Microsoft SQL Server, Oracle, DB2 and SAS. This release ships Microsoft SQL Server and PostgreSQL only. Three sentences carried over from that text now tell a reader something that is not true:

- **`docs/reference/libraries/SQLDevice.md`, in the *Settings* section.** "For a list of additional settings available for specific SQL devices see:" is followed by three bullets - a working link to the SQL Server settings, then "Oracle device settings" and "DB2 device settings" as plain text. Neither of those exists. More to the point, there is **no PostgreSQL bullet**, so the one page a reader goes to for "what settings does my device accept" tells them nothing about one of the two databases the product supports. That is the half of this that costs a reader real time.
- **`docs/reference/libraries/SimpleDevice.md`, in the *Definition* section's *See Also*.** The line reads "SQLDevice | MSSQLDevice | OracleDevice | DB2Device", the first two as links and the last two as plain text naming nothing.

A reader has no way to tell these apart from a page that is simply missing. `docs/reference/libraries/README.md` records in prose that these sentences are stale 2007 text, which is honest but is not where the reader meets them.

## Why this is not simply edited

Both pages are **generated**. They are produced from the DocBook sources under `legacy/Documentation/` by `tools/docs-convert/`, and the conversion is deliberately reproducible: rerunning it over the committed tables reproduces every published page byte for byte, which is the property the remaining conversion tickets rely on to prove they changed nothing they did not mean to. A hand-edit to one of these pages is silently thrown away by the next rerun.

The converter can retarget or unlink a *link* (`tools/docs-convert/reference-links.tsv` does exactly that for the chapters of the unshipped libraries). It has no way to delete or rewrite a sentence, and giving it one would be building a prose-editing mechanism into a tool that is deleted shortly afterwards.

So the fix belongs **after** the conversion sources are gone. `docs-remove-old-sources` deletes `legacy/Documentation/` and `tools/docs-convert/`; from that commit on, every page under `docs/` is an ordinary Markdown file that can be edited like any other, and this becomes a short editorial pass.

## What the fix should cover

Not just the three sentences above. By then four more conversion tickets will have landed pages from the same 2007 sources, so the work is a sweep, not a patch:

- Search all of `docs/reference/` for prose naming a component this release does not ship - the Oracle, DB2, SAS, ADO and Microsoft Oracle libraries are the known set, and `docs/reference/libraries/README.md` names where they are parked.
- Cut or rewrite each mention so the sentence around it still reads correctly. Where a list loses entries, check whether the sentence introducing it still makes sense.
- Add the PostgreSQL entry to the SQLDevice settings list. `docs/reference/libraries/PGSQLDevice.md` currently has no settings section of its own; whoever does this should decide whether to write one (the device's own settings - `Server`, `Port`, `Database`, `SearchPath` - appear only in `libraries/PGSQLDevice/Source/.../PostgreSQLDeviceSession.cs`) or to point the bullet at the generic settings table and say the device adds those four.
- Remove the paragraph in `docs/reference/libraries/README.md` that apologises for these sentences, since it will no longer be true.

## Four more instances, on two pages outside `docs/reference/` - found by `docs-reference-chapters`

That ticket converted two guide chapters from the same 2007 sources, into `docs/developers-guide/`, and hit this ticket's situation exactly: statements the shipping code contradicts, on a generated page that a rerun would overwrite. **So the sweep above is a sweep of `docs/developers-guide/` too, not only of `docs/reference/`.** The four it noticed, each verified against the code:

- **`docs/developers-guide/realsql.md`, *Operator Invocation*, says that "operators which take variable arguments may not be invoked, because RealSQL has no imperative mechanism for declaring a variable".** The parser accepts them: `ActualParameter` in `src/engine/DAE/Language.RealSQL/RealSQLParser.cs` reads an optional `var` keyword and builds a by-reference parameter, and `var` is in the language's keyword table. This is the one of the four that could cost a reader real time, because it tells them a thing they can do is impossible. Whoever fixes it should decide which side is right - the sentence may describe an intent that the parser never enforced, in which case the engine is the thing to question and this becomes a `fix/` ticket rather than an edit.
- **The same page's grammar is a simplified one.** Its `<binary expression>` and `<arithmetic expression>` productions collapse what the parser actually implements as nine precedence levels (logical operator clause, logical binary and ternary, logical-and, bitwise binary, comparison, additive, multiplicative, exponent, unary). The accurate grammar is in that parser's own comments, all 49 productions. Not wrong, but a reader deriving precedence from this page will get it wrong; a sentence saying the page gives a simplified form and naming where the full one is would settle it.
- **`docs/developers-guide/d4-programming-topics.md` names two accessor classes without the suffix the engine uses** - `System.ScalarReadAccessor` and `System.ScalarWriteAccessor`, where `src/engine/DAE/Schema/DataTypes.d4` writes `System.ScalarReadAccessorNode` and `System.ScalarWriteAccessorNode`. Every other class name in that block matches the engine exactly, so this reads as a typo in the 2007 source rather than a change since.
- **The same page carries four plain source defects** that a rerun faithfully reproduces: an unterminated quote in the `System.String` definition (`class "System.SystemStringUTF8Node,`), prose announcing "seven properties" above a list of six, a `DateTime` selector example passing seven arguments to a six-property representation, and the spellings "ommitted" and "separataed" (the last on the RealSQL page).

The last two items are copy-editing and could be declined without cost. The first is the one that matters.

## Expected behaviour

Every sentence in the reference and in the converted guide chapters describes something a reader of this release can actually use; the list of device-specific settings covers both shipped database devices; and no code sample in them is syntactically broken.
