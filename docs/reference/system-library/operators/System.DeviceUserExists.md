# System.DeviceUserExists

**Declarations**

```d4
create operator .System.DeviceUserExists(const AUserID : System.UserID, const ADeviceName : System.Name) : System.Boolean
 class "System.SystemDeviceUserExistsNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

Returns `true` if the specified Device User (AUserID) exists, otherwise returns `false`.

**Using System.DeviceUserExists**

```d4
CreateUser("JS1", "John Smith", "Password");
create device Linter
    class "Alphora.Dataphor.DAE.Device.Linter.LinterDevice,AlphoraLinterDevice";
CreateDeviceUser ("JS1", "Linter", "SYSTEM", "DevicePassword");
select DeviceUserExists("SYSTEM", "Linter");
```

```text
true
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
