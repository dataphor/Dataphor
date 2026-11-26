# PGSQLDevice

The SQL device for PostgreSQL. It is one of the two database back ends this release supports, and like every specific SQL device it describes only how PostgreSQL's dialect differs from the Common SQL the [SQLDevice](SQLDevice.md) library translates D4 into. The device it registers is written `class "PostgreSQLDevice.PostgreSQLDevice"` in a `create device` statement, and it reaches the server through the [PGSQLConnection](PGSQLConnection.md) library.

This library has no chapter in the 2007 documentation — it is new in this release — so this page records only what the descriptor, the registered classes and the library's own files state. Its settings are those of the generic SQL device.

## Definition

**Version**

3.0.0.0

**Requisites**

| Name | Version |
|---|---|
| System | \* |
| SQLDevice | \* |
| PGSQLConnection | \* |

**PGSQLDevice Files**

| Name | Is Assembly |
|---|---|
| AlphoraPGSQLDevice.dll | True |

### Registered Assemblies

AlphoraPGSQLDevice

### Registered Classes

The library registers its classes in the `PostgreSQLDevice` namespace, built by reflecting over the types in its assembly, so the full list is not reproducible from the source tree. The names that appear in D4 in this repository, each qualified with that namespace, are:

- `PostgreSQLDevice` — the device itself, named in a `create device` statement.
- `PostgreSQLConnectionStringBuilder` — the device's default `ConnectionStringBuilderClass`.
- The device scalar types and operators the device's own catalog script (`libraries/PGSQLDevice/Source/SystemCatalog.d4`, applied when a device of this class is created) maps: `PostgreSQLBinary`, `PostgreSQLBoolean`, `PostgreSQLByte`, `PostgreSQLDate`, `PostgreSQLDateTime`, `PostgreSQLGraphic`, `PostgreSQLGuid`, `PostgreSQLIndexOf`, `PostgreSQLMSSQLBinary`, `PostgreSQLMoney`, `PostgreSQLPos`, `PostgreSQLPower`, `PostgreSQLRetrieve`, `PostgreSQLString`, `PostgreSQLText`, `PostgreSQLTime`, `PostgreSQLToBigInt`, `PostgreSQLToBit`, `PostgreSQLToDecimal288`, `PostgreSQLToMoney` and `PostgreSQLUnion`.

## Example

The simplest device definition, which connects to a PostgreSQL server on the local host as the stock `postgres` user and database:

```d4
create device PGSQLTestDevice
    reconciliation { mode = { command }, master = device }
    class "PostgreSQLDevice";

CreateDeviceUser("System", "PGSQLTestDevice", "postgres", "the_password");
CreateDeviceUser("Admin", "PGSQLTestDevice", "postgres", "the_password");

EnsureDeviceStarted("PGSQLTestDevice");
```

`Server` and `Database` attributes name another host or database:

```d4
create device PGSQLTestDevice
    reconciliation { mode = { command }, master = device }
    class "PostgreSQLDevice"
    attributes
    {
        "Server" = "my.host.or.ip",
        "Database" = "my_database_name"
    };
```

**See Also**

[SQLDevice](SQLDevice.md) | [PGSQLConnection](PGSQLConnection.md) | [Devices](../../developers-guide/devices.md) | [Devices (architecture)](../../architecture/devices.md)
