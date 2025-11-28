using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class SFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource _SFXObject;
    [SerializeField] private AudioSource _audioSourceMenu;
    [SerializeField] private AudioClip _navigateAudioClip;
    [SerializeField] private AudioClip _validateAudioClip;
    [SerializeField] private AudioClip _exitAudioClip;
    [SerializeField] private AudioClip _playAudioClip;
    [SerializeField] private AudioClip _cancelAudioClip;

    public static SFXManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public void PlaySFXClip(AudioClip audioClip, Transform transform, float volume)
    {
        AudioSource audioSource = Instantiate(_SFXObject, transform.position, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayNavigate()
    {
        _audioSourceMenu.PlayOneShot(_navigateAudioClip, 1.0f);
    }

    //private void OnEnable()
    //{
    //    InputSystemUIInputModule ui = FindObjectOfType<InputSystemUIInputModule>();
    //    ui.move.action.performed += OnNavigate;
    //    ui.submit.action.performed += OnSubmit;
    //}

    //private void OnDisable()
    //{
    //    InputSystemUIInputModule ui = FindObjectOfType<InputSystemUIInputModule>();
    //    ui.move.action.performed -= OnNavigate;
    //    ui.submit.action.performed -= OnSubmit;
    //}

    //private void OnNavigate(InputAction.CallbackContext ctx)
    //{
    //    Vector2 dir = ctx.ReadValue<Vector2>();

    //    if (dir != Vector2.zero)
    //        PlayNavigate();
    //}

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        //GameObject selected = EventSystem.current.currentSelectedGameObject;

        //if (selected == null) return;

        //if (selected.gameObject.CompareTag("ExitButton"))
        //{
        //    PlaySFXClip(_exitAudioClip, selected.transform, 1.0f);
        //}
        //else if (selected.gameObject.CompareTag("PlayButton"))
        //{
        //    PlaySFXClip(_playAudioClip, selected.transform, 1.0f);
        //}
        //else if (selected.gameObject.CompareTag("CancelButton"))
        //{
        //    PlaySFXClip(_cancelAudioClip, selected.transform, 1.0f);
        //}
        //else if (selected.gameObject.CompareTag("OtherButton"))
        //{
        //    PlaySFXClip(_validateAudioClip, selected.transform, 1.0f);
        //}
    }

}
