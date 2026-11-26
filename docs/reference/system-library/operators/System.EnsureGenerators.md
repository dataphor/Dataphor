# System.EnsureGenerators

**Declarations**

```d4
create operator .System.EnsureGenerators()
begin
    if not(exists((
        .System.TableVars
            where (.System.Name.ReadValue(.Name) = (.System.Name.ReadValue(.System.LibraryName()) + '.Generators'))
    ))) then
        begin
            .System.CreateGenerators();
        end;
end;
```

**Remarks**

`EnsureGenerators` ensures that a `Generators` table exists. If one does not exist, the `CreateGenerators` operator is used to create a `Generators` table and a `GetNextGenerator` operator.

**Using System.EnsureGenerators**

```d4
EnsureGenerators();
```

**See Also**

[System.CreateGenerators](System.CreateGenerators.md) \| [Ensure Generators (Tutorial)](../../../users-guide/tutorial-movie-rental.md#ensure-generators)
