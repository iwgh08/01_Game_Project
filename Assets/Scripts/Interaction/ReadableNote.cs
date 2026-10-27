using UnityEngine;
using UnityEngine.Events;

// 읽을 수 있는 문서(쪽지, 일기장, 포스터 등). 읽은 문서는 소지품 화면에서 다시 볼 수 있다.
public class ReadableNote : MonoBehaviour, IInteractable
{
    [SerializeField] DocumentData document;
    [SerializeField] string prompt = "읽기";
    [SerializeField] bool takeAfterReading;
    [SerializeField, TextArea] string thoughtAfterReading;
    [SerializeField] UnityEvent onFirstRead = new();

    bool hasRead;

    public string Prompt => document != null ? prompt : null;

    public void Interact(PlayerInteractor player)
    {
        player.Inventory.AddDocument(document);
        DocumentViewer.Instance.Show(document, OnClosed);
    }

    void OnClosed()
    {
        if (!hasRead)
        {
            hasRead = true;
            if (!string.IsNullOrEmpty(thoughtAfterReading)) SubtitleUI.Say(thoughtAfterReading);
            onFirstRead.Invoke();
        }
        if (takeAfterReading) gameObject.SetActive(false);
    }
}
