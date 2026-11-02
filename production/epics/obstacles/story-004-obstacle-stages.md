# Story 004: 장애물 소개·활용 스테이지

> **Epic**: obstacles
> **Status**: Ready
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: L
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 장애물마다 소개 스테이지 1개 + 활용 스테이지 1~2개 (obstacles EPIC scope 4)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계" and "Player goal & fail state", scoped to this story:*

- [ ] 장애물마다 소개 스테이지 1개와 활용 스테이지 1~2개가 있다 (잠긴 줄 → 방화벽 → 버그 칸 순).
- [ ] 새 스테이지는 Stage05 뒤에 순서대로 진행된다. 튜토리얼과 Stage01-05의 순서와 동작은 바뀌지 않는다.
- [ ] 소개 스테이지는 그 장애물 하나만 새로 등장하고, 영어 안내문 한 줄(약 55자 이하, 규칙만, 키 설명 없음)로 규칙을 설명한다.
- [ ] 활용 스테이지는 그 장애물을 써야만(또는 피해야만) 풀린다. 안내문은 비워도 된다.
- [ ] 모든 새 스테이지의 par는 장애물 규칙을 반영한 BFS 최소값이고, `shiftLimit`은 par + 2다.
- [ ] 모든 새 스테이지가 클리어 가능하며, 마지막 스테이지 다음에 ALL CLEAR가 나온다.
- [ ] 방화벽 칸을 출구로 두지 않는다 (Story 002 기준).

---

## Implementation Notes

- 새 `StageData` 에셋을 `Assets/Data/Stages/`에 만든다. 새 로직 없음.
- BFS 솔버(scratchpad, 방법은 `production/qa/evidence/story-004-tutorial-stages/bfs-and-play.md`)에 잠긴 줄, 방화벽, 버그 칸 규칙을 추가하고, 기존 8개 스테이지 par(4/4/2, 6/7/6/5/11)를 그대로 재현하는지 먼저 확인한다.
- `StageManager.stages` 배열 변경은 씬 수정이다. 수정 전에 사용자 확인을 받는다. 에디터 API로 바꾸고 저장한다.
- onboarding 튜토리얼은 고치지 않는다.

---

## Out of Scope

- 후보 장애물 (휘발성 칸, 클럭 줄, 단방향 줄, 무거운 줄)
- 스테이지 선택, 저장

---

## Files

- `Assets/Data/Stages/` 신규 에셋 (장애물별 소개 1 + 활용 1~2)
- `Assets/Scenes/SampleScene.unity` (`StageManager.stages` 배열, 사용자 확인 후)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: smoke check (advisory) + BFS 검증 기록. 플레이어가 보는 변화이므로 새 스테이지별 retained screenshot을 `production/qa/evidence/story-004-obstacle-stages/`에 남긴다.

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: `production/epics/obstacles/story-001-locked-row.md`, `production/epics/obstacles/story-002-firewall.md`, `production/epics/obstacles/story-003-bug-cell.md`
- Unlocks: None
