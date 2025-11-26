using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Windows;



[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(GameMode))]
public class PlayerMovement : MonoBehaviour
{
    [Header("======| Movement attributes |======")]
    [Header("")]
    [Range(1.0f, 100.0f)]
    [SerializeField] private float _3DSpd = 30.0f;
    [Range(1.0f, 100.0f)]
    [SerializeField] private float _2DSpd = 15.0f;
    [Range(1.0f, 100.0f)]
    [SerializeField] private float _acceleration = 20.0f;
    [Range(1.0f, 100.0f)]
    [SerializeField] private float _brake = 25.0f;
    [Range(1.0f, 100.0f)]
    [SerializeField] private float _glide = 10.0f;
    [Range(1.0f, 15.0f)]
    [SerializeField] private float _jumpForce = 6.0f;


    [Header("======| Global Input Action Asset |======")]
    [Header("")]
    [SerializeField] private InputActionAsset _inputActions;
    

    [Header("======| Actions Ref |======")]
    [Header("")]
    [SerializeField] private InputActionReference _pushItemsActionReference;
    [SerializeField] private InputActionReference _moveActionReference;
    [SerializeField] private InputActionReference _switchModeActionReference;
    [SerializeField] private InputActionReference _jumpActionReference;

    [Header("======| Init Player |======")]
    [Header("")]
    [SerializeField] private GameObject _spawner;

    private bool _isAlreadySpeaking = false;
    private bool _isTalking = false;

    private TalkCameraScript _talkCamScript;
    private PlayerState _playerState;
    private GameMode _gameMode;
    private Rigidbody _rigidbody;

    private Vector3 _dir;
    private Vector2 _moveAmt;
    private Vector2 _lookAmt;
    private Vector2 _velocity;

    private LayerMask _groundLayer;
    private LayerMask _movableLayer;

    [Header("======| Gravity Changer |======")]
    [Header("")]
    [SerializeField] private float _delayMidJump = 1.0f;
    [SerializeField] private Vector3 _elevateJumpGravity = new Vector3(0f, -3.0f, 0f);
    [SerializeField] private Vector3 _fallenJumpGravity = new Vector3(0f, -20.0f, 0f);
    [SerializeField] private Vector3 _defaultGravity = new Vector3(0f, -9.81f, 0f);

    private GameObject _tookObject;

    private float _timerMidJump = 0.0f;

    private float _timerResetJump = 0.0f;
    private float _delayResetJump = 1.0f;

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
        _gameMode = GetComponent<GameMode>();
        _rigidbody = GetComponent<Rigidbody>();
        _groundLayer = LayerMask.GetMask("Ground");
        _movableLayer = LayerMask.GetMask("IsMovable");

        transform.position = _spawner.transform.position;

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
    private void Update()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        switch (_playerState.GetPlayerState())
        {
            case PlayerState.PlayerStateEnum.IDLE:
                _moveAmt = Vector2.zero;
                break;
            case PlayerState.PlayerStateEnum.WALK:
                Move();
                break;
            case PlayerState.PlayerStateEnum.TALKING:
                MoveCamToTalk();
                break;
            case PlayerState.PlayerStateEnum.JUMPING:
                Move();
                ResetJump();
                break;
            case PlayerState.PlayerStateEnum.MOVINGOBJECT:
                Move();
                MoveObject();
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
    #region JumpEvents
    private void JumpAction_canceled(InputAction.CallbackContext obj)
    {
    }

    private void JumpAction_performed(InputAction.CallbackContext obj)
    {
    }

    private void JumpAction_started(InputAction.CallbackContext obj)
    {
        if (_playerState.IsJumping() || _gameMode.Is3DMode()) return;

        Jump();
    }
    #endregion

    #region SwitchModeEvents
    private void SwitchMode_canceled(InputAction.CallbackContext obj)
    {
    }

    private void SwitchMode_performed(InputAction.CallbackContext obj)
    {
    }

    private void SwitchMode_started(InputAction.CallbackContext obj)
    {
        _gameMode.SwitchMode();
    }
    #endregion

    #region MovingEvents
    private void Moving_canceled(InputAction.CallbackContext obj)
    {
        if (_playerState.IsMovingObject()) return;

        _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
    }

    private void Moving_performed(InputAction.CallbackContext obj)
    {
        if (_gameMode.Is2DMode() && _moveActionReference.action.ReadValue<Vector2>().y != 0.0f) return;
        
        _moveAmt = _moveActionReference.action.ReadValue<Vector2>();
    }

    private void Moving_started(InputAction.CallbackContext obj)
    {
        _playerState.SetState(PlayerState.PlayerStateEnum.WALK);
    }
    #endregion

    #region PushItemEvents
    private void PushItem_canceled(InputAction.CallbackContext obj)
    {
    }

    private void PushItem_performed(InputAction.CallbackContext obj)
    {
    }

    private void PushItem_started(InputAction.CallbackContext obj)
    {
        if (_playerState.IsMovingObject())
            DropObject();
        else
            DetectObject();
    }


    #endregion
    #endregion

    #region InputFunction
    #region Movement
    private void Move()
    {
        Vector3 movement;

        Vector3 direction = Vector3.zero;

        if (_gameMode.Is2DMode())
        {
            direction = new Vector3(_moveAmt.x, 0, 0);

            Vector3 targetPos = _rigidbody.position + direction * _2DSpd * Time.deltaTime;
            _rigidbody.MovePosition(targetPos);
        }
        else if (_gameMode.Is3DMode())
        {
            direction = new Vector3(_moveAmt.x, 0, _moveAmt.y);

            _rigidbody.AddForce(direction.normalized * _3DSpd, ForceMode.Acceleration);
        }

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public void ResetMovement()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _velocity = Vector3.zero;
    }
    #endregion

    #region CamEffect
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
    #endregion

    #region JumpFunctions
    private void Jump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        LayerMask canJumpLayer = _groundLayer + _movableLayer;

        if (Physics.Raycast(ray, out hit, 0.1f, canJumpLayer) && _rigidbody.linearVelocity.z == 0.0f)
        {
            _playerState.SetState(PlayerState.PlayerStateEnum.JUMPING);
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    private void ResetJump()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 0.01f, _groundLayer) && _rigidbody.linearVelocity.z != 0.0f)
            _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
    }
    #endregion

    #region InteractWithObjectFunctions
    private void DetectObject()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 3.0f, _movableLayer))
        {
            _playerState.SetState(PlayerState.PlayerStateEnum.MOVINGOBJECT);
            _tookObject = hit.transform.gameObject;
            // + cameraZoom
        }
    }

    private void MoveObject()
    {
        float posY = transform.position.y + _tookObject.transform.position.y / 2;
        Vector3 newVec = new Vector3(transform.position.x, posY, transform.position.z);

        _tookObject.transform.position = newVec + transform.forward;
    }
    private void DropObject()
    {
        _tookObject = null;
        _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
    }

    public void GetBumped(Vector3 direction)
    {
        _rigidbody.AddForce(direction, ForceMode.Impulse);
        Debug.Log("Get Bumped");
    }
    #endregion

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
