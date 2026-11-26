description: A setting that controls whether a database connection asks for per-user credentials cannot be turned on: whichever value is given, the code stores "off".
architecture: docs/modernization-plan.md#ws7-devices-and-libraries
files: src/engine/DAE/Schema/Device.cs, src/engine/DAE/Device.Memory/MemoryDevice.cs
repro: static
severity: edge-case
likelihood: unusual
tradeoffs: The property's default is already "on", so nothing that leaves it alone is affected and the only definitions this can bite are ones that set it explicitly - and a definition that set it to True has been getting False for as long as the line has been there, so correcting it changes behaviour for exactly those definitions rather than fixing something they are waiting for. A maintainer may reasonably decide the honest fix is to remove the setter rather than make it work, which is a smaller compatibility surface and says what the code has always done.
----
## What is wrong

`Schema.Device.RequiresAuthentication` is a published property of every device, which is to say a device definition can set it as an attribute (`attributes { "RequiresAuthentication" = "False" }`) and an instance can set it as a device setting. Its setter ignores its argument:

```csharp
private bool _requiresAuthentication;
public bool RequiresAuthentication
{
	get { return _requiresAuthentication; }
	set { _requiresAuthentication = false; }
}
```

`src/engine/DAE/Schema/Device.cs`. So the property can be turned off and never on. The constructor sets the field directly (`_requiresAuthentication = true`), which is why the default is nonetheless "on" and why nothing has noticed.

## What it controls

Whether a device session looks for a **device user** before it connects. `Device.GetDeviceSessionInfo` resolves one only when `RequiresAuthentication` is set, and falls back to the device's own `UserID` and `Password`, and then to the Dataphor session's own user id and password, when it is not. For a SQL device that is the difference between each Dataphor user reaching the database as themselves - which is what `CreateDeviceUser` exists for - and every user reaching it as whatever the device itself was given.

So the reachable consequence is narrow but real: a definition that turns the setting off, and a later definition or device setting that tries to turn it back on, leaves a device that silently ignores its device users and connects as the device instead. Nothing reports it; a device user that is never consulted looks exactly like one that was consulted and matched.

`MemoryDevice`'s constructor sets `RequiresAuthentication = false` through the setter, which is the one caller in the tree and gets the answer it wants either way - which is presumably how the line survived.

## Why this is a decision rather than a repair

Writing `_requiresAuthentication = value` is a one-word change, but it is a change an existing deployment can notice: a device definition or a device setting saying `RequiresAuthentication = "True"` has been getting `False` for as long as the line has been there, and after the change that device starts consulting device users. For a deployment that has device users defined and does not expect them to be used, connections would begin arriving at the database as different logins. That is a compatibility surface (`tickets/rules/10-dataphor.md`), so it needs a decision rather than being fixed in passing - and if it is fixed, it needs an entry in `docs/migration-notes.md`.

The alternative is to make the code say what it does: drop the setter, so that `RequiresAuthentication` is read-only and the two devices that want it off set the field, and a definition naming it fails with the error a definition naming any unknown property gets. That keeps today's behaviour exactly and removes the trap.

## How it was found

Read while `device-sqlserver-version-matrix` was working out how to give a test device a SQL login, which needed to know whether the device would consult a device user at all. `repro: static` - the behaviour was read out of the code, and the measurement that would confirm it is a device created with `"RequiresAuthentication" = "True"` and a device user defined for it, which should then be ignored.
