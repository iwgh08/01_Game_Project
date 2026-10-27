using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 화면 아래 자막 (주인공의 혼잣말, 테이프 대사). 여러 줄이 오면 차례로 보여 준다.
public class SubtitleUI : MonoBehaviour
{
    public static SubtitleUI Instance { get; private set; }

    [SerializeField] TMP_Text text;
    [SerializeField] CanvasGroup group;

    readonly Queue<(string line, float duration)> queue = new();
    float timer;

    void Awake()
    {
        Instance = this;
        text.text = "";
        group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Say(string line)
    {
        if (Instance != null) Instance.SayLine(line);
    }

    public static float DurationFor(string line) => Mathf.Clamp(1.8f + line.Length * 0.07f, 2.2f, 7f);

    // UnityEvent에서 연결하는 용도
    public void SayLine(string line)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (timer > 0f && text.text == line) return;
        foreach (var queued in queue)
            if (queued.line == line) return;

        queue.Enqueue((line, DurationFor(line)));
        if (timer <= 0f) Next();
    }

    public void SayLines(string[] lines)
    {
        foreach (string line in lines) SayLine(line);
    }

    // 대기 중인 자막을 지우고 바로 보여 준다 (테이프 재생처럼 시간을 맞춰야 할 때)
    public void ShowNow(string line, float duration)
    {
        queue.Clear();
        queue.Enqueue((line, duration));
        Next();
    }

    void Next()
    {
        if (queue.Count == 0)
        {
            timer = 0f;
            return;
        }
        var (line, duration) = queue.Dequeue();
        text.text = line;
        timer = duration;
    }

    void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Next();
        }
        group.alpha = Mathf.MoveTowards(group.alpha, timer > 0f ? 1f : 0f, Time.unscaledDeltaTime * 5f);
    }
}
