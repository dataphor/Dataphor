# System.CreateGenerators

**Declarations**

```d4
create operator .System.CreateGenerators()
begin
    .System.Execute('
                create table Generators
                {
                    ID : System.Name,
                    NextKey : System.Integer,
                    key { ID }
                } tags { DAE.ShouldTranslate = ''false'' };
                
                create operator GetNextGenerator(const AID : System.Name) : System.Integer
                begin
                    update Generators set { NextKey := NextKey + 1 } where ID = AID;
                    if not exists (Generators where ID = AID) then
                        insert table { row { AID ID, 2 NextKey } } into Generators;
                    result := Generators[AID].NextKey - 1;
                end tags { DAE.IsDeterministic = ''false'', DAE.IsRepeatable = ''false'', DAE.ShouldTranslate = ''false'' };
                
                grant usage on GetNextGenerator(System.Name) to role System.User;
            ');
end;
```

**Remarks**

`CreateGenerators` creates a `Generators` table and `GetNextGenerator` operator in the current library.

> [!NOTE]
> [EnsureGenerators](System.EnsureGenerators.md) will first check to see if the `Generators` table already exists and will call `CreateGenerators` only if necessary.

The `Generators` table is created using the following statement:

```d4
create table Generators
{
    ID : System.Name,
    NextKey : System.Integer,
    key { ID }
} tags { DAE.ShouldTranslate = 'false' };
```

The `GetNextGenerator` operator is created using the following script:

```d4
create operator GetNextGenerator(const AID : System.Name) : System.Integer
begin
    update Generators
        set { NextKey := NextKey + 1 } 
        where ID = AID;
    if not exists (Generators where ID = AID) then
        insert table { row { AID ID, 2 NextKey } }
            into Generators;
    result := Generators[AID].NextKey - 1;
end
    tags 
    { 
        DAE.IsDeterministic = 'false', 
        DAE.IsRepeatable = 'false', 
        DAE.ShouldTranslate = 'false' 
    };
                
grant usage on GetNextGenerator(System.Name)
    to role System.User;
```

**Using System.CreateGenerators**

```d4
CreateGenerators();
```

**See Also**

[System.EnsureGenerators](System.EnsureGenerators.md) \| [Ensure Generators (Tutorial)](../../../users-guide/tutorial-movie-rental.md#ensure-generators)
