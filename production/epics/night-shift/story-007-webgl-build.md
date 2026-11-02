# Story 007: WebGL 빌드 확인

> **Epic**: night-shift
> **Status**: Done
> **Layer**: Feature
> **Type**: Config/Data
> **Estimate**: S
> **Manifest Version**: N/A (minimal — no control manifest)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/game-brief.md`
**Requirement**: Brief MVP feature 7 (itch.io용 WebGL 빌드 확인)

**ADR Governing Implementation**: N/A (minimal — no ADRs)
**ADR Decision Summary**: N/A (minimal — no ADRs)
**ADR Version**: N/A (minimal — no ADRs)

**Engine**: Unity 6000.3.14f1 | **Risk**: HIGH
**Engine Notes**: none (no ADR engine-compatibility analysis at minimal)

**Control Manifest Rules (this layer)**: N/A (minimal — no control manifest)

---

## Acceptance Criteria

*From `design/game-brief.md`, scoped to this story:*

- [x] 에디터에서 WebGL 플랫폼으로 빌드가 오류 없이 끝난다.
- [x] 빌드 결과를 브라우저(로컬 서버)에서 열면 스테이지 1이 보이고 키보드 입력이 동작한다.
- [x] itch.io에 올릴 수 있는 형태(zip, `index.html` 루트)임을 확인한다.
- [x] WebGL에서 막히는 API 사용이 없음을 확인한다 (스레드, 파일 I/O 등).
- [x] 확인한 빌드 방법과 결과(성공/실패, 크기)를 README나 evidence 문서에 한 줄 남긴다.

---

## Implementation Notes

- WebGL은 전용 `-build...Player` 플래그가 없어서 `commands.build`로 못 만든다. 에디터에서 직접 빌드하거나 `BuildPipeline`을 쓰는 별도 스크립트가 필요하다.
- WebGL 지원 모듈이 설치돼 있는지 먼저 확인한다 (Unity Hub의 6000.3.14f1 모듈).
- Unity 6.x 웹 관련 옵션은 `docs/engine-reference/unity/`에서 확인한 뒤 쓴다. 출처 없는 설정은 추측하지 않는다.
- 이 스토리는 프로토타입 단계에서 업로드 경로를 한 번 확인하는 것이 목적이다.

---

## Out of Scope

- 최종 제출 빌드, 압축·로딩 최적화
- 모바일 터치 대응

---

## Files

- `ProjectSettings/` 의 빌드 설정 (에디터에서 변경, 직접 편집 금지)
- `README.md` (빌드 방법 한 줄)

---

## QA Test Cases

*N/A — no qa-lead specs at this tier; implement against the Acceptance Criteria above*

---

## Test Evidence

**Story Type**: Config/Data
**Required evidence**: 스모크 체크 기록 `production/qa/smoke-[date].md` (advisory)

**Status**: [x] `production/qa/smoke-2026-11-02.md`

---

## Dependencies

- Depends on: `production/epics/night-shift/story-006-exit-and-stages.md`
- Unlocks: None
