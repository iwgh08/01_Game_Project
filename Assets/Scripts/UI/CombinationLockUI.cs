using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 번호 자물쇠 화면. ▲▼로 숫자를 돌리고, 번호가 맞는 순간 열린다.
public class CombinationLockUI : MonoBehaviour, IClosableUI
{
    public static CombinationLockUI Instance { get; private set; }

    [SerializeField] GameObject root;
    [SerializeField] TMP_Text[] digitTexts;
    [SerializeField] Button[] upButtons;
    [SerializeField] Button[] downButtons;

    CombinationLock target;
    int[] digits;
    bool solving;

    void Awake()
    {
        Instance = this;
        root.SetActive(false);
        digits = new int[digitTexts.Length];
        for (int i = 0; i < digitTexts.Length; i++)
        {
            int index = i;
            upButtons[i].onClick.AddListener(() => Turn(index, 1));
            downButtons[i].onClick.AddListener(() => Turn(index, -1));
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Open(CombinationLock lockTarget)
    {
        target = lockTarget;
        solving = false;
        Refresh();
        root.SetActive(true);
        GameManager.Instance.OpenUI(this);
    }

    public void Close()
    {
        if (!root.activeSelf) return;
        root.SetActive(false);
        GameManager.Instance.CloseUI(this);
    }

    void Turn(int index, int delta)
    {
        if (solving) return;
        digits[index] = (digits[index] + delta + 10) % 10;
        Sfx.Play2D(ProceduralSounds.SwitchClick(), 0.4f);
        Refresh();
        if (target.TryCode(string.Join("", digits))) StartCoroutine(CloseSoon());
    }

    IEnumerator CloseSoon()
    {
        solving = true;
        yield return new WaitForSeconds(0.7f);
        Close();
    }

    void Refresh()
    {
        for (int i = 0; i < digitTexts.Length; i++) digitTexts[i].text = digits[i].ToString();
    }
}
