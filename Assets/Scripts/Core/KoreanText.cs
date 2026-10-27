// 받침에 따라 조사를 고른다. 예: "열쇠를", "손전등을", "열쇠로", "손전등으로"
public static class KoreanText
{
    public static string WithObject(string word) => word + (HasFinalConsonant(word, out _) ? "을" : "를");

    public static string WithInstrument(string word)
    {
        bool hasFinal = HasFinalConsonant(word, out bool isRieul);
        return word + (hasFinal && !isRieul ? "으로" : "로");
    }

    static bool HasFinalConsonant(string word, out bool isRieul)
    {
        isRieul = false;
        if (string.IsNullOrEmpty(word)) return false;
        char last = word[word.Length - 1];
        if (last < '가' || last > '힣') return false;
        int final = (last - '가') % 28;
        isRieul = final == 8;
        return final != 0;
    }
}
