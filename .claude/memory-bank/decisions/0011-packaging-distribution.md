# 0011 — Packaging & distribution: self-contained cross-platform binaries on GitHub Releases

- **Status:** Accepted (2026-06-07)

## Context
ContextTax ran only via `dotnet run --project src/ContextTax.Cli -- …` — it needed the .NET 10 SDK
installed and an ugly prefix that killed the hero pipe `pbpaste | contexttax response -e`. With the repo
going **public** (+ an article) and meant for others to **install and use** across **Linux, macOS, and
Windows** — an audience that skews **non-.NET** (Node/Python/TS) — we needed a real, downloadable tool
with zero prerequisites and a repo that looks the part.

## Decision
Ship **self-contained, single-file binaries on GitHub Releases** (no NuGet, no .NET on the user's box):

- **Form:** `dotnet publish --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true`
  per RID (`osx-arm64`, `osx-x64`, `linux-x64`, `win-x64`). Runtime bundled → no prerequisite. Compression
  ≈ halves the bundle (~80 → ~40 MB).
- **No trim / no AOT.** Spectre.Console.Cli + ModelContextProtocol resolve via **reflection** (trim/AOT-
  fragile), and the o200k vocab is a big **embedded data resource** trimming can't shrink → low reward,
  high risk. Compression instead.
- **Binary named `contexttax`** (`<AssemblyName>`), with **`<InvariantGlobalization>true</…>`** (SP8 already
  forced `InvariantCulture` everywhere, so ICU is dead weight — dropping it shrinks the single file).
- **`--version` works.** Sourced from the assembly informational version (set by `-p:Version` from the tag).
  Spectre routes the built-in `-v/--version` into the **default command** when `SetDefaultCommand` is used,
  so it's intercepted up front in `Program.cs` (the advertised flag actually prints now).
- **Release on tag.** A new `.github/workflows/release.yml` (`on: push: tags: v*`, `contents: write`) runs a
  DRY `scripts/publish.sh` on one `ubuntu` runner (cross-compiling self-contained needs no native toolchain
  — we're not doing AOT) and publishes a GitHub Release via `softprops/action-gh-release` with **stable asset
  names** `contexttax-<rid>(.exe)` + the install scripts. `ci.yml` is untouched. Stable names make
  `releases/latest/download/…` URLs work for the README and installers.
- **One-line install.** `install.sh` (macOS/Linux) + `install.ps1` (Windows) detect OS/arch and fetch the
  right asset from the latest release.
- **Unsigned**, with a documented Gatekeeper/SmartScreen workaround (signing = Apple Dev $99/yr, deferred).
- **README front door:** the editorial look lives in committed **SVGs** referenced via `<img>` + `<picture>`
  (GitHub strips inline `<svg>`/CSS but renders `<img>`-referenced SVG 1:1, and swaps light/dark by theme);
  the body stays plain markdown (Install / Usage / Downloads — copy-pasteable, never imagized). A combined
  **hero** (wordmark + tagline + the loud number) is one image; a terminal **demo** is another.
- **One real, reproducible "loud number."** Dogfooded against real **public** servers (`≈ estimate`,
  keyless) and the loudest/most-recognisable chosen: **GitHub MCP** — default toolset ≈ **8,616 tok / 4.3%**
  of a 200K window (kicker: ≈ 16,327 / 8.2% with all toolsets), shown next to Playwright (~3,239) and Azure
  (~16,364) as evidence. Schema cost is deterministic → anyone re-runs the command and gets the same number
  (citability). Measured via a throwaway MCP `tools/list` probe (dummy token, **schema only, no API calls**);
  the third-party schemas are **not committed**.

## Consequences
- Zero-prerequisite install on three OSes; the hero pipe `pbpaste | contexttax response -e` works from a
  downloaded binary. Each release is one `git tag`.
- Binaries are ~40 MB and **unsigned** (a first-run quarantine prompt, documented). The o200k vocab is the
  bulk and is intentionally kept (keyless estimate stays offline).
- The preview-SDK pin is **irrelevant to users** (the runtime is bundled); no NuGet means no preview-package
  friction.
- The headline is an **`≈` estimate**, not ground truth (the maintainer is on a $0-balance API account) —
  honestly labelled, with a fine-print disclaimer; a ground-truth upgrade later is a one-line SVG edit.
- The SVG hero is **not selectable text** (acceptable for a marketing banner); all commands deliberately
  stay markdown so they remain copy-pasteable, searchable, and accessible.
- **Rejected:** a **.NET global tool / NuGet** (ties to the .NET audience + requires the .NET runtime);
  **trimming / NativeAOT** (reflection-fragile, low payoff against the embedded vocab); **code signing**
  ($99/yr, deferred). **Live `--call`** to measure a *real* response remains its own future cycle (breaks
  the read-only stance of ADR 0007).

## Finish gate
Flipping the repo to **public** is a manual maintainer step in the GitHub UI (the PAT can't change
visibility) — required before the article, does not block development.
