#!/usr/bin/env sh
# Install the latest contexttax binary for this OS/arch.
# Override the target dir with: INSTALL_DIR=/somewhere sh install.sh
set -eu

REPO="PavelTkachenk0/ContextTax"
BIN="contexttax"
INSTALL_DIR="${INSTALL_DIR:-/usr/local/bin}"

os="$(uname -s)"
arch="$(uname -m)"
case "$os" in
  Darwin) os_id="osx" ;;
  Linux)  os_id="linux" ;;
  *) echo "Unsupported OS: $os — build from source: https://github.com/$REPO" >&2; exit 1 ;;
esac
case "$arch" in
  arm64|aarch64) arch_id="arm64" ;;
  x86_64|amd64)  arch_id="x64" ;;
  *) echo "Unsupported arch: $arch — build from source: https://github.com/$REPO" >&2; exit 1 ;;
esac
if [ "$os_id" = "linux" ] && [ "$arch_id" = "arm64" ]; then
  echo "No prebuilt binary for linux-arm64 yet — build from source: https://github.com/$REPO" >&2
  exit 1
fi

asset="$BIN-$os_id-$arch_id"
url="https://github.com/$REPO/releases/latest/download/$asset"

tmp="$(mktemp)"
echo "Downloading $asset …"
if command -v curl >/dev/null 2>&1; then
  curl -fsSL "$url" -o "$tmp"
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$tmp" "$url"
else
  echo "Need curl or wget installed." >&2; exit 1
fi
chmod +x "$tmp"

mkdir -p "$INSTALL_DIR" 2>/dev/null || true
if [ -w "$INSTALL_DIR" ]; then
  mv "$tmp" "$INSTALL_DIR/$BIN"
else
  INSTALL_DIR="$HOME/.local/bin"
  mkdir -p "$INSTALL_DIR"
  mv "$tmp" "$INSTALL_DIR/$BIN"
  printf 'Installed to %s — add to PATH: export PATH="%s:$PATH"\n' "$INSTALL_DIR" "$INSTALL_DIR"
fi

echo "✓ installed: $("$INSTALL_DIR/$BIN" --version 2>/dev/null || echo "$INSTALL_DIR/$BIN")"
echo "  try:  pbpaste | contexttax response -e"
