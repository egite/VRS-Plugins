# Notes for Claude

## Plugin source changes → ask about rebuilding the zip

Each plugin ships as `<Name>.zip` at the repo root, containing the compiled DLL, manifest XML, and `Web/` assets. After any change to a plugin's source (`.cs`, manifest `.xml`, or anything under `Plugin.<Name>/Web/`), the tracked `<Name>.zip` is stale — the DLL inside it predates the source change.

**Always ask the user whether they want the zip rebuilt** before treating a plugin source change as complete. If they say yes:

1. Rebuild the DLL: `build-<Name>.bat` (Release for most plugins, Debug for PilotsView and SnapToOwnship — see `build-deployment-zips.bat` for the per-plugin config).
2. Refresh `<Name>.zip` at the repo root with the new DLL + XML + `Web/` tree. Use `tar.exe --format=zip` (bsdtar — Compress-Archive produces directory entries that Linux unzip extracts as mode 0o000, breaking VRS WebAdmin on Pi/Mono).
