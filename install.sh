#!/usr/bin/env bash
set -e

curl -LO https://github.com/chess10kp/orgi/releases/download/v0.1.8/orgi-linux-x64.tar.gz

tar -xzf orgi-linux-x64.tar.gz

cp orgi-linux-x64/Orgi.Core ~/.local/bin/orgi
chmod +x ~/.local/bin/orgi

# Ensure ~/.local/bin is in PATH
if ! grep -q "~/.local/bin" ~/.bashrc 2>/dev/null; then
    echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.bashrc
fi

# Install shell completion
if [[ "$SHELL" == *"bash"* ]]; then
    mkdir -p ~/.bash_completion.d
    ~/.local/bin/orgi completion bash > ~/.bash_completion.d/orgi
    if ! grep -q "source ~/.bash_completion.d/orgi" ~/.bashrc; then
        echo "source ~/.bash_completion.d/orgi" >> ~/.bashrc
    fi
elif [[ "$SHELL" == *"zsh"* ]]; then
    mkdir -p ~/.zsh/completions
    ~/.local/bin/orgi completion zsh > ~/.zsh/completions/_orgi
    if ! grep -q "fpath=(~/.zsh/completions \$fpath)" ~/.zshrc; then
        echo "fpath=(~/.zsh/completions \$fpath)" >> ~/.zshrc
    fi
    if ! grep -q "compinit" ~/.zshrc; then
        echo "compinit" >> ~/.zshrc
    fi
fi

echo "Installation complete. Restart your shell or run 'source ~/.bashrc' (bash) or 'source ~/.zshrc' (zsh) to enable completion."
