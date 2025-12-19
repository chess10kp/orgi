#!/usr/bin/env bash
set -e


curl -LO https://github.com/chess10kp/orgi/releases/download/v0.1.0/orgi-linux-x64.tar.gz

tar -xzf orgi-linux-x64.tar.gz

sudo cp orgi-linux-x64/Orgi.Core /usr/local/bin/orgi
sudo chmod +x /usr/local/bin/orgi
