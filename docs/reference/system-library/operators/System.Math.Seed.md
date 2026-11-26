# System.Math.Seed

**Declarations**

```d4
create operator .System.Math.Seed()
 class "System.SeedNode";
create operator .System.Math.Seed(const AValue : System.Integer)
 class "System.SeedNode";
```

**Remarks**

`Math.Seed` sets the seed value for generating pseudo-random numbers. If no arguments are specified, then a number from the system clock will be used. If a value is passed to the operator, then that number will be used for the seed.

**Using System.Math.Seed**

```d4
Math.Seed();
```

**See Also**

[System.Math.Random](System.Math.Random.md)
