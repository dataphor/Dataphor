# Dataphor Reference

![Dataphor Logo](../images/dataphor-logo.png)

## Summary

Reference material for the Dataphor product: the D4 language's own grammar and vocabulary, the libraries this release ships, the metadata tags that guide user-interface derivation, the error-code ranges, the behaviour switches the build is compiled with, and overviews of the two .NET layers an application reaches the server through. The guides are elsewhere - the [User's Guide](../users-guide/README.md) is the product from a user's side and the [Developer's Guide](../developers-guide/README.md) teaches D4 and application design. These pages are for looking something up.

Most of this was converted from the DocBook sources the published 3.x help collection was built from, so **the material records what shipped rather than a reading of today's source.** Where a signature or a sentence disagrees with this repository, the disagreement is worth knowing; `docs-reference-generator` is the work item that will emit the parts of it a running server can be asked for.

## Sections

| Section | What is in it |
|---|---|
| [System library](system-library/README.md) | The core library: 297 [operator](system-library/README.md#operators) pages grouped as the printed reference grouped them, and 20 [scalar type](system-library/README.md#scalar-types) pages. The D4 language's own vocabulary. |
| [Libraries](libraries/README.md) | One page per shipped library - what it is for, what it needs, the files and classes it registers, and its settings, types and operators. |
| [D4 Language Grammar](d4-grammar.md) | The complete grammar of D4, as productions. |
| [Tags](tags.md) | The metadata tags, with the `Frontend.*` tags that guide form derivation and the `DAE.*` and `Storage.*` tags that guide the engine and the devices. |
| [Error Message Prefix Codes](error-codes.md) | Which range of error numbers belongs to which part of the product, and where each range's messages are declared. |
| [Compiler Defines](compiler-defines.md) | The conditional-compilation symbols the product is built with, what each one changes, and which are load-bearing. |
| [Frontend Components](frontend-nodes/README.md) | What a user-interface description is made of, and - because the element reference was generated and no copy survives - where to read about the elements instead. |
| [Call-level Interface (CLI)](call-level-interface.md) | The interfaces a .NET program connects, prepares, executes and reads results with. An overview; there is no generated API reference. |
| [Data Access Components (DAC)](data-access-components.md) | The .NET components and data-aware controls a Windows application binds to. An overview, on the same footing as the CLI page. |

Two reference sections of the 3.x documentation are not here, because neither has a source that can be converted: **the system tables** - their columns, keys and descriptions - which [the System library page](system-library/README.md#what-is-missing-the-system-tables) explains, and **the Frontend element reference**, which [the Frontend Components page](frontend-nodes/README.md) explains. Both need a page generated from something running, not a document translated.

The **editor shortcut keys**, which the printed reference carried as an appendix, are under [Editor Shortcut Keys](../users-guide/tutorials.md#editor-shortcut-keys) in the User's Guide: the help collection attached that appendix to the User's Guide and the converted sources kept it in that position.

## Legal

Information in this Dataphor Documentation is subject to change without notice. No part of this document may be reproduced by any means or for any purpose without the express written permission of Alphora. The software described in this document is furnished under a license agreement and may be used or copied only in accordance with the terms of the license agreement.

```text
Copyright © 2016 Alphora
```

All rights reserved.

Dataphor ® is a registered trademark of Alphora. The trademarks referenced in this document are the property of their respective owners and used in reference to their products.
