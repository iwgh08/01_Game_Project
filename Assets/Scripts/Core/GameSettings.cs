using UnityEngine;

// 플레이어 설정값 (PlayerPrefs에 저장)
public static class GameSettings
{
    const string SensitivityKey = "MouseSensitivity";
    public const float MinSensitivity = 0.2f;
    public const float MaxSensitivity = 3f;

    static float? sensitivity;

    public static float MouseSensitivity
    {
        get => sensitivity ??= PlayerPrefs.GetFloat(SensitivityKey, 1f);
        set
        {
            sensitivity = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(SensitivityKey, sensitivity.Value);
        }
    }
}
