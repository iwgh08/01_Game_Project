using UnityEngine;

// 복도 끝에 서 있다가, 플레이어가 잠깐 바라보거나 가까이 오면 조명이 지직거리며 사라지는 실루엣
public class Apparition : MonoBehaviour
{
    [SerializeField] float vanishDistance = 9f;
    [SerializeField] float watchTime = 1.5f;
    [SerializeField] float viewAngle = 22f;
    [SerializeField] FlickerLight[] lightsToGlitch;

    float watched;

    void OnEnable() => watched = 0f;

    void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 eye = cam.transform.position;
        Vector3 target = transform.position + Vector3.up * 1.2f;
        Vector3 toTarget = target - eye;
        if (toTarget.magnitude < vanishDistance)
        {
            Vanish();
            return;
        }

        bool inView = Vector3.Angle(cam.transform.forward, toTarget) < viewAngle
                      && !Physics.Linecast(eye, target, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (inView) watched += Time.deltaTime;
        if (watched >= watchTime) Vanish();
    }

    void Vanish()
    {
        foreach (var flicker in lightsToGlitch)
            if (flicker != null) flicker.Glitch(0.8f);
        Sfx.Play3D(ProceduralSounds.LightPop(), transform.position + Vector3.up * 2.5f, 0.8f);
        gameObject.SetActive(false);
    }
}
