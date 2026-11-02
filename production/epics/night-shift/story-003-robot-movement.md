# Story 003: 로봇 이동·줄과 함께 밀림·추락

> **Epic**: night-shift
> **Status**: Done
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 3 (로봇 걷기·내려가기, 줄과 함께 밀리기, 추락 판정)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [x] 같은 줄에서 옆 칸이 켜져 있으면 로봇이 좌우로 걷는다. 꺼져 있으면 걷지 못한다.
- [x] 바로 아래 칸이 켜져 있으면 아래 줄로 내려간다. 위로는 올라가지 못한다.
- [x] 걷기와 내려가기는 횟수 제한이 없다 (시프트 횟수만 소모한다).
- [x] 로봇이 서 있는 줄을 밀면 로봇도 같은 방향으로 한 칸 같이 이동한다.
- [x] 로봇이 칸 0~7 밖으로 밀려나면 추락으로 판정되고 즉시 재시작 대상이 된다.
- [x] 다른 줄을 밀 때는 로봇이 움직이지 않는다.

---

## Implementation Notes

- `BoardLogic`에 이동 메서드를 추가한다. 시프트 시 로봇이 그 줄에 있으면 `robotCol`을 ∓1 하고, 0~7 밖이면 추락 처리.
- 로봇이 밟고 있던 칸이 밀려 사라져도 로봇은 줄과 함께 움직이므로 같은 줄 위에 남는다. 이 경우 추락은 끝 밖으로 나갈 때만이다.
- 판정은 순수 C#에서 하고 View는 결과만 반영한다.

---

## Out of Scope

- Story 004: 시프트 횟수 차감
- Story 005: 되돌리기와 재시작 동작 자체 (추락은 재시작 "신호"까지만)
- 출구 도달 판정 (story 006)

---

## Files

- `Assets/00_Scripts/Board/BoardLogic.cs`
- `Assets/Tests/EditMode/Board/RobotMovementTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/RobotMovementTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: Waived (qa.level minimal, no asmdef). Verified 2026-11-02 by editor `script eval_file`: 14/14 checks PASS (walk, descend, no up, ride row, other row, fall at 0 and 7).

---

## Dependencies

- Depends on: `production/epics/night-shift/story-001-shift-logic.md`
- Unlocks: `production/epics/night-shift/story-004-shift-limit.md`, `story-006-exit-and-stages.md`
