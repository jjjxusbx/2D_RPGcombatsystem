using UnityEngine;

public class PlayerInputReader : MonoBehaviour, IPlayerIntentProvider
{
    [SerializeField] private Camera aimCamera;

    private void Awake()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    public PlayerIntent GetIntent()
    {
        var intent = new PlayerIntent
        {
            MoveDirection = GetMoveInput(),
            AimPosition = GetAimPosition(),
            IsSprint = Input.GetKey(KeyCode.LeftShift),
            AttackPressed = Input.GetMouseButtonDown(0),
            DodgePressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(1)
        };

        if (intent.MoveDirection.sqrMagnitude > 0.001f)
            intent.Type = PlayerIntentType.Move;
        if (intent.AttackPressed)
            intent.Type = PlayerIntentType.Attack;
        if (intent.DodgePressed)
            intent.Type = PlayerIntentType.Dodge;

        return intent;
    }

    private Vector2 GetMoveInput()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        return new Vector2(h, v).normalized;
    }

    private Vector2 GetAimPosition()
    {
        if (aimCamera == null) return Vector2.zero;
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -aimCamera.transform.position.z;
        return aimCamera.ScreenToWorldPoint(mousePos);
    }
}
