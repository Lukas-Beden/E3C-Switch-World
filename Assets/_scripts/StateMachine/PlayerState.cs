using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum PlayerStateEnum
    {
        IDLE,
        WALK,
        TALKING,
        JUMPING
    }

    [SerializeField] private PlayerStateEnum _currentPlayerState = PlayerStateEnum.IDLE;

    private static readonly Dictionary<PlayerStateEnum, HashSet<PlayerStateEnum>> _allowedTransitions = new()
    {
        { PlayerStateEnum.IDLE, new() { PlayerStateEnum.WALK, PlayerStateEnum.TALKING, PlayerStateEnum.JUMPING } },
        { PlayerStateEnum.WALK, new() { PlayerStateEnum.IDLE, PlayerStateEnum.TALKING, PlayerStateEnum.JUMPING } },
        { PlayerStateEnum.TALKING, new() { PlayerStateEnum.IDLE } },
        { PlayerStateEnum.JUMPING, new() { PlayerStateEnum.IDLE, PlayerStateEnum.WALK } }
    };

    public void SetState(PlayerStateEnum newState)
    {
        if (IsAllowed(newState))
        {
            _currentPlayerState = newState;
        }
    }

    public bool IsJumping()
    {
        return _currentPlayerState == PlayerStateEnum.JUMPING;
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
