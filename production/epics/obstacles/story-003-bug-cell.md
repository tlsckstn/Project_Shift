# Story 003: 버그 칸

> **Epic**: obstacles
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 버그 칸 (obstacles EPIC scope 3)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story:*

- [x] `StageData`에 선택 필드로 버그 칸 목록(보드 좌표 row, column)이 있고, 비어 있으면 기존 스테이지와 동작이 같다.
- [x] 버그 칸은 보드 좌표에 고정된다. 줄을 밀어도 움직이지 않는다.
- [x] 로봇이 걷기로 버그 칸에 들어가면 추락과 같은 실패: 스테이지 재시작 + 실패 메시지.
- [x] 내려가기로 버그 칸에 들어가도 같은 실패.
- [x] 줄에 실려 밀려 버그 칸에 들어가도 같은 실패.
- [x] 실패 후 Undo 기록과 남은 시프트, PAR는 추락 때와 똑같이 처리된다.
- [x] 로봇이 버그 칸에 들어가지 않으면(줄의 비트만 버그 칸 밑을 지나가면) 아무 일도 없다.
- [x] 버그 칸은 화면에서 일반 발판, 잠긴 줄, 방화벽과 구분된다.

---

## Implementation Notes

- 실패 판정은 기존 추락 판정과 한 경로로 합친다: `BoardState`에 "실패 상태" 판정을 두고 `StageManager.AfterAction`이 그것 하나만 본다. 추락 전용 분기를 따로 늘리지 않는다.
- 메시지는 원인이 보이게 한다 (예: 추락 `FELL! Restarted`, 버그 칸은 별도 문구). 문구는 영어.
- 버그 칸 좌표는 `StageData.CreateState()`가 `BoardState`에 넘기고, `Clone()`은 같은 배열을 공유한다.

---

## Out of Scope

- 버그 칸 소개·활용 스테이지 (Story 004)
- 실패 연출 (애니메이션, 사운드)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Board/BoardState.cs`
- `Assets/00_Scripts/Board/BoardView.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/BugCellTests.cs` (tests are waived at `qa.level: minimal`). 플레이어가 보는 변화이므로 retained screenshot을 `production/qa/evidence/story-003-bug-cell/`에 남긴다.

**Status**: [x] Tests waived (minimal). Logic 7/7 via `unity command script eval_file` (walk, descend and ride into a bug cell all fail; bug stays fixed; bits under a bug are harmless; a fall still reports as a fall; no bugs = old behaviour; clone keeps bug cells). Play mode on an in-memory Stage05 copy: walk-in and ride-in both show `BUG! Restarted` and reset SHIFTS/PAR like a fall (same `AfterAction` path: `BoardState.IsFailed`). Screenshots `production/qa/evidence/story-003-bug-cell/stage05-all-obstacles.png` (bug, firewall, locked row together), `stage05-bug-restarted.png` (960x600), `stamp.txt`. Display: magenta diamond on the cell.

---

## Dependencies

- Depends on: `production/epics/obstacles/story-002-firewall.md` (표시 방식이 겹치지 않게 순서대로)
- Unlocks: `production/epics/obstacles/story-004-obstacle-stages.md`

---

## Completion Notes
**Completed**: 2026-11-02
**Criteria**: 8/8 passing
**Deviations**: Implemented directly instead of via gameplay-programmer agent; no engine-specialist review. No scene edit. BoardLogic unchanged (failure is judged after the move). StageData firewall/bug conversion merged into one helper.
**Test Evidence**: waived at qa.level minimal; logic 7/7 via eval; screenshots `production/qa/evidence/story-003-bug-cell/`
**Reviewed by**: skipped — Solo mode
