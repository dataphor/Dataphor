# System.SaveUpgrade

**Declarations**

```d4
create operator .System.SaveUpgrade(const ALibraryName : System.Name, const AVersion : System.VersionNumber, const AScript : System.String)
 class "System.SaveUpgradeNode" static tags { DAE.IsRemotable = "False", DAE.IsFunctional = "False" };
```

**Remarks**

`SaveUpgrade` saves the specified `AScript` to `ALibraryName` as version number `AVersion`.

**Using System.SaveUpgrade**

See the [System.DeleteUpgrade](System.DeleteUpgrade.md) example.

**See Also**

[Deployment and Maintenance](../../../developers-guide/working-with-libraries.md#deployment-and-maintenance) \| [Upgrading Libraries](../../../users-guide/dataphoria-libraries.md#upgrading-libraries) \| [Library Upgrades](../../../users-guide/dataphoria-libraries.md#library-upgrades) \| [Versioning](../../../developers-guide/working-with-libraries.md#versioning) \| [System.DeleteUpgrade](System.DeleteUpgrade.md) \| [System.InjectUpgrade](System.InjectUpgrade.md) \| [System.LoadUpgrade](System.LoadUpgrade.md) \| [System.UpgradeLibraries](System.UpgradeLibraries.md) \| [System.UpgradeLibrary](System.UpgradeLibrary.md) \| [System.UpgradeVersions](System.UpgradeVersions.md)
