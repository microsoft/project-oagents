#!/bin/bash

curl -k https://localhost:8081/_explorer/emulator.pem > ~/emulatorcert.crt
sudo cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates
sleep 5
dotnet run --project util/seed-memory/seed-memory.csproj