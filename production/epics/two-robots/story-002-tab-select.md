# Story 002: Q 전환과 선택 표시

> **Epic**: two-robots
> **Status**: In Progress
> **Layer**: Feature
> **Type**: UI
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP 2 — "Q 전환과 선택 로봇 표시 (색 구분 + 테두리)"

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md` "여러 로봇", scoped to this story:*

- [ ] `Q`를 누르면 조종 대상이 다음 로봇으로 차례로 바뀐다 (0 → 1 → 2 → 0) (Story 001의 선택 전환 메서드 호출). 시프트 횟수·PAR는 변하지 않는다.
- [ ] 로봇마다(최대 3대) 서로 다른 색이고, 선택된 로봇에는 테두리가 있어 한눈에 구분된다.
- [ ] 로봇이 1대인 스테이지에서는 `Q`를 눌러도 아무 일도 없고, 표시도 기존과 같다.
- [ ] 같은 칸에 로봇 2~3대가 겹쳐도 선택된 쪽이 구분된다.
- [ ] 조작키 패널에 `Q` 항목이 여러 로봇 스테이지에서만 보인다 (규칙 설명이 아니라 키 안내이므로 패널에 둔다).
- [ ] 줄 밀기 키를 `Q`/`E`에서 `A`/`D`로 바꾼다 (`A` = 왼쪽 `<<`, `D` = 오른쪽 `>>`). `E`는 더 이상 쓰지 않는다. 로봇 수와 관계없이 모든 스테이지에 적용된다.
- [ ] 조작키 패널이 새 배치(`W`/`S` 줄 선택, `A`/`D` 줄 밀기, 여러 로봇 스테이지에서만 `Q` 로봇 전환)를 보여 준다.
- [ ] WebGL 빌드에서 `A`/`D`/`Q`가 동작한다.
- [ ] 각 스테이지 화면 스크린샷이 `production/qa/evidence/story-002-tab-select/`에 남아 있다.

---

## Implementation Notes

- 입력은 기존 `InputController` 패턴을 따른다. 새 입력 방식을 만들지 않는다.
- 바인딩은 `Assets/InputSystem_Actions.inputactions`의 `Puzzle` 맵에 있다: `ShiftLeft`/`ShiftRight` 경로를 `<Keyboard>/a`·`<Keyboard>/d`로 바꾸고, `SwitchRobot` 액션을 `<Keyboard>/q`로 추가한다.
- 키 결정 (2026-11-02): WASD = 줄 조작(왼손), 방향키 = 로봇(오른손), `Q` = 로봇 전환. `Tab`은 WebGL에서 브라우저 포커스를 가져갈 위험이 있어 쓰지 않는다.
- 위험: WASD를 캐릭터 이동으로 기대하는 플레이어가 `A`를 누르면 줄이 밀려 시프트 1회를 쓴다. 다음 플레이테스트 관찰 항목에 넣는다.
- 출구 색을 로봇 색과 맞추는 표시는 Story 003(출구가 로봇별이 되는 시점)에서 쓰므로 색 정의를 한 곳에 둔다.
- 씬 파일(`.unity`) 수정이 필요하면 먼저 사용자에게 확인한다.

---

## Out of Scope

- 클리어/실패 판정 (Story 003)
- 안내문 문구 (Story 006)

---

## Files

- `Assets/InputSystem_Actions.inputactions`
- `Assets/00_Scripts/Input/InputController.cs`
- `Assets/00_Scripts/Stage/StageManager.cs`
- `Assets/00_Scripts/Board/BoardView.cs`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: 2대·3대 스테이지에서 로봇 선택 전환 전·후 스크린샷, 1대 스테이지 스크린샷을 `production/qa/evidence/story-002-tab-select/`에 retained (tests waived at `qa.level: minimal`, screenshot is not).

**Status**: [x] Screenshots retained in `production/qa/evidence/story-002-tab-select/` (01 two robots robot 0 selected, 02 after Q robot 1 selected, 03 three robots overlapping robot 2 selected on top, 04 one-robot stage: no outline, no Q in panel). Real key path checked in play mode via the input queue: Q cycles 0-1 (2 robots) and 0-1-2-0 (3 robots) with no shift spent, Q is a no-op with 1 robot, A/D shift (cost 1), old Q/E shift keys gone (E does nothing). Console errors 0. Two-robot states were injected in memory (no two-robot StageData yet). NOT checked: WebGL build with A/D/Q (no rebuild yet).

---

## Dependencies

- Depends on: `production/epics/two-robots/story-001-two-robot-state.md`
- Unlocks: `production/epics/two-robots/story-006-two-robot-stages.md`
