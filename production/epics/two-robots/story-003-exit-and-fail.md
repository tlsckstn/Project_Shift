# Story 003: 출구·실패 판정

> **Epic**: two-robots
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: —

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 3 — "두 로봇 모두 출구 도달 판정, 한 명이라도 추락·버그 칸이면 실패"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "두 번째 로봇", scoped to this story:*

- [ ] `StageData`에 로봇별 시작 위치와 출구 위치(row, col)가 있다. 로봇 1대 스테이지는 기존 출구 필드 그대로 동작한다.
- [ ] 클리어 = 두 로봇이 같은 순간에 각자 자기 출구에 서 있다.
- [ ] 한 로봇만 출구에 있으면 클리어가 아니다.
- [ ] 출구에 도착한 로봇이 이후 줄에 밀려 출구를 벗어나면 도착 상태가 풀린다 (현재 상태로만 판정한다).
- [ ] 한 로봇이라도 추락하거나 버그 칸에 있으면 실패다. 둘 중 누가 실패해도 같은 재시작 경로·메시지를 쓴다 (`BoardState.IsFailed` 한 경로).
- [ ] 실패 후 Undo 기록, 남은 시프트, PAR는 기존 추락 때와 똑같이 처리된다.
- [ ] 로봇 1대 스테이지의 클리어·실패 동작은 이전과 같다.

---

## Implementation Notes

- obstacles 에픽의 버그 칸 스토리(`production/epics/obstacles/story-003-bug-cell.md`)와 같게: 실패 판정은 `BoardState.IsFailed` 하나로 합치고 `StageManager.AfterAction`은 그것만 본다. 로봇 개수만큼 분기를 늘리지 않고 로봇 목록을 순회한다.
- 출구는 로봇 번호와 1:1로 매긴다 (로봇 i의 출구 i).
- 클리어 판정 타이밍은 행동이 끝난 직후 상태 하나로 본다. 중간 상태는 보지 않는다.

---

## Out of Scope

- 출구 색 표시 (Story 002, 006에서 스테이지 확인 시)
- Undo (Story 004), BFS (Story 005)

---

## Files

- `Assets/00_Scripts/Board/BoardState.cs`
- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/Tests/EditMode/Board/TwoRobotExitTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/TwoRobotExitTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`
- Unlocks: `production/epics/two-robots/story-005-bfs-two-robots.md`, `production/epics/two-robots/story-006-two-robot-stages.md`
