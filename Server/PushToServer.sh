#!/bin/bash
echo "Copying server files from Unity"
sh CopyGameServer.sh

echo "Pushing changes to server"
rsync -aP --exclude bin/ --exclude obj/ --exclude .vscode/ . gameserv@45.76.125.216:/home/gameserv/ggj2020 --delete

echo "Killing old game server"
ssh gameserv@45.76.125.216 "screen -S GameServer -X quit"

echo "Setting permissions"
ssh gameserv@45.76.125.216 "chmod 755 ~/ggj2020/StartServer.sh"

echo "Starting new server"
ssh gameserv@45.76.125.216 "sh ~/ggj2020/StartServer.sh"