#!/bin/bash

echo "=========================================="
echo "        NOAZUL MIGRATOR"
echo "=========================================="
echo

# Vai para a pasta onde está o script
cd "$(dirname "$0")"

dotnet ef database update \
  --project src/NoAzul.Infra.Data \
  --startup-project src/NoAzul.Infra.Data

echo
echo "Done."
