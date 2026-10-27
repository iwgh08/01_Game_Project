using UnityEngine;

// 방송 송출 구역 스위치. 켜진 구역의 스피커로만 방송이 나간다.
public class ZoneSwitch : MonoBehaviour, IInteractable
{
    [SerializeField] BroadcastConsole console;
    [SerializeField] string zoneName;
    [SerializeField] bool shouldBeOn;
    [SerializeField] AudioSource[] speakers;
    [SerializeField] Transform lever;
    [SerializeField] Renderer lamp;
    [SerializeField] Color lampColor = new(0.3f, 1f, 0.35f);

    MaterialPropertyBlock block;

    public bool IsOn { get; private set; }
    public bool ShouldBeOn => shouldBeOn;
    public AudioSource[] Speakers => speakers;

    public string Prompt => $"'{zoneName}' 구역 스위치 {(IsOn ? "끄기" : "켜기")}";

    void Start() => Refresh();

    public void Interact(PlayerInteractor player)
    {
        if (console.IsBusy) return;
        IsOn = !IsOn;
        Sfx.Play3D(ProceduralSounds.SwitchClick(), transform.position, 0.7f);
        Refresh();
    }

    public void Refresh()
    {
        if (lever != null) lever.localRotation = Quaternion.Euler(IsOn ? -25f : 25f, 0f, 0f);
        if (lamp == null) return;
        bool glowing = IsOn && console.Powered;
        block ??= new MaterialPropertyBlock();
        lamp.GetPropertyBlock(block);
        block.SetColor("_BaseColor", glowing ? lampColor : new Color(0.08f, 0.08f, 0.08f));
        block.SetColor("_EmissionColor", glowing ? lampColor * 2.5f : Color.black);
        lamp.SetPropertyBlock(block);
    }
}
