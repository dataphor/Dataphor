# System.SetDefaultDeviceName

**Declarations**

```d4
create operator .System.SetDefaultDeviceName(const ADeviceName : System.Name)
 class "System.SystemSetDefaultDeviceNameNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
create operator .System.SetDefaultDeviceName(const ALibraryName : System.Name, const ADeviceName : System.Name)
 class "System.SystemSetDefaultDeviceNameNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SetDefaultDeviceName` sets the default device name (ADeviceName) for the current session of Dataphor. After executing `SetDefaultDeviceName`, all base table variables are created in the specified device unless another device is specified in the create table statement. Because the device is specified by name, it does not have to exist when `SetDefaultDeviceName` is called. If the device does not exist when the default device is accessed, by a create or other operation, D4 raises an exception. Optionally the library (ALibraryName) name can be specified.

**Using System.SetDefaultDeviceName**

```d4
SetDefaultDeviceName("MSSQL");
```

**See Also**

[Security](../../../users-guide/security.md) \| [Devices](../../../developers-guide/d4-catalog.md#device) \| [System.CreateDeviceUser](System.CreateDeviceUser.md) \| [System.DeviceUserExists](System.DeviceUserExists.md) \| [System.GetDefaultDeviceName](System.GetDefaultDeviceName.md) \| [System.SetDeviceUserID](System.SetDeviceUserID.md) \| [System.SetDeviceUserPassword](System.SetDeviceUserPassword.md)
