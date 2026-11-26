# System.UpgradeLibrary

**Declarations**

```d4
create operator .System.UpgradeLibrary(const AName : System.Name)
 class "System.SystemUpgradeLibraryNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`UpgradeLibrary` runs the required existing upgrade scripts in the library `AName`. It also ensures all required libraries are upgraded recursively.

**Using System.UpgradeLibrary**

See the [System.DeleteUpgrade](System.DeleteUpgrade.md) example.

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.DeleteUpgrade](System.DeleteUpgrade.md) \| [System.InjectUpgrade](System.InjectUpgrade.md) \| [System.LoadUpgrade](System.LoadUpgrade.md) \| [System.SaveUpgrade](System.SaveUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeVersions](System.UpgradeVersions.md)
