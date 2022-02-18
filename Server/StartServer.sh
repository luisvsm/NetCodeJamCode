#!/bin/bash
screen -dm -S NetrisServer bash -c "dotnet run -p ~/netris/ >> ~/netris.log"