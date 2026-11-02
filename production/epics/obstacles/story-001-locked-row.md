# Story 001: 잠긴 줄

> **Epic**: obstacles
> **Status**: Ready
> **Layer**: Core
> **Type**: Logic
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 잠긴 줄 (obstacles EPIC scope 1)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story:*

- [ ] `StageData`에 선택 필드로 잠긴 줄 목록(줄 인덱스)이 있고, 비어 있으면 기존 스테이지와 동작이 같다.
- [ ] 잠긴 줄을 `<<` 또는 `>>`로 밀면 줄 값, 로봇 위치, 남은 시프트, PAR 모두 그대로다.
- [ ] 잠긴 줄을 미는 시도는 Undo 기록에 남지 않는다 (다음 Undo는 그 전 동작을 되돌린다).
- [ ] 잠긴 줄 위에서도 걷기와 내려가기는 평소처럼 동작한다.
- [ ] 잠긴 줄도 선택할 수 있다 (선택 하이라이트가 보인다).
- [ ] 잠긴 줄은 화면에서 일반 줄과 구분된다 (색 또는 모양, 이후 방화벽·버그 칸 표시와도 겹치지 않는 표현).

---

## Implementation Notes

- 규칙은 `BoardLogic.TryShift`에 둔다 (순수 C#). 잠긴 줄이면 false를 반환하고 아무것도 바꾸지 않는다. 그러면 `StageManager.AfterAction`이 호출되지 않아 Undo 기록도 남지 않는다.
- 잠긴 줄 정보는 `StageData.CreateState()`가 `BoardState`에 넘긴다. 스테이지 중에 바뀌지 않으므로 `Clone()`은 같은 배열을 공유해도 된다.
- 표시는 `BoardView`가 기존 셀 풀링 구조 안에서 한다. 새 라벨, 매니저는 만들지 않는다.

---

## Out of Scope

- Story 002, 003: 방화벽, 버그 칸
- Story 004: 잠긴 줄 소개·활용 스테이지
- 시도했지만 막혔다는 효과음, 애니메이션

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
**Required evidence**: `Assets/Tests/EditMode/Board/LockedRowTests.cs` (tests are waived at `qa.level: minimal`). 플레이어가 보는 변화이므로 retained screenshot을 `production/qa/evidence/story-001-locked-row/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/onboarding/story-001-par-and-slack.md` (Complete)
- Unlocks: `production/epics/obstacles/story-004-obstacle-stages.md`
