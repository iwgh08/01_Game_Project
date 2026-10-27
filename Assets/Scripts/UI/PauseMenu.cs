using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Esc 일시정지 메뉴: 계속하기, 마우스 감도, 메인 메뉴로
public class PauseMenu : MonoBehaviour, IClosableUI
{
    [SerializeField] GameObject root;
    [SerializeField] Button resumeButton;
    [SerializeField] Button menuButton;
    [SerializeField] Slider sensitivitySlider;
    [SerializeField] TMP_Text sensitivityLabel;

    void Awake()
    {
        root.SetActive(false);
        resumeButton.onClick.AddListener(Close);
        menuButton.onClick.AddListener(GoToMainMenu);
        sensitivitySlider.minValue = GameSettings.MinSensitivity;
        sensitivitySlider.maxValue = GameSettings.MaxSensitivity;
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
    }

    public void Open()
    {
        sensitivitySlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        UpdateLabel();
        root.SetActive(true);
        GameManager.Instance.OpenUI(this, pauseGame: true);
    }

    public void Close()
    {
        if (!root.activeSelf) return;
        root.SetActive(false);
        GameManager.Instance.CloseUI(this);
    }

    void OnSensitivityChanged(float value)
    {
        GameSettings.MouseSensitivity = value;
        UpdateLabel();
    }

    void UpdateLabel() => sensitivityLabel.text = $"마우스 감도  {GameSettings.MouseSensitivity:0.0}";

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
