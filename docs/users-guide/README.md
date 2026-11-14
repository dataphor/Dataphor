# User’s Guide

![Dataphor Logo](../images/dataphor-logo.png)

## Summary

The Dataphor User’s Guide introduces the Dataphor product, and provides information on using the Dataphor product. The Guide provides an overview of the motivation behind Dataphor, introduces the Dataphor platform and the anatomy of a Dataphor application, then provides several tutorials. In addition, the guide provides a section on using applications built with Dataphor, and discusses administration of a Dataphor Server.

## Using Dataphor

**Sections**

1.  [Product Tour](product-tour.md)
2.  [Dataphoria](dataphoria.md)
3.  [Dataphor Service](dataphor-service.md)
4.  [Security](security.md)
5.  [Using Dataphor Applications](using-dataphor-applications.md)

Application development in the software industry is a complex and multi-faceted problem. Software architects and engineers are faced with a multitude of issues ranging from performance and scalability to security and usability. Over the years, more and more of these issues have been resolved generally by implementing the solutions in server layer software. With each layer of automation, application development becomes more and more declarative, and less and less procedural. In other words, the developer simply states what is to be done, not how it should be accomplished.

Dataphor represents a next step in that evolution from procedural to declarative development. The product is designed from the ground up to be a declarative development environment. This part of the Dataphor User’s Guide provides an introduction to the Dataphor application development platform and the technologies it contains. It introduces each component of the product, and provides detailed information on using the product to develop, deploy, and maintain Dataphor applications. This part is organized as follows:

**[Product Tour](product-tour.md)**  
Provides an overview of the entire Dataphor platform, and describes the concrete elements of the architecture.

**[Dataphoria](dataphoria.md)**  
Covers Dataphoria, the Dataphor development environment.

**[Dataphor Service](dataphor-service.md)**  
Discusses installation and administration of the Dataphor Service.

**[Security](security.md)**  
Discusses the Dataphor Security Model from the Administrator’s perspective.

**[Using Dataphor Applications](using-dataphor-applications.md)**  
Contains an in-depth discussion on using Dataphor applications from the perspective of an end-user.

The first four chapters are intended to be read by Dataphor developers and administrators, while the final chapter is intended to be read by anyone using a Dataphor application.

> The Dataphor Platform is largely based on the concepts portrayed in **The Third Manifesto: Foundation for Future Database Systems** \[3\] by C. J. Date and Hugh Darwen. The book is the culmination of thirty years of research and development in the relational model, and brings together in one volume the current state of the art in relational theory. For a complete list of the references used in building the Dataphor Server, refer to the Notices and References section of this manual. Alphora gratefully acknowledges all those who have contributed time and energy to the computer science field in general, and the relational field in particular.

## Tutorials

**Tutorial libraries included in Dataphoria**

1.  [Movie Rental](tutorial-movie-rental.md)
2.  [Book Publishing (with Pubs sample database)](tutorial-book-publishing.md)
3.  [Frontend Components](tutorial-sample-components.md)

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
