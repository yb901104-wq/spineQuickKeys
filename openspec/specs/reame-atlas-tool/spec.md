# reame-atlas-tool

## Overview

原 ReName-UnpackingAtlas 工具的 Form1 迁入 KeyMacro，作为 MainForm 按钮触发的子窗口。包含三个 Tab 页功能：批量重命名、Spine 文件整理、Spine 图集解包。

## Functional Requirements

### FR1: File rename tab
- **WHEN** user selects files → list displays, keyword replacement renames in place
- **WHEN** user selects folder → option to scan subdirectories, unified rename with counter

### FR2: Spine file organizer tab
- **WHEN** user selects source folder + target folder → files are sorted by base name into subdirectories
- **WHEN** checkbox checked → `.skel.bytes` ↔ `.skel` and `.atlas.txt` ↔ `.atlas` suffix swap during copy

### FR3: Atlas unpacker tab
- **WHEN** user selects folder containing `.atlas` + PNG files → parse atlas regions, crop and save individual sprites
- Supports rotation and offset correction from atlas metadata

### FR4: Launch from MainForm
- **WHEN** user clicks "图集工具" button in MainForm toolbar
- **THEN** `ReameTool.Form1` opens as modal dialog
- **THEN** MainForm remains responsive (dialog is on its own message loop)

## Non-functional Requirements

- All original UI layout, button sizes, positions, spacing remain unchanged
- No modification to original business logic code
- Icon loads from embedded resource (oubao.ico)
