using UnityEngine;

// 이 위치에서 연출용 효과음을 낸다. 트리거 이벤트에서 Play()를 연결해 쓴다.
public class SoundEmitter : MonoBehaviour
{
    [SerializeField] SoundKind sound;
    [SerializeField] float volume = 1f;
    [SerializeField] float maxDistance = 30f;

    public void Play() => Sfx.Play3D(ProceduralSounds.Get(sound), transform.position, volume, 2f, maxDistance);
}
