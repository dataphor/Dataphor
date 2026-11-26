# System.SetDeviceUserID

**Declarations**

```d4
create operator .System.SetDeviceUserID(const AUserID : System.UserID, const ADeviceName : System.Name, const ADeviceUserID : System.String)
 class "System.SystemSetDeviceUserIDNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDeviceUserID` alters a mapping previously created by `CreateDeviceUser`. This only changes the login used to connect to the specified device.

**Using System.SetDeviceUserID**

```d4
create device Test
    class "Alphora.Dataphor.DAE.Device.Memory.MemoryDevice,Alphora.Dataphor.DAE";
CreateDeviceUser("Admin", "Test", "Admin", "secret");
SetDeviceUserID("Admin", "Test", "Administrator");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
