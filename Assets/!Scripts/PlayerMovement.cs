using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _cc;
    private PlayerNetwork _playerNetwork;
    [SerializeField] private InputActionReference _moveRef;
    private float _verticalVelocity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _playerNetwork = GetComponent<PlayerNetwork>();
    }
    private void Update()
    {
        if (!IsOwner) return;

        if(!_playerNetwork.IsAlive.Value) return;

        Vector2 input = _moveRef.action.ReadValue<Vector2>();

        Vector3 move = new Vector3(input.x, 0f, input.y).normalized * _speed;

        _verticalVelocity += _gravity * Time.deltaTime;

        move.y = _verticalVelocity;

        _cc.Move(move * Time.deltaTime);

        if (_cc.isGrounded) _verticalVelocity = 0f;
    }
}
