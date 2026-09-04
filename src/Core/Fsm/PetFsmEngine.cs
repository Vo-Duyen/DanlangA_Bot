using System.Text.Json;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Core.Fsm;

public sealed class PetFsmEngine : IFsmEngine
{
    private FsmConfig _config = new();
    private StateDefinition? _currentState;
    private double _stateTimer;
    private double _frameTimer;
    private int _currentFrameIndex;
    private readonly Random _random = new();

    public string CurrentStateName { get; private set; } = "idle";
    public int CurrentFps => _currentState?.Fps ?? 0;
    public int CurrentFrameIndex => _currentFrameIndex;
    public int CurrentAtlasRow => _currentState?.AtlasRow ?? 0;

    public event Action<string, int, int, int>? OnFrameChanged;

    public void Initialize(string statesJsonPath)
    {
        if (File.Exists(statesJsonPath))
        {
            try
            {
                var json = File.ReadAllText(statesJsonPath);
                var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.FsmConfig);
                if (loaded != null && loaded.States.Count > 0)
                {
                    _config = loaded;
                }
            }
            catch
            {
                // ponytail: [fallback defaults] -> [robust logging]
            }
        }

        if (_config.States.Count == 0)
        {
            _config.States["idle"] = new StateDefinition { Fps = 4, FrameCount = 6, AtlasRow = 0, Loop = true };
        }

        SwitchToState(_config.InitialState.Length > 0 && _config.States.ContainsKey(_config.InitialState) ? _config.InitialState : "idle");
    }

    public void Update(double deltaSeconds)
    {
        if (_currentState == null) return;

        if (_currentState.Fps > 0 && _currentState.FrameCount > 1)
        {
            _frameTimer += deltaSeconds;
            double frameDuration = 1.0 / _currentState.Fps;
            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                int nextFrame = _currentFrameIndex + 1;
                if (nextFrame >= _currentState.FrameCount)
                {
                    if (_currentState.Loop)
                    {
                        _currentFrameIndex = 0;
                    }
                    else
                    {
                        TransitionToNextState();
                        return;
                    }
                }
                else
                {
                    _currentFrameIndex = nextFrame;
                }
                FireFrameChanged();
            }
        }

        if (_stateTimer > 0)
        {
            _stateTimer -= deltaSeconds;
            if (_stateTimer <= 0)
            {
                TransitionToNextState();
            }
        }
    }

    public void TriggerState(string stateName, bool force = false)
    {
        if (!_config.States.ContainsKey(stateName)) return;
        if (!force && CurrentStateName == "dragging") return;
        SwitchToState(stateName);
    }

    private void SwitchToState(string stateName)
    {
        if (!_config.States.TryGetValue(stateName, out var stateDef)) return;

        CurrentStateName = stateName;
        _currentState = stateDef;
        _currentFrameIndex = 0;
        _frameTimer = 0;

        if (stateDef.MinDurationMs > 0 && stateDef.MaxDurationMs >= stateDef.MinDurationMs)
        {
            int duration = _random.Next(stateDef.MinDurationMs, stateDef.MaxDurationMs + 1);
            _stateTimer = duration / 1000.0;
        }
        else
        {
            _stateTimer = 0;
        }

        FireFrameChanged();
    }

    private void FireFrameChanged()
    {
        OnFrameChanged?.Invoke(CurrentStateName, _currentFrameIndex, CurrentAtlasRow, CurrentFps);
    }

    private void TransitionToNextState()
    {
        if (_currentState == null || _currentState.Transitions.Count == 0)
        {
            SwitchToState("idle");
            return;
        }

        int totalWeight = 0;
        foreach (var t in _currentState.Transitions)
            totalWeight += t.Weight;

        if (totalWeight <= 0) { SwitchToState("idle"); return; }

        int pick = _random.Next(0, totalWeight);
        int accumulated = 0;
        foreach (var t in _currentState.Transitions)
        {
            accumulated += t.Weight;
            if (pick < accumulated)
            {
                SwitchToState(t.Target);
                return;
            }
        }

        SwitchToState("idle");
    }
}
