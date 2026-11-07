# WuffPad

Development of the OpenSource Xml Editor Wuffpad (especially designed for translating strings of the [@werewolfbot](https://t.me/werewolfbot) on telegram)

> **WuffPad is discontinued upstream.** The original project server
> (`crazypokemondev.de`, which also served the download) was taken offline, and the
> application refused to start when it could not download its helper files. This
> fork keeps the editor usable: everything it needs is now embedded in the
> executable, so it starts and works offline.

## Running it

* Download the build from the *Actions* tab (`Build` run → `WuffPad-bin` artifact),
  unzip it anywhere and start `XML-Editor WuffPad.exe`.
  *The `Release` workflow attaches a `WuffPad.zip` to a release once it runs from the
  default branch.*
* Requires .NET Framework 4.8 (preinstalled on Windows 10 1903+ and Windows 11).
* No installation, no internet connection required. On first start the helper files
  are written to a `Resources` folder next to the executable (or to
  `%LOCALAPPDATA%\WuffPad\Resources` when the install folder is read-only).

`XML-Editor WuffPad.exe --selftest` runs the start-up checks without opening a window,
writes a report to `%TEMP%\wuffpad-selftest.txt` and exits with code 0 when
everything is in order.

## What was broken and what changed

At start-up WuffPad used to download four helper files from
`http://crazypokemondev.de/WuffPad/Resources/` and gave up when that failed:

| file | use |
| --- | --- |
| `language.xml` | English blueprint for *File → New* |
| `descriptions.dict` | key → description shown in the list |
| `standardKeys.db` | keys a translation should contain (save-time check) |
| `emojis.txt` | emoji keyboard |

They are now embedded in `XML-Editor WuffPad.exe` and extracted on first start
(`XML-Editor WuffPad/Core/BuiltinResources.cs`). A background updater
(`Core/ResourceUpdater.cs`) can still refresh them if someone points
`UpdateUrl` (in `App.config` or the `WUFFPAD_UPDATE_URL` environment variable) at a
folder that provides `versions.txt` plus those files; it never blocks the start and
never overwrites a local file unless the server actually answered.

Other server dependencies were removed or made honest:

* *Info → #closedlist* / *#underdev* read the public `https://t.me/s/<channel>`
  pages instead of the dead PHP endpoints.
* *Info → Bug Report and Feedback* opens a prefilled GitHub issue; the PHP mail
  script it called is gone.
* *Upload* and *Login* now say that the server is gone instead of silently failing.
  (Nobody can upload translations through WuffPad any more.)
* The unused `Telegram.Bot` dependency was dropped.
* Opening a `.xml` file from Explorer no longer leaves an empty second window
  behind, and the editor no longer needs a writable install directory.

## What still works

Everything that does not need the server: opening, editing, adding and deleting
strings and values, the emoji keyboard, find, the emoji/language properties
dialogs, the (experimental) script window and the save-time checks — the static
placeholder, gif length, missing key and duplicate key checks that made WuffPad
nicer than editing raw XML.

## Developing

* `dotnet`/MSBuild on Windows, or the `Build` GitHub workflow.
* `tools/make-builtin-resources.py` regenerates the embedded helper files from the
  upstream langfiles of [GreyWolfDev/Werewolf](https://github.com/GreyWolfDev/Werewolf)
  (GPL-3.0); the committed copies are snapshots of those.
* `tools/smoke-test.ps1` runs the self-test from a clean copy of the build, which is
  also what CI does.

## Downloads

The old download link (`185.249.197.95/WuffPad/WuffPad.zip`) is dead; use the
Actions artifact or a release of this fork.
