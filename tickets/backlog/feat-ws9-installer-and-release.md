description: Replace the old installer with one that installs, upgrades and uninstalls cleanly, registers the server as a service, and is built by the normal build; also keep ClickOnce deployment of the Windows client possible.
architecture: docs/modernization-plan.md#ws9-installer-and-release
prereq: feat-ws5-protocol-and-hosting, feat-ws6-windows-client-and-ide
tradeoffs: WiX is pinned to an older major version to avoid its maintenance fee (Q11), so the installer may miss later WiX fixes and eventually need a toolset decision of its own.
----
## What this is

An umbrella for WS9. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then. The `prereq:` above only orders planning: WS9 is cut after WS5 and WS6 have been cut, so its tickets can name their real slugs (`host-generic-host`, `ui-windows-client-runs`).

**Promote when** gate G3 holds (Phase 4).

## Work items

Scope per the plan's WS9 target list and work-item table; D10 (WiX SDK-style project, one per-machine x64 MSI from a self-contained publish, `MajorUpgrade` with the existing `UpgradeCode`).

- `installer-publish-profiles`, `installer-msi` (deletes `Setup/` and `Utilities/Wix`; fixes K16), `installer-service-and-firewall`, `installer-test` (WS3 layer L8), `installer-clickonce-client` (ClickOnce is how existing sites deploy the client, Q3), `release-checklist-and-changelog`.

Q11 is answered: pin the newest WiX major version that does not carry the "Open Source Maintenance Fee" (Dataphor is open source; commercial use is downstream). `installer-msi` establishes which version that is from the WiX release notes and licence, and records the pin and its reason beside the package reference.

Instance data under `%ProgramData%\Alphora\Dataphor` is never removed by upgrade or uninstall, and a 3.x installation is left alone side by side.

## Cutting rules

Re-read the WS9 section, the runtime layout and the host as built; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
