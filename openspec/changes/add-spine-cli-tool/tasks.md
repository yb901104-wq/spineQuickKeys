## 1. SpineCliService

- [x] 1.1 Create `SpineCliService` with DetectFromRegistry, RunAsync, and all CLI command methods
- [x] 1.2 Create `CliResult` model and `SpineCliEntry` data model

## 2. BatchCliWindow

- [x] 2.1 Create window shell with top Spine.com path bar and TabControl
- [x] 2.2 Implement "合并" tab page (source/target lists, +/-, validation, execute)
- [x] 2.3 Implement "批量导出" tab page (scan, list with status, output dir, export/pack/version buttons)
- [x] 2.4 Add missing export.json handling (default export + popup + log file)

## 3. MainForm integration

- [x] 3.1 Add "CLI批量合并/导出" button to MainForm toolbar
- [x] 3.2 Wire to BatchCliWindow.ShowDialog
- [x] 3.3 Persist Spine.com path via ConfigService
