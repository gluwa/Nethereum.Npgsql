#!/usr/bin/env bash
# Cross-runtime migration check: one database is migrated and written by EF Core 8 (net8.0), then
# EF Core 10 (net10.0), then EF Core 8 again (upgrade + rollback), and the reverse order, on each
# PostgreSQL major version. Requires Docker and the .NET 8 and 10 SDKs.
#
#   ./run-cross-runtime.sh            # PostgreSQL 15 16 17 18
#   ./run-cross-runtime.sh 16 17      # selected versions
set -euo pipefail

cd "$(dirname "$0")"
versions=("${@:-15 16 17 18}")
read -r -a versions <<< "${versions[*]}"
password="$(od -An -N12 -tx1 /dev/urandom | tr -d ' \n')"
orders=("net8.0 net10.0 net8.0" "net10.0 net8.0 net10.0")
stages=(Stage1_seed_on_initial_migration Stage2_upgrade_to_latest_and_write Stage3_switch_back_and_verify)

dotnet build -nologo -v q -clp:ErrorsOnly

failed=0
for pg in "${versions[@]}"; do
  name="bigdecimal-cross-pg$pg-$$"
  docker run -d --rm --name "$name" -e POSTGRES_PASSWORD="$password" -p 127.0.0.1::5432 "postgres:$pg" > /dev/null
  trap 'docker rm -f "$name" > /dev/null 2>&1 || true' EXIT
  port="$(docker port "$name" 5432/tcp | head -1 | sed 's/.*://')"
  until docker exec "$name" pg_isready -U postgres -q; do sleep 1; done
  sleep 1

  for order in "${orders[@]}"; do
    read -r -a tfms <<< "$order"
    db="cross_$(echo "$order" | tr -dc '0-9')"
    docker exec "$name" psql -U postgres -qc "CREATE DATABASE $db"
    export MIGRATION_CROSS_CS="Host=127.0.0.1;Port=$port;Username=postgres;Password=$password;Database=$db"
    for i in 0 1 2; do
      label="PG$pg [${order// / -> }] ${stages[$i]} on ${tfms[$i]}"
      if dotnet test --no-build -nologo -f "${tfms[$i]}" --filter "Name=${stages[$i]}" --logger "console;verbosity=minimal" > "cross.log" 2>&1 \
         && grep -q "Passed!" cross.log; then
        echo "PASS  $label"
      else
        echo "FAIL  $label"; sed -n '/Error Message/,/Stack Trace/p' cross.log | head -20; failed=1; break
      fi
    done
  done
  docker rm -f "$name" > /dev/null
done
rm -f cross.log
exit $failed
