using UnityEngine;
using UnityEngine.InputSystem;

// 손전등: 카메라를 살짝 늦게 따라가고, 가끔 혼자 깜빡인다. F로 켜고 끈다.
public class Flashlight : MonoBehaviour
{
    [SerializeField] Light spot;
    [SerializeField] Transform followTarget;
    [SerializeField] Vector3 offset = new(0.18f, -0.15f, 0.1f);
    [SerializeField] float followSharpness = 12f;
    [SerializeField] bool owned;
    [SerializeField] Vector2 flickerInterval = new(35f, 80f);

    float baseIntensity, flickerTimer, flickerLeft;
    bool isOn;

    void Awake()
    {
        baseIntensity = spot.intensity;
        flickerTimer = Random.Range(flickerInterval.x, flickerInterval.y);
        SetOn(owned, playSound: false);
    }

    // 손전등을 주웠을 때 (PickupItem의 이벤트에서 호출)
    public void Give()
    {
        owned = true;
        SetOn(true);
        if (HUD.Instance != null) HUD.Instance.ShowHint("[F] 손전등 켜기 / 끄기");
    }

    void Update()
    {
        if (!owned) return;
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        var keyboard = Keyboard.current;
        if (playing && keyboard != null && keyboard.fKey.wasPressedThisFrame) SetOn(!isOn);
        if (isOn) UpdateFlicker();
    }

    void UpdateFlicker()
    {
        if (flickerLeft > 0f)
        {
            flickerLeft -= Time.deltaTime;
            spot.intensity = flickerLeft > 0f && Random.value < 0.5f ? baseIntensity * Random.Range(0f, 0.3f) : baseIntensity;
            return;
        }
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            flickerTimer = Random.Range(flickerInterval.x, flickerInterval.y);
            flickerLeft = Random.Range(0.2f, 0.6f);
        }
    }

    void LateUpdate()
    {
        transform.position = followTarget.TransformPoint(offset);
        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, followTarget.rotation, t);
    }

    void SetOn(bool on, bool playSound = true)
    {
        isOn = on;
        spot.enabled = on;
        spot.intensity = baseIntensity;
        if (playSound) Sfx.Play2D(ProceduralSounds.SwitchClick(), 0.5f);
    }
}
