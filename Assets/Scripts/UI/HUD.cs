using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 조준점, 상호작용 문구, 조작 안내
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [SerializeField] CanvasGroup group;
    [SerializeField] Image crosshair;
    [SerializeField] TMP_Text promptText;
    [SerializeField] CanvasGroup hintGroup;
    [SerializeField] TMP_Text hintText;

    string currentPrompt;
    float hintTimer;

    void Awake()
    {
        Instance = this;
        promptText.text = "";
        hintGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetPrompt(string prompt)
    {
        if (prompt == currentPrompt) return;
        currentPrompt = prompt;
        bool has = !string.IsNullOrEmpty(prompt);
        promptText.text = has ? $"<color=#E8D9A8>[E]</color>  {prompt}" : "";
        crosshair.rectTransform.localScale = Vector3.one * (has ? 1.6f : 1f);
        crosshair.color = new Color(1f, 1f, 1f, has ? 0.9f : 0.4f);
    }

    public void ShowHint(string text, float seconds = 6f)
    {
        hintText.text = text;
        hintTimer = seconds;
    }

    void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        float dt = Time.unscaledDeltaTime;
        group.alpha = Mathf.MoveTowards(group.alpha, playing ? 1f : 0f, dt * 6f);
        hintTimer -= dt;
        hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, hintTimer > 0f ? 1f : 0f, dt * 2f);
    }
}
