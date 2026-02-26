#!/bin/bash
set -e

SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S ${SQL_SERVER} -U sa -P ${SA_PASSWORD} -C -b"
BACKUP_DIR="/backups"
INTERVAL_SECONDS=$(( ${BACKUP_INTERVAL_HOURS:-6} * 3600 ))
RETENTION_DAYS=7

DATABASES=(
  "WatchCoreGatewayDB"
  "WatchVoiceEmergencyDB"
  "WatchMeshNetworkDB"
  "WatchWearableDB"
  "WatchAuthSecurityDB"
  "WatchFirstResponderDB"
  "WatchFamilyHealthDB"
  "WatchDisasterReliefDB"
  "WatchDoctorServicesDB"
  "WatchGamificationDB"
)

run_full_backup() {
  local TIMESTAMP=$(date +%Y%m%d_%H%M%S)
  echo ""
  echo "=========================================="
  echo "  FULL BACKUP - ${TIMESTAMP}"
  echo "=========================================="

  for DB in "${DATABASES[@]}"; do
    local BACKUP_FILE="${BACKUP_DIR}/${DB}_FULL_${TIMESTAMP}.bak"
    echo "[$(date +%H:%M:%S)] Backing up ${DB}..."

    $SQLCMD -Q "
      BACKUP DATABASE [${DB}]
      TO DISK = '${BACKUP_FILE}'
      WITH FORMAT,
           INIT,
           NAME = '${DB} Full Backup ${TIMESTAMP}',
           COMPRESSION,
           CHECKSUM,
           STATS = 25;
    "

    if [ $? -eq 0 ]; then
      local SIZE=$(du -h "${BACKUP_FILE}" 2>/dev/null | cut -f1)
      echo "[$(date +%H:%M:%S)] OK: ${DB} -> ${BACKUP_FILE} (${SIZE})"
    else
      echo "[$(date +%H:%M:%S)] FAILED: ${DB}"
    fi
  done

  # Verify latest backups
  echo ""
  echo "--- Verifying backups ---"
  for DB in "${DATABASES[@]}"; do
    $SQLCMD -Q "
      RESTORE VERIFYONLY
      FROM DISK = '${BACKUP_DIR}/${DB}_FULL_${TIMESTAMP}.bak'
      WITH CHECKSUM;
    " && echo "  VERIFIED: ${DB}" || echo "  VERIFY FAILED: ${DB}"
  done

  echo ""
  echo "Full backup complete at $(date)"
}

cleanup_old_backups() {
  echo ""
  echo "--- Cleaning up backups older than ${RETENTION_DAYS} days ---"
  local COUNT=$(find "${BACKUP_DIR}" -name "*.bak" -mtime +${RETENTION_DAYS} 2>/dev/null | wc -l)
  if [ "$COUNT" -gt 0 ]; then
    find "${BACKUP_DIR}" -name "*.bak" -mtime +${RETENTION_DAYS} -delete
    echo "Removed ${COUNT} old backup files"
  else
    echo "No old backups to clean up"
  fi
}

echo "=== TheWatch SQL Server Backup Scheduler ==="
echo "Interval: every ${BACKUP_INTERVAL_HOURS:-6} hours"
echo "Retention: ${RETENTION_DAYS} days"
echo "Backup dir: ${BACKUP_DIR}"
echo ""

# Run initial backup immediately
run_full_backup
cleanup_old_backups

# Then loop on schedule
while true; do
  echo ""
  echo "Next backup in ${BACKUP_INTERVAL_HOURS:-6} hours ($(date -d "+${BACKUP_INTERVAL_HOURS:-6} hours" 2>/dev/null || echo 'scheduled'))"
  sleep ${INTERVAL_SECONDS}
  run_full_backup
  cleanup_old_backups
done
