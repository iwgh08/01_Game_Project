using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 화면 가운데에서 레이를 쏴서 상호작용할 대상을 찾고, E를 누르면 실행한다.
[RequireComponent(typeof(Inventory))]
public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera viewCamera;
    [SerializeField] float range = 2.3f;
    // 기본값은 Ignore Raycast 레이어(플레이어 자신)를 뺀 전부
    [SerializeField] LayerMask mask = Physics.DefaultRaycastLayers;

    readonly List<IInteractable> candidates = new();
    InputAction interactAction;

    public Inventory Inventory { get; private set; }

    void Awake()
    {
        Inventory = GetComponent<Inventory>();
        interactAction = InputSystem.actions.FindAction("Player/Interact", true);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        bool active = gm == null || gm.IsPlaying;
        IInteractable target = active ? FindTarget() : null;

        if (HUD.Instance != null) HUD.Instance.SetPrompt(target?.Prompt);

        // Interact 액션에는 Hold 인터랙션이 걸려 있지만 WasPressedThisFrame은 누른 순간 바로 반응한다
        bool justChangedState = gm != null && gm.StateChangedFrame == Time.frameCount;
        if (target != null && !justChangedState && interactAction.WasPressedThisFrame())
            target.Interact(this);
    }

    IInteractable FindTarget()
    {
        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore)) return null;

        // 한 오브젝트에 여러 상호작용이 겹치면(예: 자물쇠가 달린 사물함 문) 지금 가능한 첫 번째를 고른다
        hit.collider.GetComponentsInParent(false, candidates);
        foreach (var candidate in candidates)
        {
            if (candidate is Behaviour behaviour && behaviour.isActiveAndEnabled && !string.IsNullOrEmpty(candidate.Prompt))
                return candidate;
        }
        return null;
    }
}
