# Epic: Night Shift <<

> **Status**: In progress
> **Source**: `design/game-brief.md` (minimal tier, no GDD or ADR)

## Goal

CPU 안에서 야간 근무하는 작은 로봇이 비트 줄을 `<<` `>>`로 밀어 발판을 옮기고 출구까지 길을 만드는 퍼즐 게임. Game Off 2026 (주제 SHIFT) 출품작의 프로토타입을 11/9까지 검증한다.

## Scope

브리프의 MVP 7개. 로테이트·캐리·산술 시프트, 그래픽·사운드, 스토리·메뉴, 저장, 레벨 에디터는 범위 밖.

## Order

브리프의 Build order: 시프트 로직 → 보드 표시 → 로봇 이동 → 횟수 제한 → 되돌리기 → 출구·스테이지 → WebGL.

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 시프트·소실 로직 (BoardLogic) | Logic | Complete | N/A |
| 002 | 보드 표시와 10진수 값 (BoardView) | UI | Ready | N/A |
| 003 | 로봇 이동·줄과 함께 밀림·추락 | Logic | Ready | N/A |
| 004 | 시프트 횟수 제한과 표시 | Logic | Ready | N/A |
| 005 | 되돌리기(Undo)와 재시작 | Logic | Ready | N/A |
| 006 | 출구 판정과 스테이지 5개 진행 | Integration | Ready | N/A |
| 007 | WebGL 빌드 확인 | Config/Data | Ready | N/A |
