# G-Helper personal UI fork

This personal fork refreshes [G-Helper by seerge and contributors](https://github.com/seerge/g-helper). Original GPL-3.0 licensing, attribution, hardware control, profiles, configuration format, hotkeys and tray implementation remain in place. This is an independently built, unsigned personal variant, not an official upstream release.

## Build and run

Requires Windows x64 and the .NET 10 SDK to build; the .NET 10 Desktop Runtime to run. No additional application dependencies were added.

```powershell
dotnet build app/GHelper.csproj -c Release -p:GITHUB_ACTIONS=true
dotnet publish app/GHelper.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:GITHUB_ACTIONS=true -o artifacts/GHelper-Apple-v2
```

Run `artifacts/GHelper-Apple-v2/GHelper.exe`. `GITHUB_ACTIONS=true` suppresses upstream's build-time stop-running-app and ZIP targets; it does not change application behavior. The project-local SDK used during development is under ignored `tools/dotnet/`; substitute that executable for `dotnet` if needed.

The normal application uses the existing G-Helper configuration and hardware behavior. Close your existing G-Helper before trying this build. The built-in updater still checks upstream releases; applying an upstream update can replace this personal UI. No update, hardware, startup-task or persistence logic was redirected.

## What changed

- A compact, content-sized window with grouped surfaces. The oversized title/header and fixed-height viewport from the first revision were removed. The normal G16 layout fits completely without scrollbars, including Visual Mode, charge status, startup and footer actions.
- Shared opaque light/dark colors and spacing/corner tokens in `app/UI/DesignSystem.cs`. Existing system theme selection and optional flat appearance remain available; high-contrast colors use Windows system colors.
- Reusable card panels and native CheckBox-based toggles for startup and general settings. Fan/GPU/advanced groups share the same surfaces; other windows inherit the common controls and palette.
- Soft blue selected surfaces, blue text and cached tinted icons, plus flat rounded buttons/dropdown outlines, with existing native text/image layout, mnemonics, focus, disabled state and accessibility. Existing native TrackBars keep their input/range behavior and receive a simpler track/thumb paint treatment.
- Cached button geometry replaces per-paint window Region assignment and gradient drawing. Slider/badge decoration timers were removed. Slider paint brushes are disposed deterministically.

GPU/display hover explanations now use the existing ToolTip component instead of permanently reserving blank lines. Nonempty keyboard/backlight notices remain clickable and visible. The battery slider and 100% action share a two-column row, and startup/charge status sit inside the battery card.

Only one presentation setup call was added to the main constructor. Hardware event subscriptions and all hardware/configuration source files are unchanged. Original control names and hardware-specific visibility decisions remain in use. No web renderer, new package, font download, polling task or continuous animation was added.

## Screenshots

These are actual WinForms `DrawToBitmap` renders at the host's 144 DPI (150%), with explicitly synthetic sensor readings, fan curves and selection state. The verification copy does not run hardware initialization. `DrawToBitmap` does not capture DWM framing and can show the native print appearance of dropdowns/TrackBars rather than their on-screen paint treatment.

![Light Control Center](apple-ui/main-light.png)
![Dark Control Center](apple-ui/main-dark.png)
![General settings](apple-ui/settings-light.png)
![Fans and power](apple-ui/fans-dark.png)

## Verification and performance

Upstream baseline: `03c6b8a36a350b142e8c6dc278a79ea3e3f740d8`. Release build and framework-dependent single-file publish succeeded with zero warnings/errors using SDK 10.0.403. The application project and package references are unchanged.

The UI verifier builds a disposable copy of the source and replaces eight form constructors **only in that copy** with designer/theme initialization, bypassing hardware event wiring and two hardware-reading field initializers. It also bypasses main-window system-power handling. The test entry point and startup-local configuration exist only in the disposable copy. The production executable has its original entry point and full normal initialization.

```powershell
./tests/UiVerification/Verify.ps1 -Output ./artifacts/ui-check -Dotnet dotnet
# Use an unmodified upstream checkout to compare:
./tests/UiVerification/Verify.ps1 -Source C:/path/to/upstream -Output ./artifacts/upstream-check -Dotnet dotnet
```

Choose fresh output folders. The verifier captures eight forms in both themes and checks native controls and compact G16 visibility/alignment. All 111 original main-window child control names are retained; one battery layout row is added. Screens rendered: main, fans, extra settings, updates, handheld, Matrix, Slash, overlay configuration.

### Compact v2 validation (2026-10-26)

The fixture now includes the G16-style Visual Mode dropdowns, charge/startup row, energy-saver action and normal footer. At the same physical host DPI (144 / 150%), unmodified upstream measures **621 x 915 pixels** and this revision measures **622 x 911 pixels**. Both show no horizontal or vertical scrollbar. This compares identical fixture content; it is not a comparison across different monitor scales or user screenshots.

All 111 original main-window child control names remain; a battery layout row adds one control. Verification checks that the G16 controls are visible and inside the client area, the slider/100% action do not overlap, existing hover strings reach the ToolTip, dynamic backlight notices appear when needed, native click/disabled dispatch and slider keyboard/discrete values work, toggle accessibility is retained, and tinted icon caches are reused/restored for inactive/disabled states. Eight forms were rendered in both themes.

The build and framework-dependent single-file publish succeeded with zero warnings/errors. `-ScreensOnly` provides a fast main-window render/layout pass without the multi-window benchmark. The verifier suppresses package vulnerability network queries for its disposable copy; normal application package references are unchanged.

Synthetic v2 results from five batches of 5,000 warmed button paints and five warmed main-form construction samples:

| Measurement | Light | Dark |
| --- | ---: | ---: |
| Main window at 150% | 622 x 911 px | 622 x 911 px |
| Scrollbars in G16 fixture | None | None |
| Median 5,000 button paints | 1,346.2 ms | 1,405.3 ms |
| Median allocated bytes / 5,000 paints | 4,960,320 | 4,960,320 |
| Median warm main-form construction | 56.83 ms | 50.72 ms |
| Private bytes after UI suite | 40,939,520 | 51,511,296 |
| CPU time over 2-second UI idle sample | 15.625 ms | 15.625 ms |

These are synthetic UI measurements, not the application's normal startup/idle/RAM usage. Host load makes elapsed timings noisy. Current raw data is in `measurements/compact-v2-*.json`; the `redesign-*.json` and `upstream-*.json` files are historical first-revision measurements and should not be presented as current full-application comparisons. Layout baseline data is in `upstream-g16-layout-*.json`. No repeated icon tinting, new animation timers or polling were introduced; tint bitmaps change only when the source icon/theme changes.

**No claim of complete no-regression or hardware acceptance is made.** Real-device startup, long idle CPU/RAM, sensor updates, tray reopen/hide behavior, hardware actions, hotkeys, profile persistence, live theme switching, screen-reader/keyboard interaction, all hardware-specific layouts, and physical 100%/200%/mixed-monitor DPI still need supported-device verification. Peripheral dialogs inherit shared styling but were not included in this UI capture suite. Scrolling remains an accessibility fallback for unusually short screens or additional hardware sections; it is absent from the tested normal G16 layout.

## Maintenance

Keep hardware modules untouched when merging upstream. UI changes are concentrated in `app/UI/`, `Settings.Appearance.cs`, and small designer substitutions/padding changes in three forms. Tests and screenshots are separate from the production application. Existing icons and system typography are retained for localization/layout compatibility. GPL-3.0 and original upstream documentation remain intact.
