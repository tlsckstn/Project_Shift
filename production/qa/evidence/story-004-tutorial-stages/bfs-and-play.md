# Story 004 BFS 검증 (2026-11-02)

BoardLogic 규칙을 옮긴 0-1 BFS (걷기·내려가기 0회, 시프트 1회, 추락 상태 제외). 기존 스테이지 par 재현으로 솔버 검증.

```
Stage01 par 6 OK
Stage02 par 7 OK
Stage03 par 6 OK
Stage04 par 5 OK
Stage05 par 11 OK
Tutorial01 par 4 
Tutorial02 par 4 
Tutorial03 par 2 
```

Play mode (StageManager 공개 메서드): T1 걷기3 + >>4 → T2, T2 걷기3 + >>4 + 내려가기 → T3, T3 >>2 → FELL! Restarted, T3 ←1 + <<2 + 내려가기 → Stage01 (안내문 숨김). 콘솔 에러 0.
