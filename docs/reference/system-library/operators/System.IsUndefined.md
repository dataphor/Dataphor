# System.IsUndefined

**Declarations**

```d4
create operator .System.IsUndefined(const AValue : System.VersionNumber) : System.Boolean
begin
    .result := .System.iEqual(.AValue, .System.VersionNumber(-(1), -(1), -(1), -(1)));
end;
```

**Remarks**

`IsUndefined` returns `true` if the specified `VersionNumber`, `AValue`, is the special `VersionNumber` value `VersionNumberUndefined`. Otherwise false is returned.

**Using System.IsUndefined**

```d4
create session table Example in Temp
{
 VersNbr : VersionNumber,
 key{VersNbr}
};
begin
    insert table { row { VersionNumberUndefined() VersNbr } } into Example;
    insert table { row { "2.3.*".ToVersionNumber() VersNbr } } into Example;
    insert table { row { "2.3.4.5".ToVersionNumber() VersNbr } } into Example;
end;
select Example
    add { VersNbr.IsUndefined() UnDefined };
drop table Example;
```

```text
VersNbr UnDefined
------- ---------
*       True
2.3.*   False
2.3.4.5 False
```

**See Also**

[System.VersionNumber](../types/System.VersionNumber.md) \| [System.ToVersionNumber](System.ToVersionNumber.md)
