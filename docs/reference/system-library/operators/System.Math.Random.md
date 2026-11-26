# System.Math.Random

**Declarations**

```d4
create operator .System.Math.Random() : System.Decimal
 class "System.DecimalRandomNode" static tags { DAE.IsDeterministic = "False", DAE.IsRepeatable = "False" };
create operator .System.Math.Random(const AUpperBound : System.Integer) : System.Integer
 class "System.IntegerRandomNode" static tags { DAE.IsDeterministic = "False", DAE.IsRepeatable = "False" };
create operator .System.Math.Random(const ALowerBound : System.Integer, const AUpperBound : System.Integer) : System.Integer
 class "System.IntegerIntegerRandomNode" static tags { DAE.IsDeterministic = "False", DAE.IsRepeatable = "False" };
```

**Remarks**

`Random` returns a pseudo-random number. The output of this operator depends on the parameters. It returns a random decimal between 0 and 1, if no parameters are passed. If one integer is passed then the number returned will be an integer between zero and one less than the number specified, inclusive. If two integers are specified then the number returned will be between the first number and one less than the second number, inclusive. The one Integer overload is equivalent to calling the two integer overload with 0 as the first parameter. The first number can not be less than the second number. In the one integer overload the number can not be less than zero. Both numbers can be negative in the two integer overload if the second is still larger, not larger magnitude, than the second number. If the numbers are the same or are only one apart, then the number generated will be the same each time. The first time this operator is used, a seed is generated from the system time. A seed can be specified with the `Seed` operator.

**Using System.Math.Random**

```d4
select TableDee add
{
    Math.Random() RandDecimal,
    Math.Random(15) RandInteger,
    Math.Random(-100,0) RandIntInt
};
```

**See Also**

[System.Math.Seed](System.Math.Seed.md)
