# Story 005: BFS 여러 로봇 확장

> **Epic**: two-robots
> **Status**: In Progress
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 5 — "BFS 검증을 여러 로봇 상태로 확장 (par 계산, 3대도 에디터에서 몇 초 안)"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "여러 로봇" + "다음 단계" (모든 스테이지는 BFS로 최소 횟수를 검증한다):*

- [ ] BFS 상태가 줄 값, 모든 로봇 위치(1~3대), 선택 로봇을 포함한다. 로봇 수를 고정하지 않는다.
- [ ] `Q` 전환은 시프트 0회로 취급한다 (전환만 하는 순환 상태가 무한 루프를 만들지 않는다).
- [ ] 최소 시프트 횟수(par)를 구한다. 풀 수 없는 스테이지는 "풀 수 없음"으로 보고한다.
- [ ] 이동 규칙은 `BoardLogic`을 그대로 호출해 만든다 (규칙을 BFS 안에서 다시 구현하지 않는다).
- [ ] 기존 14개 스테이지의 par가 이전 BFS 결과와 정확히 같다 (회귀).
- [ ] 2대 스테이지와 3대 스테이지를 각각 검증하는 데 에디터에서 몇 초 안에 끝난다. 3대가 느리면 구현 전에 사용자에게 알린다 (상태 키 압축, 3대 스테이지의 줄 수 제한 등).
- [ ] 해답 경로(행동 목록)를 얻을 수 있어 스테이지 해답을 직접 재생해 볼 수 있다.

---

## Implementation Notes

- (정정, 2026-11-02) 저장소에는 BFS 코드가 없었다. 기존 par는 규칙을 옮겨 적은 일회성 솔버로 구했다 (`production/qa/evidence/story-004-obstacle-stages/bfs-and-play.md`). 이 스토리가 `BoardLogic`을 직접 호출하는 솔버를 `Assets/Editor/StageSolver.cs`(에디터 전용, 빌드에 들어가지 않음)로 새로 만들었다. par는 계속 데이터로 저장하고 런타임 BFS는 하지 않는다.
- 사용법: `ProjectShift.Editor.StageSolver.Solve(stageData)`가 par와 해답 행동 목록을 돌려준다. 시프트 제한이 탐색 한도이므로 새 스테이지를 설계할 때는 제한을 넉넉히 두고 돌린 뒤 par + 2로 맞춘다. 3대 스테이지는 par 8 이하로 설계하면 검증이 약 5초 이하다.
- `Q`가 0비용이므로 시프트 횟수 기준 레벨 BFS에서 같은 레벨 안의 전환을 먼저 닫는다 (0-1 BFS 또는 레벨별 전환 확장).
- par 정의는 기존과 같다: 시프트 제한 = par + 2 (결정), PAR 표시 = par 안에서 남은 시프트.

---

## Out of Scope

- 스테이지 데이터 입력 (Story 006)
- 규칙 자체 변경 (Story 001, 003)

---

## Files

- `Assets/Editor/StageSolver.cs` (new; editor-only, BoardLogic-driven 0-1 BFS with solution path)

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Stage/TwoRobotBfsTests.cs` (tests are waived at `qa.level: minimal`). 14개 스테이지 par 전후 비교 결과를 Completion Notes에 남긴다.

**Status**: [x] Tests waived (minimal). Run through `unity command script eval_file`: (1) existing 14 stages: solver par equals the stored par 14/14 (total 45 ms, worst Stage05 3816 states 23 ms); (2) hand-computed two-robot cases: shared-row shift par 1 with path ShiftRight r0, switch-only par 0 with SwitchRobot in the path, unreachable exit unsolved, already-cleared par 0 with an empty path; (3) solution replay through the real StageManager in play mode: all 14 existing stages plus three temporary stages (two with 2 robots, one with 3 robots) cleared and the run ended in ALL CLEAR, 17/17, console errors 0; (4) three robots on three rows, worst case (an unsolvable stage, so the whole space within the shift limit is searched): limit 6 = 270k states in 1.8 s, limit 8 = 766k states in 5.4 s, limit 10 = 1.69M states in 12.8 s. A solvable stage stops at its par, so a 3-robot stage with par up to 8 verifies in about 5 s or less.

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`, `production/epics/two-robots/story-003-exit-and-fail.md`
- Unlocks: `production/epics/two-robots/story-006-two-robot-stages.md`
