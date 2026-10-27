using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tab으로 여는 소지품 화면: 가진 물건과 읽은 기록(다시 읽기 가능)
public class InventoryScreen : MonoBehaviour, IClosableUI
{
    [SerializeField] GameObject root;
    [SerializeField] Inventory inventory;
    [SerializeField] RectTransform itemList;
    [SerializeField] TMP_Text itemTemplate;
    [SerializeField] RectTransform documentList;
    [SerializeField] Button documentTemplate;

    readonly List<GameObject> spawned = new();
    int openedFrame;

    void Awake()
    {
        root.SetActive(false);
        itemTemplate.gameObject.SetActive(false);
        documentTemplate.gameObject.SetActive(false);
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.tabKey.wasPressedThisFrame) return;

        var gm = GameManager.Instance;
        if (root.activeSelf)
        {
            if (Time.frameCount != openedFrame) Close();
        }
        else if (gm.IsPlaying && gm.StateChangedFrame != Time.frameCount) Open();
    }

    void Open()
    {
        Rebuild();
        root.SetActive(true);
        openedFrame = Time.frameCount;
        GameManager.Instance.OpenUI(this);
        Sfx.Play2D(ProceduralSounds.PaperRustle(), 0.4f);
    }

    public void Close()
    {
        if (!root.activeSelf) return;
        root.SetActive(false);
        GameManager.Instance.CloseUI(this);
    }

    void Rebuild()
    {
        foreach (var go in spawned) Destroy(go);
        spawned.Clear();

        if (inventory.Items.Count == 0) AddLine(itemList, "<color=#777777>(아무것도 없다)</color>");
        foreach (var item in inventory.Items)
            AddLine(itemList, $"<b>{item.displayName}</b>\n<size=78%><color=#A8A8A8>{item.description}</color></size>");

        if (inventory.Documents.Count == 0) AddLine(documentList, "<color=#777777>(아직 읽은 것이 없다)</color>");
        foreach (var document in inventory.Documents)
        {
            var button = Instantiate(documentTemplate, documentList);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = document.title;
            var target = document;
            button.onClick.AddListener(() => OpenDocument(target));
            spawned.Add(button.gameObject);
        }
    }

    void AddLine(RectTransform parent, string text)
    {
        var line = Instantiate(itemTemplate, parent);
        line.gameObject.SetActive(true);
        line.text = text;
        spawned.Add(line.gameObject);
    }

    void OpenDocument(DocumentData document)
    {
        Close();
        DocumentViewer.Instance.Show(document);
    }
}
