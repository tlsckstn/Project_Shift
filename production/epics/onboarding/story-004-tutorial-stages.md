# Story 004: 튜토리얼 스테이지 2~3개

> **Epic**: onboarding
> **Status**: Complete
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 튜토리얼 (스테이지 1 앞 튜토리얼 2~3개, onboarding EPIC scope 4)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "Player goal & fail state" and "다음 단계", scoped to this story:*

- [x] 스테이지 1 앞에 튜토리얼 스테이지 2~3개가 순서대로 진행된다.
- [x] 튜토리얼 전체로 기본 규칙 네 가지를 모두 다룬다: 걷기, 줄 밀기와 함께 밀림, 내려가기, 추락.
- [x] 각 튜토리얼 스테이지에 영어 안내문(`hint`) 한 줄이 있고, 규칙만 설명하며 키는 설명하지 않는다.
- [x] 각 튜토리얼 스테이지의 par는 BFS 최소값이고, `shiftLimit`은 par + 1~2다.
- [x] 각 튜토리얼 스테이지가 클리어 가능하며 출구 도달 후 다음 스테이지로 넘어간다.
- [x] 튜토리얼 이후 Stage01-05 진행 순서와 동작은 바뀌지 않는다.
- [x] 장애물(잠긴 줄, 방화벽, 버그 칸)은 등장하지 않는다.

---

## Implementation Notes

- 새 `StageData` 에셋을 `Assets/Data/Stages/`에 만든다 (예: `Tutorial01.asset`). 새 로직 없음.
- BFS 솔버는 저장소에 없다. 스크래치 스크립트로 다시 만들어 par를 검증하고 코드에는 남기지 않는다.
- `StageManager.stages` 배열 순서 변경은 씬 수정이다. 수정 전에 사용자 확인을 받는다.
- 이 튜토리얼은 obstacles 에픽에서 수정하지 않는다 (장애물 소개는 obstacles 에픽 소관).

---

## Out of Scope

- 장애물 소개 스테이지 (obstacles 에픽)
- 튜토리얼 건너뛰기, 스테이지 선택

---

## Files

- `Assets/Data/Stages/Tutorial01.asset` (par 4, 제한 6, 걷기·함께 밀림)
- `Assets/Data/Stages/Tutorial02.asset` (par 4, 제한 6, 내려가기)
- `Assets/Data/Stages/Tutorial03.asset` (par 2, 제한 4, 추락)
- `Assets/Scenes/SampleScene.unity` (`StageManager.stages` 앞에 3개 추가, 사용자 확인 2026-11-02. BoardView 새 직렬화 필드도 기본값으로 함께 저장됨)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check (advisory) + BFS 검증 결과 기록. 플레이어가 보는 변화이므로 튜토리얼 스테이지별 retained screenshot을 `production/qa/evidence/`에 남긴다.

**Status**: [x] `production/qa/evidence/story-004-tutorial-stages/` — `tutorial01~03.png` (960x600), `bfs-and-play.md` (BFS par 4/4/2, 기존 6/7/6/5/11 재현, 플레이 진행 기록), `stamp.txt`.

---

## Dependencies

- Depends on: `production/epics/onboarding/story-001-par-and-slack.md`, `production/epics/onboarding/story-003-stage-hint.md`
- Unlocks: obstacles 에픽 소개 스테이지

---

## Completion Notes
**Completed**: 2026-11-02
**Criteria**: 7/7 passing
**Deviations**: Scene edited through the Editor API with user approval (also saved BoardView's new serialized fields at their defaults). BFS solver kept in scratchpad only; method and results in `bfs-and-play.md`.
**Test Evidence**: smoke via play mode; screenshots and BFS record `production/qa/evidence/story-004-tutorial-stages/`
**Reviewed by**: skipped — Solo mode
