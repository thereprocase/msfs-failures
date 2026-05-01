# FSUIPC7 Reference Material

This directory contains reference documentation and code examples for integrating **FSUIPC7** (paid software by Pete Dowson) as an optional secondary I/O surface alongside SimConnect and MobiFlight WASM.

## What's Here

1. **QUICKSTART.md** - C# integration guide with copy-paste code examples
2. **MANIFEST.md** - Full inventory of sources, licenses, and how to access FSUIPC binary/SDK
3. **portable-sim-panels/** - Open-source WPF app with 100+ offset definitions and WebSocket server (CC BY-NC-SA)
4. **fsuipc-helper/** - Helper library abstracting connection lifecycle (see LICENSE in repo)

## Getting Started

1. **Purchase & Install**: Get FSUIPC7 from https://www.fsuipc.com/
2. **Add NuGet**: `dotnet add package FSUIPCClientDLL` (latest 3.3.16)
3. **Copy pattern from examples**: See QUICKSTART.md or portable-sim-panels/MainWindow.xaml.cs
4. **Reference offsets**: Use portable-sim-panels/FSUIPC.cs or Project Magenta list (see MANIFEST.md)

## Key Points

- FSUIPC binary is **not free**; reference material only documents the C# client interface
- FSUIPCClientDLL NuGet package is the official wrapper for .NET projects
- Two proven open-source examples provided: WPF app + helper library
- Offsets are memory addresses (hex) mapping to sim variables; full reference in portable-sim-panels
- Runtime detection pattern provided for optional FSUIPC support

## Licenses

- **FSUIPCClientDLL**: NuGet package (see package metadata)
- **portable-sim-panels**: Creative Commons BY-NC-SA 4.0
- **fsuipc-helper**: See repository LICENSE file
- **FSUIPC7 binary/SDK**: Proprietary (purchase required)

## Total Size

~2 MB (source code + examples only; FSUIPC binary not included)

---

For full integration details, see **MANIFEST.md** and **QUICKSTART.md**.
