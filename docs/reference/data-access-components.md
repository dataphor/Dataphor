# Data Access Components (DAC)

## Summary

The Data Access Components are the .NET components a Windows application uses to reach a Dataphor Server, sitting on top of the [call-level interface](call-level-interface.md): a session component that holds the connection, a data-set component that buffers a table expression and tracks a cursor position and an edit state, and a set of data-aware WinForms controls that bind to it. They are what the Windows client, the IDE and any hand-written WinForms application against Dataphor are built from. [Dataphor Server](../users-guide/dataphor-server.md#data-access-layer-dal) and [Presentation Overview](../developers-guide/presentation-overview.md) describe the layer they make up.

**This is an overview, not an API reference.** The 3.x documentation had a generated .NET API reference here - roughly two thousand pages of member documentation produced from the source comments by a tool that no longer exists, against a surface that has since moved. Rather than publish a stale reference, this page names the components a caller starts from and says where they are, so that the source itself can be read in the right place. A 2002 *DAC Developer's Guide* existed as a separate book and is not in this repository; `feat-dac-developers-guide` is the work item for a replacement.

## The components

`Alphora.Dataphor.DAE.Client` (`src/client/DAE.Client`, assembly of the same name) holds the non-visual components:

| Type | What it is |
|---|---|
| `DataSession` | The connection. Holds the server URI or an alias and the login, and when activated gives the components under it a session on the server. Everything else needs one. |
| `DataView` | A table expression, buffered on the client. Opens a cursor through the session, keeps a window of rows, exposes the current row's columns as fields, and carries the insert, edit and delete state. The one component an application spends its time with. Declared in `View.cs`, over `TableDataSet` and `DAEDataSet`; the folder also holds an older copy of the same class in `View-263.cs`, which the project does not compile. |
| `DataSource` | The indirection the controls bind to, between a `DataView` and the controls. Several controls share one `DataSource`; swapping what it points at re-points all of them. |
| `DataField` | One column of the current row, with conversions to and from the .NET types. |
| `DataLink` | How a control or a component subscribes to a data set's changes - row moved, state changed, data changed. A data-aware control holds one and answers its events. |
| `AliasManager` | The machine's list of named server connections ("aliases"), so an application can name a connection rather than a URI. [Aliases](../users-guide/aliases.md) describes them from the user's side. |

Two further assemblies belong to the same layer:

- **`Alphora.Dataphor.DAE.Client.Provider`** (`src/client/DAE.Client.Provider`) is an ADO.NET provider over the same session: `DAEConnection`, `DAECommand`, `DAEDataReader`, `DAEDataAdapter` and `DAETransaction`, the usual `DbConnection` family. It is the route for code that is written against ADO.NET and should not know it is talking to Dataphor; it does not give access to the features the components above expose.
- **`Alphora.Dataphor.DAE.Client.Controls`** (`src/windows/DAE.Client.Controls`) is the data-aware WinForms control set the components bind to - text boxes, a grid, lookups, a date-time control and the rest. [Using Dataphor Controls](../users-guide/using-dataphor-controls.md) describes them as a user meets them.

## Where it is explained

- [Dataphor Server](../users-guide/dataphor-server.md#data-access-layer-dal) and [Frontend Clients](../users-guide/frontend-clients.md) - what the layer is for, in the User's Guide.
- [Presentation Overview](../developers-guide/presentation-overview.md) and [Presentation Layer](../developers-guide/presentation-layer.md) - the same, from the Developer's Guide, with the Frontend layer above it.
- [Client-side Buffering Controls](../users-guide/client-side-buffering-controls.md) - the settings that decide how much a data set fetches and when.
- [Using Dataphor Controls](../users-guide/using-dataphor-controls.md) - the control set.
- [Aliases](../users-guide/aliases.md) - naming a server connection.
