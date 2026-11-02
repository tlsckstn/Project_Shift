# Story 004: 시프트 횟수 제한과 표시

> **Epic**: night-shift
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 4 (시프트 횟수 제한과 남은 횟수 표시)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [x] 시프트 한 번(`<<` 또는 `>>`)마다 `shiftsLeft`가 1 줄어든다.
- [x] `shiftsLeft`가 0이면 더 이상 시프트할 수 없다 (걷기·내려가기는 계속 가능).
- [x] 남은 횟수가 화면에 표시되고 시프트할 때마다 갱신된다.
- [x] 횟수를 다 쓰고도 출구에 못 가면 되돌리기 또는 재시작이 필요한 상태임을 알 수 있다. (0이 되면 출구 도달 여부와 무관하게 빨간 `SHIFTS 0  Z undo / R restart` 표시 — 출구는 Story 006에서 BoardState로 들어옴)
- [x] 소실된 비트만 밀려나는 시프트(값이 안 변하는 시프트)도 횟수를 소모한다.

---

## Implementation Notes

- 제한값은 `StageData`에서 받는다 (story 006). 이 스토리에서는 `BoardState.shiftsLeft` 필드와 차감 로직만 다룬다.
- 표시는 값이 바뀔 때만 문자열을 만든다 (매 프레임 조합 금지).

---

## Out of Scope

- Story 006: `StageData`에서 스테이지별 제한값 불러오기
- 별 평가

---

## Files

- `Assets/00_Scripts/Board/BoardLogic.cs`
- `Assets/00_Scripts/Board/BoardView.cs`
- `Assets/Tests/EditMode/Board/ShiftLimitTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/ShiftLimitTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [x] Tests waived (minimal). Verified 7/7 via `unity command script eval_file`. Screenshots: `production/qa/evidence/story-004-shifts-left.png`, `story-004-shifts-zero.png`.

---

## Dependencies

- Depends on: `production/epics/night-shift/story-001-shift-logic.md`, `story-003-robot-movement.md`
- Unlocks: `production/epics/night-shift/story-006-exit-and-stages.md`
