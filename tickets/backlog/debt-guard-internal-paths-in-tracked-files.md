description: Add an automated check that stops network share paths and personal machine paths from being committed into this public repository's build files.
architecture: docs/modernization-plan.md#ws12-repository-hygiene
files: src/windows/Dataphoria/Dataphoria.csproj, src/windows/WindowsClient/Frontend.Client.Windows.Executable.csproj, tools/docs-size-check/check_size.py, .github/workflows/build.yml
difficulty: easy
tradeoffs: The tree is clean of these paths today and the two project files that carried them are both scheduled to be rewritten by the client port, so a maintainer may reasonably judge a new check to be guarding a door nobody walks through any more.
----
## Why this exists

This repository is public, and the rule for it is that no tracked file names a customer, an internal server, a schema or a library belonging to anyone but the project. Two project files were found carrying that kind of detail inside publish settings: a path to an internal file server, a product name that was really a customer's deployment name, and an absolute path on a developer's own machine. Both were scrubbed — the first by `hygiene-scrub-inherited-identifiers`, the second during its review.

The reason it is worth guarding rather than just fixing is how it got there. Nobody typed those paths deliberately; a publishing wizard in the development tools wrote them into the project file as a side effect of someone publishing a build once. That can happen again to any project file at any time, and nothing in the build or the test suite would notice.

Searching for the names themselves is not an option, because writing the list of names to search for into a tracked file is the very thing the rule forbids. The check therefore has to be name-free: it looks for the *shape* of a path that should never appear, not for any particular organization.

## What the check should assert

Two shapes, over tracked text files:

- A Windows network share path — the form that begins with two backslashes followed by a machine name. After the scrub there are none anywhere in the tracked tree, so the whole tree is a fair scope for this one.
- An absolute path rooted at a drive letter, in build files (project, properties, targets and solution files). Only build files, because prose and comments elsewhere legitimately use example paths.

Both need an exemption mechanism, and there is one case that needs it today: the vendored third-party installer toolkit under `Utilities/Wix/3.5/` carries generic example paths in its own targets files. That directory is deleted wholesale by `installer-msi`, so the exemption can be a single path prefix rather than a general facility.

## Where it should live

The repository already has the precedent for a small checked-in guard script — `tools/docs-size-check/check_size.py`, run after editing an architecture page — and for a test that reads the tree and fails when it disagrees with what is checked in (`ShippedMapTest` in the coverage tool's tests). Either shape is acceptable. Whichever is chosen, it has to run in continuous integration on every push, not only when somebody remembers it, because the failure it catches is silent by nature.

## What is deliberately out of scope

Finding *other* inherited names that are still in the tree. The search that cleaned these two files looked for one specific organization name, and a path-shaped check finds a different category again. Neither amounts to a general audit of the tracked tree for names belonging to outside parties, and no mechanical check can do that one — it needs a human reading, and it is not what this ticket asks for.
