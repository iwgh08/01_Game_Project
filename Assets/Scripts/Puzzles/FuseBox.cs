using UnityEngine;
using UnityEngine.Events;

// 배전함. 퓨즈를 가지고 있으면 끼워서 전원을 살린다.
public class FuseBox : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData fuse;
    [SerializeField] GameObject fuseVisual;
    [SerializeField] UnityEvent onPowered = new();

    bool powered;

    public string Prompt => powered ? null : "배전함 살펴보기";

    public void Interact(PlayerInteractor player)
    {
        if (!player.Inventory.Has(fuse))
        {
            SubtitleUI.Say("퓨즈 한 칸이 비어 있다. 그래서 불이 안 들어오는 건가.");
            return;
        }

        player.Inventory.Remove(fuse);
        powered = true;
        if (fuseVisual != null) fuseVisual.SetActive(true);
        Sfx.Play3D(ProceduralSounds.FuseClunk(), transform.position, 0.9f);
        SubtitleUI.Say("퓨즈를 끼웠다.");
        onPowered.Invoke();
    }
}
