# Story 006: 여러 로봇 소개·활용 스테이지 (2대·3대)

> **Epic**: two-robots
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: —

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 6 — "2대 소개 스테이지 1개 + 2대 활용 2~3개 + 3대 스테이지 1~2개(맨 끝), 안내문 한 줄"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "여러 로봇" + "다음 단계":*

- [ ] 2대 소개 1개 → 2대 활용 2~3개 → 3대 스테이지 1~2개 순서로 `StageData[]`의 기존 11개(장애물 소개·활용 포함) 뒤에 들어 있다 (기존 스테이지 변경 없음).
- [ ] 소개 스테이지는 `Q`로 로봇을 한 번 이상 바꿔야만 풀린다 (BFS 해답에 전환이 포함됨).
- [ ] 활용 스테이지와 3대 스테이지는 기존 장애물(방화벽, 버그 칸, 잠긴 줄) 중 하나 이상을 함께 쓴다.
- [ ] 3대 스테이지는 세 로봇을 모두 한 번 이상 조종해야 풀린다 (BFS 해답 기준).
- [ ] 모든 스테이지는 Story 005의 BFS로 풀 수 있음을 확인했고 par가 입력되어 있다. 시프트 제한 = par + 2.
- [ ] 각 스테이지에 안내문 한 줄(영어, 규칙만 설명하고 키는 설명하지 않는다)이 있다.
- [ ] 소개 스테이지의 안내문은 "두 로봇 모두 출구에 도달해야 하고 시프트는 공유된다"는 규칙을 담는다.
- [ ] 각 스테이지를 실제 플레이로 해답대로 풀어 클리어된다 (BFS 해답 재생 또는 수동).
- [ ] 소개 스테이지 화면 스크린샷이 `production/qa/evidence/story-006-two-robot-stages/`에 남아 있다.
- [ ] WebGL 빌드에서 3대 스테이지까지 진행됨을 확인한다 (`Build Finished, Result: Success.` + 스크린샷).

---

## Implementation Notes

- 스테이지는 코드 변경 없이 데이터만 추가한다. 코드 변경이 필요해지면 Story 001~005가 부족한 것이므로 해당 스토리로 되돌린다.
- 줄은 8칸, 보드는 2~3줄 (기존 규칙). 출구 색은 로봇 색과 맞춘다.
- 안내문은 영어 한 줄, 키(Q)는 조작키 패널이 맡는다.
- 씬 파일 수정이 필요하면 먼저 사용자에게 확인한다.

---

## Out of Scope

- 새 규칙·새 장애물
- 기존 11개 스테이지 조정

---

## Files

- `Assets/00_Scripts/Stage/StageData.cs` 또는 `StageData[]`가 정의된 에셋/배열 위치 (구현 때 확정)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check pass (`production/qa/smoke-*.md`) + 플레이어가 보는 변화이므로 retained screenshot (`production/qa/evidence/story-006-two-robot-stages/`)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/two-robots/story-002-tab-select.md`, `production/epics/two-robots/story-003-exit-and-fail.md`, `production/epics/two-robots/story-004-undo-selection.md`, `production/epics/two-robots/story-005-bfs-two-robots.md`
- Unlocks: None
