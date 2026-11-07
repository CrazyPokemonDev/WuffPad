## Why

WuffPad no longer starts. The launcher shows *"Cannot start application — The application is missing required files"* because `MainWindow()` called `FetchNewestFiles()`, which downloaded `versions.txt`, `language.xml`, `descriptions.dict`, `standardKeys.db` and `emojis.txt` from `http://crazypokemondev.de/WuffPad/Resources/`. That host (and `185.249.197.95`) was taken offline, the exception was swallowed by an empty `catch`, and the window was created half-initialised. None of those files were ever in the repository.

This PR makes the editor work without the server, and keeps a downloadable build alive.

## What changed

**Helper files are embedded in the executable** (`Core/BuiltinResources.cs`)
Extracted into `Resources/` on first start (`Core/AppData.cs`), or into `%LOCALAPPDATA%\WuffPad\Resources` when the install folder is read-only. If a file is missing, locked or corrupt, the editor reads the embedded copy instead of failing. Regenerated from the upstream [GreyWolfDev/Werewolf](https://github.com/GreyWolfDev/Werewolf) langfiles (GPL-3.0) by the new `tools/make-builtin-resources.py`: 655 description entries derived from the in-file `{0} - player name` comments, 655 keys, 319 emoji buttons.

**Updating is optional and never blocks** (`Core/ResourceUpdater.cs`)
Only runs when `UpdateUrl` (`App.config`) or `WUFFPAD_UPDATE_URL` is set; refreshes in the background with a 15 s timeout and only overwrites files the server actually answered for.

**Other dead endpoints**
- `Info → #closedlist` / `#underdev` read the public `https://t.me/s/<channel>` pages instead of the dead PHP endpoints (`Core/TelegramChannelFeed.cs`).
- Feedback opens a prefilled GitHub issue instead of a dead PHP mail script.
- Upload and Login now say the server is gone. **Uploading cannot work again** — there is no backend to accept files.
- The unused `Telegram.Bot` dependency is dropped.

**Bugs found on the way**
- Opening a `.xml` from Explorer built a throwaway `new MainWindow()` and left a second, empty window behind.
- The emoji grid would be double-initialised; it is now rebuilt idempotently.
- The key list gained an empty entry from the file's trailing newline, so the save-time "missing string" check asked about a blank key.

**Toolchain**
Retargeted from .NET Framework 4.5 to 4.8: 4.5 is long EOL and its targeting pack is no longer available anywhere, so nothing could be built. 4.8 ships with Windows 10 1903+ and Windows 11.

## Verification

New `--selftest` switch (`Core/SelfTest.cs`) runs the start-up data path headless, including a load → edit → save round trip. CI (`.github/workflows/build.yml`) builds with MSBuild, then on a copy with **no `Resources/` folder** runs the self-test twice and opens the real window:

```
WuffPad opened its window: 'WuffPad'
Data folder after start-up: descriptions.dict, emojis.txt, language.xml, standardKeys.db
Result: PASS
second run: Result: PASS
```

`tools/smoke-test.ps1` reproduces that locally. `WuffPad.exe --selftest` writes a report to `%TEMP%\wuffpad-selftest.txt`.

## Download for testing

[`v1.1.0`](https://github.com/Vraxo/WuffPad/releases/tag/v1.1.0) carries `WuffPad.zip` (432 KB: the exe plus four dependency DLLs, no `Resources` folder — it is rebuilt on first start). Unzip anywhere and run; .NET Framework 4.8 is the only requirement.

`release.yml` only becomes manually dispatchable once it is on the default branch; until then a `v*` tag produces the zip.

## What still works

Opening, editing, adding/deleting strings and values, the emoji keyboard, find, language properties, the experimental script window, and the save-time validation that always made WuffPad nicer than editing raw XML (placeholder, gif length, missing key, duplicate key checks).
