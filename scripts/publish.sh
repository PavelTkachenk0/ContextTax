#!/usr/bin/env sh
# Build self-contained, single-file binaries for every supported RID into ./dist.
# Usage: scripts/publish.sh [VERSION]   (VERSION defaults to 0.0.0-dev)
set -eu

VERSION="${1:-0.0.0-dev}"
RIDS="osx-arm64 osx-x64 linux-x64 win-x64"
OUT="dist"

rm -rf "$OUT"
mkdir -p "$OUT"

for RID in $RIDS; do
  echo "→ publishing $RID ($VERSION)"
  dotnet publish src/ContextTax.Cli -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none \
    -p:Version="$VERSION" -o "$OUT/$RID"
  if [ "$RID" = "win-x64" ]; then
    mv "$OUT/$RID/contexttax.exe" "$OUT/contexttax-$RID.exe"
  else
    mv "$OUT/$RID/contexttax" "$OUT/contexttax-$RID"
  fi
  rm -rf "$OUT/$RID"
done

echo "✓ binaries:"
ls -la "$OUT"
