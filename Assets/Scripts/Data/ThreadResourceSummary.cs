using System;
using System.Collections.Generic;

/// <summary>
/// Thread가 소비(인풋) 및 생산(아웃풋)하는 자원 정보를 집계한 데이터 구조.
/// </summary>
[Serializable]
public class ThreadResourceSummary
{
    public string ThreadId { get; }
    public Dictionary<string, int> InputResources { get; }
    public Dictionary<string, int> OutputResources { get; }

    public ThreadResourceSummary(string threadId, Dictionary<string, int> inputResources, Dictionary<string, int> outputResources)
    {
        ThreadId = threadId;
        InputResources = inputResources ?? new Dictionary<string, int>();
        OutputResources = outputResources ?? new Dictionary<string, int>();
    }
}

