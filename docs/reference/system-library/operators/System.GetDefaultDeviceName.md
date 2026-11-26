# System.GetDefaultDeviceName

**Declarations**

```d4
create operator .System.GetDefaultDeviceName() : System.Name
 class "System.SystemGetDefaultDeviceNameNode" static tags { DAE.IsRemotable = "False" };
create operator .System.GetDefaultDeviceName(const ALibraryName : System.Name) : System.Name
 class "System.SystemGetDefaultDeviceNameNode" static tags { DAE.IsRemotable = "False" };
```

**Remarks**

`GetDefaultDeviceName` gets the default device name for the current session of Dataphor. All base table variables are created in the default device unless another device is specified in the create table statement. Because the device is specified by name, it does not have to exist when `SetDefaultDeviceName` is called. If the device does not exist when the default device is accessed, by a create or other operation, D4 raises an exception. Optionally the library (`ALibraryName`) name can be specified.

**Using System.GetDefaultDeviceName**

```d4
select GetDefaultDeviceName("Sample.Common");
```

```text
SampleDevice
```

**See Also**

[Devices](../../../developers-guide/d4-catalog.md#device) \| [System.SetDefaultDeviceName](System.SetDefaultDeviceName.md)
