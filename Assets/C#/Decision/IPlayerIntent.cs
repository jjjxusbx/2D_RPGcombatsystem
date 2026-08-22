using UnityEngine;

public enum PlayerIntentType
{
    None,
    Move,
    Attack,
    Dodge,
    Skill,
    Aim
}

public struct PlayerIntent
{
    public PlayerIntentType Type;
    public Vector2 MoveDirection;
    public Vector2 AimPosition;
    public bool IsSprint;
    public bool AttackPressed;
    public bool DodgePressed;
    public bool SkillPressed;
}

public interface IPlayerIntentProvider
{
    PlayerIntent GetIntent();
}
