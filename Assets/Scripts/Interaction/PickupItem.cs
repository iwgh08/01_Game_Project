using UnityEngine;
using UnityEngine.Events;

// 주워서 소지품에 넣는 물건
public class PickupItem : MonoBehaviour, IInteractable
{
    [SerializeField] ItemData item;
    [SerializeField] UnityEvent onPickedUp = new();

    public string Prompt => item != null ? $"{item.displayName} 줍기" : null;

    public void Interact(PlayerInteractor player)
    {
        player.Inventory.Add(item);
        Sfx.Play2D(ProceduralSounds.Pickup(), 0.6f);
        SubtitleUI.Say($"{KoreanText.WithObject(item.displayName)} 챙겼다.");
        onPickedUp.Invoke();
        gameObject.SetActive(false);
    }
}
