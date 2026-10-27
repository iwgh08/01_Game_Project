using UnityEngine;

// 한 번 울리고 사라지는 효과음 재생
public static class Sfx
{
    public static AudioSource Play3D(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1.5f, float maxDistance = 25f)
    {
        if (clip == null) return null;
        var source = CreateSource(clip, volume);
        source.transform.position = position;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.dopplerLevel = 0f;
        source.Play();
        return source;
    }

    public static AudioSource Play2D(AudioClip clip, float volume = 1f, bool ignorePause = false)
    {
        if (clip == null) return null;
        var source = CreateSource(clip, volume);
        source.spatialBlend = 0f;
        source.ignoreListenerPause = ignorePause;
        source.Play();
        return source;
    }

    static AudioSource CreateSource(AudioClip clip, float volume)
    {
        var go = new GameObject("Sfx_" + clip.name);
        var source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.playOnAwake = false;
        Object.Destroy(go, clip.length + 0.2f);
        return source;
    }
}
