using UnityEngine;
using UnityEngine.Events;

// 경첩으로 도는 문 또는 옆으로 미는 미닫이문. 사물함 문에도 쓴다.
// 회전형이면 이 오브젝트의 위치가 경첩 축이다.
public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] bool locked;
    [SerializeField] ItemData requiredKey;
    [SerializeField] string lockedMessage = "잠겨 있다.";
    [SerializeField] string openPrompt = "문 열기";
    [SerializeField] string closePrompt = "문 닫기";
    [SerializeField] bool startOpen;

    [Header("회전문")]
    [SerializeField] Vector3 axis = Vector3.up;
    [SerializeField] float openAngle = 95f;

    [Header("미닫이문")]
    [SerializeField] bool sliding;
    [SerializeField] Vector3 slideOffset = new(-0.95f, 0f, 0f);

    [SerializeField] float openDuration = 1.1f;
    [SerializeField] UnityEvent onUnlocked = new();

    Quaternion closedRotation;
    Vector3 closedPosition;
    float progress, speed;
    bool isOpen;

    public string Prompt => isOpen ? closePrompt : openPrompt;

    void Awake()
    {
        closedRotation = transform.localRotation;
        closedPosition = transform.localPosition;
        speed = 1f / openDuration;
        if (startOpen)
        {
            isOpen = true;
            progress = 1f;
            ApplyPose();
        }
    }

    public void Interact(PlayerInteractor player)
    {
        if (locked)
        {
            if (requiredKey != null && player.Inventory.Has(requiredKey))
            {
                player.Inventory.Remove(requiredKey);
                SubtitleUI.Say($"{KoreanText.WithInstrument(requiredKey.displayName)} 열었다.");
                Unlock();
                Open();
            }
            else
            {
                Sfx.Play3D(ProceduralSounds.DoorRattle(), transform.position, 0.8f);
                SubtitleUI.Say(lockedMessage);
            }
            return;
        }

        if (isOpen) Close();
        else Open();
    }

    public void Unlock()
    {
        if (!locked) return;
        locked = false;
        Sfx.Play3D(ProceduralSounds.UnlockClick(), transform.position, 0.8f);
        onUnlocked.Invoke();
    }

    public void Lock() => locked = true;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        speed = 1f / openDuration;
        PlayMoveSound(0.6f);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        speed = 1f / openDuration;
        PlayMoveSound(0.45f);
    }

    // 인트로 연출: 문이 세게 닫히고 잠긴다
    public void SlamShut()
    {
        isOpen = false;
        locked = true;
        speed = 1f / 0.25f;
        Sfx.Play3D(ProceduralSounds.DoorSlam(), transform.position, 1f, 3f, 40f);
    }

    void PlayMoveSound(float volume)
    {
        var clip = sliding ? ProceduralSounds.SlideDoor() : ProceduralSounds.DoorCreak();
        Sfx.Play3D(clip, transform.position, volume);
    }

    void Update()
    {
        float target = isOpen ? 1f : 0f;
        if (Mathf.Approximately(progress, target)) return;
        progress = Mathf.MoveTowards(progress, target, speed * Time.deltaTime);
        ApplyPose();
    }

    void ApplyPose()
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        if (sliding) transform.localPosition = closedPosition + slideOffset * eased;
        else transform.localRotation = closedRotation * Quaternion.AngleAxis(eased * openAngle, axis);
    }
}
