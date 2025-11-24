using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;



[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(GameMode))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private GameObject _player;

    [Header("======| Movement attributes |======")]
    [Header("")]
    [Range(1.0f, 20.0f)]
    [SerializeField] private float _spd;

    [Range(1.0f, 20.0f)]
    [SerializeField] private float _rotateSpd = 10.0f;

    [Header("======| Global Input Action Asset |======")]
    [Header("")]
    [SerializeField] private InputActionAsset _inputActions;
    

    [Header("======| Actions Ref |======")]
    [Header("")]
    [SerializeField] private InputActionReference _pushItemsActionReference;
    [SerializeField] private InputActionReference _moveActionReference;
    [SerializeField] private InputActionReference _switchModeActionReference;
    [SerializeField] private InputActionReference _jumpActionReference;


    private bool _isAlreadySpeaking = false;
    private bool _isTalking = false;

    private TalkCameraScript _talkCamScript;
    private PlayerState _playerState;
    private Rigidbody _rigidbody;

    private Vector3 _dir;
    private Vector2 _moveAmt;
    private Vector2 _lookAmt;

    private void OnEnable()
    {
        _inputActions.FindActionMap("Gameplay").Enable();
    }

    private void OnDisable()
    {
        _inputActions.FindActionMap("Gameplay").Disable();
    }
    private void Start()
    {
        _talkCamScript = GetComponent<TalkCameraScript>();
        _playerState = GetComponent<PlayerState>();
        _rigidbody = GetComponent<Rigidbody>();

        #region CommandSetup
        _pushItemsActionReference.action.Enable();
        _moveActionReference.action.Enable();
        _switchModeActionReference.action.Enable();
        _jumpActionReference.action.Enable();

        _pushItemsActionReference.action.started += PushItem_started;
        _pushItemsActionReference.action.performed += PushItem_performed;
        _pushItemsActionReference.action.canceled += PushItem_canceled;

        _moveActionReference.action.started += Moving_started;
        _moveActionReference.action.performed += Moving_performed;
        _moveActionReference.action.canceled += Moving_canceled;

        _switchModeActionReference.action.started += SwitchMode_started;
        _switchModeActionReference.action.performed += SwitchMode_performed;
        _switchModeActionReference.action.canceled += SwitchMode_canceled;

        _jumpActionReference.action.started += JumpAction_started;
        _jumpActionReference.action.performed += JumpAction_performed;
        _jumpActionReference.action.canceled += JumpAction_canceled;
        #endregion
    }
    private void FixedUpdate()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        switch (_playerState.GetPlayerState())
        {
            case PlayerState.PlayerStateEnum.IDLE:
                break;
            case PlayerState.PlayerStateEnum.WALK:
                Move();
                break;
            case PlayerState.PlayerStateEnum.TALKING:
                MoveCamToTalk();
                break;
        }
    }

    private void OnDestroy()
    {
        _pushItemsActionReference.action.Disable();
        _moveActionReference.action.Disable();
        _switchModeActionReference.action.Disable();
        _jumpActionReference.action.Disable();
    }

    #region InputEvents
    private void JumpAction_canceled(InputAction.CallbackContext obj)
    {
        Debug.Log("JumpAction canceled");
    }

    private void JumpAction_performed(InputAction.CallbackContext obj)
    {
        Debug.Log("JumpAction performed");
    }

    private void JumpAction_started(InputAction.CallbackContext obj)
    {
        Debug.Log("JumpAction started");
    }

    private void SwitchMode_canceled(InputAction.CallbackContext obj)
    {
        Debug.Log("SwitchMode canceled");
    }

    private void SwitchMode_performed(InputAction.CallbackContext obj)
    {
        Debug.Log("SwitchMode performed");
    }

    private void SwitchMode_started(InputAction.CallbackContext obj)
    {
        Debug.Log("SwitchMode started");
    }

   

    #region MovingEvents
    private void Moving_canceled(InputAction.CallbackContext obj)
    {
        //if (_playerState.GetPlayerState() == PlayerState.PlayerStateEnum.TALKING) return;

        //_playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
        //_moveAmt = Vector2.zero;
        Debug.Log("Moving canceled");
    }

    private void Moving_performed(InputAction.CallbackContext obj)
    {
        //_moveAmt = _3DMoveActionReference.action.ReadValue<Vector2>();
        //_lookAmt = _lookAction.ReadValue<Vector2>();
        Debug.Log("Moving performed");
    }

    private void Moving_started(InputAction.CallbackContext obj)
    {
        //_playerState.SetState(PlayerState.PlayerStateEnum.WALK);
        Debug.Log("Moving started");
    }
    #endregion

    #region SpeakingEvents
    private void PushItem_canceled(InputAction.CallbackContext obj)
    {
        //_playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
        Debug.Log("pushItem canceled");
    }

    private void PushItem_performed(InputAction.CallbackContext obj)
    {
        Debug.Log("pushItem performed");
    }

    private void PushItem_started(InputAction.CallbackContext obj)
    {
        Debug.Log("pushItem started");
    }
    #endregion
    #endregion

    #region InputFunction
    private void Move()
    {
        if (!_isTalking)
            _rigidbody.MovePosition(new Vector3(_rigidbody.position.x + _moveAmt.x * _spd * Time.deltaTime, _rigidbody.position.y, _rigidbody.position.z + _moveAmt.y * _spd * Time.deltaTime));
    }

    private void MoveCamToTalk()
    {
        if (_playerState.GetPlayerState() == PlayerState.PlayerStateEnum.TALKING)
        {
            _talkCamScript.ZoomIn();
            _isAlreadySpeaking = true;
        }
        else
        {
            _talkCamScript.ZoomOut();
            _isAlreadySpeaking = false;
        }
    }

    //public void APressed()
    //{
    //    if (_isAlreadySpeaking == false)
    //    {
    //        _playerState.SetState(PlayerState.PlayerStateEnum.TALKING);
    //    }
    //    else
    //    {
    //        _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
    //        _talkCameraScript.ZoomOut();
    //    }
    //}
    #endregion
}
