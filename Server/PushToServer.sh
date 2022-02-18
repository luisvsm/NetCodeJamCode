#!/bin/bash
echo "Copying server files from Unity"
sh CopyGameServer.sh

echo "Pushing changes to server"
rsync -aP --exclude bin/ --exclude obj/ --exclude .vscode/ . gameserv@45.76.125.216:/home/gameserv/netris --delete

echo "Killing old game server"
ssh gameserv@45.76.125.216 "screen -S GameServer -X quit"

echo "Setting permissions"
ssh gameserv@45.76.125.216 "chmod 755 ~/netris/StartServer.sh"

echo "Starting new server"
ssh gameserv@45.76.125.216 "sh ~/netris/StartServer.sh"