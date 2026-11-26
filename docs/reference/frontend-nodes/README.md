# Frontend Components

## Summary

A Dataphor user interface is described by a document, not by compiled code: a form document names the elements the interface is made of - a form, a grid, a frame, a notebook page, a search control, an action - and sets their properties. The language those documents are written in is called the Dataphor user-Interface Language, or DIL; a Frontend client reads a DIL document and builds the interface from it. See [Presentation Overview](../../developers-guide/presentation-overview.md) in the Developer's Guide for where that layer sits.

**There is no element reference in this documentation set, and there is no source for one.** The published 3.x documentation had one - a page per element, listing its properties with their types and defaults - but it was generated from the client's own node interfaces when the help file was built, and no copy of that output survives. Both DocBook sources that would hold it are placeholders saying the section is produced by automation, and the published help file has no such pages at all. So the element reference is not missing from this conversion; it was never a document.

What the client's interfaces still carry is the material it was generated from: the published properties of the node interfaces in `src/client/Frontend.Client/` carry XML documentation comments giving the type, the default and the remarks, each marked with a `<doc/>` element for the publishing tool to pick up - 292 of them across the three interface files. Reading those files is the authoritative answer about any particular element today. `feat-frontend-node-reference` is the work item that would turn them back into a reference.

## Where to read instead

| For | Read |
|---|---|
| How a form is derived from the schema, and how derivation is guided by metadata tags | [Form Automation](../../developers-guide/form-automation.md), and [Tags](../tags.md) for the tags themselves |
| What the presentation layer is and how a client, the Frontend server and the engine divide the work | [Presentation Layer](../../developers-guide/presentation-layer.md) and [Presentation Overview](../../developers-guide/presentation-overview.md) |
| What the elements look like to a user - forms, grids, navigation, searching | [Using Dataphor Forms](../../users-guide/using-dataphor-forms.md) |
| How documents and customizations are stored, and how the designer edits them | [Documents and Designers](../../users-guide/documents-and-designers.md) |
| The D4 operators that load, save, derive and customize documents | [the Frontend library reference](../libraries/Frontend.md) |
| How the pieces fit in this repository | [Frontend: from schema to screen](../../architecture/frontend.md) |
| The elements themselves, with their properties | the node interfaces in `src/client/Frontend.Client/` (`ElementInterfaces.cs`, `ActionInterfaces.cs`, `DataInterfaces.cs`), and the Windows client's implementations of them in `src/windows/Frontend.Client.Windows/` |

## What a document looks like

This much is worth stating here, because it is prose from the 3.x sources that no converted chapter carries. Everything below has been checked against the client this release ships.

Each element tag of a DIL document names a node type the client has a class for, and each attribute sets a published property of that node. An attribute's text is converted to the property's type and assigned. The root `interface` element is the exception: the client has already created the form object the document is loaded into, so that name is resolved by nobody and is not in the node-type list below. A document is stored as a form document (`.dfd`); a customization of another document is stored separately (`.dfdx`) and merged onto its ancestors when it is loaded.

An empty interface:

```xml
<interface
  xmlns:bop="www.alphora.com/schemas/bop"
  text="form name goes here"
  mainsource="MainSourceName goes here"
>

</interface>
```

An `interface` may contain exactly one element node, plus any number of actions, sources and other non-visual nodes. The designer in the IDE enforces which node may appear where, so trying it there is quicker than reasoning about it.

- **`xmlns:bop`** declares the namespace of the serializer that reads the document (`www.alphora.com/schemas/bop`, implemented in `src/engine/Base/BOP`). It is what makes the attributes below meaningful rather than ordinary XML attributes.
- **`bop:name`** names an element so that other elements can refer to it - a control naming its source, an action naming the action it calls. Nearly every element carries one. It is also the identity a customization matches on when its delta is merged onto an ancestor document.
- **`text`** on the `interface` is the title the form is given.
- **`mainsource`** names one of the sources defined in the body of the `interface` as the form's primary source, which is then available to the whole document under that alias. Several behaviours need it - a form opened in an input mode, an action that takes its arguments from the form - and fail with an error naming the missing `MainSource` if it is not set.

### Keyboard accelerators

The text of a node may ask for a keyboard accelerator by marking a character with an ampersand (`&`), and the request is a hint rather than an instruction. The Windows client allocates the requested character if it is still free on that form; if it is not, the client picks the first free character of the text instead, and if the form has no free character left the text is shown with no accelerator at all. A doubled ampersand (`&&`) is a literal ampersand. A text that begins with a tilde (`~`) is exempt: the tilde is dropped, no accelerator is allocated, and any ampersands in the rest are shown literally. The code is `AcceleratorManager` in `src/windows/Frontend.Client.Windows/Form.cs`.

### The designer's toolbox

The elements the form designer offers in its toolbox are not discovered by reflection; they are listed in a node-type document, one per client type, named `<ClientType>.NodeTypes.dnt` - `Windows.NodeTypes.dnt` for the Windows client, in `libraries/Frontend/Documents/`. A library may carry one of its own, and the client asks the server for the combined list through the D4 operator `Frontend.GetNodeTypes`. **A node type that is not listed in such a document does not appear in the toolbox**, so extending the element set means adding the node class and listing it. To give a new node an icon in the designer, put `DesignerImageAttribute` on its class (`src/client/Frontend.Client/DesignerAttributes.cs`).
