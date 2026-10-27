using System;
using UnityEngine;

// 읽을 수 있는 문서의 내용. 본문에서 "---"만 있는 줄은 페이지 구분이다.
[CreateAssetMenu(menuName = "방과후 방송/문서", fileName = "Document")]
public class DocumentData : ScriptableObject
{
    public string title;
    [TextArea(8, 30)] public string body;
    public bool handwritten;
    public Color paperColor = new(0.92f, 0.89f, 0.8f);

    public string[] GetPages()
    {
        string normalized = (body ?? "").Replace("\r\n", "\n");
        string[] pages = normalized.Split(new[] { "\n---\n" }, StringSplitOptions.None);
        for (int i = 0; i < pages.Length; i++) pages[i] = pages[i].Trim('\n');
        return pages;
    }
}
