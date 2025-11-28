using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerState))]
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

    [Header("======| Cinemachine Camera |======")]
    [Header("")]
    [SerializeField] private CinemachineCamera _camera2D;
    [SerializeField] private CinemachineCamera _camera3D;

    [Header("======| Switch World |======")]
    [Header("")]
    [SerializeField] private GameObject _environment;
    [SerializeField] private GameObject _map;
    [SerializeField] private GameObject _wheelchair;

    [Header("======| Player Sounds |======")]
    [Header("")]
    [SerializeField] private AudioClip _jumpAudioClip;
    [SerializeField] private AudioClip[] _walkAudioClips;
    [SerializeField] private AudioClip[] _wheelAudioClips;
    [SerializeField] private AudioClip _takeObjectAudioClip;
    [SerializeField] private AudioClip _dropObjectAudioClip;
    [SerializeField] private AudioClip _hurtAudioClip;
    [SerializeField] private AudioClip _fallAudioClip;
    [SerializeField] private AudioClip _switchAudioClip;

    private bool _isAlreadySpeaking = false;
    private bool _isTalking = false;

    private TalkCameraScript _talkCamScript;
    private PlayerState _playerState;
    private GameMode _gameMode;
    private Rigidbody _rigidbody;
    private Animator _animator;

    private Vector3 _oldPlayerPos = new();
    private Dictionary<GameObject, Vector3> _allEnvironmentGO = new();
    private Dictionary<GameObject, Vector3> _2DEnvironmentGO = new();
    private GameObject[] _2DAssets;
    private GameObject[] _3DAssets;

    private Vector3 _dir;
    private Vector2 _moveAmt;
    private Vector2 _saveMoveAmt;
    private Vector2 _lookAmt;
    private Vector2 _velocity;

    [Header("======| LayerMask |======")]
    [Header("")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _movableLayer;
    [SerializeField] private LayerMask _2DObject;
    [SerializeField] private LayerMask _3DObject;
    [SerializeField] private LayerMask _setActiveIn2D;

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

    private Coroutine _walkCoroutine;

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
        _animator = GetComponent<Animator>();

        _2DAssets = GameObject.FindGameObjectsWithTag("2DAssets");
        _3DAssets = GameObject.FindGameObjectsWithTag("3DAssets");

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
        Debug.Log(_playerState.GetPlayerState());
    }

    private void UpdateState()
    {
        if (_gameMode.IsMenuMode()) return;

        switch (_playerState.GetPlayerState())
        {
            case PlayerState.PlayerStateEnum.IDLE:
                _moveAmt = Vector2.zero;
                _animator.SetBool("IsGrabbing", false);
                _animator.SetBool("IsMoving", false);
                break;
            case PlayerState.PlayerStateEnum.WALK:
                Move();
                _animator.SetBool("IsGrabbing", false);
                break;
            case PlayerState.PlayerStateEnum.TALKING:
                MoveCamToTalk();
                break;
            case PlayerState.PlayerStateEnum.JUMPING:
                Move();
                StartCoroutine(ResetJump(0.1f));
                _animator.SetBool("IsGrabbing", false);
                _animator.SetTrigger("JumpTrigger");
                break;
            case PlayerState.PlayerStateEnum.MOVINGOBJECT:
                Move();
                MoveObject();
                _animator.SetBool("IsGrabbing", true);
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
        SwitchWorld();
    }
    #endregion

    #region MovingEvents
    private void Moving_canceled(InputAction.CallbackContext obj)
    {
        if (_playerState.IsMovingObject() == false)
            _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);

        _saveMoveAmt = _moveAmt;
        _moveAmt = new Vector2(0, 0);

        if (_walkCoroutine != null)
        {
            StopCoroutine(_walkCoroutine);
            _walkCoroutine = null;
        }
    }

    private void Moving_performed(InputAction.CallbackContext obj)
    {
        Vector2 move = _moveActionReference.action.ReadValue<Vector2>();
        float deadzone = 0.01f;

        if (_gameMode.Is2DMode() && Mathf.Abs(move.y) > deadzone) return;
        _animator.SetBool("IsMoving", true);
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

            if (_walkCoroutine == null && _playerState.IsJumping() == false)
                _walkCoroutine = StartCoroutine(PlayWalkSound());
        }
        else if (_gameMode.Is3DMode())
        {
            _dir = new Vector3(_moveAmt.x, 0, _moveAmt.y);

            _rigidbody.AddForce(_dir.normalized * _3DSpd * Time.timeScale, ForceMode.Acceleration);
            
            if (_walkCoroutine == null && _playerState.IsJumping() == false)
                _walkCoroutine = StartCoroutine(PlayWheelSound());
        }

        if (_dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(_dir);
        }
    }

    IEnumerator PlayWalkSound()
    {
        while (true)
        {
            int index = UnityEngine.Random.Range(0, _walkAudioClips.Length);
            float delay = UnityEngine.Random.Range(0.5f, 1.5f);

            SFXManager.Instance.PlaySFXClip(_walkAudioClips[index], transform, 1.0f);
            yield return new WaitForSeconds(delay);
        }

        _walkCoroutine = null;
    }

    IEnumerator PlayWheelSound()
    {
        while (true)
        {
            int index = UnityEngine.Random.Range(0, _wheelAudioClips.Length);
            float delay = UnityEngine.Random.Range(0.5f, 1.5f);

            SFXManager.Instance.PlaySFXClip(_wheelAudioClips[index], transform, 1.0f);
            yield return new WaitForSeconds(delay);
        }

        _walkCoroutine = null;
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
        LayerMask canJumpLayer = _groundLayer | _movableLayer;

        if (Physics.Raycast(ray, out hit, 0.1f, canJumpLayer) && Mathf.Abs(_rigidbody.linearVelocity.z) < 0.1f)
        {
            _playerState.SetState(PlayerState.PlayerStateEnum.JUMPING);
            _rigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
            SFXManager.Instance.PlaySFXClip(_jumpAudioClip, transform, 1.0f);
        }
    }

    IEnumerator ResetJump(float delay)
    {
        yield return new WaitForSeconds(delay);

        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        LayerMask canJumpLayer = _groundLayer | _movableLayer;

        if (Physics.Raycast(ray, out hit, 0.1f, canJumpLayer))
        {
            if (_moveAmt.sqrMagnitude < 0.01f)
                _playerState.SetState(PlayerState.PlayerStateEnum.IDLE);
            else
                _playerState.SetState(PlayerState.PlayerStateEnum.WALK);
        }
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
            SFXManager.Instance.PlaySFXClip(_takeObjectAudioClip, transform, 1.0f);
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
        SFXManager.Instance.PlaySFXClip(_dropObjectAudioClip, transform, 1.0f);
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

    #region SwitchWorld
    private void SwitchWorld()
    {
        SFXManager.Instance.PlaySFXClip(_switchAudioClip, transform, 1.0f);
        CapsuleCollider coll = GetComponent<CapsuleCollider>();

        if (_gameMode.Is2DMode())
        {
            _animator.SetBool("Is3DWorld", false);
            _wheelchair.SetActive(false);
            transform.position = new Vector3(transform.position.x, transform.position.y - 0.25f, transform.position.z);
            _wheelchair.transform.position = new Vector3(_wheelchair.transform.position.x, _wheelchair.transform.position.y + 0.25f, _wheelchair.transform.position.z);
            coll.center = new Vector3(coll.center.x, coll.center.y , coll.center.z);
            foreach (GameObject go in _2DAssets) { go.GetComponent<MeshRenderer>().enabled = true; }
            foreach (GameObject go in _3DAssets) { go.GetComponent<MeshRenderer>().enabled = false; }
            SwitchTo2DMode();
        }
        else
        {
            _animator.SetBool("Is3DWorld", true);
            _wheelchair.SetActive(true);
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.25f, transform.position.z);
            _wheelchair.transform.position = new Vector3(_wheelchair.transform.position.x, _wheelchair.transform.position.y - 0.25f, _wheelchair.transform.position.z);
            coll.center = new Vector3(coll.center.x, coll.center.y , coll.center.z);
            foreach (GameObject go in _2DAssets) { go.GetComponent<MeshRenderer>().enabled = false; }
            foreach (GameObject go in _3DAssets) { go.GetComponent<MeshRenderer>().enabled = true; }
            SwitchTo3DMode();
        }
    }
    #endregion

    #region Switch To 2D Mode
    private void SwitchTo2DMode()
    {
        _allEnvironmentGO.Clear();
        _2DEnvironmentGO.Clear();

        BoxCollider mapCollider = _map.GetComponent<BoxCollider>();
        int coordToTeleport = (int)mapCollider.bounds.min.z + 2;

        PositionEnvironmentObjects(coordToTeleport);
        _oldPlayerPos = gameObject.transform.position;

        // Récupère les objets statiques et mobiles (sans le player)
        List<GameObject> staticObjects = GetObjectsByLayer(_2DObject);
        List<GameObject> movableObjects = GetMovableObjects();

        // Place les objets mobiles contre les obstacles et construit la liste complète
        List<GameObject> allObstacles = ProcessMovableObjects(staticObjects, movableObjects);

        // Traite le player en dernier pour le positionner correctement
        ProcessPlayer(coordToTeleport, allObstacles);

        SetCameraPriorities(camera2DPriority: 1, camera3DPriority: 0);
    }
    #endregion

    #region Switch To 3D Mode
    private void SwitchTo3DMode()
    {
        RestoreEnvironmentPositions();
        RestorePlayerPosition();
        SetCameraPriorities(camera2DPriority: 0, camera3DPriority: 1);
    }
    #endregion

    #region Environment Positioning
    private void PositionEnvironmentObjects(int coordToTeleport)
    {
        foreach (Transform ts in _environment.transform)
        {
            _allEnvironmentGO[ts.gameObject] = ts.position;

            if (ShouldMoveToForeground(ts.gameObject))
            {
                MoveObjectToForeground(ts, coordToTeleport);
            }
            else
            {
                MoveObjectToBackground(ts);
            }
        }
    }

    private bool ShouldMoveToForeground(GameObject obj)
    {
        return IsInLayerMask(obj, _movableLayer) ||
               IsInLayerMask(obj, _2DObject) ||
               IsInLayerMask(obj, _setActiveIn2D);
    }

    private void MoveObjectToForeground(Transform ts, int zPosition)
    {
        if (IsInLayerMask(ts.gameObject, _setActiveIn2D))
        {
            ts.gameObject.SetActive(true);
        }

        Vector3 newPosition = ts.position;
        newPosition.z = zPosition;
        ts.position = newPosition;
        _2DEnvironmentGO[ts.gameObject] = newPosition;
    }

    private void MoveObjectToBackground(Transform ts)
    {
        Vector3 newPosition = ts.position;
        newPosition.z = 500f;
        ts.position = newPosition;

        MeshRenderer renderer = ts.gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 175f / 255f;
            renderer.material.color = color;
        }
    }
    #endregion

    #region Object Processing
    private List<GameObject> GetObjectsByLayer(LayerMask layer)
    {
        return _2DEnvironmentGO.Keys.Where(obj => IsInLayerMask(obj, layer)).ToList();
    }

    private List<GameObject> GetMovableObjects()
    {
        List<GameObject> movableObjects = _2DEnvironmentGO.Keys
            .Where(obj => IsInLayerMask(obj, _movableLayer))
            .ToList();

        movableObjects.Remove(gameObject);
        return movableObjects;
    }

    private List<GameObject> ProcessMovableObjects(List<GameObject> staticObjects, List<GameObject> movableObjects)
    {
        List<GameObject> allObstacles = new List<GameObject>(staticObjects);

        // Pour chaque objet mobile, trouve l'obstacle le plus proche et snap dessus
        foreach (var movable in movableObjects)
        {
            Collider movableCollider = movable.GetComponent<Collider>();
            if (movableCollider == null) continue;

            GameObject closestObstacle = FindClosestObstacle(movableCollider, allObstacles, movable);

            if (closestObstacle != null)
            {
                SnapMovableToSide(closestObstacle.transform, movable.transform);
                Physics.SyncTransforms();
                ResolveAllOverlaps(movable, allObstacles);
                allObstacles.Add(movable);
            }
        }

        return allObstacles;
    }

    private GameObject FindClosestObstacle(Collider movableCollider, List<GameObject> obstacles, GameObject movable)
    {
        GameObject closestObstacle = null;
        float closestDistance = float.MaxValue;

        foreach (var obstacle in obstacles)
        {
            if (obstacle == movable) continue;

            Collider obstacleCollider = obstacle.GetComponent<Collider>();
            if (obstacleCollider == null) continue;

            float distance = Vector3.Distance(
                new Vector3(movableCollider.bounds.center.x, movableCollider.bounds.center.y, 0),
                new Vector3(obstacleCollider.bounds.center.x, obstacleCollider.bounds.center.y, 0)
            );

            // Vérifie si l'objet mobile est dans la zone d'influence de l'obstacle
            Bounds expandedBounds = obstacleCollider.bounds;
            expandedBounds.Expand(movableCollider.bounds.size.x + 0.1f);

            if (expandedBounds.Contains(new Vector3(movableCollider.bounds.center.x, movableCollider.bounds.center.y, obstacleCollider.bounds.center.z)))
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestObstacle = obstacle;
                }
            }
        }

        return closestObstacle;
    }
    #endregion

    #region Player Processing
    private void ProcessPlayer(int coordToTeleport, List<GameObject> allObstacles)
    {
        MovePlayerTo2D(coordToTeleport);
        PositionPlayerRelativeToObstacles(allObstacles);
    }

    private void MovePlayerTo2D(int zPosition)
    {
        gameObject.transform.position = new Vector3(
            gameObject.transform.position.x,
            gameObject.transform.position.y,
            zPosition
        );

        gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void PositionPlayerRelativeToObstacles(List<GameObject> allObstacles)
    {
        _2DEnvironmentGO[gameObject] = gameObject.transform.position;

        Collider playerCollider = gameObject.GetComponent<Collider>();
        if (playerCollider == null) return;

        Physics.SyncTransforms();

        List<GameObject> nearbyObstacles = FindNearbyObstacles(playerCollider, allObstacles);

        if (nearbyObstacles.Count > 0)
        {
            PushPlayerOutOfObstacles(playerCollider, nearbyObstacles);
        }
    }

    private List<GameObject> FindNearbyObstacles(Collider playerCollider, List<GameObject> allObstacles)
    {
        const float searchRadius = 5f;
        List<GameObject> nearbyObstacles = new List<GameObject>();

        foreach (var obstacle in allObstacles)
        {
            Collider obstacleCollider = obstacle.GetComponent<Collider>();
            if (obstacleCollider == null) continue;

            float distance = Vector3.Distance(
                new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.center.y, 0),
                new Vector3(obstacleCollider.bounds.center.x, obstacleCollider.bounds.center.y, 0)
            );

            if (distance < searchRadius)
            {
                nearbyObstacles.Add(obstacle);
            }
        }

        return nearbyObstacles;
    }

    private void PushPlayerOutOfObstacles(Collider playerCollider, List<GameObject> nearbyObstacles)
    {
        // Calcule la zone totale occupée par tous les obstacles proches
        (float leftBound, float rightBound) = CalculateObstacleBounds(nearbyObstacles);

        float playerX = playerCollider.bounds.center.x;
        float distanceToLeft = playerX - leftBound;
        float distanceToRight = rightBound - playerX;

        // Pousse le player vers le côté le plus proche pour sortir de la zone d'obstacles
        Vector3 newPosition = CalculateNewPlayerPosition(playerCollider, leftBound, rightBound, distanceToLeft, distanceToRight);

        gameObject.transform.position = newPosition;
        _2DEnvironmentGO[gameObject] = newPosition;
        Physics.SyncTransforms();
    }

    private (float leftBound, float rightBound) CalculateObstacleBounds(List<GameObject> obstacles)
    {
        float leftBound = float.MaxValue;
        float rightBound = float.MinValue;

        foreach (var obstacle in obstacles)
        {
            Collider obstacleCollider = obstacle.GetComponent<Collider>();
            leftBound = Mathf.Min(leftBound, obstacleCollider.bounds.min.x);
            rightBound = Mathf.Max(rightBound, obstacleCollider.bounds.max.x);
        }

        return (leftBound, rightBound);
    }

    private Vector3 CalculateNewPlayerPosition(Collider playerCollider, float leftBound, float rightBound, float distanceToLeft, float distanceToRight)
    {
        Vector3 newPosition = gameObject.transform.position;
        const float safetyMargin = 1.0f;

        if (distanceToLeft < distanceToRight)
        {
            // Pousse vers la gauche
            float targetX = leftBound - playerCollider.bounds.extents.x - safetyMargin;
            newPosition.x = gameObject.transform.position.x + (targetX - playerCollider.bounds.center.x);
        }
        else
        {
            // Pousse vers la droite
            float targetX = rightBound + playerCollider.bounds.extents.x + safetyMargin;
            newPosition.x = gameObject.transform.position.x + (targetX - playerCollider.bounds.center.x);
        }

        return newPosition;
    }
    #endregion

    #region Restore 3D State
    private void RestoreEnvironmentPositions()
    {
        foreach (var kvp in _allEnvironmentGO)
        {
            if (_2DEnvironmentGO.ContainsKey(kvp.Key))
            {
                if (HasObjectMoved(kvp.Key, kvp.Value))
                {
                    RestorePositionWithZCorrection(kvp.Key, kvp.Value);
                }
                else
                {
                    kvp.Key.transform.position = kvp.Value;
                }
            }
            else
            {
                RestoreObjectFully(kvp.Key, kvp.Value);
            }

            if (IsInLayerMask(kvp.Key, _setActiveIn2D))
            {
                kvp.Key.SetActive(false);
            }
        }
    }

    private bool HasObjectMoved(GameObject obj, Vector3 originalPosition)
    {
        return Vector3.Distance(_2DEnvironmentGO[obj], obj.transform.position) > 0.01f;
    }

    private void RestorePositionWithZCorrection(GameObject obj, Vector3 originalPosition)
    {
        Vector3 newPosition = obj.transform.position;
        newPosition.z = originalPosition.z;
        obj.transform.position = newPosition;
    }

    private void RestoreObjectFully(GameObject obj, Vector3 originalPosition)
    {
        obj.transform.position = originalPosition;

        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Color color = renderer.material.color;
            color.a = 1f;
            renderer.material.color = color;
        }
    }

    private void RestorePlayerPosition()
    {
        if (_oldPlayerPos != Vector3.zero)
        {
            gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

            Vector3 newPosition = gameObject.transform.position;
            newPosition.z = _oldPlayerPos.z;
            gameObject.transform.position = newPosition;
        }
    }
    #endregion

    #region Camera Management
    private void SetCameraPriorities(int camera2DPriority, int camera3DPriority)
    {
        _camera2D.Priority = camera2DPriority;
        _camera3D.Priority = camera3DPriority;
    }
    #endregion

    #region Utils
    public static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return ((1 << obj.layer) & mask) != 0;
    }

    private void SnapMovableToSide(Transform staticObj, Transform movable)
    {
        Collider staticCollider = staticObj.GetComponent<Collider>();
        Collider movableCollider = movable.GetComponent<Collider>();

        if (staticCollider == null || movableCollider == null) return;

        Vector3 newPosition = movable.position;
        float movableCenter = movableCollider.bounds.center.x;
        float staticCenter = staticCollider.bounds.center.x;

        if (movableCenter > staticCenter)
        {
            newPosition.x = staticCollider.bounds.max.x + movableCollider.bounds.extents.x;
        }
        else
        {
            newPosition.x = staticCollider.bounds.min.x - movableCollider.bounds.extents.x;
        }

        movable.position = newPosition;
        _2DEnvironmentGO[movable.gameObject] = newPosition;
        Physics.SyncTransforms();
    }

    private void ResolveAllOverlaps(GameObject movable, List<GameObject> allObstacles)
    {
        Collider movableCollider = movable.GetComponent<Collider>();
        if (movableCollider == null) return;

        int layerMask = ~_groundLayer.value;
        const int maxIterations = 10;

        // Itère jusqu'à ce qu'il n'y ait plus d'overlaps ou atteigne la limite d'itérations
        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            Physics.SyncTransforms();

            List<Collider> validOverlaps = FindValidOverlaps(movableCollider, movable, allObstacles, layerMask);

            if (validOverlaps.Count == 0) break;

            Collider closestOverlap = FindClosestOverlap(movableCollider, validOverlaps);

            if (closestOverlap != null)
            {
                PushMovableAwayFromOverlap(movable, movableCollider, closestOverlap);
            }
            else
            {
                break;
            }
        }
    }

    private List<Collider> FindValidOverlaps(Collider movableCollider, GameObject movable, List<GameObject> allObstacles, int layerMask)
    {
        Collider[] overlaps = Physics.OverlapBox(
            center: movableCollider.bounds.center,
            halfExtents: movableCollider.bounds.extents * 0.99f,
            orientation: movable.transform.rotation,
            layerMask: layerMask,
            queryTriggerInteraction: QueryTriggerInteraction.Ignore
        );

        List<Collider> validOverlaps = new List<Collider>();
        foreach (var overlap in overlaps)
        {
            if (overlap.gameObject != movable && allObstacles.Contains(overlap.gameObject))
            {
                validOverlaps.Add(overlap);
            }
        }

        return validOverlaps;
    }

    private Collider FindClosestOverlap(Collider movableCollider, List<Collider> validOverlaps)
    {
        Collider closestOverlap = null;
        float closestDistance = float.MaxValue;

        foreach (var overlap in validOverlaps)
        {
            float distance = Mathf.Abs(movableCollider.bounds.center.x - overlap.bounds.center.x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestOverlap = overlap;
            }
        }

        return closestOverlap;
    }

    private void PushMovableAwayFromOverlap(GameObject movable, Collider movableCollider, Collider obstacle)
    {
        float movableX = movableCollider.bounds.center.x;
        float obstacleX = obstacle.bounds.center.x;
        Vector3 newPosition = movable.transform.position;
        const float safetyMargin = 0.01f;

        // Pousse l'objet du côté opposé à l'obstacle
        if (movableX > obstacleX)
        {
            float targetX = obstacle.bounds.max.x + movableCollider.bounds.extents.x + safetyMargin;
            newPosition.x = movable.transform.position.x + (targetX - movableCollider.bounds.center.x);
        }
        else
        {
            float targetX = obstacle.bounds.min.x - movableCollider.bounds.extents.x - safetyMargin;
            newPosition.x = movable.transform.position.x + (targetX - movableCollider.bounds.center.x);
        }

        movable.transform.position = newPosition;
        _2DEnvironmentGO[movable] = newPosition;
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
