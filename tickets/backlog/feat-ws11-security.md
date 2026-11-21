description: Remove known-weak security defaults — passwords stored with a key built into the product, an empty administrator password, outdated vulnerable packages — without breaking existing catalog rebuild scripts.
architecture: docs/modernization-plan.md#ws11-security
tradeoffs: Every item touches a compatibility surface (scripted users and device users, login behaviour), so each needs owner sign-off and could be judged too risky for the first 4.0 release.
----
## What this is

An umbrella for WS11. When promoted to `plan/`, the planning agent replaces it with one ticket per work item below and deletes this file; its slug retires then.

**Promote when** Phase 5 starts, except `security-dependency-audit`, which is worth cutting as soon as `build-ci` has landed — promote early for that item alone if wanted.

## Work items

Findings and target per the plan's WS11 section; K13 (password key and IV compiled into the product, in two places).

- `security-password-storage-design` — `plan/` ticket ending in owner sign-off (`blocked/` with the design as the proposal). Catalog rebuild scripts create users and device users with passwords, some in the existing encrypted form, so the scripted forms are a compatibility surface. Needs `store-rebuild-workflow`.
- `security-password-storage` — implement the signed-off design.
- `security-admin-default`, `security-dependency-audit`, `security-operator-rights-review`, `security-threat-model-doc`.
- `security-transport-design` and `security-transport` - transport security as a designed capability, which the product does not have: `protocol-transport-security` established that every "use a secure connection" setting the product offers has never reached a binding, decided to report the request rather than remove the settings or invent a design, and handed the design here (decision D17 in the plan). The design ticket ends in owner sign-off: which credential type, where a server's certificate comes from, whether a client's identity is checked, what an off-domain client does, and whether an instance that requires security and cannot get it should then refuse to start. Until it lands, a site that needs an encrypted channel supplies its own binding through the client and host hooks, which is the only route there has ever been.

None of this is fixed inline in other tickets.

## Cutting rules

Re-read the WS11 section and the current code; one ticket per work item; `prereq:` only on slugs present on the board or in `tickets/.pruned-tickets.jsonl`.
