# Error Message Prefix Codes

## Summary

Error codes in the Dataphor Server consist of six digits. The first three digits are the *prefix* code identifying the source of the error, while the last three digits identify the error. The following table lists the prefix codes in use by the Dataphor Server:

| Prefix | Source | Exception type |
|--------|--------|----------------|
| 100 | Base Dataphor Exception | `BaseException` |
| 101 | BOP | `BOP.BOPException` |
| 102 | Lexer | `DAE.Language.LexerException` |
| 103 | Syntax | `DAE.Language.SyntaxException` |
| 104 | Runtime | `DAE.Runtime.RuntimeException` |
| 105 | Compiler | `DAE.Compiling.CompilerException` |
| 106 | Server | `DAE.Server.ServerException` |
| 107 | Language | `DAE.Language.LanguageException` |
| 108 | Machine | `DAE.Machine` |
| 109 | Parser | `DAE.Language.ParserException` |
| 110 | Scan | `DAE.Runtime.Data.ScanException` |
| 111 | Conveyor | `DAE.Streams.ConveyorException` |
| 112 | Streams | `DAE.Streams.StreamsException` |
| 113 | Schema | `DAE.Schema.SchemaException` |
| 114 | Device | `DAE.Device.DeviceException` |
| 115 | Catalog | `DAE.Device.Catalog.CatalogException` |
| 116 | Storage | `DAE.Storage` |
| 117 | Index | `DAE.Runtime.Data.IndexException` |
| 118 | Data Access Layer | `DAEClient` |
| 121 | Client | `DAE.Client.ClientException` |
| 122 | Controls | `DAE.Client.Controls.ControlsException` |
| 123 | Provider | `DAE.Client.Provider.ProviderException` |
| 124 | Dataphoria | `Dataphoria.DataphoriaException` |
| 125 | Application Transaction | `DAE.Device.ApplicationTransaction.ApplicationTransactionException` |
| 126 | SQL Device | `DAE.Device.SQL.SQLException` |
| 127 | Connection | `DAE.Connection.ConnectionException` |
| 128 | Designer | `DAE.Client.Controls.Design.DesignException` |
| 129 | COM Client | `DAE.Client.COM.COMClientException` |
| 130 | Catalog store | `DAE.Store.StoreException` |
| 200 | Frontend Server | `Frontend.Server.ServerException` |
| 201 | Frontend Client | `Frontend.Client.ClientException` |
| 202 | Frontend Device | `Frontend.Server.Device.FrontendDeviceException` |
| 203 | Simple Device | `DAE.Device.Simple.SimpleDeviceException` |
| 204 | Web Client — retired, reserved | `Frontend.Client.Web.WebClientException` |
| 205 | Windows Client | `Frontend.Client.Windows.WindowsClientException` |
| 206 | Web DAC controls — retired, reserved | `Frontend.Client.Web.WebDAC.ControlsException` |
| 207 | Silverlight Client — retired, reserved | `Frontend.Client.Silverlight.SilverlightClientException` |

Every exception type above omits the namespace prefix `Alphora.Dataphor.`, which they all share.

## How to read the table

- **Prefixes 119 and 120 are not allocated.** They have never been in this table; a new source takes the next free prefix rather than one of these.
- **Three prefixes are retired but reserved.** 204, 206 and 207 belonged to the two browser clients that 4.0 removed (the Web Client and its data-aware controls, and the Silverlight client). The rows stay so that the prefixes are not handed out again: an error code that reached a log, a support ticket or an application's error handling in a 3.x deployment still means what it meant then.
- **Eight prefixes have no exception class in the source tree today.** 108 (Machine), 116 (Storage), 118 (Data Access Layer) and 129 (COM Client) name sources that no longer have a class of their own, and 204 to 207 belong to the removed clients, 205's Windows client raising `Frontend.Client.ClientException` (201) instead. Their type names are the ones the allocation record carried, kept for the same reason the retired rows are kept.

## This table is a compatibility surface

Error codes are one of the surfaces listed under *Compatibility surfaces* in [Rules that are easy to break](../../AGENTS.md#rules-that-are-easy-to-break): an existing application may test for a code, and a deployment's logs and support history are full of them. This page is the allocation record, so two rules follow from that:

- **No prefix is renumbered.** A code that has shipped means one thing for ever.
- **No row is deleted, even for a source that has been removed.** A deleted row is a prefix that gets allocated again, which makes one code mean two things. Mark it retired instead, as 204, 206 and 207 are above.

Adding a prefix for a new source is an ordinary change: take the next free number, add the row, and the error codes in the new exception class start at that prefix followed by `000`.
