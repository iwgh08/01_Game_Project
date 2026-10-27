using UnityEngine;

// 바람·저음 배경음을 깔고, 가끔 플레이어 주변 어딘가에서 작은 소리(노크, 삐걱임, 물방울)를 낸다.
[RequireComponent(typeof(AudioSource))]
public class AmbienceController : MonoBehaviour
{
    [SerializeField] float volume = 0.45f;
    [SerializeField] Vector2 oneShotInterval = new(18f, 40f);
    [SerializeField] float oneShotVolume = 0.35f;

    static readonly SoundKind[] OneShots = { SoundKind.Knock, SoundKind.WoodCreak, SoundKind.Drip, SoundKind.WoodCreak, SoundKind.Drip };

    float timer;

    void Start()
    {
        var source = GetComponent<AudioSource>();
        source.clip = ProceduralSounds.Ambience();
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.Play();
        timer = Random.Range(oneShotInterval.x, oneShotInterval.y);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = Random.Range(oneShotInterval.x, oneShotInterval.y);
        var listener = Camera.main;
        if (listener == null) return;

        Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(6f, 12f);
        Vector3 position = listener.transform.position + new Vector3(offset.x, Random.Range(-0.5f, 1f), offset.y);
        var kind = OneShots[Random.Range(0, OneShots.Length)];
        Sfx.Play3D(ProceduralSounds.Get(kind), position, oneShotVolume, 2f, 25f);
    }
}
