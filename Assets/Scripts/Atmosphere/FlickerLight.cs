using UnityEngine;

// 형광등. 켜짐/깜빡임/꺼짐 상태가 있고, 연출에서 끄거나 켜거나 잠깐 지직거리게 할 수 있다.
public class FlickerLight : MonoBehaviour
{
    public enum Mode { On, Flicker, Off }

    [SerializeField] Light lightSource;
    [SerializeField] Renderer tube;
    [SerializeField] Color emissionColor = new(0.85f, 0.95f, 1f);
    [SerializeField] float emissionIntensity = 2.5f;
    [SerializeField] Mode mode = Mode.On;
    [SerializeField] AudioSource buzz;

    MaterialPropertyBlock block;
    float baseIntensity, baseBuzzVolume, timer, glitchTimer;
    bool lit = true;

    void Awake()
    {
        if (lightSource != null) baseIntensity = lightSource.intensity;
        if (buzz != null)
        {
            baseBuzzVolume = buzz.volume;
            buzz.clip = ProceduralSounds.FluorescentBuzz();
            buzz.loop = true;
            if (mode != Mode.Off) buzz.Play();
        }
        Apply(mode == Mode.Off ? 0f : 1f);
    }

    void Update()
    {
        if (glitchTimer > 0f)
        {
            glitchTimer -= Time.deltaTime;
            if (glitchTimer > 0f) Apply(Random.value < 0.5f ? 0f : Random.Range(0.5f, 1.2f));
            else Apply(mode == Mode.Off ? 0f : 1f);
            return;
        }
        if (mode != Mode.Flicker) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        lit = !lit;
        timer = lit ? Random.Range(0.08f, 2.5f) : Random.Range(0.03f, 0.25f);
        Apply(lit ? Random.Range(0.7f, 1f) : Random.Range(0f, 0.15f));
    }

    public void TurnOff()
    {
        mode = Mode.Off;
        glitchTimer = 0f;
        Apply(0f);
        if (buzz != null) buzz.Stop();
        Sfx.Play3D(ProceduralSounds.LightPop(), transform.position, 0.6f);
    }

    // 형광등처럼 몇 번 깜빡이다가 켜진다
    public void TurnOn()
    {
        mode = Mode.On;
        glitchTimer = 0.6f;
        if (buzz != null && !buzz.isPlaying) buzz.Play();
    }

    public void Glitch(float duration) => glitchTimer = duration;

    void Apply(float level)
    {
        if (lightSource != null)
        {
            lightSource.intensity = baseIntensity * level;
            lightSource.enabled = level > 0.01f;
        }
        if (tube != null)
        {
            block ??= new MaterialPropertyBlock();
            tube.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", emissionColor * (emissionIntensity * level));
            tube.SetPropertyBlock(block);
        }
        if (buzz != null) buzz.volume = baseBuzzVolume * Mathf.Clamp01(level);
    }
}
