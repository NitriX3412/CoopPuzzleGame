using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Canvas _UI;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _nicknameText;
    [SerializeField] private RespawnTimerUI _timerResp;

    public readonly SyncVar<int> Health = new SyncVar<int>(new SyncTypeSettings(0f, Channel.Reliable));
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(new SyncTypeSettings(0f, Channel.Reliable));
    public readonly SyncVar<int> CurrentAmmo = new SyncVar<int>(new SyncTypeSettings(0f, Channel.Reliable));
    public readonly SyncVar<string> Nickname = new SyncVar<string>(new SyncTypeSettings(0f, Channel.Reliable));

    public float Cooldown = 0.4f;
    public int MaxAmmo = 10;

    private void Awake()
    {
        Health.OnChange += OnHealthChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        CurrentAmmo.OnChange += OnAmmoChanged;
        Nickname.OnChange += OnNicknameChanged;
    }

    private void OnDestroy()
    {
        Health.OnChange -= OnHealthChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
        CurrentAmmo.OnChange -= OnAmmoChanged;
        Nickname.OnChange -= OnNicknameChanged;
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        ResetStats();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (base.IsOwner)
        {
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }
    }

    public void ResetStats()
    {
        Health.Value = 100;
        IsAlive.Value = true;
        CurrentAmmo.Value = MaxAmmo;
    }

    [ServerRpc]
    private void SetNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? "Player_" + OwnerId : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHealthChanged(int prev, int next, bool asServer)
    {
        _hpText.text = $"HP: {next}";
        if (!base.IsServerInitialized) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        GetComponent<MeshRenderer>().enabled = next;
        GetComponent<CharacterController>().enabled = next;
        _UI.enabled = next;

        if (base.IsOwner && !prev && next == false)
            _timerResp.StartTimer();
    }

    private void OnAmmoChanged(int prev, int next, bool asServer)
    {
        _ammoText.text = $"Ammo: {next}";
    }

    private void OnNicknameChanged(string prev, string next, bool asServer)
    {
        _nicknameText.text = next;
    }

    [ObserversRpc]
    public void TeleportPlayerTo(Vector3 position)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.position = position;
            cc.enabled = true;
        }
        else
        {
            transform.position = position;
        }
    }
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);
        Vector3 respawnPos = GameManager.Instance.GetSpawnPosition(base.OwnerId);
        transform.position = respawnPos;
        TeleportPlayerTo(respawnPos);
        ResetStats();
    }
}