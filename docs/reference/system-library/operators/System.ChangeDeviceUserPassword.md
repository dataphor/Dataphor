# System.ChangeDeviceUserPassword

**Declarations**

```d4
create operator .System.ChangeDeviceUserPassword(const ADeviceName : System.Name, const AOldPassword : System.String, const ANewPassword : System.String)
 class "System.SystemChangeDeviceUserPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`ChangeDeviceUserPassword` makes it possible for passwords to be changed. By specifying the device, the old password and a new password, this operator changes the password used to access the device for the current user.

**Using System.ChangeDeviceUserPassword**

```d4
CreateUser("JS1", "John Smith", "Password");
create device Linter
    class "Alphora.Dataphor.DAE.Device.Linter.LinterDevice,AlphoraLinterDevice";
CreateDeviceUser("JS1", "Linter", "SYSTEM", "DevicePassword");
ChangeDeviceUserPassword("Linter", "SYSTEM", "NewDevicePassword");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [Users](../../../users-guide/dataphoria-security.md#users) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
