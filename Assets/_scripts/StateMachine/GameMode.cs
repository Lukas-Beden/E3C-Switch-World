using System.Linq;
using UnityEngine;
using static PlayerState;

public class GameMode : MonoBehaviour
{
    public enum GMode
    {
        _3D,
        _2D,
        MENU
    }

    [SerializeField] private GMode _currentGameMode = GMode._2D;

    private GMode _gameModeBeforePause;
    public void SetGameMode(GMode newState)
    {
        _currentGameMode = newState;
    }

    public GMode GetGameMode()
    {
        return _currentGameMode;
    }

    public GMode GetGameModeBeforePause()
    {
        return _gameModeBeforePause;
    }

    public bool Is2DMode()
    {
        return _currentGameMode == GMode._2D;
    }

    public bool Is3DMode()
    {
        return _currentGameMode == GMode._3D;
    }

    public bool IsMenuMode()
    {
        if (_currentGameMode != GMode.MENU)
            _gameModeBeforePause = _currentGameMode;

        return _currentGameMode == GMode.MENU;
    }

    public void SwitchMode()
    {
        if (_currentGameMode == GMode._3D) _currentGameMode = GMode._2D;
        else if (_currentGameMode == GMode._2D) _currentGameMode = GMode._3D;
    }
}
