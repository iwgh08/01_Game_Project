using System.Collections;
using UnityEngine;

// 시작 연출: 20년 전 문자 → 오늘 밤 → 화면이 밝아지며 조작 시작
public class IntroSequence : MonoBehaviour
{
    [SerializeField] bool playIntro = true;

    IEnumerator Start()
    {
        var overlay = CinematicOverlay.Instance;
        var gm = GameManager.Instance;
        overlay.SetBlack(1f);
        gm.SetState(GameState.Cutscene);

        if (playIntro)
        {
            yield return new WaitForSecondsRealtime(0.8f);
            yield return overlay.ShowText(
                "<size=60%><color=#8A8A8A>2006. 10. 17.  오후 6:42</color></size>\n\n<b>서진</b>\n방송실로 와. 들려줄 게 있어.",
                1.2f, 3.8f, 1.2f);
            yield return overlay.ShowText("2026년 10월 17일 밤\n<size=70%>철거를 일주일 앞둔 학교</size>", 1.2f, 2.8f, 1.2f);
        }

        yield return overlay.FadeBlack(0f, 2.5f);
        gm.SetState(GameState.Playing);
        if (HUD.Instance != null)
            HUD.Instance.ShowHint("WASD 이동 · 마우스 시점 · Shift 달리기 · C 앉기\nE 상호작용 · Tab 소지품 · Esc 일시정지", 9f);
        SubtitleUI.Say("…20년 만이네.");
    }
}
