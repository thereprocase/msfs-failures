# MsfsFailures 0.1.0 — Distribution

## What's in this directory

| File | Description |
|------|-------------|
| `MsfsFailures-0.1.0-x64.msi` | Windows x64 MSI installer (self-contained, no .NET 8 runtime needed) |
| `README.md` | This file |

`dist/*.msi` and `dist/*.zip` are gitignored — they are not committed. The installer source lives in `installer/`.

---

## Installing with the MSI

```
msiexec /i MsfsFailures-0.1.0-x64.msi
```

Or double-click `MsfsFailures-0.1.0-x64.msi` in Explorer.

Installs to: `C:\Program Files\MsfsFailures\`
Adds a Start Menu shortcut under **MsfsFailures**.
Uninstall via **Settings → Apps → MsfsFailures → Uninstall**.

---

## Code-signing notice (UNSIGNED BUILD)

This MSI is **not code-signed**. On first install Windows SmartScreen will show a warning ("Windows protected your PC"). Click **More info → Run anyway** to proceed.

Signing will land once a trusted certificate is available.

---

## First run

On first launch the app creates:

- **Database:** `%LOCALAPPDATA%\MsfsFailures\fleet.db` — SQLite database seeded with sample airframes (includes N172AB, a Cessna 172).
- **Logs:** `%LOCALAPPDATA%\MsfsFailures\logs\msfs-failures-*.log`

---

## Sim connection mode

Control via the `MSFS_FAILURES_SIM` environment variable:

| Value | Behaviour |
|-------|-----------|
| `auto` *(default)* | Tries SimConnect; falls back to mock if MSFS is not running |
| `real` | Forces SimConnect — fails hard if MSFS is not running |
| `mock` | Always uses the offline mock sim bus |

Set it before launching:
```
set MSFS_FAILURES_SIM=auto
"C:\Program Files\MsfsFailures\MsfsFailures.App.exe"
```

---

## MSFS test flight

1. Load any Cessna 172 in MSFS 2020/2024 and enter a flight.
2. Launch MsfsFailures.
3. Open the **IN FLIGHT** tab — the app picks up live SimConnect data automatically.
4. The seeded **N172AB** row in the fleet view will be the active airframe.

---

## ZIP fallback

If you receive `MsfsFailures-0.1.0-x64.zip` instead of the MSI, extract it anywhere and run `MsfsFailures.App.exe` directly. No installation required.

---

## Build reproducibility

```
# From repo root:
dotnet.exe publish src/MsfsFailures.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish/win-x64/
python3 installer/gen_wxs.py
wix.exe build installer/MsfsFailures.Installer/Product.wxs installer/MsfsFailures.Installer/Files.wxs -arch x64 -out dist/MsfsFailures-0.1.0-x64.msi
```

WiX version: 5.0.2 (installed via `dotnet.exe tool install --global wix --version 5.*`).
