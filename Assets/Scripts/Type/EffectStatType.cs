/// <summary>
/// 적용할 수 있는 스탯의 종류입니다.
/// </summary>
public enum EffectStatType
{
    //생산 및 자원 관련
    Thread_Efficiency_Mult,        // 생산 효율 (결과물 뻥튀기)
    Thread_ResourceCost_Mult,      // 자원 소모 배율 (소모량 감소/증가)
    Thread_Maintenance_Flat,     // 유지비 고정값
    Thread_Maintenance_Mult,     // 유지비 배율 (예: 노후화로 인한 유지비 증가)

    //개별/전체 직원 상태 관련
    Employee_Satisfaction_Per,      // 만족도 수치 가감 (일일 변동량에 합산)
    Employee_Efficiency_Flat,       // 개별 직원의 기본 숙련도 보너스
    Employee_Efficiency_Mult,       // 직원의 최종 효율 배율 (컨디션 등)

    //시스템 전체 관련
    GameSpeed_Mult,       // 전체 게임 진행 속도
    Revenue_Mult,         // 전체 매출 배율 (세금이나 상업 보너스)
}