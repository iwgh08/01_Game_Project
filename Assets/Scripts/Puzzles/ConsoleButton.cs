using UnityEngine;

// 방송 콘솔의 버튼 (테이프 재생 / 송출)
public class ConsoleButton : MonoBehaviour, IInteractable
{
    public enum ButtonAction { PlayTape, Broadcast }

    [SerializeField] BroadcastConsole console;
    [SerializeField] ButtonAction action;
    [SerializeField] string prompt = "버튼 누르기";
    [SerializeField] Transform pressPart;

    Vector3 restPosition;
    float pressTimer;

    public string Prompt => prompt;

    void Awake()
    {
        if (pressPart != null) restPosition = pressPart.localPosition;
    }

    public void Interact(PlayerInteractor player)
    {
        pressTimer = 0.15f;
        Sfx.Play3D(ProceduralSounds.ButtonPress(), transform.position, 0.6f);
        if (action == ButtonAction.PlayTape) console.PlayTape(player);
        else console.Broadcast();
    }

    void Update()
    {
        if (pressTimer <= 0f || pressPart == null) return;
        pressTimer -= Time.deltaTime;
        pressPart.localPosition = restPosition + (pressTimer > 0f ? Vector3.down * 0.008f : Vector3.zero);
    }
}
