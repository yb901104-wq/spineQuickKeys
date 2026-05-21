# spine-cli-tool

## Requirements

### REQ1: SpineCliService
- Detect Spine.com from registry / common install paths / manual selection
- Run async CLI commands, capture stdout/stderr/exit code
- Report errors to user

### REQ2: BatchCliWindow layout
- Top bar: Spine path input + [检测] + [选择] buttons
- TabControl: "合并" / "批量导出"

### REQ3: Merge tab
- Source file list: + (单个文件选择) / - (删除选中)
- Target file list: + (选择目录 → 遍历子目录 .spine → 弹窗勾选) / - (删除选中)
- Each row shows export.json detection status (green/red)
- Validation: only one side can have multiple entries
- [执行合并] button
  - Source .spine → CLI -r to target_merged.spine
  - Source .json/.skel → temp.spine → CLI -r → delete temp
  - Output: target dir / {原名}_merged.spine
- Error handling: show CLI errors in MessageBox

### REQ4: Export tab
- Source dir select → scan .spine recursively → SubfolderSelectDialog → confirm → load list
- List columns: filename | export.json status (green/red) | full path
- [刷新] refresh scan and status
- Output dir: input + browse
- [导出] → Spine -e for each file (with or without export.json)
- [单纹理图] → Spine -p for each file
- [改版本号] → InputBox → --update → save as {name}_{ver}.spine → refresh list
- Log: output dir / cli_export_log.txt, timestamped append

### REQ5: Missing export.json handling
- Still export with default (json+pack)
- Popup summary after export
- Append log to cli_export_log.txt

### REQ6: Config persistence
- Save/load Spine.com path via ConfigService
- Save/load last used directories
