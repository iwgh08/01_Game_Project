public enum GameState
{
    Playing,    // 직접 조작 중
    UI,         // 문서·소지품·자물쇠 화면 (커서 표시)
    Paused,     // 일시정지 (시간 정지)
    Cutscene    // 인트로·엔딩 연출 (조작 불가)
}
