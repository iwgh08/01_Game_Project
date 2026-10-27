using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// 건반을 정해진 순서대로 누르면 풀린다. 중간에 틀리면 불협화음이 나고 처음부터 다시.
public class PianoPuzzle : MonoBehaviour
{
    public static readonly string[] NoteNames = { "도", "레", "미", "파", "솔", "라", "시" };

    [SerializeField] int[] solution = { 2, 4, 5, 4, 2, 1 };   // 미 솔 라 솔 미 레
    [SerializeField] UnityEvent onSolved = new();

    int progress;
    bool solved;

    public void Press(int note, Vector3 position)
    {
        Sfx.Play3D(ProceduralSounds.PianoNote(note), position, 0.9f);
        if (solved) return;

        if (note == solution[progress])
        {
            progress++;
            if (progress == solution.Length)
            {
                solved = true;
                StartCoroutine(SolvedRoutine());
            }
            return;
        }

        if (progress > 0) StartCoroutine(WrongRoutine());
        progress = note == solution[0] ? 1 : 0;
    }

    // 음악실에 들어설 때 피아노가 혼자 울리는 연출
    public void PlayGhostNote() => Sfx.Play3D(ProceduralSounds.GhostNote(), transform.position, 1f, 2f, 30f);

    IEnumerator WrongRoutine()
    {
        yield return new WaitForSeconds(0.35f);
        Sfx.Play3D(ProceduralSounds.PianoDissonant(), transform.position, 0.7f);
    }

    IEnumerator SolvedRoutine()
    {
        yield return new WaitForSeconds(1.2f);
        SubtitleUI.Say("…서진이 노래다. 건반 아래에서 딸깍 소리가 났다.");
        onSolved.Invoke();
    }
}
