using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 현관을 나서면 암전 후 엔딩 문구를 보여 주고 타이틀로 돌아간다
public class EndingSequence : MonoBehaviour
{
    [SerializeField, TextArea] string[] lines =
    {
        "학교 문을 나서자, 등 뒤에서 노래가 멈췄다.",
        "20년 전 그날, 나는 방송실에 가지 않았다.",
        "그리고 오늘에서야\n그 노래를 끝까지 들었다.",
        "<size=130%>방과후 방송</size>\n\n<size=60%>끝</size>"
    };

    bool playing;

    public void Play()
    {
        if (playing) return;
        playing = true;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        GameManager.Instance.SetState(GameState.Cutscene);
        var overlay = CinematicOverlay.Instance;

        float startVolume = AudioListener.volume;
        for (float t = 0f; t < 2.5f; t += Time.unscaledDeltaTime)
        {
            overlay.SetBlack(t / 2.5f);
            AudioListener.volume = Mathf.Lerp(startVolume, 0f, t / 2.5f);
            yield return null;
        }
        overlay.SetBlack(1f);

        foreach (string line in lines) yield return overlay.ShowText(line, 1.2f, 3.2f, 1.2f);
        yield return new WaitForSecondsRealtime(1f);

        AudioListener.volume = startVolume;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
