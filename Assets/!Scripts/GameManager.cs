using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 20f;

    [SerializeField] private UIGameResults _gameResults;

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);

    private Dictionary<NetworkConnection, int> playerScores = new Dictionary<NetworkConnection, int>();

    [System.Serializable]
    public struct PlayerScoreData
    {
        public string Nickname;
        public int Score;

        public PlayerScoreData(string nickname, int score)
        {
            Nickname = nickname;
            Score = score;
        }
    }

    public enum GameState
    {
        WaitingForPlayers,
        InProgress,
        ShowingResults
    }

    private void Awake()
    {
        if (Instance != null) Destroy(this);
        else Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        InstanceFinder.ServerManager.OnRemoteConnectionState += OnPlayerConnectionChanged;
        Debug.Log("[GameManager] Server started. Waiting for players...");
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ServerManager != null)
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnPlayerConnectionChanged;
    }

    private void OnPlayerConnectionChanged(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (!IsServerInitialized) return;

        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            // Игрок подключился
            playerScores[conn] = 0;
            ConnectedPlayers.Value = InstanceFinder.ServerManager.Clients.Count;
            Debug.Log($"[GameManager] Player connected. Total: {ConnectedPlayers.Value}/{_requiredPlayers}");

            if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
                StartMatch();
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            // Игрок отключился
            if (playerScores.ContainsKey(conn))
                playerScores.Remove(conn);
            ConnectedPlayers.Value = InstanceFinder.ServerManager.Clients.Count;
            Debug.Log($"[GameManager] Player disconnected. Total: {ConnectedPlayers.Value}");

            // Если матч идёт и игроков стало меньше required – завершить матч.
            if (CurrentState.Value == GameState.InProgress && ConnectedPlayers.Value < _requiredPlayers)
            {
                Debug.Log("[GameManager] Not enough players, resetting to lobby.");
                ResetToLobby();
            }
        }
    }

    private void StartMatch()
    {
        if (!IsServerInitialized) return;
        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;
        Debug.Log("[GameManager] Match started!");
    }

    private void Update()
    {
        if (!IsServerInitialized) return;
        if (CurrentState.Value != GameState.InProgress) return;

        MatchTimer.Value -= Time.deltaTime;
        if (MatchTimer.Value <= 0f)
            EndMatch();
    }

    private void EndMatch()
    {
        if (!IsServerInitialized) return;
        CurrentState.Value = GameState.ShowingResults;
        Debug.Log("[GameManager] Match ended! Showing results...");

        List<PlayerScoreData> results = new List<PlayerScoreData>();
        foreach (var kvp in playerScores)
        {
            NetworkConnection conn = kvp.Key;
            int score = kvp.Value;

            string nickname = "Unknown";
            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    nickname = pn.Nickname.Value;
                    break;
                }
            }

            results.Add(new PlayerScoreData(nickname, score));
        }

        ShowResultsToClients(results.ToArray());

        Invoke(nameof(ResetToLobby), 5f);
    }

    [ObserversRpc] // Эта функция вызовется на всех клиентах
    private void ShowResultsToClients(PlayerScoreData[] results)
    {
        Debug.Log("[Client] Match ended, show results screen.");

        _gameResults.ShowScore(results);
    }

    [ObserversRpc]
    private void HideResultsToClients()
    {
        _gameResults.HideScore();
    }

    private void ResetToLobby()
    {
        if (!IsServerInitialized) return;

        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            if (playerScores.ContainsKey(conn))
                playerScores[conn] = 0;

            foreach (var nob in conn.Objects)
            {
                PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
                if (pn != null)
                {
                    pn.ResetStats();
                }
            }
        }

        HideResultsToClients();

        MatchTimer.Value = _matchDuration;
        CurrentState.Value = GameState.WaitingForPlayers;
        Debug.Log("[GameManager] Lobby reset. Waiting for players...");

        if(ConnectedPlayers.Value >= _requiredPlayers) StartMatch();
    }

    [Server]
    public void AddScore(NetworkConnection player, int amount)
    {
        if (!IsServerInitialized) return;
        if (playerScores.ContainsKey(player))
            playerScores[player] += amount;
        else
            playerScores[player] = amount;
    }

    [Server]
    public Dictionary<NetworkConnection, int> GetAllScores()
    {
        return playerScores;
    }
}