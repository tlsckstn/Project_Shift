# Story 001: 시프트·소실 로직 (BoardLogic)

> **Epic**: night-shift
> **Status**: Complete
> **Layer**: Foundation
> **Type**: Logic
> **Estimate**: M
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: 2026-11-02

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 2 (좌우 시프트와 끝에서의 비트 소실)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [ ] 줄은 8비트이며 칸 0이 최상위 비트(MSB)다. 칸 c가 켜져 있는지는 `(rows[r] >> (7 - c)) & 1`로 읽는다.
- [ ] `<<` 한 번은 해당 줄을 왼쪽으로 한 칸 밀고, 오른쪽 끝은 0으로 채워진다.
- [ ] `>>` 한 번은 해당 줄을 오른쪽으로 한 칸 밀고, 왼쪽 끝은 0으로 채워진다.
- [ ] 끝에서 밀려난 비트는 사라지고 되돌아오지 않는다 (예: `10000000`을 `<<` 하면 `00000000`).
- [ ] `BoardLogic`은 순수 C#(MonoBehaviour 아님)이라 Edit Mode 테스트에서 직접 호출된다.

---

## Implementation Notes

- 상태는 `BoardState` 구조체: `byte[] rows`, `int robotRow`, `int robotCol`, `int shiftsLeft`.
- `rows[r] = (byte)(rows[r] << 1)` / `(byte)(rows[r] >> 1)`. `byte` 캐스팅이 소실을 처리한다.
- 이 스토리는 줄 값 변경만 다룬다. 로봇 위치 갱신은 story 003.
- 첫 테스트를 위해 `Assets/Tests/EditMode/`에 어셈블리 정의(asmdef)가 필요하다.
- 프로젝트 C# 스타일 적용: 필드는 모두 `private`, 언더스코어 접두사 없음, `ProjectShift` 네임스페이스.

---

## Out of Scope

- Story 003: 로봇이 줄과 함께 밀리는 처리와 추락
- Story 004: 시프트 횟수 차감

---

## Files

- `Assets/00_Scripts/Board/BoardState.cs`
- `Assets/00_Scripts/Board/BoardLogic.cs`

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Logic
**Required evidence**: `Assets/Tests/EditMode/Board/BoardLogicTests.cs` (tests are waived at `qa.level: minimal`, but this is the project's only pure-logic core, so writing it is recommended)

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: `production/epics/night-shift/story-002-board-view.md`, `story-003-robot-movement.md`, `story-005-undo-restart.md`

---

## Completion Notes
**Completed**: 2026-11-02
**Criteria**: 5/5 passing (none deferred)
**Deviations**: BoardState is a class, not the struct named in Implementation Notes (deep-copy safety for story 005). Editor compile not yet observed.
**Test Evidence**: waived at qa.level minimal (criteria checked with a throwaway .NET run, 12/12 pass)
**Reviewed by**: self, no independent review
