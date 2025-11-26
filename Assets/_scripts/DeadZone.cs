using System;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [Range(0, 10)]
    [SerializeField] private int _damageOnTrigger = 1;

    private PlayerMovement _playerMovement;

    private void Start()
    {
        _playerMovement = _player.GetComponent<PlayerMovement>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _playerMovement.ResetMovement();
            _playerMovement.gameObject.transform.SetPositionAndRotation(GetCloserRespawnCoord(), Quaternion.identity);
            _player.GetComponent<HealthSystem>().GetDamage(_damageOnTrigger);
        }
    }

    private Vector3 GetCloserRespawnCoord()
    {
        GameObject[] respawn = GameObject.FindGameObjectsWithTag("Respawn");
        GameObject closerRespawn;

        if (respawn.Length <= 0) 
            throw new ArgumentException("No Respawn on scene", nameof(respawn.Length));

        closerRespawn = respawn[0];

        float closestDistanceSqr = (closerRespawn.transform.position - _playerMovement.transform.position).sqrMagnitude;

        foreach (GameObject obj in respawn)
        {
            float distanceSqr = (obj.transform.position - _playerMovement.transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closerRespawn = obj;
                closestDistanceSqr = distanceSqr;
            }
        }

        return closerRespawn.transform.position;
    }
}
