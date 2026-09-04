namespace DanlangA_Bot.Core.Contracts;

public interface IFsmEngine
{
    string CurrentStateName { get; }
    int CurrentFps { get; }
    int CurrentFrameIndex { get; }
    int CurrentAtlasRow { get; }
    void Initialize(string statesJsonPath);
    void Update(double deltaSeconds);
    void TriggerState(string stateName, bool force = false);

    /// <summary>Fired when frame changes: (stateName, frameIndex, atlasRow, fps)</summary>
    event Action<string, int, int, int>? OnFrameChanged;
}
