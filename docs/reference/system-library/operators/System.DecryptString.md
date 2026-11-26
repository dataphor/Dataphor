# System.DecryptString

**Declarations**

```d4
create operator .System.DecryptString(const AEncrypted : System.String) : System.String
 class "System.SystemDecryptStringNode";
```

**Remarks**

Returns a decrypted representation of the given encrypted string. The encrypted string should have been produced using [EncryptString](System.EncryptString.md). Unlike EncryptString, DecryptString is deterministic because given a certain encrypted string it will always produce the same encrypted result. EncryptString and DecryptString are based on symetrical (hidden key) encryption using the Rijndael algorithm.

**Using System.DecryptString**

```d4
select DecryptString("ptTFNJl2/2JHNnIgPhORS/tgmjcPPpOE/WyPSO9u8uY=");
```

```text
Text To Hide
```

**See Also**

[Security Operators](System.SecurityOperators.md) \| [System.EncryptString](System.EncryptString.md)
