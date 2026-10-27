using UnityEngine;

// 살펴보면 주인공의 혼잣말 자막이 나오는 대상 (창문, 시계 등)
public class Examinable : MonoBehaviour, IInteractable
{
    [SerializeField] string prompt = "살펴보기";
    [SerializeField, TextArea] string[] lines;

    public string Prompt => lines != null && lines.Length > 0 ? prompt : null;

    public void Interact(PlayerInteractor player)
    {
        if (SubtitleUI.Instance != null) SubtitleUI.Instance.SayLines(lines);
    }
}
