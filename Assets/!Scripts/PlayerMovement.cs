using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;
    [SerializeField] private InputActionReference _moveRef;

    private CharacterController _cc;
    private PlayerNetwork _playerNetwork;
    private float _verticalVelocity;

    public struct MoveData : IReplicateData
    {
        public Vector2 Input;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Vector3 Velocity;
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }

    public override void OnStartNetwork()
    {
        if (base.Owner.IsLocalClient)
        {
            base.TimeManager.OnTick += TimeManager_OnTick;
        }
    }

    public override void OnStopNetwork()
    {
        if (base.IsOwner)
            base.TimeManager.OnTick -= TimeManager_OnTick;
    }

    private void TimeManager_OnTick()
    {
        if (!_playerNetwork.IsAlive.Value) return;
        
        RunInputs(CreateReplicateData());
    }
    public override void CreateReconcile()
    {
        ReconcileData data = new ReconcileData
        {
            Position = transform.position,
            Velocity = new Vector3(0f, _verticalVelocity, 0f)
        };
        Reconcile(data);
    }

    /// <summary>
    /// Создает данные для Replicate метода. Вызывается на клиенте каждый тик.
    /// </summary>
    private MoveData CreateReplicateData()
    {
        if (!base.IsOwner) return default;
        return new MoveData
        {
            Input = _moveRef.action.ReadValue<Vector2>()
        };
    }

    /// <summary>
    /// Метод с атрибутом Replicate. Выполняется и на клиенте (для предсказания), и на сервере.
    /// </summary>
    [Replicate]
    private void RunInputs(MoveData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
    {
        Vector3 move = new Vector3(data.Input.x, 0f, data.Input.y);
        ApplyMovement(ref move);
    }

    /// <summary>
    /// Основная логика движения, вынесенная в отдельный метод для удобства.
    /// </summary>
    private void ApplyMovement(ref Vector3 moveDirection)
    {
        moveDirection = moveDirection.normalized * _speed;

        if (!_cc.isGrounded)
            _verticalVelocity += _gravity * (float)base.TimeManager.TickDelta;
        else
            _verticalVelocity = 0f;

        moveDirection.y = _verticalVelocity;

        _cc.Move(moveDirection * (float)base.TimeManager.TickDelta);
    }

    /// <summary>
    /// Метод с атрибутом Reconcile. Вызывается на клиенте для коррекции состояния.
    /// </summary>
    [Reconcile]
    private void Reconcile(ReconcileData data, Channel channel = Channel.Unreliable)
    {
        transform.position = data.Position;
        _verticalVelocity = data.Velocity.y;
    }
}