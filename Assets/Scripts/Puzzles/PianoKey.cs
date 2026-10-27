using UnityEngine;

// 피아노 건반 하나
public class PianoKey : MonoBehaviour, IInteractable
{
    [SerializeField] PianoPuzzle puzzle;
    [SerializeField, Range(0, 6)] int note;

    Vector3 restPosition;
    float pressTimer;

    public string Prompt => $"건반 누르기 ({PianoPuzzle.NoteNames[note]})";

    void Awake() => restPosition = transform.localPosition;

    public void Interact(PlayerInteractor player)
    {
        pressTimer = 0.18f;
        puzzle.Press(note, transform.position);
    }

    void Update()
    {
        if (pressTimer <= 0f) return;
        pressTimer -= Time.deltaTime;
        transform.localPosition = restPosition + (pressTimer > 0f ? Vector3.down * 0.012f : Vector3.zero);
    }
}
