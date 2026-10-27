using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면
public class MainMenu : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] Button quitButton;
    [SerializeField] TMP_Text title;
    [SerializeField] CanvasGroup fader;
    [SerializeField] AudioSource ambience;

    bool starting;
    float flickerTimer = 3f, flickerLeft;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ambience.clip = ProceduralSounds.Ambience();
        ambience.loop = true;
        ambience.Play();

        startButton.onClick.AddListener(StartGame);
        quitButton.onClick.AddListener(Quit);
        StartCoroutine(Fade(1f, 0f, 2f));
    }

    void Update()
    {
        // 제목 글자가 가끔 형광등처럼 깜빡인다
        if (flickerLeft > 0f)
        {
            flickerLeft -= Time.deltaTime;
            title.alpha = flickerLeft > 0f && Random.value < 0.5f ? Random.Range(0.1f, 0.5f) : 1f;
            return;
        }
        flickerTimer -= Time.deltaTime;
        if (flickerTimer > 0f) return;
        flickerTimer = Random.Range(4f, 9f);
        flickerLeft = Random.Range(0.15f, 0.45f);
    }

    void StartGame()
    {
        if (starting) return;
        starting = true;
        StartCoroutine(StartRoutine());
    }

    IEnumerator StartRoutine()
    {
        Sfx.Play2D(ProceduralSounds.DoorCreak(), 0.6f);
        float startVolume = ambience.volume;
        for (float t = 0f; t < 1.8f; t += Time.deltaTime)
        {
            fader.alpha = t / 1.8f;
            ambience.volume = Mathf.Lerp(startVolume, 0f, t / 1.8f);
            yield return null;
        }
        fader.alpha = 1f;
        SceneManager.LoadScene(SceneNames.School);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            fader.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        fader.alpha = to;
        fader.blocksRaycasts = false;
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
