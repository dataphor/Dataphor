# System.IsSpecial

**Declarations**

```d4
create operator .System.IsSpecial(const AValue : System.Scalar) : System.Boolean
 class "System.ScalarIsSpecialNode";
create operator .System.IsSpecial(const AValue : System.Boolean) : System.Boolean
 class "System.ScalarIsSpecialNode";
create operator .System.IsSpecial(const AValue : System.Decimal) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Long) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Integer) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Short) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Byte) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.String) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.TimeSpan) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.DateTime) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Date) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Time) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Money) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Guid) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Binary) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Graphic) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Error) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.Name) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.UserID) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.VersionNumber) : System.Boolean
begin
    .result := (false or .System.iEqual(.AValue, .System.VersionNumber(-(1), -(1), -(1), -(1))));
end;
create operator .System.IsSpecial(const AValue : System.LibraryReference) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.FileReference) : System.Boolean
begin
    .result := false;
end;
create operator .System.IsSpecial(const AValue : System.LibraryDescriptor) : System.Boolean
begin
    .result := false;
end;
```

**Remarks**

`IsSpecial` checks to see if a given value is equal to some special defined for the type of the value. When a new scalar type is created, an overload of `IsSpecial` is created to provide a means of checking for special values.

**Using System.IsSpecial**

```d4
select IsSpecial(VersionNumberUndefined());
select IsSpecial(ToVersionNumber("*"));
select IsSpecial(ToVersionNumber("2.1.*.*"));
```

```text
True
True
False
```

**See Also**

[Scalar Values, Special values](../../../developers-guide/d4-catalog.md#specials) \| [System.IsUndefined](System.IsUndefined.md)
