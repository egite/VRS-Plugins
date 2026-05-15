# Virtual Radar Server Plugins

A collection of plugins for [Virtual Radar Server](http://www.virtualradarserver.co.uk/) 3.x, targeting both the Windows (.NET Framework 4.8 / System.Data.SQLite) and Mono / Raspberry Pi (Mono.Data.Sqlite) builds.

## Plugins

### CustomLinks

> Adds user-configurable links to the aircraft detail panel in the VRS web UI. Each link is a template that substitutes fields like ICAO, registration, callsign, or operator code, so you can wire in external lookup sites (photo databases, registries, flight trackers, etc.) and have them open pre-populated for the selected aircraft.

### LiveATC

> Lets you double-click near an airport on the map to open its [LiveATC.net](https://www.liveatc.net/) page in a new browser tab. The plugin ships with an airport database and matches the click location to the nearest airport with LiveATC coverage.

### LogoMarkers

> Replaces the default aircraft SVG icons on the map with composite markers that combine the operator's logo and a heading arrow, rendered server-side. Useful when you'd rather identify aircraft by airline branding than by silhouette.

### PilotsView

> Provides a Google Earth "pilot's view" KML feed that follows a selected aircraft from a chase-camera perspective. Uses a background-sampled position queue (one snapshot per second per tracked aircraft) so the camera always animates between two real samples, avoiding the prediction stalls that break Google Earth's `onStop` callback.

### RegistrationData

> Adds a per-aircraft lookup page to VRS plus configurable highlighting in the aircraft list, all backed by an embedded SQLite database that the plugin downloads and refreshes on a schedule from public sources.
>
> **Data sources** (each independently scheduled and updatable):
>
> - **FAA Aircraft** (US) — registry, model reference, engine reference
> - **FAA Airmen** (US) — pilot certificates and ratings (used for pilot matching)
> - **FAA SDR** — Service Difficulty Reports (maintenance defects / incidents)
> - **NTSB** — US accident & incident database (read from the published Access `.mdb`)
> - **CCAR** (Canada) — Transport Canada Civil Aircraft Register and owners table
> - **CASA** (Australia) — Civil Aviation Safety Authority register
> - **NZCAA** (New Zealand) — Civil Aviation Authority register
>
> **Aircraft lookup page** (opens from the aircraft detail view, optionally in a new tab):
>
> - Registration, owner, make/model, engine, weight (lbs or kg)
> - Photo grid with optional DuckDuckGo image fallback
> - Silhouette and operator logo
> - Pilot matches against FAA airmen by owner name and address, with fuzzy / Levenshtein matching, configurable distance threshold, optional state filtering, and confidence badges (exact / close / possible)
> - NTSB history and SDR reports for the registration
>
> **Aircraft list integration:**
>
> - Row / cell highlighting with a configurable priority order across multiple signals: a user-maintained "pink" registration list, model-ICAO highlights, pilot-matched aircraft, NTSB-flagged aircraft, and SDR-flagged aircraft
> - LADD (Limiting Aircraft Data Displayed) indicator with its own colour, for US aircraft on the FAA's privacy list
> - Toggles for which signals contribute colour, and whether colour is applied to the whole row or only the reg / callsign columns
>
> **Operations:**
>
> - Per-source update intervals (days) and last-download timestamps
> - Custom override URLs per source in case official download locations change
> - Manual "download now" buttons per source in the options dialog

### SnapToOwnship

> Adds a control to the web UI that snaps and centres the map on the configured ownship position. Designed to work alongside the Stratux plugin so you can re-centre on your aircraft with one click after panning around.

### Stratux

> Connects to a [Stratux](http://stratux.me/) ADS-B receiver over the local network, polls its GPS situation feed for ownship position, and uses it as the current location in Virtual Radar Server. Lets VRS automatically track your own aircraft when running in-flight on a tablet or Pi.

### TileServerMBTiles

> Serves map tiles to the VRS web UI from local [`.mbtiles`](https://github.com/mapbox/mbtiles-spec) files, so the map works offline or with self-hosted tile sources (FAA sectional / IFR low / IFR high charts, custom basemaps, etc.). Drop one or more `.mbtiles` files into a folder, point the plugin at it, and each file appears as a selectable base map with an opacity slider.

## Building

Each plugin builds as a single .NET Framework 4.8 DLL. Requirements:

- Visual Studio 2015 or later, **or** Build Tools for Visual Studio (any edition with the .NET desktop workload).
- A local copy of Virtual Radar Server 3.x for the referenced `VirtualRadar.Interface.dll` / `VirtualRadar.Localisation.dll` / `VirtualRadar.WinForms.dll`. The csproj files reference them via `..\Plugin.CustomLinks\bin\Debug\` — adjust the hint paths if your VRS install lives elsewhere.

Per-plugin build scripts are in the repo root:

```
build-CustomLinks.bat
build-LiveATC.bat
build-LogoMarkers.bat
build-PilotsView.bat
build-RegistrationData.bat
build-SnapToOwnship.bat
build-Stratux.bat
build-TileServerMBTiles.bat
```

Each one calls `_build-plugin.bat`, which locates MSBuild (via `vswhere`, with explicit fallbacks for VS 2017/2019/2022 and standalone MSBuild 14.0) and rebuilds the plugin in the appropriate Debug/Release configuration.

## Packaging for deployment

`build-plugin-zips.bat` packages each compiled plugin into a `.tar.gz` under `dist/`, containing the DLL, the manifest XML, and any `Web/` assets. On the target machine, from inside the VRS `Plugins/` folder:

```sh
tar xzf TileServerMBTiles.tar.gz
```

The archives use `root:root` ownership and 0644/0755 modes so the web assets are traversable when extracted on Linux / Raspberry Pi.
