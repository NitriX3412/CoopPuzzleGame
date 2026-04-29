using FishNet.Managing;
using FishNet.Object;
using UnityEngine;
using System.Collections;


public class HealthPackSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 5f;

    public override void OnStartServer()
    {
        foreach (var point in _spawnPoints) SpawnPickup(point.position);
    }

    public void OnPickedUp(Vector3 position)
    {
        StartCoroutine(RespawnAfterDelay(position));
    }

    private void SpawnPickup(Vector3 position)
    {
        var go = Instantiate(_healthPickupPrefab, position, Quaternion.identity);
        go.GetComponent<HealthPack>().Init(this);
        base.ServerManager.Spawn(go);
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }
}