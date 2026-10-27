using System;
using System.Collections.Generic;
using UnityEngine;

// 가진 물건과 읽은 문서 목록
public class Inventory : MonoBehaviour
{
    readonly List<ItemData> items = new();
    readonly List<DocumentData> documents = new();

    public IReadOnlyList<ItemData> Items => items;
    public IReadOnlyList<DocumentData> Documents => documents;
    public event Action Changed;

    public bool Has(ItemData item) => items.Contains(item);

    public void Add(ItemData item)
    {
        if (item == null || items.Contains(item)) return;
        items.Add(item);
        Changed?.Invoke();
    }

    public bool Remove(ItemData item)
    {
        if (!items.Remove(item)) return false;
        Changed?.Invoke();
        return true;
    }

    public void AddDocument(DocumentData document)
    {
        if (document == null || documents.Contains(document)) return;
        documents.Add(document);
        Changed?.Invoke();
    }
}
