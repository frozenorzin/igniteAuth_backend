# Secret configuration

See the [rapid test setup](readme.md#rapid-test-setup-stm32-reference-system) for the complete workflow.

Set `IGNITE_AUTH_SECRET` through the environment or an untracked `.env` file. The value must match the secret used to generate both hashed intent catalogs. After changing the secret or plain policy catalogs, run `dotnet run --project IgniteAuth -- --generate-hashes` and rebuild IgAPI. IgAPI checks consistency on startup and rejects a mismatch.

The example intent secret is public and only suitable for local tests. Use a strong, private secret and secure storage for any real deployment. The sample JWT key is never used for signing: Development generates an ephemeral key unless a private `Jwt:Key` is configured; other environments require a private key. The fixed demo accounts are also publicly known and must be replaced before exposing the service.
