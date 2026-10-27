using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 문서를 종이 위에 크게 보여 준다. 여러 장이면 E/클릭으로 넘긴다.
public class DocumentViewer : MonoBehaviour, IClosableUI
{
    public static DocumentViewer Instance { get; private set; }

    [SerializeField] GameObject root;
    [SerializeField] Image paper;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] TMP_Text pageText;
    [SerializeField] TMP_FontAsset printFont;
    [SerializeField] TMP_FontAsset handwrittenFont;
    [SerializeField] float printSize = 30f;
    [SerializeField] float handwrittenSize = 42f;
    [SerializeField] float printTitleSize = 40f;
    [SerializeField] float handwrittenTitleSize = 54f;

    string[] pages;
    int page, openedFrame;
    Action onClosed;

    void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(DocumentData document, Action closed = null)
    {
        pages = document.GetPages();
        onClosed = closed;
        openedFrame = Time.frameCount;

        var font = document.handwritten ? handwrittenFont : printFont;
        paper.color = document.paperColor;
        titleText.font = font;
        bodyText.font = font;
        titleText.text = document.title;
        titleText.fontSize = document.handwritten ? handwrittenTitleSize : printTitleSize;
        bodyText.fontSize = document.handwritten ? handwrittenSize : printSize;

        root.SetActive(true);
        GameManager.Instance.OpenUI(this);
        Sfx.Play2D(ProceduralSounds.PaperRustle(), 0.6f);
        ShowPage(0);
    }

    void ShowPage(int index)
    {
        page = index;
        bodyText.text = pages[index];
        if (pages.Length == 1)
        {
            pageText.text = "[E] 닫기";
            return;
        }
        string next = index < pages.Length - 1 ? "다음 장" : "닫기";
        string prev = index > 0 ? "   [A] 이전 장" : "";
        pageText.text = $"{index + 1} / {pages.Length}      [E] {next}{prev}";
    }

    void Update()
    {
        if (!root.activeSelf || Time.frameCount == openedFrame) return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        bool next = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame
                                          || keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
                    || (mouse != null && mouse.leftButton.wasPressedThisFrame);
        bool previous = keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame);

        if (next)
        {
            if (page < pages.Length - 1)
            {
                ShowPage(page + 1);
                Sfx.Play2D(ProceduralSounds.PaperRustle(), 0.4f);
            }
            else Close();
        }
        else if (previous && page > 0) ShowPage(page - 1);
    }

    public void Close()
    {
        if (!root.activeSelf) return;
        root.SetActive(false);
        GameManager.Instance.CloseUI(this);
        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}
