// 플레이어가 바라보고 E로 상호작용할 수 있는 대상
public interface IInteractable
{
    // 화면에 보일 문구. 비어 있으면 지금은 상호작용할 수 없다.
    string Prompt { get; }
    void Interact(PlayerInteractor player);
}
