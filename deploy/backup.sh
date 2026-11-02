#!/usr/bin/env bash
# Cópia do banco (implantação, Entrega F): pg_dump -Fc executado no contêiner db, gravado em
# /opt/koinoniahub/backups/koinoniahub-AAAAMMDD-HHMM.dump, com retenção de 7 dias.
# Usuário e banco vêm do ambiente do próprio contêiner (POSTGRES_USER, POSTGRES_DB).
#
# Cron sugerido (todo dia às 03:00), em `sudo crontab -e`:
#   0 3 * * * /opt/koinoniahub/app/deploy/backup.sh >> /var/log/koinoniahub-backup.log 2>&1
#
# Restauração: deploy/README.md.
set -euo pipefail

APP_DIR="${KOINONIAHUB_APP_DIR:-/opt/koinoniahub/app}"
BACKUP_DIR="${KOINONIAHUB_BACKUP_DIR:-/opt/koinoniahub/backups}"
RETENCAO_DIAS=7

cd "$APP_DIR"
mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"

arquivo="$BACKUP_DIR/koinoniahub-$(date +%Y%m%d-%H%M).dump"
docker compose -f docker-compose.prod.yml exec -T db \
    sh -c 'pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB"' > "$arquivo"

find "$BACKUP_DIR" -name 'koinoniahub-*.dump' -type f -mtime +"$RETENCAO_DIAS" -delete

echo "$(date '+%F %T') copia gravada: $arquivo ($(du -h "$arquivo" | cut -f1))"
