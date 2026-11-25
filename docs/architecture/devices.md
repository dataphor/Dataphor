# Devices

What a device does with the plan nodes it claims, and which devices ship. Part of the [Dataphor architecture](../architecture.md).

A device translates plan nodes it claims to support into a native command and leaves the rest to the engine (`Compiling/Visitors/DetermineDeviceVisitor`). `System.MemoryDevice` (in the engine) and `SimpleDevice` (memory plus file persistence) need nothing external. `SQLDevice` is the base for SQL translation (10k lines); `MSSQLDevice` and `PGSQLDevice` specialize it and call into a `SQLConnection` implementation (the Oracle, ADO and SAS device and connection libraries that did the same are parked under `legacy/libraries/` and not built). The connection base classes are in `src/engine/DAE/Connection` and `src/engine/Server/Connection` (the `.cs` files of the same names under `libraries/Connection/Source` are uncompiled leftovers).

A device's operator map is a D4 script in its own library (`Source/SystemCatalog.d4`), run once when the device registers, and each `create operator System.iFoo(table) class "SQLDevice.SQLFoo"` in it resolves its operator against the catalog before anything is translated (`Compiler.CompileDeviceOperatorMap`). A mapping that names an operator the engine no longer declares therefore fails the whole script, and the device does not register - which is why retiring a built-in operator means editing every device that maps it, in the same change.

NOTE: the parked Oracle and SAS catalogs under `legacy/libraries/` still map `System.iRedefine(table)`, which `debt-unreachable-redefine-nodes` retired from the engine along with the `MSSQLDevice` and `PGSQLDevice` mappings. Whoever revives one of those devices has to drop that line; nothing builds or runs those trees today, so nothing catches it first.
