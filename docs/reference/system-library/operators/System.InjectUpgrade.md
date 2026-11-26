# System.InjectUpgrade

**Declarations**

```d4
create operator .System.InjectUpgrade(const ALibraryName : System.Name, const AScript : System.String) : System.VersionNumber
 class "System.InjectUpgradeNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`InjectUpgrade` increments the revision number of `ALibraryName` by one. The revision number is the third component of the version number. If the version number of `ALibraryName` is not specified to at least the third position an error will be raised.

`InjectUpgrade` then saves the upgrade script `AScript` in `ALibraryName` with the new version number from above.

**Using System.InjectUpgrade**

See the [System.DeleteUpgrade](System.DeleteUpgrade.md) example.

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.DeleteUpgrade](System.DeleteUpgrade.md) \| [System.LoadUpgrade](System.LoadUpgrade.md) \| [System.SaveUpgrade](System.SaveUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeLibrary](System.UpgradeLibrary.md) \| [System.UpgradeVersions](System.UpgradeVersions.md)
