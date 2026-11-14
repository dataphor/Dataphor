# Developer’s Guide

![Dataphor Logo](../images/dataphor-logo.png)

**Sections**

1.  [Introduction](introduction.md)
2.  [Logical Application Design](logical-application-design.md)
3.  [Presentation Layer](presentation-layer.md)
4.  [Physical Realization](physical-realization.md)
5.  [D4 Language Guide](d4-language-guide.md)

## Summary

The Dataphor Developer’s Guide discusses the declarative approach to application development as a solution to many of the technical problems associated with producing software. It is primarily intended to be read by software engineers; however, much of the material is pertinent for application and database administrators as well, particularly the discussions in Part I.

Part I is a general introduction to the motivation and foundation of the Dataphor product. It discusses the declarative approach to application development, and provides an architectural overview of the Dataphor product.

Part II discusses logical application design using the declarative approach. It describes in detail the conceptual level of the Dataphor product, including the D4 language, and how it can best be utilized to achieve the benefits of the declarative approach.

Part III discusses the presentation layer of the Dataphor product, including facilities for traditional Rapid Application Development (RAD) as well as describing the Automated Application Development (AAD) paradigm.

Part IV discusses the physical realization of a Dataphor application. It provides in-depth discussion of the Storage Integration Architecture (SIA) which provides the mapping between the logical description of the application and the physical implementation of that application.

Part V is the D4 Language Guide, which provides a complete description of the syntax and semantics of the D4 language.

## Type Conventions

This section describes the type conventions used in this manual.

<table>
<colgroup>
<col style="width: 50%" />
<col style="width: 50%" />
</colgroup>
<thead>
<tr>
<th style="text-align: left;">Description</th>
<th style="text-align: left;">Example</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align: left;"><p>Regular text</p></td>
<td style="text-align: left;"><p>D4 provides a powerful set of data manipulation capabilities, as well as a rich type system for describing even the most complex data.</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Code block with <strong>bold</strong> denoting keywords</p></td>
<td style="text-align: left;"><pre class="d4"><code>create table Employee { ID : Integer, Name : String, key { ID } };</code></pre></td>
</tr>
<tr>
<td style="text-align: left;"><p>inline code with <strong>bold</strong> denoting keywords</p></td>
<td style="text-align: left;"><p>Any given value has a type associated with it, for example, <strong>table</strong> \{ <strong>row</strong> \{ 5 ID } } is a table value with type <strong>table</strong> \{ ID : System.Integer }.</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Keywords (also used in code blocks)</p></td>
<td style="text-align: left;"><p><strong>select</strong></p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Introduction of new terms.</p></td>
<td style="text-align: left;"><p><em>physical representation</em></p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Dataphor Identifiers</p></td>
<td style="text-align: left;"><p><code>System.Abs</code> returns the Absolute value (nonnegative) of a given value.</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Keyboard Keys</p></td>
<td style="text-align: left;"><p>F1</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Key Combinations</p></td>
<td style="text-align: left;"><p>Ctrl+C</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Buttons in the interfaces</p></td>
<td style="text-align: left;"><p>Add…</p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Commands or Executable files</p></td>
<td style="text-align: left;"><p><code>DAEService.exe</code></p></td>
</tr>
<tr>
<td style="text-align: left;"><p>Parameters for Executable files</p></td>
<td style="text-align: left;"><p><code>/i</code></p></td>
</tr>
</tbody>
</table>

## Legal

Information in this Dataphor Documentation is subject to change without notice. No part of this document may be reproduced by any means or for any purpose without the express written permission of Alphora. The software described in this document is furnished under a license agreement and may be used or copied only in accordance with the terms of the license agreement.

```text
Copyright © 2016 Alphora
```

All rights reserved.

Dataphor ® is a registered trademark of Alphora. The trademarks referenced in this document are the property of their respective owners and used in reference to their products.
