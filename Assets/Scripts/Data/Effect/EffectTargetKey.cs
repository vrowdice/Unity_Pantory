using System;

/// <summary>
/// 이펙트 대상을 식별하는 복합 키 구조체
/// 딕셔너리 키로 사용하여 3중 딕셔너리를 2중으로 줄입니다.
/// </summary>
public struct EffectTargetKey : IEquatable<EffectTargetKey>
{
    public readonly EffectTargetType Type;
    public readonly string Id;

    public EffectTargetKey(EffectTargetType type, string id)
    {
        Type = type;
        Id = id ?? string.Empty;
    }

    public bool Equals(EffectTargetKey other)
    {
        return Type == other.Type && Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is EffectTargetKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (Type, Id).GetHashCode();
    }

    public static bool operator ==(EffectTargetKey left, EffectTargetKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EffectTargetKey left, EffectTargetKey right)
    {
        return !left.Equals(right);
    }
}
