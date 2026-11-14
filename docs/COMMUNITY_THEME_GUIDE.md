# Community Theme Submission & Authoring Guide

Welcome to the **FoliCon Community Theme System**! This guide explains how to create, test, and distribute custom themes for FoliCon—either as a pure, lightweight declarative `theme.json` file, or as an advanced `.folicontheme` zip bundle.

---

## 1. Quick Start: Choosing a Format

| Format | File Structure | Best For | Security |
| :--- | :--- | :--- | :--- |
| **Declarative JSON** (`theme.json`) | Single JSON file (+ optional `preview.png`) | Color palette customizations, custom gradients, material adjustments | **100% Declarative** (Instant validation) |
| **Theme Package** (`.folicontheme`) | Zip archive containing `theme.json`, `preview.png`, and optional `Theme.xaml` | Complex visual styles, custom control brushes | **Sandboxed XML Allowlist** |

---

## 2. Declarative Theme Schema (`theme.json`)

Create a folder named with your unique lowercase theme ID (e.g. `cyberpunk-2077` or `catppuccin-mocha`) containing `theme.json`:

```json
{
  "$schema": "https://raw.githubusercontent.com/DineshSolanki/FoliCon-Overlays/main/schemas/theme-v1.json",
  "id": "cyberpunk-2077",
  "name": "Cyberpunk 2077",
  "description": "High-voltage neon yellow and electric cyan inspired by Night City.",
  "author": "JohnnySilver",
  "version": "1.0.0",
  "baseTheme": "Dark",
  "minAppVersion": "5.3.1",
  "tags": ["Cyberpunk", "Neon", "High Contrast"],
  "colors": {
    "background": "#0E0E12",
    "cardSurface": "#181822",
    "elevatedSurface": "#242436",
    "primary": "#FCEE0A",
    "primaryHover": "#FFF23B",
    "primaryPressed": "#D6C904",
    "accent": "#00F0FF",
    "border": "#2D2D44",
    "primaryText": "#F8FAFC",
    "secondaryText": "#00F0FF",
    "thirdText": "#94A3B8"
  },
  "materials": {
    "acrylicOpacity": 0.85,
    "glowSpread": 12.0,
    "heroGradient": {
      "angle": 135.0,
      "stops": [
        { "color": "#25181822", "offset": 0.0 },
        { "color": "#400E0E12", "offset": 1.0 }
      ]
    },
    "actionButtonGradient": {
      "angle": 90.0,
      "stops": [
        { "color": "#FCEE0A", "offset": 0.0 },
        { "color": "#00F0FF", "offset": 1.0 }
      ]
    }
  }
}
```

### Color Palette Requirements
- Colors must be valid hex codes: `#RGB`, `#ARGB`, `#RRGGBB`, or `#AARRGGBB`.
- `id` must be lower-case alphanumeric with hyphens (`^[a-z0-9]+(?:-[a-z0-9]+)*$`).
- `baseTheme` must be either `"Dark"` or `"Light"`.

---

## 3. Creating a `.folicontheme` Package

To create an advanced package archive:
1. Include `theme.json` in the root of your zip file.
2. Include a `preview.png` (recommended 640×360 px).
3. (Optional) Include custom resource overrides in `Theme.xaml`.

> [!NOTE]
> FoliCon enforces a sandboxed XML allowlist on XAML dictionaries inside packages: only `<ResourceDictionary>`, `<SolidColorBrush>`, `<LinearGradientBrush>`, and `<GradientStop>` elements are permitted. Executable code, bindings, and external references are strictly prohibited.

Package file naming: `<theme-id>.folicontheme`.

You can also use the **Export as .folicontheme** feature directly in FoliCon's Community Store UI (`✦ Store` -> `···` on your theme card).

---

## 4. Universal Deeplink Installation (`folicon://`)

FoliCon supports one-click browser installation links for themes and overlays:

### Theme Install Deeplink
```
folicon://theme/install?id=cyberpunk-2077
```

With specific repository:
```
folicon://theme/install?id=cyberpunk-2077&repo=DineshSolanki/FoliCon-Overlays
```

### Theme Live Preview Deeplink
```
folicon://theme/preview?id=cyberpunk-2077
```

### Community Store Deeplink
```
folicon://store?tab=themes&search=cyberpunk
```

---

## 5. Submitting to the Community Hub

1. Fork the community repository: [DineshSolanki/FoliCon-Overlays](https://github.com/DineshSolanki/FoliCon-Overlays).
2. Create a folder under `themes/<your-theme-id>/`.
3. Add your `theme.json`, `preview.png`, and optional `<theme-id>.folicontheme`.
4. Add an entry to `themes/catalog.json`:
   ```json
   {
     "id": "your-theme-id",
     "name": "Your Theme Name",
     "description": "Your theme description",
     "author": "YourGitHubUsername",
     "version": "1.0.0",
     "baseTheme": "Dark",
     "tags": ["Dark", "Minimalist"],
     "manifestUrl": "themes/your-theme-id/theme.json",
     "previewUrl": "themes/your-theme-id/preview.png",
     "downloadUrl": "themes/your-theme-id/your-theme-id.folicontheme",
     "sha256": "<calculated-sha256-hex-hash>"
   }
   ```
5. Submit a Pull Request. Once merged, your theme will be discoverable worldwide in the FoliCon Community Store!
