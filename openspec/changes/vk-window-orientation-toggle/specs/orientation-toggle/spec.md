## ADDED Requirements

### Requirement: Horizontal/vertical orientation switchable

The VirtualKeyWindow SHALL support two layout orientations:

**Horizontal orientation (横排)**:
- `FlowLayoutPanel.FlowDirection = LeftToRight`
- `WrapContents = false`
- Window width = margin + Σ(button width + ExtraGap) + (N-1)×gap + margin
- Window height = titleBar + margin + btnH + margin

**Vertical orientation (竖排)**:
- `FlowLayoutPanel.FlowDirection = TopDown`
- `WrapContents = false`
- Window width = margin + max(button width + ExtraGap across all) + margin
- Window height = titleBar + margin + Σ(btnH + ExtraGap) + (N-1)×gap + margin

#### Scenario: Switch to vertical orientation
- **WHEN** user right-clicks blank area and selects "竖向模式"
- **THEN** buttons rearrange from top to bottom
- **THEN** window width adjusts to the widest button
- **THEN** window height adjusts to the sum of all button heights plus gaps

#### Scenario: Switch back to horizontal orientation
- **WHEN** user right-clicks blank area and selects "横向模式" (or unchecks "竖向模式")
- **THEN** buttons rearrange back to a single horizontal row
- **THEN** window dimensions match the original horizontal calculation

#### Scenario: Horizontal layout is unchanged
- **WHEN** in horizontal mode
- **THEN** all layout calculations, button sizes, gaps, and margins are identical to the pre-change behavior

### Requirement: Vertical mode layout calculation

When in vertical orientation (`_vertical = true`), `RecalculateSize()` SHALL compute:

- `totalW = margin + max(BaseBtnWidth(style) × S + ExtraGap × S) + margin (+ ncW)`
- `totalH = barH + margin + Σ(BASE_BTN_H × S + ExtraGap × S) + (N-1) × gap + margin (+ ncH)`
- `gap = max(1, BASE_GAP × S)`
- `margin = max(1, BASE_MARGIN × S)`
- `S = GetEffectiveScale()` (DPI × user scale)

Button widgets SHALL use vertical margin (top/bottom) instead of horizontal margin (left/right):
- `w.Margin = new Padding(0, halfGap, 0, halfGap + extraGap)`

#### Scenario: Add button in vertical mode
- **WHEN** in vertical mode
- **WHEN** user adds a new button
- **THEN** window height increases to accommodate the new button
- **THEN** window width stays unchanged (determined by widest button)

#### Scenario: Remove button in vertical mode
- **WHEN** in vertical mode
- **WHEN** user deletes a button
- **THEN** window height decreases

#### Scenario: ExtraGap applies vertically in vertical mode
- **WHEN** in vertical mode
- **WHEN** user sets ExtraGap on a button
- **THEN** the extra gap is applied below the button (vertical spacing)

### Requirement: Drag reorder adapts to orientation

In horizontal mode, drag distance uses dx (left/right). In vertical mode, drag distance uses dy (up/down).

- Drag threshold: `30 × effScale`
- Move steps: `|dx| / (60 × effScale)` for horizontal, `|dy| / (60 × effScale)` for vertical

#### Scenario: Drag reorder in vertical mode
- **WHEN** in vertical mode
- **WHEN** user drags a button up or down past the threshold
- **THEN** the button moves to the corresponding position in the list

#### Scenario: Drag reorder in horizontal mode
- **WHEN** in horizontal mode
- **WHEN** user drags a button left or right past the threshold
- **THEN** the behavior is identical to the current horizontal drag reorder

### Requirement: Corner rendering adapts to orientation

In horizontal mode, the first button has left-rounded corners, the last button has right-rounded corners.
In vertical mode, the first button has top-rounded corners, the last button has bottom-rounded corners.

When `VerticalMode = true` on the widget:
- `roundLeft` parameter to `MakeRoundedPath` SHALL round top corners (left-top + right-top)
- `roundRight` parameter SHALL round bottom corners (left-bottom + right-bottom)

#### Scenario: Vertical mode corner rounding
- **WHEN** in vertical mode
- **WHEN** drawing the first button
- **THEN** top corners are rounded
- **WHEN** drawing the last button
- **THEN** bottom corners are rounded

#### Scenario: Horizontal mode corner rounding unchanged
- **WHEN** in horizontal mode
- **THEN** corner rounding is identical to current behavior (first=left-round, last=right-round)

### Requirement: Orientation persisted in layout file

The `VirtualLayoutSerializer.LayoutData` SHALL store a `VerticalMode` boolean field.
Default value when loading a layout file without this field SHALL be `false` (horizontal).
The orientation SHALL be saved on every layout save and restored on load.

#### Scenario: Save orientation
- **WHEN** user switches orientation
- **THEN** the current orientation is saved to the layout file

#### Scenario: Load orientation
- **WHEN** VirtualKeyWindow loads its layout
- **THEN** the saved orientation is restored
- **THEN** buttons render in the saved orientation

### Requirement: Orientation menu item

The blank area context menu SHALL include an orientation toggle item.
The item text SHALL show the current orientation as checked and the target as the option.
(Suggestion: "竖向模式" as a checkable item, or a submenu "方向" → "横排"/"竖排".)

#### Scenario: Orientation toggle menu
- **WHEN** in horizontal mode
- **WHEN** user right-clicks blank area
- **THEN** the menu shows the option to switch to vertical mode
- **WHEN** user clicks the option
- **THEN** orientation switches
