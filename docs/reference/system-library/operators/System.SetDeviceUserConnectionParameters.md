# System.SetDeviceUserConnectionParameters

**Declarations**

```d4
create operator .System.SetDeviceUserConnectionParameters(const AUserID : System.UserID, const ADeviceName : System.Name, const AConnectionParameters : System.String)
 class "System.SystemSetDeviceUserConnectionParametersNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDeviceUserConnectionParameters` alters a mapping previously created by `CreateDeviceUser`. This only changes the connection parameters used to connect to the specified device.

**Using System.SetDeviceUserConnectionParameters**

```d4
create device MSSQL reconciliation { mode = { command }, master = server }
    class "Alphora.Dataphor.DAE.Device.MSSQL.MSSQLDevice.AlphoraMSSQLDevice";
    {
      "ServerName" = ".",
      "DatebaseName" = "SP",
                        "ConnectionParameters" = "maxpoolsize=200"
    };
CreateDeviceUser("Admin", "MSSQL", "Admin", "secret");
SetDeviceUserConnectionParameters("Admin", "MSSQL", "maxpoolsize=1000");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
