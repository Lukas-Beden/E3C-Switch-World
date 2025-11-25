using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum PlayerStateEnum
    {
        IDLE,
        WALK,
        TALKING,
        JUMPING,
        MOVINGOBJECT
    }

    [SerializeField] private PlayerStateEnum _currentPlayerState = PlayerStateEnum.IDLE;

    private static readonly Dictionary<PlayerStateEnum, HashSet<PlayerStateEnum>> _allowedTransitions = new()
    {
        { PlayerStateEnum.IDLE, new() { PlayerStateEnum.WALK, PlayerStateEnum.TALKING, PlayerStateEnum.JUMPING, PlayerStateEnum.MOVINGOBJECT } },
        { PlayerStateEnum.WALK, new() { PlayerStateEnum.IDLE, PlayerStateEnum.TALKING, PlayerStateEnum.JUMPING, PlayerStateEnum.MOVINGOBJECT } },
        { PlayerStateEnum.TALKING, new() { PlayerStateEnum.IDLE } },
        { PlayerStateEnum.JUMPING, new() { PlayerStateEnum.IDLE, PlayerStateEnum.WALK } },
        { PlayerStateEnum.MOVINGOBJECT, new() { PlayerStateEnum.IDLE } }
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

    public bool IsMovingObject()
    {
        return _currentPlayerState == PlayerStateEnum.MOVINGOBJECT;
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
