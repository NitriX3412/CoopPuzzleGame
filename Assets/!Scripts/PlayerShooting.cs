using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShooting : NetworkBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Animator _animator;
    [SerializeField] private InputActionReference _shootRef;
    [SerializeField] private AudioSource _shootAudioSource;
    [SerializeField] private AudioClip _shootClip;

    private float _lastShotTime;

    private PlayerNetwork _playerNetwork;

    public override void OnStartNetwork()
    {
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (_shootRef.action.WasPressedThisFrame())
        {
            ShootServerRpc(_firePoint.position, _firePoint.forward);
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 pos, Vector3 dir, NetworkConnection sender = null)
    {
        if (_playerNetwork.Health.Value <= 0) return;
        if (_playerNetwork.CurrentAmmo.Value <= 0) return;
        if (Time.time < _lastShotTime + _playerNetwork.Cooldown) return;

        _lastShotTime = Time.time;
        _playerNetwork.CurrentAmmo.Value--;

        GameObject go = Instantiate(_projectilePrefab, pos + dir * 1.2f, Quaternion.LookRotation(dir));
        NetworkObject no = go.GetComponent<NetworkObject>();
        base.ServerManager.Spawn(no, sender);

        PlayShootEffectObservers();
    }

    [ObserversRpc]
    private void PlayShootEffectObservers()
    {
        if (_animator != null) _animator.SetTrigger("Shoot");

        if (!IsClientInitialized) return;

        if (_shootAudioSource != null && _shootClip != null)
        {
            _shootAudioSource.PlayOneShot(_shootClip);
        }
    }
}