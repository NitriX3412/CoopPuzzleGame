using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private Canvas _UI;

    public NetworkVariable<FixedString32Bytes> Nickname = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> HP = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> IsAlive = new(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CurrentAmmo = new(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float _cooldown = 0.4f;
    public int _maxAmmo = 10;

    [SerializeField] private RespawnTimerUI _timerResp;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    private void OnHpChanged(int prev, int next)
    {
        if (!IsServer) return;
        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            StartCoroutine(RespawnRoutine());
        }
    }

    private void OnIsAliveChanged(bool prev, bool next)
    {
        gameObject.GetComponent<MeshRenderer>().enabled = next;
        gameObject.GetComponent<CharacterController>().enabled = next;
        _UI.enabled = next;
        if(IsOwner && prev) _timerResp.StartTimer();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue;
        if(string.IsNullOrWhiteSpace(nickname))
        {
            safeValue = "Player_" + OwnerClientId;
        }
        else
        {
            safeValue = nickname.Trim();
        }
        Nickname.Value = safeValue;
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(3f);

        transform.position = new Vector3(0, 1, 0);

        HP.Value = 100;
        IsAlive.Value = true;
    }
}