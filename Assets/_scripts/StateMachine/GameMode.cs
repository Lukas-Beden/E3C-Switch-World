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

    private GMode _currentGameMode = GMode._3D;
    public void SetGameMode(GMode newState)
    {
        _currentGameMode = newState;
    }

    public GMode GetPlayerState()
    {
        return _currentGameMode;
    }
}
