using System.Collections;
using TMPro;
using UnityEngine;

// 암전과 화면 가운데 글자 (인트로·엔딩 연출용)
public class CinematicOverlay : MonoBehaviour
{
    public static CinematicOverlay Instance { get; private set; }

    [SerializeField] CanvasGroup black;
    [SerializeField] CanvasGroup textGroup;
    [SerializeField] TMP_Text text;

    void Awake()
    {
        Instance = this;
        textGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetBlack(float alpha) => black.alpha = alpha;

    public IEnumerator FadeBlack(float to, float duration) => Fade(black, to, duration);

    public IEnumerator ShowText(string message, float fadeIn, float hold, float fadeOut)
    {
        text.text = message;
        yield return Fade(textGroup, 1f, fadeIn);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(textGroup, 0f, fadeOut);
    }

    static IEnumerator Fade(CanvasGroup group, float to, float duration)
    {
        float from = group.alpha;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
