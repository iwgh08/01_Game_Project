using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 게임 상태(조작/화면/일시정지/연출)와 커서 잠금, Esc 처리를 맡는다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] PauseMenu pauseMenu;

    IClosableUI currentUI;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    // 상태가 바뀐 프레임에는 같은 키 입력을 한 번 더 처리하지 않도록 기록해 둔다
    public int StateChangedFrame { get; private set; } = -1;
    public event Action<GameState> StateChanged;

    void Awake()
    {
        Instance = this;
        ProceduralSounds.Warmup();
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    void Start() => ApplyState();

    void OnApplicationFocus(bool focused)
    {
        if (focused) ApplyState();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || StateChangedFrame == Time.frameCount) return;

        if (currentUI != null) currentUI.Close();
        else if (State == GameState.Playing && pauseMenu != null) pauseMenu.Open();
    }

    public void OpenUI(IClosableUI ui, bool pauseGame = false)
    {
        if (currentUI != null && currentUI != ui) currentUI.Close();
        currentUI = ui;
        SetState(pauseGame ? GameState.Paused : GameState.UI);
    }

    public void CloseUI(IClosableUI ui)
    {
        if (currentUI != ui) return;
        currentUI = null;
        if (State != GameState.Cutscene) SetState(GameState.Playing);
    }

    public void SetState(GameState state)
    {
        State = state;
        StateChangedFrame = Time.frameCount;
        ApplyState();
        StateChanged?.Invoke(state);
    }

    void ApplyState()
    {
        bool paused = State == GameState.Paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;

        bool cursorFree = State == GameState.UI || State == GameState.Paused;
        Cursor.lockState = cursorFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = cursorFree;
    }
}
