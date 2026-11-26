# PGSQLConnection

The connection class the PostgreSQL device uses to reach a server. It plays the same part for [PGSQLDevice](PGSQLDevice.md) that the [Connection](Connection.md) library's classes play for the SQL Server device: the device translates D4 into SQL, and a connection class carries the statement to the target system and the result back. It is built on Npgsql, which it ships as a file of its own.

This library has no chapter in the 2007 documentation — it is new in this release — so this page records only what the descriptor and the registered class state.

## Definition

**Version**

3.0.0.0

**Requisites**

| Name | Version |
|---|---|
| Connection | \* |

**PGSQLConnection Files**

| Name | Is Assembly |
|---|---|
| Alphora.Dataphor.DAE.Connection.PGSQL.dll | True |
| Npgsql.dll | False |

`Npgsql.dll` is marked as not an assembly in the descriptor, so the server copies it into place with the library but does not register it; it is reached as an ordinary dependency of the connection assembly. Its file reference also carries the `WindowsServer` environment, so it is only deployed there.

### Registered Assemblies

Alphora.Dataphor.DAE.Connection.PGSQL

### Registered Classes

One, and the namespace is not the library's own name:

`PostgreSQLConnection.PostgreSQLConnection`

That is the string a device's `ConnectionClass` attribute names. The PostgreSQL device sets it by default, so a `create device` statement normally has no need to.

**See Also**

[PGSQLDevice](PGSQLDevice.md) | [Connection](Connection.md) | [SQLDevice](SQLDevice.md) | [Devices](../../developers-guide/devices.md)
