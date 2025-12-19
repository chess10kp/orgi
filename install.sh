#!/usr/bin/env bash
set -e


curl -LO https://github.com/chess10kp/orgi/releases/download/v0.1.0/orgi-linux-x64.tar.gz

tar -xzf orgi-linux-x64.tar.gz
cd orgi-linux-x64

sudo cp orgi /usr/local/bin/
sudo chmod +x /usr/local/bin/orgi
