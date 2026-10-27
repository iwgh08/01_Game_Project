using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// 방송실 콘솔: 전원 → 테이프 재생(서진이의 녹음) → 구역 스위치를 맞추고 송출 → 현관문이 열린다
public class BroadcastConsole : MonoBehaviour
{
    [SerializeField] ZoneSwitch[] zoneSwitches;
    [SerializeField] Renderer[] powerLamps;
    [SerializeField] Color lampColor = new(1f, 0.25f, 0.1f);
    [SerializeField] AudioSource monitorSpeaker;
    // "("로 시작하는 줄은 효과음 설명, 나머지는 서진이의 목소리
    [SerializeField, TextArea] string[] tapeLines;
    [SerializeField] DocumentData tapeTranscript;
    [SerializeField] UnityEvent onBroadcastComplete = new();

    bool tapePlayed, finished;

    public bool Powered { get; private set; }
    public bool IsBusy { get; private set; }

    void Start() => SetLamps(false);

    public void PowerOn()
    {
        Powered = true;
        SetLamps(true);
        foreach (var zone in zoneSwitches) zone.Refresh();
    }

    public void PlayTape(PlayerInteractor player)
    {
        if (!Powered)
        {
            SubtitleUI.Say("버튼을 눌러도 반응이 없다. 전기가 안 들어오는 것 같다.");
            return;
        }
        if (IsBusy) return;
        StartCoroutine(TapeRoutine(player));
    }

    public void Broadcast()
    {
        if (!Powered)
        {
            SubtitleUI.Say("전원이 들어오지 않는다.");
            return;
        }
        if (IsBusy || finished) return;
        if (!tapePlayed)
        {
            SubtitleUI.Say("송출할 게 없다. 테이프부터 들어 봐야겠다.");
            return;
        }

        bool anyOn = false, correct = true;
        foreach (var zone in zoneSwitches)
        {
            anyOn |= zone.IsOn;
            if (zone.IsOn != zone.ShouldBeOn) correct = false;
        }
        if (!anyOn)
        {
            SubtitleUI.Say("송출 구역 스위치가 전부 꺼져 있다.");
            return;
        }
        StartCoroutine(correct ? BroadcastRoutine() : WrongRoutine());
    }

    IEnumerator TapeRoutine(PlayerInteractor player)
    {
        IsBusy = true;
        monitorSpeaker.clip = ProceduralSounds.TapeHiss();
        monitorSpeaker.loop = true;
        monitorSpeaker.Play();
        yield return new WaitForSeconds(1.2f);

        for (int i = 0; i < tapeLines.Length; i++)
        {
            string line = tapeLines[i];
            bool spoken = !line.StartsWith("(");
            float duration = SubtitleUI.DurationFor(line) + 0.6f;
            string shown = spoken ? $"<color=#9EC3D9>서진: “{line}”</color>" : $"<color=#8A8A8A>{line}</color>";
            SubtitleUI.Instance.ShowNow(shown, duration);
            if (spoken) Sfx.Play3D(ProceduralSounds.Murmur(i, duration - 0.6f), monitorSpeaker.transform.position, 0.55f, 1.5f, 12f);
            yield return new WaitForSeconds(duration);
        }

        monitorSpeaker.Stop();
        tapePlayed = true;
        IsBusy = false;
        if (tapeTranscript != null) player.Inventory.AddDocument(tapeTranscript);
        SubtitleUI.Say("…들은 내용을 수첩에 적어 두었다. (Tab)");
    }

    IEnumerator WrongRoutine()
    {
        IsBusy = true;
        Sfx.Play3D(ProceduralSounds.Static(), monitorSpeaker.transform.position, 0.7f);
        yield return new WaitForSeconds(2f);
        SubtitleUI.Say("…스피커에서 잡음만 흘러나온다. 서진이가 말한 곳이 아닌가 보다.");
        IsBusy = false;
    }

    IEnumerator BroadcastRoutine()
    {
        IsBusy = true;
        finished = true;

        var chime = ProceduralSounds.Chime();
        PlayOnSpeakers(chime, 1f, false);
        yield return new WaitForSeconds(chime.length);

        var song = ProceduralSounds.Song();
        PlayOnSpeakers(song, 1f, false);
        SubtitleUI.Say("…서진이의 노래가 학교에 울려 퍼진다.");
        yield return new WaitForSeconds(song.length + 1f);

        // 방송이 끝난 뒤에도 노래는 교실과 음악실에서 작게 맴돈다
        foreach (var zone in zoneSwitches)
        {
            if (!zone.IsOn) continue;
            foreach (var speaker in zone.Speakers) PlayOn(speaker, song, 0.5f, true);
        }

        IsBusy = false;
        onBroadcastComplete.Invoke();
    }

    void PlayOnSpeakers(AudioClip clip, float volume, bool loop)
    {
        PlayOn(monitorSpeaker, clip, volume, loop);
        foreach (var zone in zoneSwitches)
        {
            if (!zone.IsOn) continue;
            foreach (var speaker in zone.Speakers) PlayOn(speaker, clip, volume, loop);
        }
    }

    static void PlayOn(AudioSource source, AudioClip clip, float volume, bool loop)
    {
        if (source == null) return;
        source.clip = clip;
        source.volume = volume;
        source.loop = loop;
        source.Play();
    }

    void SetLamps(bool on)
    {
        var block = new MaterialPropertyBlock();
        foreach (var lamp in powerLamps)
        {
            if (lamp == null) continue;
            lamp.GetPropertyBlock(block);
            block.SetColor("_BaseColor", on ? lampColor : new Color(0.08f, 0.08f, 0.08f));
            block.SetColor("_EmissionColor", on ? lampColor * 3f : Color.black);
            lamp.SetPropertyBlock(block);
        }
    }
}
