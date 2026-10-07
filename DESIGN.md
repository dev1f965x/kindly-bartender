# DESIGN.md

How Kindly Bartender looks. The app is a small Windows utility: it should look like part of Windows 11, not like a game or a brand. Words are in [CONTENT.md](CONTENT.md).

## Direction

- Use the WPF Fluent theme (`ThemeMode="System"` on the application) and its controls without restyling them. Light and dark mode follow Windows.
- Use the Windows accent color for the one primary button in a window (the theme's accent button style) and for nothing else.
- No game imagery, Blizzard logos or colors, gradients, shadows beyond the theme's, decorative illustrations, or custom fonts.
- Windows are small and fixed-width. Content flows top to bottom in one column and scrolls vertically (ScrollViewer) when it exceeds the window's maximum height; the button row stays visible.

## Tokens

Colors come from the Fluent theme's resources (`TextFillColorPrimaryBrush`, `TextFillColorSecondaryBrush`, `CardBackgroundFillColorDefaultBrush`, `SystemFillColorCautionBrush`, `AccentFillColorDefaultBrush`), always referenced with `DynamicResource` so that light, dark, and contrast themes switch while the app runs. The app defines no colors of its own. Resource keys are confirmed against the .NET 10 Fluent theme when the windows are built; a missing key is replaced by the nearest theme key and recorded here.

| Token | Value | Use |
| --- | --- | --- |
| `space.xs` | 4 | Between a label and its note |
| `space.s` | 8 | Between controls in a group |
| `space.m` | 12 | Between groups inside a card |
| `space.l` | 24 | Window padding and between sections |
| `width.window` | 480 | Settings and About windows |
| `width.setup` | 520 | Log settings window, which holds the longest text |
| `type.title` | 20, SemiBold | Window title in the content area |
| `type.heading` | 14, SemiBold | Section headings |
| `type.body` | 14 | Body text and controls |
| `type.caption` | 12, secondary text color | Notes under controls, file paths |
| `font.mono` | Cascadia Mono, then Consolas | File paths |

Sizes are in device-independent pixels and scale with the Windows text size setting.

## Layout rules

- Every window: title, then sections separated by `space.l`, then a button row aligned to the end. The primary button is last and is the default button.
- A section is a heading and its controls. Related options sit in a card (`CardBackgroundFillColorDefaultBrush`, theme corner radius).
- Warnings use the theme's caution color only on the icon, with text in the primary color, so they do not rely on color alone.
- Long text wraps; no text is truncated with an ellipsis.

## Tray icon

- An original glyph: a mug outline with a small dot, drawn in a single color that follows the taskbar's light or dark theme. No game artwork.
- States are shown by a small overlay mark, not by color alone: none for Ready, a pause mark for Paused, an exclamation mark in a circle for Setup needed, Restart needed, and Not working, and a dimmed glyph with a small clock mark for Waiting for Hearthstone. The tooltip always gives the status text.

## Accessibility

- Every control has an accessible name from its visible label. Status changes in a window are announced with a live region.
- The tab order follows the visual order. Escape closes a window; Enter activates the primary button.
- Focus stays visible: the theme's focus visual is kept and `FocusVisualStyle` is never set to null.
- Text meets 4.5:1 contrast through the theme; the app adds no low-contrast text.
- Windows work at 200% text size without clipping, and with Windows contrast themes.

## Checks

- The Settings window has no button row: changes apply at once, as in Windows Settings, and the window closes with Escape or its close button. This follows wireframe 3.
- Wireframe toggles are built as check boxes: the WPF Fluent theme in .NET 10 has no toggle switch control, and a check box is the standard Windows control for an on/off option in a dialog.
- `KB_SCREENSHOTS=<folder> dotnet test --project tests/KindlyBartender.App.Tests -- --filter-class KindlyBartender.App.Tests.Views.WindowScreenshots` renders each window in English and Korean, light and dark, into the folder; the images in `design/screenshots` come from it. The system backdrop is replaced by the theme's base fill in the images.
- The 200% text size and contrast theme checks are done by hand during acceptance testing, because they need the Windows setting changed.
- Screenshots of each window in every state, in English and Korean, in light and dark mode, at 100% and 200% text size, are attached to the pull request that builds the window.
- A separate review compares each window with the wireframes and this file before the owner's review.
