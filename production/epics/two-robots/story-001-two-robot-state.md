# Story 001: 로봇 2대 상태

> **Epic**: two-robots
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: —

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 1 — "로봇 2대를 상태에 담는 BoardLogic 확장"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "두 번째 로봇", scoped to this story:*

- [ ] `BoardState`가 로봇 목록(각자 row, col)과 선택된 로봇 번호를 가진다. 로봇이 1대인 상태는 기존과 같은 값·동작이다.
- [ ] 걷기·내려가기는 선택된 로봇만 움직이고 다른 로봇은 그대로다.
- [ ] 줄 시프트는 그 줄 위에 있는 모든 로봇을 같이 민다 (줄 밖으로 밀려나면 추락).
- [ ] 시프트 한 번은 로봇이 몇 대든 남은 횟수를 1만 쓴다. 횟수는 두 로봇 합산 하나다.
- [ ] 두 로봇은 같은 칸에 있어도 된다. 서로 이동을 막지 않는다.
- [ ] 방화벽·버그 칸·잠긴 줄 규칙이 로봇마다 똑같이 적용된다.
- [ ] `Clone()`이 두 로봇의 위치와 선택 번호를 복사한다. 복사본을 바꿔도 원본은 그대로다.
- [ ] 로봇 선택 전환 동작(`SelectNext` 같은 하나의 메서드)이 있고 시프트 횟수를 쓰지 않는다.
- [ ] 기존 11개 스테이지가 이전과 똑같이 동작한다 (로봇 1대로 변환되어 생성).

---

## Implementation Notes

- 기존 `RobotRow/RobotCol/IsRobotFallen/IsRobotOnBug/IsFailed` 소비자(`BoardLogic`, `BoardView`, `StageManager`)가 많으므로, 로봇 목록을 도입하되 "선택된 로봇" 기준 접근자를 남겨 한꺼번에 깨지 않게 한다 (YAGNI: 로봇이 1대일 때 기존 코드 경로가 그대로 통과).
- 필드는 private, 노출은 get 전용 프로퍼티 (프로젝트 C# 규칙).
- 실패·출구 판정은 Story 003, Undo는 Story 004. 여기서는 상태와 이동만.

---

## Out of Scope

- 클리어/실패 판정 (Story 003), Undo·재시작 (Story 004), BFS (Story 005)
- 입력 키와 표시 (Story 002)

---

## Files

- `Assets/00_Scripts/Board/BoardState.cs`
- `Assets/00_Scripts/Board/BoardLogic.cs`
- `Assets/00_Scripts/Stage/StageData.cs` (1대 스테이지를 새 상태로 변환)
- `Assets/Tests/EditMode/Board/TwoRobotStateTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/TwoRobotStateTests.cs` (tests are waived at `qa.level: minimal`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `story-002-tab-select.md`, `story-003-exit-and-fail.md`, `story-004-undo-selection.md`, `story-005-bfs-two-robots.md`
