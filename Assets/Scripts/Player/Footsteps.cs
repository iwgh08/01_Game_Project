using UnityEngine;

// 이동 거리에 맞춰 발소리를 낸다.
[RequireComponent(typeof(AudioSource))]
public class Footsteps : MonoBehaviour
{
    [SerializeField] FirstPersonController controller;
    [SerializeField] float strideLength = 0.75f;
    [SerializeField] float volume = 0.35f;

    AudioSource source;
    float distance;

    void Awake() => source = GetComponent<AudioSource>();

    void Update()
    {
        if (!controller.IsGrounded || controller.HorizontalSpeed < 0.1f) return;

        distance += controller.HorizontalSpeed * Time.deltaTime;
        if (distance < strideLength) return;

        distance = 0f;
        source.pitch = Random.Range(0.9f, 1.1f);
        float loudness = controller.IsCrouching ? 0.35f : 1f;
        source.PlayOneShot(ProceduralSounds.Footstep(Random.Range(0, 4)), volume * loudness);
    }
}
