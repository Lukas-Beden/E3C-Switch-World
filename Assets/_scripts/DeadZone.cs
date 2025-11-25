using System;
using UnityEngine;

public class DeadZone : MonoBehaviour
{
    [SerializeField] private PlayerMovement _player; 
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            _player.ResetMovement();
            _player.gameObject.transform.SetPositionAndRotation(GetCloserRespawnCoord(), Quaternion.identity);
        }
    }

    private Vector3 GetCloserRespawnCoord()
    {
        GameObject[] respawn = GameObject.FindGameObjectsWithTag("Respawn");
        GameObject closerRespawn;

        if (respawn.Length <= 0) 
            throw new ArgumentException("No Respawn on scene", nameof(respawn.Length));

        closerRespawn = respawn[0];

        float closestDistanceSqr = (closerRespawn.transform.position - _player.transform.position).sqrMagnitude;

        foreach (GameObject obj in respawn)
        {
            float distanceSqr = (obj.transform.position - _player.transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closerRespawn = obj;
                closestDistanceSqr = distanceSqr;
            }
        }

        return closerRespawn.transform.position;
    }
}
