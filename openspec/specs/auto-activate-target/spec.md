## ADDED Requirements

### Requirement: VirtualKeyWindow activates target before playing

When a target window is set and the user clicks a virtual button, the system SHALL:
1. Resolve target window handle via process name
2. Call `SetForegroundWindow(targetHwnd)` to activate the target
3. Wait 200ms for the target window to fully activate
4. Call `MacroPlayer.Play(sequence)` using the existing SendKeys mechanism

#### Scenario: Target set and running
- **WHEN** target is set to "Spine" and Spine is running
- **WHEN** user clicks a virtual button bound to a sequence
- **THEN** Spine window is brought to foreground
- **THEN** sequence is played via existing MacroPlayer.Play()
- **THEN** keyboard input goes to Spine

#### Scenario: No target set
- **WHEN** no target is set
- **WHEN** user clicks a virtual button
- **THEN** system plays the sequence without any window activation (current behavior)

#### Scenario: Target process not found
- **WHEN** target is set but process is not running
- **WHEN** user clicks a virtual button
- **THEN** system plays the sequence without activation (silent fallback)

### Requirement: Playback flow with scheme priority

The `OnButtonClicked` method SHALL implement the following priority logic:

```
1. Is VkPickMode? → handle pick mode, return
2. Is loop button? → handle loop, return
3. Has target window?
   ├── No → Play(seq) with current behavior
   └── Yes →
       Is scheme-A available? (not flagged as failed this session)
         ├── Yes → PlayToWindow(seq, hWnd)
         │         After playback: check if target is now foreground
         │           ├── Yes → scheme A works, no fallback needed
         │           └── No  → flag scheme A failed, switch to scheme B
         └── No  → SetForegroundWindow(hWnd)
                    Wait 200ms
                    Play(seq)
```

#### Scenario: PostMessage first try
- **WHEN** target is set and scheme-A has not failed in this session
- **WHEN** user clicks a virtual button
- **THEN** system first tries PostMessage (PlayToWindow)
- **THEN** only falls back to activation if PostMessage is detected as ineffective

#### Scenario: Fallback after PostMessage failure
- **WHEN** previous PostMessage call was detected as ineffective
- **WHEN** user clicks a virtual button again
- **THEN** system directly uses SetForegroundWindow + Play (scheme B)
