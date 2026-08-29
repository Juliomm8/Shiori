# Local Infrastructure

This document is the operational runbook for the Shiori Milestone 1 local infrastructure baseline.

It covers the Docker Compose infrastructure owned by M1-004:

- Identity PostgreSQL
- Tracking PostgreSQL
- Catalog MongoDB
- RabbitMQ

Application services such as Gateway, Identity API, Catalog API, and Tracking API are not part of this runbook yet.

---

## Prerequisites

Before starting the local infrastructure, install:

- Git
- Docker Desktop with Docker Compose support
- PowerShell on Windows, or an equivalent shell capable of supplying environment variables

Verify Docker is available:

```powershell
docker version
docker compose version
```

Docker Desktop must be running before starting the stack.

---

## Required local environment variables

Docker Compose requires three sensitive values:

```text
SHIORI_IDENTITY_POSTGRES_PASSWORD
SHIORI_TRACKING_POSTGRES_PASSWORD
SHIORI_RABBITMQ_PASSWORD
```

These values must not be committed to source control.

For a temporary PowerShell session, they can be entered without placing the values directly in command history:

```powershell
$identityPassword = Read-Host "Identity PostgreSQL password" -AsSecureString
$env:SHIORI_IDENTITY_POSTGRES_PASSWORD =
    [System.Net.NetworkCredential]::new("", $identityPassword).Password

$trackingPassword = Read-Host "Tracking PostgreSQL password" -AsSecureString
$env:SHIORI_TRACKING_POSTGRES_PASSWORD =
    [System.Net.NetworkCredential]::new("", $trackingPassword).Password

$rabbitPassword = Read-Host "RabbitMQ password" -AsSecureString
$env:SHIORI_RABBITMQ_PASSWORD =
    [System.Net.NetworkCredential]::new("", $rabbitPassword).Password

Remove-Variable identityPassword, trackingPassword, rabbitPassword
```

The resulting environment variables exist only in the current PowerShell process and its child processes.

The repository intentionally does not define the project-wide environment/secrets convention here.

> Global secrets and environment configuration are **OUT OF SCOPE — M1-005**.

Before starting Docker Compose, confirm that the variables exist without printing their values:

```powershell
$env:SHIORI_IDENTITY_POSTGRES_PASSWORD.Length
$env:SHIORI_TRACKING_POSTGRES_PASSWORD.Length
$env:SHIORI_RABBITMQ_PASSWORD.Length
```

All three commands should return a value greater than zero.

---

## Validate the Compose configuration

Before starting containers:

```powershell
docker compose config --quiet
$LASTEXITCODE
```

Expected exit code:

```text
0
```

A missing required password variable causes Compose configuration validation to fail before any container is started.

---

## Start the local infrastructure

Start the complete infrastructure baseline:

```powershell
docker compose up -d
```

The stack exposes:

| Service | Host endpoint | Purpose |
| --- | --- | --- |
| Identity PostgreSQL | `localhost:5432` | Identity-owned relational datastore |
| Tracking PostgreSQL | `localhost:5433` | Tracking-owned relational datastore |
| MongoDB | `localhost:27017` | Catalog-owned document datastore |
| RabbitMQ | `localhost:5672` | AMQP client connectivity |
| RabbitMQ Management | `http://localhost:15672` | Development management UI / HTTP API |

MongoDB also uses the one-shot `mongodb-init` helper to bootstrap or verify the local single-node replica set.

`mongodb-init` is expected to terminate successfully after its work is complete.

---

## Check container status

Use:

```powershell
docker compose ps -a
```

The persistent infrastructure containers should report healthy:

```text
postgres-identity   healthy
postgres-tracking   healthy
mongodb             healthy
rabbitmq            healthy
```

The MongoDB initialization helper should report:

```text
mongodb-init        Exited (0)
```

`Exited (0)` is the expected successful state for this one-shot helper.

A running container is not by itself considered sufficient readiness. The Compose health checks verify service-specific operational state.

---

## Verify Identity PostgreSQL

Verify that the Identity credentials authenticate against the Identity-owned database:

```powershell
docker compose exec `
  -e PGPASSWORD=$env:SHIORI_IDENTITY_POSTGRES_PASSWORD `
  postgres-identity `
  psql -w -h 127.0.0.1 -U shiori_identity -d shiori_identity `
  -Atc "SELECT current_user || '|' || current_database();"
```

Expected output:

```text
shiori_identity|shiori_identity
```

---

## Verify Tracking PostgreSQL

Verify that the Tracking credentials authenticate against the Tracking-owned database:

```powershell
docker compose exec `
  -e PGPASSWORD=$env:SHIORI_TRACKING_POSTGRES_PASSWORD `
  postgres-tracking `
  psql -w -h 127.0.0.1 -U shiori_tracking -d shiori_tracking `
  -Atc "SELECT current_user || '|' || current_database();"
```

Expected output:

```text
shiori_tracking|shiori_tracking
```

---

## Verify PostgreSQL credential isolation

Identity and Tracking intentionally use different PostgreSQL identities and passwords.

The following negative check must fail:

```powershell
docker compose exec `
  -e PGPASSWORD=$env:SHIORI_IDENTITY_POSTGRES_PASSWORD `
  postgres-identity `
  psql -w -h postgres-tracking -U shiori_identity -d shiori_tracking `
  -Atc "SELECT 1;"
```

Expected result:

```text
FATAL: password authentication failed for user "shiori_identity"
```

The command should return a non-zero exit code.

The inverse check must also fail:

```powershell
docker compose exec `
  -e PGPASSWORD=$env:SHIORI_TRACKING_POSTGRES_PASSWORD `
  postgres-tracking `
  psql -w -h postgres-identity -U shiori_tracking -d shiori_identity `
  -Atc "SELECT 1;"
```

Expected result:

```text
FATAL: password authentication failed for user "shiori_tracking"
```

These checks verify that network reachability does not imply shared database ownership or shared credentials.

---

## Verify MongoDB replica-set state

MongoDB runs as a single-node replica set named `rs0`.

Verify the current state:

```powershell
docker compose exec mongodb mongosh --quiet --eval `
  "const s = rs.status(); print(s.set + '|' + s.myState + '|' + s.members.length + '|' + s.members[0].name + '|' + s.members[0].stateStr);"
```

Expected output:

```text
rs0|1|1|mongodb:27017|PRIMARY
```

This verifies:

- replica set name is `rs0`
- the local node state is `PRIMARY`
- exactly one member is configured
- the member uses the deterministic Compose DNS identity `mongodb:27017`

No manual `rs.initiate()` command should be required after normal startup.

To inspect the bootstrap helper:

```powershell
docker compose logs mongodb-init
```

On a clean volume, the log should show automatic initialization.

On an existing valid volume, the helper should report that the replica set is already initialized and then verify it.

---

## Verify RabbitMQ

Verify that the RabbitMQ application is running:

```powershell
docker compose exec rabbitmq rabbitmq-diagnostics -q check_running
```

Verify listener connectivity:

```powershell
docker compose exec rabbitmq rabbitmq-diagnostics -q check_port_connectivity
```

Both commands should return exit code `0`.

### Verify client readiness through the management API

Build an HTTP Basic Authorization header without printing the password:

```powershell
$rabbitCredentials = "shiori:$env:SHIORI_RABBITMQ_PASSWORD"
$rabbitToken = [Convert]::ToBase64String(
    [Text.Encoding]::ASCII.GetBytes($rabbitCredentials)
)

$headers = @{
    Authorization = "Basic $rabbitToken"
}
```

Call RabbitMQ's readiness endpoint:

```powershell
$response = Invoke-WebRequest `
    -Uri "http://localhost:15672/api/health/checks/ready-to-serve-clients" `
    -Headers $headers

$response.StatusCode
$response.Content
```

Expected result:

```text
200
{"status":"ok"}
```

Remove the temporary local variables afterward:

```powershell
Remove-Variable rabbitCredentials, rabbitToken, headers, response
```

The RabbitMQ development management UI is available at:

```text
http://localhost:15672
```

Use username:

```text
shiori
```

and the value stored in `SHIORI_RABBITMQ_PASSWORD`.

---

## Inspect logs

Show logs for the complete stack:

```powershell
docker compose logs
```

Follow logs continuously:

```powershell
docker compose logs -f
```

Inspect one service:

```powershell
docker compose logs postgres-identity
docker compose logs postgres-tracking
docker compose logs mongodb
docker compose logs mongodb-init
docker compose logs rabbitmq
```

Use `Ctrl+C` to stop following logs. This does not stop the containers.

---

## Normal restart

A normal restart must preserve all named-volume state.

Restart the full infrastructure stack:

```powershell
docker compose restart
```

Then verify status:

```powershell
docker compose ps -a
```

The persistent services should return to `healthy`.

MongoDB should remain the same replica set after restart:

```powershell
docker compose exec mongodb mongosh --quiet --eval `
  "const s = rs.status(); print(s.set + '|' + s.myState + '|' + s.members.length + '|' + s.members[0].name + '|' + s.members[0].stateStr);"
```

Expected:

```text
rs0|1|1|mongodb:27017|PRIMARY
```

`mongodb-init` is not required to reinitialize an already-persisted replica set.

If the helper is run again, it must behave idempotently and verify the existing configuration instead of recreating it.

---

## Stop the local infrastructure

Stop containers without deleting them or their named volumes:

```powershell
docker compose stop
```

Start them again later:

```powershell
docker compose start
```

Alternatively, remove the containers and network while preserving named volumes:

```powershell
docker compose down
```

A later:

```powershell
docker compose up -d
```

recreates the containers while reusing persisted named volumes.

---

## Clean reset

A clean reset is destructive.

It removes the local PostgreSQL, MongoDB, and RabbitMQ named volumes and therefore deletes all local infrastructure data.

Do not run this command if the local data must be preserved.

First stop and remove the Compose environment and its named volumes:

```powershell
docker compose down -v
```

Then recreate the environment:

```powershell
docker compose up -d
```

Check status:

```powershell
docker compose ps -a
```

Expected final state:

```text
postgres-identity   healthy
postgres-tracking   healthy
mongodb             healthy
mongodb-init        Exited (0)
rabbitmq            healthy
```

Verify MongoDB again:

```powershell
docker compose exec mongodb mongosh --quiet --eval `
  "const s = rs.status(); print(s.set + '|' + s.myState + '|' + s.members.length + '|' + s.members[0].name + '|' + s.members[0].stateStr);"
```

Expected:

```text
rs0|1|1|mongodb:27017|PRIMARY
```

The clean reset should require no undocumented manual repair command.

---

## Targeted MongoDB reset

If only the local MongoDB state needs to be recreated, stop and remove only MongoDB and its one-shot helper:

```powershell
docker compose stop mongodb mongodb-init
docker compose rm -f mongodb mongodb-init
```

Remove only the MongoDB named volume:

```powershell
docker volume rm shiori-mongodb-data
```

Recreate MongoDB:

```powershell
docker compose up -d mongodb mongodb-init
```

Verify:

```powershell
docker compose ps -a
docker compose logs mongodb-init
```

The helper should initialize `rs0` automatically and finish with exit code `0`.

---

## Named volumes

The local infrastructure uses deterministic named volumes:

```text
shiori-postgres-identity-data
shiori-postgres-tracking-data
shiori-mongodb-data
shiori-rabbitmq-data
```

List them with:

```powershell
docker volume ls --filter name=shiori-
```

These volumes are development state only.

Production backup, restore, high availability, clustering, and cloud deployment topology are outside M1-004.

---

## Network and deterministic service identities

The Compose environment uses:

```text
shiori-network
```

Containers communicate through Compose service DNS names.

Relevant internal endpoints are:

```text
postgres-identity:5432
postgres-tracking:5432
mongodb:27017
rabbitmq:5672
```

Host port mappings are intentionally separate from container-to-container addressing.

For example:

```text
developer machine -> localhost:5433 -> Tracking PostgreSQL container:5432
```

while another container reaches the same database through:

```text
postgres-tracking:5432
```

---

## Scope boundaries

M1-004 establishes only the local infrastructure baseline.

The following remain outside this runbook:

- Catalog collections, indexes, validators, and document schemas
- MongoDB Change Stream consumers
- Identity or Tracking schemas and migrations
- RabbitMQ exchanges, queues, routing keys, Outbox/Inbox, or DLQ policy
- production MongoDB or RabbitMQ HA
- Kubernetes or cloud topology
- production backup/restore
- application service implementation
- project-wide environment and secrets conventions

Project-wide environment/secrets handling remains:

> **OUT OF SCOPE — M1-005**
