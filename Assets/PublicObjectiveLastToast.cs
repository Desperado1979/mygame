using UnityEngine;

/// <summary>B2·Day2：公共精英击败时，全端同显一条短时 HUD 提示（不依赖纯本地状态）。</summary>
public static class PublicObjectiveLastToast
{
    public static string Message { get; private set; }
    static float _expireUnscaled = float.NegativeInfinity;

    public static bool IsActive =>
        !string.IsNullOrEmpty(Message) && Time.unscaledTime < _expireUnscaled;

    /// <param name="seconds">可见秒数；负数时使用 <see cref="D3GrowthBalanceData.publicObjectiveToastVisibleSec"/>。</param>
    public static void Set(string text, float seconds = -1f)
    {
        Message = text ?? string.Empty;
        if (seconds < 0f)
            seconds = Mathf.Max(0.5f, D3GrowthBalance.Load().publicObjectiveToastVisibleSec);
        _expireUnscaled = Time.unscaledTime + Mathf.Max(0.5f, seconds);
    }

    public static void Clear()
    {
        Message = null;
        _expireUnscaled = float.NegativeInfinity;
    }
}
