#!/bin/sh

set -eu

MONGO_HOST="${MONGO_HOST:-mongodb}"
MONGO_PORT="${MONGO_PORT:-27017}"
MONGO_REPLICA_SET_NAME="${MONGO_REPLICA_SET_NAME:-rs0}"

MONGO_MEMBER="${MONGO_HOST}:${MONGO_PORT}"

MAX_ATTEMPTS="${MONGO_INIT_MAX_ATTEMPTS:-30}"
SLEEP_SECONDS="${MONGO_INIT_SLEEP_SECONDS:-2}"

echo "Waiting for MongoDB at ${MONGO_MEMBER}..."

attempt=1

while [ "$attempt" -le "$MAX_ATTEMPTS" ]; do
  if mongosh \
    --host "$MONGO_HOST" \
    --port "$MONGO_PORT" \
    --quiet \
    --eval '
      const result = db.adminCommand({ ping: 1 });

      if (result.ok !== 1) {
        throw new Error("MongoDB ping failed");
      }
    ' >/dev/null 2>&1; then
    break
  fi

  echo "MongoDB is not reachable yet (${attempt}/${MAX_ATTEMPTS})."

  attempt=$((attempt + 1))
  sleep "$SLEEP_SECONDS"
done

if [ "$attempt" -gt "$MAX_ATTEMPTS" ]; then
  echo "MongoDB did not become reachable in time." >&2
  exit 1
fi

echo "MongoDB is reachable. Checking replica-set state..."

REPLICA_SET_STATE="$(
  mongosh \
    --host "$MONGO_HOST" \
    --port "$MONGO_PORT" \
    --quiet \
    --eval "
      try {
        const status = db.adminCommand({ replSetGetStatus: 1 });

        if (status.set !== '${MONGO_REPLICA_SET_NAME}') {
          throw new Error(
            'Unexpected replica set: ' + status.set
          );
        }

        print('INITIALIZED');
      } catch (error) {
        if (
          error.code === 94 ||
          error.codeName === 'NotYetInitialized'
        ) {
          print('NOT_INITIALIZED');
        } else {
          throw error;
        }
      }
    "
)"

case "$REPLICA_SET_STATE" in
  NOT_INITIALIZED)
    echo "Replica set ${MONGO_REPLICA_SET_NAME} is not initialized."
    echo "Initializing ${MONGO_REPLICA_SET_NAME} with member ${MONGO_MEMBER}..."

    mongosh \
      --host "$MONGO_HOST" \
      --port "$MONGO_PORT" \
      --quiet \
      --eval "
        const result = db.adminCommand({
          replSetInitiate: {
            _id: '${MONGO_REPLICA_SET_NAME}',
            members: [
              {
                _id: 0,
                host: '${MONGO_MEMBER}'
              }
            ]
          }
        });

        if (result.ok !== 1) {
          throw new Error('Replica-set initialization failed');
        }
      "

    ;;

  INITIALIZED)
    echo "Replica set ${MONGO_REPLICA_SET_NAME} is already initialized."
    ;;

  *)
    echo "Unexpected replica-set state: ${REPLICA_SET_STATE}" >&2
    exit 1
    ;;
esac

echo "Waiting for ${MONGO_REPLICA_SET_NAME} to elect this node as PRIMARY..."

attempt=1

while [ "$attempt" -le "$MAX_ATTEMPTS" ]; do
  if mongosh \
    --host "$MONGO_HOST" \
    --port "$MONGO_PORT" \
    --quiet \
    --eval "
      const status = db.adminCommand({ replSetGetStatus: 1 });

      if (
        status.set !== '${MONGO_REPLICA_SET_NAME}' ||
        status.myState !== 1
      ) {
        throw new Error('Replica set is not PRIMARY yet');
      }
    " >/dev/null 2>&1; then
    break
  fi

  echo "Replica set is not PRIMARY yet (${attempt}/${MAX_ATTEMPTS})."

  attempt=$((attempt + 1))
  sleep "$SLEEP_SECONDS"
done

if [ "$attempt" -gt "$MAX_ATTEMPTS" ]; then
  echo "Replica set did not become PRIMARY in time." >&2
  exit 1
fi

echo "Validating replica-set configuration..."

mongosh \
  --host "$MONGO_HOST" \
  --port "$MONGO_PORT" \
  --quiet \
  --eval "
    const result = db.adminCommand({ replSetGetConfig: 1 });
    const config = result.config;

    if (config._id !== '${MONGO_REPLICA_SET_NAME}') {
      throw new Error(
        'Unexpected replica-set name: ' + config._id
      );
    }

    if (config.members.length !== 1) {
      throw new Error(
        'Expected exactly one replica-set member'
      );
    }

    const member = config.members[0];

    if (
      member._id !== 0 ||
      member.host !== '${MONGO_MEMBER}'
    ) {
      throw new Error(
        'Unexpected replica-set member configuration'
      );
    }
  " >/dev/null

echo "Replica set ${MONGO_REPLICA_SET_NAME} is ready with ${MONGO_MEMBER} as PRIMARY."