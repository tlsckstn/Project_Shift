# Story 003: 스테이지별 안내문 한 줄

> **Epic**: onboarding
> **Status**: Complete
> **Layer**: Feature
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 튜토리얼 (스테이지마다 안내문 한 줄, onboarding EPIC scope 3)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story:*

- [x] `StageData`에 선택 필드 `hint`(문자열 한 줄)가 있다.
- [x] `hint`가 비어 있으면 안내문 영역이 보이지 않는다.
- [x] `hint`가 있으면 스테이지 시작 시 화면에 표시되고, 스테이지가 바뀌면 새 스테이지 값으로 바뀐다.
- [x] 되돌리기·재시작 후에도 안내문이 계속 보인다.
- [x] 안내문은 보드, HUD, 조작키 안내(Story 002)와 겹치지 않는다.
- [x] 기존 Stage01-05는 `hint`가 비어 있어도 정상 동작한다.

---

## Implementation Notes

- 일반 기능이다: obstacles 에픽 소개 스테이지가 그대로 재사용한다. 장애물 전용 분기를 넣지 않는다.
- 문구 규칙(영어, 규칙만 설명, 키는 설명하지 않음)은 데이터 입력 규칙이다. 코드로 검사하지 않는다.
- 갱신은 스테이지 로드 시에만 한다.

---

## Out of Scope

- 여러 줄 안내, 단계별 팝업, 대사
- 튜토리얼 스테이지 문구 작성 (Story 004)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Board/BoardView.cs`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: retained screenshots in `production/qa/evidence/` (안내문 있는 스테이지, 없는 스테이지 각 1장)

**Status**: [x] `production/qa/evidence/story-003-stage-hint/` — `stage05-hint.png`, `stage05-hint-960x600.png`, `stage02-no-hint.png`, `stamp.txt`. Play mode eval (in-memory stage copies, assets untouched): hint shown on load, kept after undo/restart/fall, hidden on a stage without hint, hidden at ALL CLEAR. Console errors 0. 씬 수정 없음.

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/onboarding/story-004-tutorial-stages.md`

---

## Completion Notes
**Completed**: 2026-11-02
**Criteria**: 6/6 passing
**Deviations**: Implemented directly instead of via ui-programmer agent (size S); no engine-specialist review. No scene edit (label built in code). Hint must fit one line (~55 chars at 960x600); not enforced in code.
**Test Evidence**: waived at qa.level minimal; screenshots `production/qa/evidence/story-003-stage-hint/`
**Reviewed by**: skipped — Solo mode
