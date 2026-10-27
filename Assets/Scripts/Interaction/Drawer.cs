using UnityEngine;

// 앞으로 당겨 여는 서랍. movingPart를 지정하면 책상 전체를 바라봐도 서랍이 열린다.
public class Drawer : MonoBehaviour, IInteractable
{
    [SerializeField] Transform movingPart;
    [SerializeField] Vector3 openOffset = new(0f, 0f, 0.3f);
    [SerializeField] float duration = 0.35f;
    [SerializeField] bool locked;
    [SerializeField] string lockedMessage = "열리지 않는다.";
    [SerializeField] string openPrompt = "서랍 열기";
    [SerializeField] string closePrompt = "서랍 닫기";

    Vector3 closedPosition;
    float progress;
    bool isOpen;

    Transform Part => movingPart != null ? movingPart : transform;

    public string Prompt => isOpen ? closePrompt : openPrompt;

    void Awake() => closedPosition = Part.localPosition;

    public void Interact(PlayerInteractor player)
    {
        if (locked)
        {
            Sfx.Play3D(ProceduralSounds.DoorRattle(), Part.position, 0.4f);
            SubtitleUI.Say(lockedMessage);
            return;
        }
        if (isOpen) Close();
        else Open();
    }

    public void Unlock() => locked = false;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        Sfx.Play3D(ProceduralSounds.DrawerSlide(), Part.position, 0.5f);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        Sfx.Play3D(ProceduralSounds.DrawerSlide(), Part.position, 0.4f);
    }

    void Update()
    {
        float target = isOpen ? 1f : 0f;
        if (Mathf.Approximately(progress, target)) return;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        Part.localPosition = closedPosition + openOffset * Mathf.SmoothStep(0f, 1f, progress);
    }
}
