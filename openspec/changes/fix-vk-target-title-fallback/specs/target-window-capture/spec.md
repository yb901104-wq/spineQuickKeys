# target-window-capture: Delta

## Changed Requirements

### Preexisting Requirement: Title match fallback (bug fix)

The existing requirement at `spec.md` § "Target window is resolved by process name" point 3 states:

> If no title match or title is empty, use the first found window

Implementation did not match spec — `ResolveTargetWindow()` used `continue` on title mismatch instead of falling back to process-only match. Fix aligns implementation with existing spec, no requirement changes.
