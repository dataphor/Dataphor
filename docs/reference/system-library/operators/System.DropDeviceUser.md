# System.DropDeviceUser

**Declarations**

```d4
create operator .System.DropDeviceUser(const AUserID : System.UserID, const ADeviceName : System.Name)
 class "System.SystemDropDeviceUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`DropDeviceUser` removes the specified mapping from Dataphor user to user on device.

**Using System.DropDeviceUser**

```d4
CreateUser("JS1", "John Smith", "Password");
create device Linter
     class "Alphora.Dataphor.DAE.Device.Linter.LinterDevice,AlphoraLinterDevice";
CreateDeviceUser("JS1", "Linter", "SYSTEM", "DevicePassword");
DropDeviceUser("JS1", "Linter");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
