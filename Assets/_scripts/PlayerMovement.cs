using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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

    [SerializeField] GameObject _tookObjectPoint;


    [Header("======| Global Input Action Asset |======")]
    [Header("")]
    [SerializeField] private InputActionAsset _inputActions;
    

    [Header("======| Actions Ref |======")]
    [Header("")]
    [SerializeField] private InputActionReference _pushItemsActionReference;
    [SerializeField] private InputActionReference _moveActionReference;
    [SerializeField] private InputActionReference _switchModeActionReference;
    [SerializeField] private InputActionReference _jumpActionReference;
    [SerializeField] private InputActionReference _pauseActionReference;

    [Header("======| Init Player |======")]
    [Header("")]
    [SerializeField] private GameObject _spawner;
    [SerializeField] private GameObject _gameModeManager;

    private bool _isAlreadySpeaking = false;
    private bool _isTalking = false;

    private TalkCameraScript _talkCamScript;
    private PlayerState _playerState;
    private GameMode _gameMode;
    private Rigidbody _rigidbody;

    private Vector3 _dir;
    private Vector2 _moveAmt;
    private Vector2 _saveMoveAmt;
    private Vector2 _lookAmt;
    private Vector2 _velocity;

    private LayerMask _groundLayer;
    private LayerMask _movableLayer;

    private enum CubeFace { Front, Back, Right, Left, Top, Bottom }

    private CubeFace selectedFace;

    [Header("======| Gravity Changer |======")]
    [Header("")]
    [SerializeField] private float _delayMidJump = 1.0f;
    [SerializeField] private Vector3 _elevateJumpGravity = new Vector3(0f, -3.0f, 0f);
    [SerializeField] private Vector3 _fallenJumpGravity = new Vector3(0f, -20.0f, 0f);
    [SerializeField] private Vector3 _defaultGravity = new Vector3(0f, -9.81f, 0f);

    [SerializeField] private GameObject _menuPauseManager;

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
        _gameMode = _gameModeManager.GetComponent<GameMode>();
        _rigidbody = GetComponent<Rigidbody>();
        _groundLayer = LayerMask.GetMask("Ground");
        _movableLayer = LayerMask.GetMask("IsMovable");

        transform.position = _spawner.transform.position;

        #region CommandSetup
        _pushItemsActionReference.action.Enable();
        _moveActionReference.action.Enable();
        _switchModeActionReference.action.Enable();
        _jumpActionReference.action.Enable();
        _pauseActionReference.action.Enable();

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

        _pauseActionReference.action.started += PauseAction_started;
        _pauseActionReference.action.performed += PauseAction_performed;
        _pauseActionReference.action.canceled += PauseAction_canceled;
        #endregion
    }

    Vector3 GetFaceLocalOffset(CubeFace face, Vector3 scale)
    {
        Vector3 halfScale = scale / 2f;
        switch (face)
        {
            case CubeFace.Front: return new Vector3(0, 0, halfScale.z);
            case CubeFace.Back: return new Vector3(0, 0, -halfScale.z);
            case CubeFace.Right: return new Vector3(halfScale.x, 0, 0);
            case CubeFace.Left: return new Vector3(-halfScale.x, 0, 0);
            case CubeFace.Top: return new Vector3(0, halfScale.y, 0);
            case CubeFace.Bottom: return new Vector3(0, -halfScale.y, 0);
            default: return Vector3.zero;
        }
    }


    CubeFace GetFaceFromMove(Vector2 move, float deadzone)
    {
        if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
        {
            if (move.x > deadzone) return CubeFace.Right;
            if (move.x < -deadzone) return CubeFace.Left;
        }
        else
        {
            if (move.y > deadzone) return CubeFace.Front;
            if (move.y < -deadzone) return CubeFace.Back;

        }

        return selectedFace;
    }




    private void Update()
    {
        UpdateState();
    }

    private void UpdateState()
    {
        //temporary, will need another input logic with another ActionMap
        if (_gameMode.IsMenuMode()) return;

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
        _pauseActionReference.action.Disable();
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
        if (_playerState.IsMovingObject() == false)
            _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);

        _saveMoveAmt = _moveAmt;
        _moveAmt = new Vector3(0, 0, 0);
    }

    private void Moving_performed(InputAction.CallbackContext obj)
    {
        Vector2 move = _moveActionReference.action.ReadValue<Vector2>();
        float deadzone = 0.01f;

        if (_gameMode.Is2DMode() && Mathf.Abs(move.y) > deadzone) return;

        _moveAmt = move;
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

    #region PauseEvents
    private void PauseAction_canceled(InputAction.CallbackContext obj)
    {
    }

    private void PauseAction_performed(InputAction.CallbackContext obj)
    {
    }

    private void PauseAction_started(InputAction.CallbackContext obj)
    {
        PauseMenu();
    }
    #endregion
    #endregion

    #region InputFunction
    #region Movement
    private void Move()
    {
        _dir = Vector3.zero;

        if (_gameMode.Is2DMode())
        {
            _dir = new Vector3(_moveAmt.x, 0, 0);

            Vector3 targetPos = _rigidbody.position + _dir * _2DSpd * Time.deltaTime;
            _rigidbody.MovePosition(targetPos);
        }
        else if (_gameMode.Is3DMode())
        {
            _dir = new Vector3(_moveAmt.x, 0, _moveAmt.y);

            _rigidbody.AddForce(_dir.normalized * _3DSpd * Time.timeScale, ForceMode.Acceleration);
        }

        if (_dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_dir);
        }
    }

    public void ResetMovement()
    {
        _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
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

        if (Physics.Raycast(ray, out hit, 0.1f, _groundLayer))
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
        float deadzone = 0.01f;

        Physics.IgnoreCollision(GetComponent<Collider>(), _tookObject.GetComponent<Collider>(), true);

        BoxCollider box = _tookObject.GetComponent<BoxCollider>();

        float angle;

        if (_moveAmt == new Vector2(0, 0))
            angle = Mathf.Atan2(_saveMoveAmt.x, _saveMoveAmt.y) * Mathf.Rad2Deg;
        else
            angle = Mathf.Atan2(_moveAmt.x, _moveAmt.y) * Mathf.Rad2Deg;

        Quaternion rot = Quaternion.Euler(0f, angle, 0f);
        _tookObject.transform.rotation = rot;

        CubeFace face = GetFaceFromMove(_moveAmt, deadzone);

        Vector3 localOffset = GetFaceLocalOffset(face, _tookObject.transform.localScale);

        Vector3 finalPos = _tookObjectPoint.transform.position + _tookObjectPoint.transform.rotation * localOffset;

        _tookObject.transform.position = Vector3.MoveTowards(
            _tookObject.transform.position,
            finalPos,
            20f * Time.deltaTime
        );
    }

    private void DropObject()
    {
        Physics.IgnoreCollision(GetComponent<Collider>(), _tookObject.GetComponent<Collider>(), false);
        _tookObject = null;
        _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
    }
    #endregion

    public void GetBumped(Vector3 direction)
    {
        _rigidbody.AddForce(direction, ForceMode.Impulse);
        Debug.Log("Get Bumped");
    }

    private void PauseMenu()
    {
        if (_gameMode.IsMenuMode())
        {
            _gameMode.SetGameMode(_gameMode.GetGameModeBeforePause());
            _menuPauseManager.GetComponent<PauseMenuManager>().DisablePauseMenu();
        }
        else
        {
            _gameMode.SetGameMode(GameMode.GMode.MENU);
            _menuPauseManager.GetComponent<PauseMenuManager>().EnablePauseMenu();
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
