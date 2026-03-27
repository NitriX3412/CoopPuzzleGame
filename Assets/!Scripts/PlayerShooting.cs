using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;


    [SerializeField] private InputActionReference _shootRef;

    private float _lastShotTime;

    private PlayerNetwork _playerNetwork;

    public override void OnNetworkSpawn()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_shootRef.action.IsPressed())
            ShootServerRpc(_firePoint.position, _firePoint.forward);
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, ServerRpcParams rpc = default)
    {
        // 1. Жив ли игрок?
        if (_playerNetwork.HP.Value <= 0) return;

        // 2. Есть ли патроны?
        if (_playerNetwork.CurrentAmmo.Value <= 0) return;

        // 3. Прошёл ли кулдаун?
        if (Time.time < _lastShotTime + _playerNetwork._cooldown) return;

        _lastShotTime = Time.time;
        _playerNetwork.CurrentAmmo.Value--;

        var go = Instantiate(_projectilePrefab, pos + dir * 1.2f,
                             Quaternion.LookRotation(dir));
        var no = go.GetComponent<NetworkObject>();
        no.SpawnWithOwnership(rpc.Receive.SenderClientId);
    }
}