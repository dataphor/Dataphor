# System.Compatible

**Declarations**

```d4
create operator .System.Compatible(const ASource : System.VersionNumber, const ATarget : System.VersionNumber) : System.Boolean
 class "System.VersionNumberCompatibleNode";
```

**Remarks**

`Compatible` compares two version numbers. `Returns` `true` if `ATarget` matches all digits specified in `ASource`, if not returns false.

**Using System.Compatible**

```d4
select Compatible
(
 "1.2.*".ToVersionNumber(),
 "1.2.3.2".ToVersionNumber()
);
```

```text
True
```

**See Also**

[System.VersionNumber Scalar Type](../types/System.VersionNumber.md) \| [System.ToVersionNumber](System.ToVersionNumber.md) \| [System.IsUndefined](System.IsUndefined.md) \| [Scalar Values](../../../developers-guide/d4-language-elements.md#scalar-values)
