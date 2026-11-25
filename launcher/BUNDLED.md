# Bundled EpinelPSLauncher source

This directory is an unmodified copy of
[`EpinelPS/EpinelPSLauncher`](https://github.com/EpinelPS/EpinelPSLauncher) at
commit `d219c64347328de627764dcfd2f82f31199a7569` ("update certs"), vendored so
this repository can build the desktop launcher without a second clone.

- Upstream: <https://github.com/EpinelPS/EpinelPSLauncher>
- Commit: see `UPSTREAM-COMMIT`
- License: GNU AGPL v3 (see `LICENSE` in this directory)

It is kept unmodified on purpose. The four Windows-only helper binaries that
the Linux build needs (`LauncherUtility.exe`, `HelperDll.dll`, `UnityInit.dll`,
`sail_api_impl64.dll`) are in `../vendor/launcher-windows/`, built from the same
commit.

Build it with:

```bash
./scripts/build-launcher.sh
```

If you change the launcher, make it a real fork of the upstream project rather
than editing this copy in place; keeping this tree byte-identical to the pinned
commit makes updates and diffs tractable.
