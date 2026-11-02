# Story 005: BFS 여러 로봇 확장

> **Epic**: two-robots
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: —

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
- [ ] 기존 11개 스테이지의 par가 이전 BFS 결과와 정확히 같다 (회귀).
- [ ] 2대 스테이지와 3대 스테이지를 각각 검증하는 데 에디터에서 몇 초 안에 끝난다. 3대가 느리면 구현 전에 사용자에게 알린다 (상태 키 압축, 3대 스테이지의 줄 수 제한 등).
- [ ] 해답 경로(행동 목록)를 얻을 수 있어 스테이지 해답을 직접 재생해 볼 수 있다.

---

## Implementation Notes

- 현재 BFS는 `Assets/00_Scripts/Stage/StageData.cs`에 있다. 상태 키 만들기와 확장이 커지면 분리 여부는 `/dev-story`에서 판단한다 (분리는 필요할 때만).
- `Q`가 0비용이므로 시프트 횟수 기준 레벨 BFS에서 같은 레벨 안의 전환을 먼저 닫는다 (0-1 BFS 또는 레벨별 전환 확장).
- par 정의는 기존과 같다: 시프트 제한 = par + 2 (결정), PAR 표시 = par 안에서 남은 시프트.

---

## Out of Scope

- 스테이지 데이터 입력 (Story 006)
- 규칙 자체 변경 (Story 001, 003)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/Tests/EditMode/Stage/TwoRobotBfsTests.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Stage/TwoRobotBfsTests.cs` (tests are waived at `qa.level: minimal`). 11개 스테이지 par 전후 비교 결과를 Completion Notes에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`, `production/epics/two-robots/story-003-exit-and-fail.md`
- Unlocks: `production/epics/two-robots/story-006-two-robot-stages.md`
