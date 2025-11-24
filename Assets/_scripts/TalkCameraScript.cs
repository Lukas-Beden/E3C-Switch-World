using UnityEngine;
using Unity.Cinemachine;
using System.Threading.Tasks;
using TMPro;
using System;
using NUnit.Framework;
using System.Collections.Generic;

public class TalkCameraScript : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _originalCamera;
    [SerializeField] private CinemachineCamera _newCamera;
    [SerializeField] private GameObject _talkPannel;
    [SerializeField] private TextMeshProUGUI _textDialog;

    private List<string> _allTexts;
    private int _currentIndex = 0;

    private bool _isInRange = false;
    private PlayerState _playerState;

    private void Start()
    {
        _playerState = GetComponent<PlayerState>();
        _allTexts = new List<string>();
    }

    public void SetInRange()
    { 
        _isInRange = true;
    }

    public void SetOutRange()
    {
        _isInRange = false;
    }

    public void ZoomIn()
    {
        if (_isInRange == false) return;

        _originalCamera.Priority = 0;
        _newCamera.Priority = 3;
        ActivateUI();
    }

    public void ZoomOut()
    {
        _originalCamera.Priority = 3;
        _newCamera.Priority = 0;
        DeactivateUI();
    }

    public void ActivateUI()
    {
        _talkPannel.SetActive(true);
    }

    public void DeactivateUI() 
    { 
        _talkPannel.SetActive(false);
    }
    public void UpdateText() 
    {
        _textDialog.text = _allTexts[_currentIndex];
    }
    private void GetAllTexts()
    {
        _allTexts.Add("a");
    }
}
