# Story 002: 조작키 상시 표시

> **Epic**: onboarding
> **Status**: Ready
> **Layer**: Feature
> **Type**: UI
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief "다음 단계" — 튜토리얼 (조작키 상시 표시, onboarding EPIC scope 2)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "다음 단계", scoped to this story:*

- [ ] 화면 하단에 조작키 안내가 모든 스테이지에서 항상 보인다.
- [ ] 문구는 영어이고 `InputController`의 실제 키와 일치한다: ←/→ walk, ↓ descend, W/S select row, Q/E shift `<<` `>>`, Z undo, R restart.
- [ ] 안내가 보드, 남은 횟수, 안내문(Story 003) 표시와 겹치지 않는다.
- [ ] 정적 텍스트이며 런타임에 매 프레임 갱신하지 않는다.

---

## Implementation Notes

- 기존 HUD와 같은 TextMeshPro 라벨 하나면 충분하다. 별도 매니저나 키 리바인딩 연동은 만들지 않는다.
- 씬(`.unity`)에 라벨을 추가해야 하면 수정 전에 사용자 확인을 받는다.

---

## Out of Scope

- 키 리바인딩, 게임패드 표기
- 안내문 (Story 003)

---

## Files

- `Assets/00_Scripts/Board/BoardView.cs` (또는 씬의 정적 라벨만)
- `Assets/Scenes/` 메인 씬 (사용자 확인 후)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: retained screenshot in `production/qa/evidence/` (하단 키 안내가 보이는 화면, WebGL 해상도 포함)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: None
