# KeyMacro UI Resource Pack

This directory contains deterministic UI art resources for the future dark professional UI refactor.

Scope:
- General WinForms windows, menus, dialogs, lists, inputs, buttons, tabs, and progress bars.
- These assets are not for `KeyMacro/skins/*` and must not replace `VirtualKeyWindow` button skin images.
- Asset images intentionally contain no button text, so existing code-owned labels and functionality remain the source of truth.

State naming:
- `normal`: default unactive state.
- `hover`: mouse-over state.
- `pressed`: mouse-down state.
- `active`: selected/running/enabled state.
- `disabled`: disabled state.

The source generator is `docs/ui-refactor/tools/ResourceGenerator`.