using UnityEngine;

public class PlayerInputReader : MonoBehaviour, IPlayerIntentProvider
{
    [SerializeField] private Camera aimCamera;

    [Header("键位映射（默认与原行为一致）")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
    [SerializeField] private KeyCode dodgeKeyAlt = KeyCode.Mouse1;
    [SerializeField] private KeyCode skillKey = KeyCode.Q;
    [SerializeField] private int attackMouseButton = 0;

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }
    }

    public PlayerIntent GetIntent()
    {
        var intent = new PlayerIntent
        {
            MoveDirection = GetMoveInput(),
            AimPosition = GetAimPosition(),
            IsSprint = Input.GetKey(sprintKey),
            AttackPressed = Input.GetMouseButtonDown(attackMouseButton),
            DodgePressed = Input.GetKeyDown(dodgeKey) || Input.GetKeyDown(dodgeKeyAlt),
            SkillPressed = Input.GetKeyDown(skillKey)
        };

        if (intent.MoveDirection.sqrMagnitude > 0.001f)
        {
            intent.Type = PlayerIntentType.Move;
        }

        if (intent.AttackPressed)
        {
            intent.Type = PlayerIntentType.Attack;
        }

        if (intent.SkillPressed)
        {
            intent.Type = PlayerIntentType.Skill;
        }

        if (intent.DodgePressed)
        {
            intent.Type = PlayerIntentType.Dodge;
        }

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
        if (aimCamera == null)
        {
            return Vector2.zero;
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -aimCamera.transform.position.z;
        return aimCamera.ScreenToWorldPoint(mousePos);
    }
}