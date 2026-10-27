using UnityEngine;
using UnityEngine.Events;

// 번호 자물쇠. 상호작용하면 자물쇠 화면이 열리고, 번호가 맞으면 onSolved가 실행된다.
public class CombinationLock : MonoBehaviour, IInteractable
{
    [SerializeField] string code = "0302";
    [SerializeField] UnityEvent onSolved = new();

    public bool Solved { get; private set; }
    public int Length => code.Length;

    public string Prompt => Solved ? null : "번호 자물쇠 돌리기";

    public void Interact(PlayerInteractor player) => CombinationLockUI.Instance.Open(this);

    public bool TryCode(string entered)
    {
        if (Solved || entered != code) return false;
        Solved = true;
        Sfx.Play3D(ProceduralSounds.UnlockClick(), transform.position, 0.9f);
        SubtitleUI.Say("딸깍. 자물쇠가 열렸다.");
        onSolved.Invoke();
        return true;
    }
}
