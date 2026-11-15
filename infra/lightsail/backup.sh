#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")"

BUCKET=${BACKUP_BUCKET:-noazul-backups}
PREFIX=${BACKUP_PREFIX:-postgres}
STAMP=$(date -u +%Y%m%d-%H%M%S)
FILE=/tmp/noazul-$STAMP.dump

docker compose exec -T postgres pg_dump -Fc -U noazul noazul > "$FILE"
aws s3 cp "$FILE" "s3://$BUCKET/$PREFIX/noazul-$STAMP.dump" --only-show-errors
rm -f "$FILE"

echo "backup ok: $PREFIX/noazul-$STAMP.dump"
