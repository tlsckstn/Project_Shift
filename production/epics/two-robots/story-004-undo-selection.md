# Story 004: Undo·재시작에 선택 로봇 포함

> **Epic**: two-robots
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: —

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 4 — "Undo·재시작에 선택 로봇 포함"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "두 번째 로봇", scoped to this story:*

- [ ] Undo가 두 로봇의 위치, 줄 상태, 남은 시프트, **선택된 로봇**을 행동 직전 상태로 되돌린다.
- [ ] `Q` 전환 자체는 Undo 단계가 아니다. 전환만 하고 Undo를 누르면 그 전의 걷기/시프트가 되돌려진다 (선택은 그 행동을 했을 때의 로봇으로 복원).
- [ ] 재시작은 두 로봇을 시작 위치로, 선택은 0번 로봇으로 되돌린다.
- [ ] 실패(추락·버그 칸) 후 재시작 동작과 메시지는 기존과 같다.
- [ ] Undo 단계가 없을 때 Undo는 아무 일도 하지 않는다 (기존 동작).
- [ ] 로봇 1대 스테이지의 Undo·재시작은 이전과 같다.

---

## Implementation Notes

- `BoardHistory`가 `BoardState.Clone()` 스냅샷을 쓰고 있다면 Story 001에서 선택 번호가 `Clone()`에 포함되므로 코드 변경이 거의 없을 수 있다. 그 경우 이 스토리는 테스트와 동작 확인만 한다 (추가 코드 금지).
- 선택 전환은 기록하지 않으므로 `Q` 입력 처리에서 `BoardHistory`에 push하지 않는 것을 확인한다.

---

## Out of Scope

- 선택 표시 (Story 002), 판정 (Story 003)

---

## Files

- `Assets/00_Scripts/Board/BoardHistory.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/Tests/EditMode/Board/TwoRobotUndoTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/TwoRobotUndoTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`
- Unlocks: `production/epics/two-robots/story-006-two-robot-stages.md`
