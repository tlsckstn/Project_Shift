# Story 005: 되돌리기(Undo)와 재시작

> **Epic**: night-shift
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 5 (되돌리기와 재시작)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] 시프트·걷기·내려가기 직전마다 `BoardState` 복사본이 `Stack`에 저장된다.
- [ ] `Z`를 누르면 마지막 행동 이전 상태로 돌아간다 (줄 값, 로봇 위치, 남은 횟수 모두).
- [ ] 되돌리기 후 저장된 이전 상태와 현재 상태가 같은 `rows` 배열을 공유하지 않는다 (깊은 복사).
- [ ] 스택이 비어 있을 때 `Z`는 아무 일도 하지 않는다 (오류 없음).
- [ ] `R`을 누르면 스테이지 시작 상태로 돌아가고 되돌리기 스택이 비워진다.
- [ ] 추락한 직후에도 `Z`로 직전 상태로 돌아갈 수 있다.

---

## Implementation Notes

- `rows`는 배열이라 복사 시 반드시 `Clone()` 한다. 얕은 복사하면 되돌려도 줄 값이 안 돌아온다.
- 키 입력 연결은 story 006의 `InputController`에서 한다. 이 스토리는 `BoardState` 스택 로직까지.
- 키는 임시 조작: `Z` 되돌리기, `R` 재시작.

---

## Out of Scope

- Story 006: 재시작 시 어느 스테이지를 다시 불러올지 (`StageManager`)
- 되돌리기 애니메이션

---

## Files

- `Assets/00_Scripts/Board/BoardHistory.cs`
- `Assets/Tests/EditMode/Board/BoardHistoryTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/BoardHistoryTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/night-shift/story-001-shift-logic.md`
- Unlocks: `production/epics/night-shift/story-006-exit-and-stages.md`
