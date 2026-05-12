using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Projectile : NetworkBehaviour
{
    [SerializeField] private float _speed = 18f;
    [SerializeField] private int _damage = 20;

    private void Update()
    {
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized) return;

        var target = other.GetComponent<PlayerNetwork>();
        if (target == null) return;

        if (target.OwnerId == OwnerId) return; 

        int newHp = Mathf.Max(0, target.Health.Value - _damage);
        target.Health.Value = newHp;

        if (newHp <= 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(Owner, 1);
        }

        Despawn();
    }
}