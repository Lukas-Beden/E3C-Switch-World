using UnityEngine;
using UnityEngine.AI;

public class InteractWithPlayer : MonoBehaviour
{
    [SerializeField] private GameObject _target;
    [SerializeField] private GameObject[] _pointsPath;

    [SerializeField] private float _rangeFollow;
    [SerializeField] private float _turnSpeed;

    [SerializeField] private NavMeshAgent _agent;
    [Range(0, 10)]
    [SerializeField] private int _damageOnCollide = 2;
    [Range(1, 30)]
    [SerializeField] private int _bumpForce = 6;

    private float _distance;
    private int _indexActualPath = 0;

    private void Start()
    {
        if (_target == null)
            _target = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        if (_target == null || !_target.activeInHierarchy)
        {
            FollowPath();
            return;
        }

        Vector3 selfPos = transform.position;
        Vector3 targetPos = _target.transform.position;
        _distance = Vector3.Distance(selfPos, targetPos);

        if (_distance <= _rangeFollow)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
        else
        {
            FollowPath();
        }
    }

    private void LookAt(Transform target)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _turnSpeed);
    }

    private void FollowPath()
    {
        _agent.SetDestination(_pointsPath[_indexActualPath].transform.position);
        LookAt(_pointsPath[_indexActualPath].transform);

        float distance = Vector3.Distance(transform.position, _pointsPath[_indexActualPath].transform.position);

        if (distance <= _agent.stoppingDistance)
        {
            _indexActualPath++;
            if (_indexActualPath == _pointsPath.Length)
            {
                _indexActualPath = 0;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            _target.GetComponent<HealthSystem>().GetDamage(_damageOnCollide);
            _target.GetComponent<PlayerMovement>().GetBumped(transform.forward * _bumpForce);
        }
    }
}
