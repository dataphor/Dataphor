# System.CreateDeviceUserWithEncryptedPassword

**Declarations**

```d4
create operator .System.CreateDeviceUserWithEncryptedPassword(const AUserID : System.UserID, const ADeviceName : System.Name, const ADeviceUserID : System.String, const AEncryptedDevicePassword : System.String)
 class "System.SystemCreateDeviceUserWithEncryptedPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.CreateDeviceUserWithEncryptedPassword(const AUserID : System.UserID, const ADeviceName : System.Name, const ADeviceUserID : System.String, const AEncryptedDevicePassword : System.String, const AConnectionParameters : System.String)
 class "System.SystemCreateDeviceUserWithEncryptedPasswordNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`CreateDeviceUserWithEncryptedPassword` creates a device user for the user specified by `AUserID` in the device specified by `ADeviceName`. The device user will use the login specified by `ADeviceUserID`, and the password specified by the encrypted string `AEncryptedDevicePassword`. Additional connection parameters can be specified using `AConnectionParameters`.

For information on device user mappings and connection strings, refer to the [Basic Connectivity](../../../developers-guide/devices.md#basic-connectivity) section of the Dataphor Developer's Guide.

**Using System.CreateDeviceUserWithEncryptedPassword**

```d4
CreateUser("JS1", "John Smith", "Password");
create device Linter
    class "Alphora.Dataphor.DAE.Device.Linter.LinterDevice,AlphoraLinterDevice";
select EncryptPassword("DevicePassword");
CreateDeviceUserWithEncrypedPassword
(
 "JS1",
 "Linter",
 "SYSTEM",
 "ENOq2MyVfW04WwuFthdVmu4="
);
```

```text
ENOq2MyVfW04WwuFthdVmu4=
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
