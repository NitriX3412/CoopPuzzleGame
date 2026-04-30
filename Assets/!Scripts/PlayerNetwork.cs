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

        Health.Value = 100;
        IsAlive.Value = true;
        CurrentAmmo.Value = 10;
        Nickname.Value = "Player";
    }

    private void OnDestroy()
    {
        Health.OnChange -= OnHealthChanged;
        IsAlive.OnChange -= OnIsAliveChanged;
        CurrentAmmo.OnChange -= OnAmmoChanged;
        Nickname.OnChange -= OnNicknameChanged;
    }

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient)
        {
            Health.Value = 100;
            IsAlive.Value = true;
            CurrentAmmo.Value = 10;
            Nickname.Value = "Player";
            SetNicknameServerRpc(ConnectionUI.PlayerNickname);
        }  
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

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);
        transform.position = new Vector3(0, 1, 0);
        Health.Value = 100;
        IsAlive.Value = true;
        CurrentAmmo.Value = MaxAmmo;
    }
}