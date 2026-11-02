# Story 003: 출구·실패 판정

> **Epic**: two-robots
> **Status**: In Progress
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 3 — "모든 로봇 출구 도달 판정, 한 대라도 추락·버그 칸이면 실패"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "여러 로봇", scoped to this story:*

- [ ] `StageData`에 로봇별 시작 위치와 출구 위치(row, col)가 있다. 로봇 1대 스테이지는 기존 출구 필드 그대로 동작한다.
- [ ] 클리어 = 모든 로봇(2대 또는 3대)이 같은 순간에 각자 자기 출구에 서 있다.
- [ ] 일부 로봇만 출구에 있으면 클리어가 아니다.
- [ ] 출구에 도착한 로봇이 이후 줄에 밀려 출구를 벗어나면 도착 상태가 풀린다 (현재 상태로만 판정한다).
- [ ] 한 로봇이라도 추락하거나 버그 칸에 있으면 실패다. 둘 중 누가 실패해도 같은 재시작 경로·메시지를 쓴다 (`BoardState.IsFailed` 한 경로).
- [ ] 실패 후 Undo 기록, 남은 시프트, PAR는 기존 추락 때와 똑같이 처리된다.
- [ ] 로봇 1대 스테이지의 클리어·실패 동작은 이전과 같다.
- [ ] (범위 추가, 2026-11-02 승인) 2대 이상 스테이지에서는 로봇별 출구가 화면에 그려지고, 출구 색이 해당 로봇 색과 같다. 1대 스테이지의 출구 표시는 기존과 같다. 스토리 006은 데이터만 추가하므로 코드가 필요한 이 표시를 여기서 한다.
- [ ] 화면 스크린샷이 `production/qa/evidence/story-003-exit-and-fail/`에 남아 있다.

---

## Implementation Notes

- obstacles 에픽의 버그 칸 스토리(`production/epics/obstacles/story-003-bug-cell.md`)와 같게: 실패 판정은 `BoardState.IsFailed` 하나로 합치고 `StageManager.AfterAction`은 그것만 본다. 로봇 개수만큼 분기를 늘리지 않고 로봇 목록을 순회한다.
- 출구는 로봇 번호와 1:1로 매긴다 (로봇 i의 출구 i).
- 클리어 판정 타이밍은 행동이 끝난 직후 상태 하나로 본다. 중간 상태는 보지 않는다.

---

## Out of Scope

- Undo (Story 004), BFS (Story 005)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Board/BoardView.cs`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/TwoRobotExitTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [x] Tests waived (minimal). Logic checked through the real StageManager path in play mode with a temporary in-memory two-robot StageData: one robot on its exit is not a clear; both on their exits loads the next stage; a robot pushed off its exit by a row shift un-arrives (no clear even with the other on its exit); an unselected robot pushed off the row end fails the stage ("FELL! Restarted", shifts and positions reset, selection back to 0, same path as before). All 14 existing stages: 1 robot, not cleared at start, cleared at their exit. Screenshots in `production/qa/evidence/story-003-exit-and-fail/`.

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`
- Unlocks: `production/epics/two-robots/story-005-bfs-two-robots.md`, `production/epics/two-robots/story-006-two-robot-stages.md`
