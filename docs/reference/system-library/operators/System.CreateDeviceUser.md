# System.CreateDeviceUser

**Declarations**

```d4
create operator .System.CreateDeviceUser(const AUserID : System.UserID, const ADeviceName : System.Name, const ADeviceUserID : System.String, const ADevicePassword : System.String)
 class "System.SystemCreateDeviceUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.CreateDeviceUser(const AUserID : System.UserID, const ADeviceName : System.Name, const ADeviceUserID : System.String, const ADevicePassword : System.String, const AConnectionParameters : System.String)
 class "System.SystemCreateDeviceUserNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`CreateDeviceUser` creates a device user mapping for user specified by `AUserID` in the device specified by `ADeviceName`. The mapping will use the login specified by `ADeviceUserID`, and the password specified by `ADevicePassword` to connect to the device. Additional connection parameters can be specified using `AConnectionParameters`.

For information on device user mappings and connection strings, refer to the [Basic Connectivity](../../../developers-guide/devices.md#basic-connectivity) section of the Dataphor Developer's Guide.

**Using System.CreateDeviceUser**

```d4
CreateUser("JS1", "John Smith", "Password");
create device Linter
     class "Alphora.Dataphor.DAE.Device.Linter.LinterDevice,AlphoraLinterDevice";
CreateDeviceUser("JS1", "Linter", "SYSTEM", "DevicePassword");
```

**See Also**

[Administration, Security](../../../users-guide/security.md) \| [Catalog Object Security](../../../developers-guide/d4-catalog.md#security) \| [Catalog Element Security](../../../developers-guide/d4-catalog.md#security-1) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.ChangeDeviceUserPassword](System.ChangeDeviceUserPassword.md) \| [System.CreateDeviceUserWithEncryptedPassword](System.CreateDeviceUserWithEncryptedPassword.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.DropDeviceUser](System.DropDeviceUser.md) \| [System.SetDeviceUserConnectionParameters](System.SetDeviceUserConnectionParameters.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
