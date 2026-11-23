# Devices

What a device does with the plan nodes it claims, and which devices ship. Part of the [Dataphor architecture](../architecture.md).

A device translates plan nodes it claims to support into a native command and leaves the rest to the engine (`Compiling/Visitors/DetermineDeviceVisitor`). `System.MemoryDevice` (in the engine) and `SimpleDevice` (memory plus file persistence) need nothing external. `SQLDevice` is the base for SQL translation (10k lines); `MSSQLDevice` and `PGSQLDevice` specialize it and call into a `SQLConnection` implementation (the Oracle, ADO and SAS device and connection libraries that did the same are parked under `legacy/libraries/` and not built). The connection base classes are in `src/engine/DAE/Connection` and `src/engine/Server/Connection` (the `.cs` files of the same names under `libraries/Connection/Source` are uncompiled leftovers).
