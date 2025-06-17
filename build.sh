#!/bin/bash

USER="WindowsVM"
IP="192.168.122.65"
VM_NAME="win10"
STEAM_GAME_ID="312520"


# Measure total build time
start_time=$(date +%s)

if ! pgrep -x "gnome-boxes" > /dev/null; then
  echo "Launching GNOME Boxes..."
  nohup gnome-boxes >/dev/null 2>&1 &
  sleep 3  # Give it a moment to get going
fi

# Start VM (if not running)
virsh list --name | grep -q "^${VM_NAME}$" || virsh start ${VM_NAME}

# Wait for Windows to boot and SSH to be available
while ! sshpass -p "ssh" ssh -o ConnectTimeout=5 ${USER}@${IP} "echo 1" &>/dev/null; do
  echo "Waiting for Windows SSH..."
  sleep 5
done

# Sync source files to Windows VM
sshpass -p "ssh" ssh ${USER}@${IP} "powershell -command \"if (!(Test-Path -Path 'C:\\ModBuild\\RainworldBattleRoyale')) { New-Item -ItemType Directory -Path 'C:\\ModBuild\\RainworldBattleRoyale' }\""
sshpass -p "ssh" scp -r /mnt/1tb/repos/RainworldBattleRoyale/ ${USER}@${IP}:"C:/ModBuild/"


# Run build command remotely
sshpass -p "ssh" ssh ${USER}@${IP} powershell -command "\"& 'C:\\Program Files\\Microsoft Visual Studio\\2022\\Community\\MSBuild\\Current\\Bin\\MSBuild.exe' 'C:\\ModBuild\\RainworldBattleRoyale\\RainworldBattleRoyale.csproj' /p:Configuration=Debug /v:m /nologo\""

end_time=$(date +%s)
elapsed=$((end_time - start_time))

echo "Build took $elapsed seconds."

# Pull built DLL back to Fedora
sshpass -p "ssh" scp ${USER}@${IP}:"C:/ModBuild/RainworldBattleRoyale/bin/Debug/net480/RainworldBattleRoyale.dll" /mnt/1tb/Rainworld/battleroyale/plugins/

virsh shutdown ${VM_NAME}

echo "Launching Steam game..."
steam -applaunch $STEAM_GAME_ID &
