namespace DanlangA_Bot.Core.Contracts;

public interface IPetWindow
{
    nint Handle { get; }
    void Initialize();
    void SetPositionPercent(double xPercent, double yPercent);
    void SetScale(double scale);
    double CurrentScale { get; }
    void SetClickThrough(bool enabled);
    bool IsClickThrough { get; }
    void ShowNotification(string text, string mood, int durationMs);
    void UpdateSurface(nint hdcSrc, int width, int height, byte alpha = 255);
    void RunMessageLoop();
    void Close();
}
