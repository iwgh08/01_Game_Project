using UnityEngine;
using UnityEngine.Events;

// 플레이어가 들어오면 이벤트를 실행하는 영역. armed가 꺼져 있으면 Arm()이 불릴 때까지 반응하지 않는다.
[RequireComponent(typeof(BoxCollider))]
public class TriggerZone : MonoBehaviour
{
    [SerializeField] bool armed = true;
    [SerializeField] bool once = true;
    [SerializeField] UnityEvent onEnter = new();

    bool fired;

    public void Arm() => armed = true;

    void OnTriggerEnter(Collider other)
    {
        if (!armed || (once && fired) || !other.CompareTag("Player")) return;
        fired = true;
        onEnter.Invoke();
    }
}
