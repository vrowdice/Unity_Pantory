/// <summary>
/// 연산 방식입니다.
/// </summary>
public enum ModifierType
{
    Flat,       // 합연산 (Base + Value)
    PercentAdd, // 합연산 퍼센트 (Base * (1 + Value + Value...))
    PercentMult // 곱연산 퍼센트 (Base * Value * Value...)
}