# IgniteAuth v0.9

## Control-Plane Security Framework for Intent-Based Operation Authorization

IgniteAuth is a security framework designed around a simple principle:

> **Authentication establishes who is making a request. IgniteAuth evaluates what the system is being asked to do, why it is being requested, and whether that operation should be permitted.**

IgniteAuth v0.9 implements a deterministic Control Plane that validates **Intent, Command, Target, and Intent-to-Command relationships** before allowing an operation to proceed.

The current implementation uses **ASP.NET Core, C#, and structured policy data**, with HMAC-SHA256 used to represent and verify registered intents.

---

## Rapid test setup: STM32 reference system

1. Install the .NET 10 SDK, copy `.env.example` to `.env` if needed, and set `IGNITE_AUTH_SECRET` to the key used to generate the checked-in hashed catalogs. The example key is for local testing only.
2. Run `dotnet run --project IgAPI --launch-profile http`. The development URL is `http://localhost:5249`.
3. Send `POST /api/auth/login` with `{"username":"admin","password":"password123"}` or `{"username":"test","password":"passwordtest"}`. Use the returned token as `Authorization: Bearer <accessToken>`; tokens last 15 minutes.
4. `GET /api/ReferenceSystem` returns the registered subsystems, intent identifiers, commands, and mappings. `POST /api/ControlPlane/decide` (also `/decide`) evaluates a request. For example:

```json
{
  "referenceSystemId": 1,
  "subSystemId": "GPIO",
  "subSystem": "GPIO",
  "intent": "IntentHash1",
  "command": "ReadPinState"
}
```

IgAPI issues a fresh positive `cpcId` when it receives each decision request and
returns that ID in the response. A caller does not allocate or submit the ID.

The caller's identity is taken from the JWT; a `userId` supplied in the body is ignored. `referenceSystemId` must be `1`, and `subSystemId` must equal the exact subsystem name returned by the catalog. Subsystem and command names are case-sensitive; intents are canonicalized before HMAC verification. A valid request returns HTTP 200 with `decision: "Allow"`; invalid requests return HTTP 200 with `decision: "Deny"` and a reason code. Malformed HTTP JSON returns 400; absent or invalid tokens return 401; tokens without an accepted role return 403.

| Subsystem | Intent | Command | Normal user |
| --- | --- | --- | --- |
| GPIO | IntentHash1 | ReadPinState | Allowed |
| GPIO | IntentHash2 | SetPinState | Denied |
| ADC | IntentHash3 | ReadADCChannel | Allowed |
| Memory | IntentHash4 | ReadMemoryRegion | Allowed |
| FirmwareManager | IntentHash5 | GetFirmwareVersion | Allowed |
| FirmwareManager | IntentHash6 | RequestFirmwareUpdate | Denied |
| SystemMonitor | IntentHash7 | ReadSystemHealth | Allowed |

The admin role may request all seven operations, subject to the same control-plane validation. The normal user's permissions use an explicit subsystem-and-command allowlist; new commands are denied until reviewed. These are policy decisions only: this backend does not execute or verify hardware operations. The current target check is at subsystem level; it does not constrain individual pins, addresses, memory ranges, or other hardware resources. A hardware executor must enforce the decision and independently enforce resource-level safety.

### Catalog and secret changes

IgAPI validates the plain catalogs, hashed catalogs, subsystem/command mappings, and configured secret on startup. If the secret or catalog changes, regenerate both HMAC files with `dotnet run --project IgniteAuth -- --generate-hashes`, then rebuild IgAPI so its output receives the updated files. Keep the key and corresponding hashed catalogs together. A mismatch stops startup rather than silently denying all intents.

### Response logs

IgAPI writes one JSON record per HTTP response to `logs/IgniteAuth.log` in the solution directory, or under its content root when running without the solution directory. Each record includes the UTC time, HTTP status, duration, trace ID, and authenticated user and role when available. Control-plane decisions also include the subsystem, target, intent, command, verdict, and reason code. Login bodies, authorization headers, query strings, and response bodies are not logged; control-plane fields are logged as submitted. Runtime logs are excluded from Git; manage retention and filesystem access for the deployment.

### Security and testing limits

These two fixed, publicly documented passwords are for controlled rapid testing. They must be replaced by managed accounts and changed before exposing this API to untrusted users. Never deploy the example intent secret or JWT key. In Development, the sample JWT key is replaced with a random per-process key, so tokens stop working after restart unless a private `Jwt:Key` is configured. Outside Development, the sample JWT key is rejected; set a unique private value, for example with the `Jwt__Key` environment variable. Use HTTPS and protect signing keys.

Run core checks with `dotnet run --project tests/IgniteAuth.SmokeTests`. With IgAPI running, run `./tests/http-smoke.ps1 -BaseUrl http://localhost:5249` from PowerShell. The HTTP test with `-JwtKey <private-test-key>` additionally checks signed tokens with missing and unknown roles; supply the same temporary test key to IgAPI via `Jwt__Key`.

---

## Core Idea

A system request can be represented as:

```text
User / Client
      │
      │  Intent + Command + Target
      ▼
┌──────────────────────┐
│   IgniteAuth Control │
│        Plane         │
└──────────┬───────────┘
           │
           ├── Authentication
           ├── Intent Validation
           ├── Command Validation
           ├── Intent → Command Mapping
           └── Policy Evaluation
                    │
                    ▼
              ALLOW / DENY
                    │
                    ▼
              Target System
```

The objective is to move security enforcement closer to the **control plane of a system**, rather than relying only on application-level authentication or authorization.

---

## v0.9 Validation Pipeline

A request is evaluated through multiple independent checks:

### 1. Intent Validation

The incoming plain-text intent is canonicalized and converted using HMAC-SHA256.

```text
Plain Intent
     ↓
Canonicalization
     ↓
HMAC-SHA256
     ↓
Registered Intent Hash
     ↓
Valid / Invalid
```

The client can provide a human-readable intent while the Control Plane performs deterministic verification using server-controlled policy data.

### 2. Command Validation

The requested operation is checked against the commands registered for the target subsystem.

```text
Subsystem → Allowed Commands
```

### 3. Intent → Command Validation

A valid intent alone is not sufficient.

IgniteAuth verifies that the requested command is actually associated with the declared intent.

```text
Intent
  │
  └──► Command
          │
          └──► Target
```

### 4. Final Decision

The Control Plane produces a final authorization decision after checking the caller, payload, reference system and subsystem target:

```text
Valid Caller, Payload and Target
     AND
Intent Valid
     AND
Command Valid
     AND
Intent ↔ Command Valid
     │
     ▼
   ALLOW
```

Any failed validation results in:

```text
DENY
```

---

## Example Request

See the STM32 request and complete operation table in [Rapid test setup](#rapid-test-setup-stm32-reference-system).

---

## Architecture

```text
                    CLIENT
                      │
                      ▼
              ┌───────────────┐
              │     IgAPI     │
              │ ASP.NET Core  │
              └───────┬───────┘
                      │
                      ▼
             ┌─────────────────┐
             │ IgniteAuth Core │
             └────────┬────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   IntentValidator  CommandValidator  IntentCommandMapper
        │             │             │
        └─────────────┼─────────────┘
                      ▼
               Decision Engine
                      │
                 ALLOW / DENY
```

---

## Project Structure

```text
IgniteAuth_V0.9
│
├── IgAPI/
│   └── ASP.NET Core API layer
│
├── IgniteAuth/
│   ├── Data/
│   │   ├── IntentHashes.json
│   │   ├── CommandData.json
│   │   └── IntentCommandMapping.json
│   │
│   ├── Processors/
│   │   └── Validation/
│   │       ├── IntentValidator
│   │       ├── CommandValidator
│   │       └── IntentCommandMapper
│   │
│   └── Utilities/
│       └── IntentHasher
│
└── IgniteAuth_V0.9.slnx
```

---

## Cryptographic Intent Representation

IgniteAuth uses HMAC-SHA256 to generate deterministic representations of registered intents.

Conceptually:

```text
CanonicalIntent + ServerSecret
            ↓
       HMAC-SHA256
            ↓
        IntentHash
```

The secret key is controlled by the server/control plane and should not be embedded in client applications or committed to source control.

For development, the implementation can read the key from:

```text
IGNITE_AUTH_SECRET
```

Production deployments should use an appropriate secret-management mechanism.

---

## Persistent Research Analytics

Every completed authorization evaluation is persisted by IgAPI to an append-only, schema-versioned CSV file:

```text
logs/analytics/control-plane-decisions.v1.csv
```

The dataset contains UTC time, event and trace identifiers, the server-issued CPC ID, reference system, authenticated role, subsystem, target, intent, command, decision, reason code, and policy-processing duration. Usernames and credentials are not stored. CSV cells are quoted and spreadsheet formula prefixes are neutralized on disk.

Authenticated `Admin` and `User` sessions can read the latest observation window through either route:

```http
GET /api/analytics?limit=100&fromUtc=2026-10-01T00:00:00Z&subsystem=GPIO&decision=Allow
GET /analytics?limit=100
```

`limit` defaults to 100 and is capped at 1000. Filters are optional. Aggregates describe the returned window, while `totalStoredObservations` and `matchedObservations` distinguish total and filtered dataset sizes.

Latency is measured around policy evaluation with `Stopwatch`, a monotonic server clock. It excludes network transfer, response serialization, and CSV persistence. For reproducible results, report the application version, host, workload, filters, observation window, and sample size.

The CSV implementation serializes access inside one IgAPI process and is appropriate for a single-node research deployment. Archive or rotate the file between experiments when a clean cohort is required. Multi-process or multi-host deployments should replace the store with a transactional database or telemetry pipeline while retaining the event schema. A decision response is not returned if its analytics observation cannot be persisted, preventing apparently successful but unrecorded trials.

The operational JSON log and the analytics CSV have different purposes: the JSON log is for diagnostics and security operations; the CSV is a stable rectangular dataset for statistical analysis.

---

## Design Principles

### Deny by Default

An operation is not permitted simply because the requester is authenticated.

The requested operation must satisfy the Control Plane's validation rules.

### Intent Before Execution

The framework treats the **purpose of an operation** as a security-relevant property.

### Explicit Relationships

IgniteAuth maintains explicit relationships between:

```text
Intent → Command → Target
```

rather than treating authorization as a single boolean attached to a user.

### Control Plane Enforcement

The client expresses the request.

The Control Plane owns the validation and authorization decision.

### Deterministic v0.9

The current version deliberately uses deterministic policy validation.

This provides a predictable foundation before introducing more context-aware decision mechanisms.

---

## Current Scope

IgniteAuth v0.9 focuses on:

* Intent representation and validation
* HMAC-SHA256 based intent verification
* Command validation
* Intent-to-command mapping
* Subsystem-aware validation
* Deterministic ALLOW/DENY decisions
* ASP.NET Core integration
* Separation between client request semantics and Control Plane enforcement

---

## Future Direction

Future versions are intended to extend the deterministic Control Plane with **context-aware policy evaluation**.

Potential inputs include:

```text
Intent
Command
Target
User / Identity
Request Source
Runtime Context
System State
Historical Activity
Policy
```

The long-term direction is to investigate whether intelligent or agentic validation can evaluate **intent and context together**, while keeping the final enforcement boundary inside the Control Plane.

The goal is not to replace deterministic security controls with an autonomous agent.

The goal is to build a Control Plane capable of making increasingly context-aware security decisions while preserving explicit enforcement boundaries.

---

## Status

**Version:** v0.9
**Implementation:** ASP.NET Core / C#
**Security Model:** Intent-based Control Plane authorization
**Policy Model:** Deterministic / static validation
**Cryptographic Primitive:** HMAC-SHA256

IgniteAuth is an evolving research and engineering project exploring how authorization can move from:

```text
"Who are you?"
```

toward:

```text
"What are you trying to do,
where are you trying to do it,
and should the system permit it?"
```

---

## Author

**IgniteAuth**

A long-term exploration of control-plane security, intent validation, and system-level authorization.
