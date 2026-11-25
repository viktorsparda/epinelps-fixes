# Windows launcher helpers (pinned)

These four binaries are built from the **Windows-only** parts of the bundled
[`EpinelPSLauncher`](../launcher) source at commit
`d219c64347328de627764dcfd2f82f31199a7569`:

- `LauncherUtility.exe` — native-AOT Windows helper that injects the login
  session and starts the game. Built from `LauncherUtility/`.
- `sail_api_impl64.dll` — built from `sail_api_impl64/`.
- `HelperDll.dll` — built from `HelperDll/`.
- `UnityInit.dll` — built from `UnityInit/`.

The Linux launcher (`EpinelPSLauncher`) needs them at runtime, but they cannot
be cross-built from Linux: `LauncherUtility` uses Native AOT for `win-x64`, and
the three DLLs are MSVC C++ projects. They are therefore vendored here as
pinned binary dependencies. Their source is in `../../launcher/` at the same
commit. Rebuild them on Windows with the upstream workflow if that commit ever
changes.

Verify them with:

```bash
cd vendor/launcher-windows && sha256sum -c SHA256SUMS
```

`sodium.dll` and `myCA.pem` are built/copied from source by
`scripts/build-launcher.sh`.
