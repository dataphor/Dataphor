# System.Math.Factorial

**Declarations**

```d4
create operator .System.Math.Factorial(const AValue : System.Integer) : System.Integer
 class "System.IntegerFactorialNode";
```

**Remarks**

`Factorial` computes the factorial of the specified number (*n*!).

`0! = 1`

`n! = n * (n-1)!`

**Using System.Math.Factorial**

```d4
select TableDee add { Math.Factorial(10) Fact};
```

```text
Fact    
------- 
3628800
```
