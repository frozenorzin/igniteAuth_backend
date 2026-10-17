# Security implementation status

IgAPI authenticates two fixed test accounts, issues short-lived JWTs with roles, and checks an explicit read-only command allowlist for the normal role. IgniteAuth checks request fields, subsystem targets, intents, commands and mappings. Startup validates the catalog and HMAC secret.

This is a test setup, not a production-ready security system. The fixed passwords and example secrets are public. Replace the accounts with managed authentication, use private secrets and HTTPS, and add hardware resource-level authorization and enforcement before exposing the system. See [README](readme.md#rapid-test-setup-stm32-reference-system) for setup, permissions and testing.
