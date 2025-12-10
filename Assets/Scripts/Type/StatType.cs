/// <summary>
/// 적용할 수 있는 스탯의 종류입니다.
/// </summary>
public enum StatType
{
    // [핵심] 스레드(공장) 관련
    ThreadWorkRateFlat,      // 작업률 고정 합연산 (자동화의 핵심! 직원이 없어도 이 수치만큼 오름)
    ThreadWorkRateMult,      // 작업률 % 곱연산 (전체 속도 증폭)
    
    // 생산/소비 관련
    ProductionEfficiency,    // 생산량 뻥튀기
    ResourceConsumption,     // 자원 소모량 감소 (음수 사용)
    
    // 경영 관련
    GlobalSpeedMultiplier,   // 게임 전체 속도 (무한 연구 등)
    FactoryMaintenanceCost,  // 공장 유지비
}