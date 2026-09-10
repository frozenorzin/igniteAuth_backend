# IgniteAuth v0.9

## Control-Plane Security Framework for Intent-Based Operation Authorization

IgniteAuth is a security framework designed around a simple principle:

> **Authentication establishes who is making a request. IgniteAuth evaluates what the system is being asked to do, why it is being requested, and whether that operation should be permitted.**

IgniteAuth v0.9 implements a deterministic Control Plane that validates **Intent, Command, Target, and Intent-to-Command relationships** before allowing an operation to proceed.

The current implementation uses **ASP.NET Core, C#, and structured policy data**, with HMAC-SHA256 used to represent and verify registered intents.

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

The Control Plane produces a final authorization decision:

```text
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

A client may submit a request in plain semantic form:

```json
{
  "uid": "apex",
  "intent": "VerificationAndTesting",
  "command": "WriteToModel",
  "target": "subSystem_x"
}
```

The client does not need to understand the internal policy representation.

The Control Plane performs the canonicalization, cryptographic transformation, lookup, and policy validation.

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
