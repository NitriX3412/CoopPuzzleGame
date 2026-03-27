using Unity.Netcode;
using UnityEngine;
using System.Collections;


public class HealthPackSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _healthPickupPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _respawnDelay = 5f;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += SpawnAll;
    }

    private void SpawnAll()
    {
        if (!NetworkManager.Singleton.IsServer) return;
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
        go.GetComponent<NetworkObject>().Spawn();
    }

    private IEnumerator RespawnAfterDelay(Vector3 position)
    {
        yield return new WaitForSeconds(_respawnDelay);
        SpawnPickup(position);
    }
}