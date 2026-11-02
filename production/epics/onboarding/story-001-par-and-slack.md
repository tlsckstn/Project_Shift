# Story 001: 시프트 제한 여유와 par 표시

> **Epic**: onboarding
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 제한값 (onboarding EPIC scope 1)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story:*

- [ ] `StageData`에 `par`(최소 시프트 횟수) 필드가 있고 읽기 전용 프로퍼티로 노출된다.
- [ ] HUD에 남은 시프트와 별개로 par가 표시된다 (영어, 예: `SHIFTS 7  PAR 6`).
- [ ] par 표시는 스테이지가 바뀔 때만 갱신된다 (매 프레임 문자열 조합 없음).
- [ ] Stage01-05의 par가 BFS 최소값 6/7/6/5/11로 입력된다.
- [ ] Stage01-05의 `shiftLimit`이 각각 par + 1~2로 재조정된다.
- [ ] 시프트를 다 쓴 상태의 빨간 `SHIFTS 0  Z undo / R restart` 표시는 그대로 동작한다.
- [ ] par가 0인 스테이지(값 미입력)는 par를 표시하지 않는다.

---

## Implementation Notes

- par는 데이터로 저장한다. 런타임 BFS 계산은 하지 않는다.
- 표시는 `BoardView`의 기존 `shiftsLabel` 갱신 경로를 따른다.
- 스테이지 에셋(`Assets/Data/Stages/*.asset`) 값 변경은 인스펙터 또는 `unity command script eval`로 한다.

---

## Out of Scope

- Story 003: 안내문
- 별 평가 (par 대비 점수)

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Board/BoardView.cs`
- `Assets/Data/Stages/Stage01.asset` ~ `Stage05.asset`

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: retained screenshot in `production/qa/evidence/` (par 표시가 보이는 스테이지 화면). Tests waived at `qa.level: minimal`.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/night-shift/story-006-exit-and-stages.md` (Done)
- Unlocks: `production/epics/onboarding/story-004-tutorial-stages.md`
