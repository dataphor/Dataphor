# System.EncryptString

**Declarations**

```d4
create operator .System.EncryptString(const AUnencrypted : System.String) : System.String
 class "System.SystemEncryptStringNode" static tags { DAE.IsDeterministic = "False", DAE.IsRepeatable = "False" };
```

**Remarks**

Returns an encrypted representation of the specified string. A SALT (random number) is encrypted with the string to ensure that routine never produces the same encrypted string. This technique is used to counter dictionary based hacks. Because of the included SALT, EncryptString is not deterministic. EncryptString and DecryptString are based on symetrical (hidden key) encryption using the Rijndael algorithm.

**Using System.EncryptString**

```d4
select EncryptString("Text To Hide");
```

**See Also**

[Security Operators](System.SecurityOperators.md) \| [System.DecryptString](System.DecryptString.md)
