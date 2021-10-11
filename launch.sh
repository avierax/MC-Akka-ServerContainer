#!/bin/env bash 
echo launching wrapper
export SERVERDIR="/mnt/c/Minecraft/servers/17-biomemeltpot/server"
export SERVERJAR="/mnt/c/Minecraft/servers/17-biomemeltpot/server/server.jar"
export BACKUPDIR="/mnt/c/Minecraft/servers/17-biomemeltpot/backups"
./McServerWrapper/bin/Release/net5.0/linux-x64/McServerWrapper
