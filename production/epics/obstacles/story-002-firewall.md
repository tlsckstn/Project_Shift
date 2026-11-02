# Story 002: 방화벽

> **Epic**: obstacles
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 방화벽 (obstacles EPIC scope 2)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story. 규칙 해석은 사용자 결정 (2026-11-02, 안 A): 벽은 걸어서·내려가서 들어가는 것만 막는다.*

- [ ] `StageData`에 선택 필드로 방화벽 칸 목록(보드 좌표 row, column)이 있고, 비어 있으면 기존 스테이지와 동작이 같다.
- [ ] 방화벽은 보드 좌표에 고정된다. 줄을 밀어도 벽은 움직이지 않고, 그 칸의 비트만 밑으로 지나간다.
- [ ] 걷기로 방화벽 칸에 들어갈 수 없다 (발판이 켜져 있어도 막힌다).
- [ ] 내려가기로 방화벽 칸에 들어갈 수 없다.
- [ ] 줄에 실려 밀리면 로봇이 방화벽 칸으로 들어가거나 통과할 수 있다.
- [ ] 방화벽 칸 위에 멈춘 로봇은 그 자리에 서 있을 수 있고, 거기서 걷거나 내려가서 나가는 것은 평소 규칙대로 된다.
- [ ] 방화벽 칸이 출구여도 걷기·내려가기로는 들어갈 수 없다 (밀려서 도착하면 클리어). 스테이지 데이터에서는 이런 배치를 피한다.
- [ ] 방화벽은 화면에서 일반 발판, 잠긴 줄, 버그 칸과 구분된다.

---

## Implementation Notes

- 규칙은 `BoardLogic.TryWalk`, `TryDescend`의 목표 칸 검사에 넣는다 (순수 C#). `TryShift`와 `MoveRobotWithRow`는 바꾸지 않는다.
- 방화벽 좌표는 `StageData.CreateState()`가 `BoardState`에 넘기고, `Clone()`은 같은 배열을 공유한다.
- 표시는 셀 위 오버레이. 줄이 밀려도 오버레이는 보드 좌표에 고정한다.

---

## Out of Scope

- 버그 칸 (Story 003)
- 방화벽 소개·활용 스테이지 (Story 004)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Board/BoardState.cs`
- `Assets/00_Scripts/Board/BoardLogic.cs`
- `Assets/00_Scripts/Board/BoardView.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/FirewallTests.cs` (tests are waived at `qa.level: minimal`). 플레이어가 보는 변화이므로 retained screenshot을 `production/qa/evidence/story-002-firewall/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/obstacles/story-001-locked-row.md` (표시 방식이 겹치지 않게 순서대로)
- Unlocks: `production/epics/obstacles/story-004-obstacle-stages.md`
