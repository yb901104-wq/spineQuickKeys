## ADDED Requirements

### Requirement: Skin system loads from directory

The system SHALL support loading visual resources from a skin directory.
The default skin directory path SHALL be: `%APPDATA%\KeyMacro\skins\default\`
User can switch to a different skin via `VirtualLayoutSerializer.LayoutData.SkinPath`.
When `SkinPath` is null or empty, the system SHALL use built-in GDI+ defaults.

#### Scenario: Load skin from directory
- **WHEN** SkinPath is set to a valid skin directory
- **WHEN** the directory contains skin.json and PNG files
- **THEN** the system loads colors from skin.json
- **THEN** the system loads PNG images for button/window rendering

#### Scenario: Fallback when no skin
- **WHEN** SkinPath is null or empty
- **WHEN** directory does not exist or files are missing
- **THEN** the system uses built-in GDI+ drawing defaults

### Requirement: Skin colors configurable in JSON

skin.json SHALL support the following color keys:
- `window_bg`, `window_border`, `window_rim` — window chrome
- `btn_bg_top`, `btn_bg_bottom` — button gradient
- `btn_text`, `btn_dim_text` — button text colors
- `btn_active_glow` — loop active glow color
- `toolbar_bg`, `toolbar_text` — toolbar colors

Each value SHALL be a hex color string (e.g., `"#0D0D0D"`).
Missing color keys SHALL fall back to hardcoded defaults.

#### Scenario: Skin colors applied
- **WHEN** skin.json has valid color values
- **THEN** the window and buttons render using those colors

#### Scenario: Missing color falls back
- **WHEN** skin.json is missing some color keys
- **THEN** the missing keys use hardcoded GDI+ defaults

### Requirement: Button icons from PNG files

The system SHALL load PNG images from the skin directory:
- `btn_normal.png` — normal button state
- `btn_hover.png` — hover state (future use)
- `btn_pressed.png` — pressed state
- `btn_active.png` — active/loop glow state

When an image exists, the system SHALL draw it instead of the GDI+ gradient/text rendering.
When an image does not exist, the system SHALL use GDI+ drawing for that state.

#### Scenario: Button with image
- **WHEN** btn_normal.png exists in the skin directory
- **WHEN** button is in normal state
- **THEN** the PNG image is drawn as the button background

#### Scenario: Button without image
- **WHEN** btn_normal.png does not exist
- **THEN** the existing GDI+ gradient is drawn

### Requirement: Window background 9-slice support

The `window_bg.png` image SHALL support 9-slice scaling.
The system SHALL define fixed slice margins: 4 pixels on each side.
The four corners SHALL be drawn at their original size.
The four edges SHALL be stretched in one direction.
The center SHALL be stretched in both directions.

#### Scenario: 9-slice background
- **WHEN** window_bg.png exists
- **WHEN** window is resized
- **THEN** the image scales correctly using 9-slice rules

### Requirement: VkSkinLoader service

The system SHALL provide a `VkSkinLoader` service class with:
- `Load(string skinPath)` — loads skin data from directory
- `GetColor(string key, Color default)` — returns color or fallback
- `GetButtonImage(string state)` — returns Image? or null
- `GetWindowBackground()` — returns Image? or null
- Internal caching of loaded images

The loader SHALL be instantiated once by VirtualKeyWindow and shared with VirtualButtonWidget.

#### Scenario: Loader caches images
- **WHEN** VkSkinLoader loads a PNG
- **THEN** the image is cached in memory
- **THEN** subsequent requests return the cached instance

### Requirement: Skin path persisted

`VirtualLayoutSerializer.LayoutData` SHALL include a `SkinPath` field (string?).
The value SHALL be saved and restored with the layout.

#### Scenario: Skin restored on load
- **WHEN** SkinPath is set in layout data
- **WHEN** VirtualKeyWindow loads
- **THEN** the skin at SkinPath is loaded automatically
