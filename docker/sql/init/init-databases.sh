#!/bin/bash
set -e

echo "=== TheWatch Database Initialization ==="
echo "Waiting for SQL Server to be fully ready..."
sleep 5

SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S ${SQL_SERVER} -U sa -P ${SA_PASSWORD} -C -b"

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

for DB in "${DATABASES[@]}"; do
  echo "Creating database: ${DB}"
  $SQLCMD -Q "
    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '${DB}')
    BEGIN
      CREATE DATABASE [${DB}];
      PRINT 'Created ${DB}';
    END
    ELSE
      PRINT '${DB} already exists';
  "
done

# Set recovery model to FULL for proper backup chains
for DB in "${DATABASES[@]}"; do
  echo "Setting FULL recovery model for: ${DB}"
  $SQLCMD -Q "ALTER DATABASE [${DB}] SET RECOVERY FULL;"
done

echo ""
echo "=== All 10 TheWatch databases initialized ==="
$SQLCMD -Q "SELECT name, state_desc, recovery_model_desc FROM sys.databases WHERE name LIKE 'Watch%' ORDER BY name;"
