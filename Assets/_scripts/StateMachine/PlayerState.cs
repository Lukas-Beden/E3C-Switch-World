using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum PlayerStateEnum
    {
        IDLE,
        WALK,
        TALKING
    }

    [SerializeField] private PlayerStateEnum _currentPlayerState = PlayerStateEnum.IDLE;

    private static readonly Dictionary<PlayerStateEnum, HashSet<PlayerStateEnum>> _allowedTransitions = new()
    {
        { PlayerStateEnum.IDLE, new() { PlayerStateEnum.WALK, PlayerStateEnum.TALKING } },
        { PlayerStateEnum.WALK, new() { PlayerStateEnum.IDLE, PlayerStateEnum.TALKING } },
        { PlayerStateEnum.TALKING, new() { PlayerStateEnum.IDLE } },
    };

    public void SetState(PlayerStateEnum newState)
    {
        if (IsAllowed(newState))
        {
            _currentPlayerState = newState;
        }
    }

    public PlayerStateEnum GetPlayerState()
    {
        return _currentPlayerState;
    }

    private bool IsAllowed(PlayerStateEnum newState)
    {
        return _allowedTransitions[_currentPlayerState].Contains(newState);
    }
}
