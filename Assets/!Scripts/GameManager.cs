using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Match Settings")]
    [SerializeField] private int _requiredPlayers = 2;
    [SerializeField] private float _matchDuration = 20f;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("UI")]
    [SerializeField] private UIGameResults _gameResults;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _readyButtonPanel;

    public readonly SyncVar<GameState> CurrentState = new SyncVar<GameState>(GameState.WaitingForPlayers);
    public readonly SyncVar<int> ConnectedPlayers = new SyncVar<int>(0);
    public readonly SyncVar<float> MatchTimer = new SyncVar<float>(0f);

    private Dictionary<NetworkConnection, int> playerScores = new Dictionary<NetworkConnection, int>();
    private Dictionary<int, Transform> playerSpawnPoint = new Dictionary<int, Transform>();
    private HashSet<NetworkConnection> readyPlayers = new HashSet<NetworkConnection>();

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
            playerScores[conn] = 0;
            ConnectedPlayers.Value = InstanceFinder.ServerManager.Clients.Count;

            int spawnIndex = ConnectedPlayers.Value - 1;
            if (spawnIndex >= _spawnPoints.Length) spawnIndex = 0; //Не должно сработать при двух игроках, заглушка
            playerSpawnPoint[conn.ClientId] = _spawnPoints[spawnIndex];

            Debug.Log($"playerSpawnPoint: {playerSpawnPoint[conn.ClientId]}");

            Debug.Log($"[GameManager] Player connected. Total: {ConnectedPlayers.Value}/{_requiredPlayers}");

            if (CurrentState.Value == GameState.WaitingForPlayers && ConnectedPlayers.Value >= _requiredPlayers)
                Invoke(nameof(StartMatch), 3);
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            playerScores.Remove(conn);
            playerSpawnPoint.Remove(conn.ClientId);
            readyPlayers.Remove(conn);
            ConnectedPlayers.Value = InstanceFinder.ServerManager.Clients.Count;

            Debug.Log($"[GameManager] Player disconnected. Total: {ConnectedPlayers.Value}");

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

        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            SpawnPlayerAtHisPoint(conn);
        }

        Debug.Log("[GameManager] Match started!");
    }

    private void SpawnPlayerAtHisPoint(NetworkConnection conn)
    {
        Debug.Log($"conn: {conn}");
        if (!playerSpawnPoint.TryGetValue(conn.ClientId, out Transform point))
            return;

        Debug.Log("1!!!!");

        foreach (var nob in conn.Objects)
        {
            Debug.Log("2!!!!");
            PlayerNetwork pn = nob.GetComponent<PlayerNetwork>();
            if (pn != null)
            {
                Debug.Log("3!!!!");
                pn.TeleportPlayerTo(point.position);
                pn.ResetStats();
                break;
            }
        }
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

        ShowReadyButtonToClients();
    }

    [ObserversRpc]
    private void ShowReadyButtonToClients()
    {
        if (_readyButtonPanel != null)
            _readyButtonPanel.SetActive(true);
    }

    [ObserversRpc]
    private void HideReadyButtonToClients()
    {
        if (_readyButtonPanel != null)
            _readyButtonPanel.SetActive(false);
    }

    public void OnPlayerReady()
    {
        if (!IsOwner) return;
        ReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyServerRpc(NetworkConnection sender = null)
    {
        if (CurrentState.Value != GameState.ShowingResults) return;

        if (readyPlayers.Add(sender))
        {
            Debug.Log($"[GameManager] Player {sender.ClientId} is ready.");
            CheckAllReady();
        }
    }

    private void CheckAllReady()
    {
        if (readyPlayers.Count >= ConnectedPlayers.Value && ConnectedPlayers.Value == _requiredPlayers)
        {
            // Все готовы – рестартим матч
            readyPlayers.Clear();
            ResetMatch();
        }
    }

    private void ResetMatch()
    {
        foreach (var conn in playerScores.Keys.ToList())
            playerScores[conn] = 0;
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            SpawnPlayerAtHisPoint(conn);
        }

        HideReadyButtonToClients();

        HideResultsToClients();

        CurrentState.Value = GameState.InProgress;
        MatchTimer.Value = _matchDuration;
        Debug.Log("[GameManager] Match restarted!");
    }

    [ObserversRpc]
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

        readyPlayers.Clear();
        foreach (var conn in playerScores.Keys.ToList())
            playerScores[conn] = 0;

        HideReadyButtonToClients();
        HideResultsToClients();

        CurrentState.Value = GameState.WaitingForPlayers;
        MatchTimer.Value = _matchDuration;
        Debug.Log("[GameManager] Lobby reset. Waiting for players...");

        if (ConnectedPlayers.Value >= _requiredPlayers)
            StartMatch();
    }

    public Vector3 GetSpawnPosition(int conn)
    {
        if (playerSpawnPoint.TryGetValue(conn, out Transform point))
            return point.position;
        return Vector3.zero;
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