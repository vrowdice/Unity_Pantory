using System;
using System.Collections;
using UnityEngine;

public static class StaggeredSpawnUtils
{
    public static void Stop(MonoBehaviour host, ref Coroutine runningCoroutine)
    {
        if (runningCoroutine == null)
            return;

        host.StopCoroutine(runningCoroutine);
        runningCoroutine = null;
    }

    public static void Restart(MonoBehaviour host, ref Coroutine runningCoroutine, IEnumerator routine)
    {
        Stop(host, ref runningCoroutine);
        runningCoroutine = host.StartCoroutine(routine);
    }

    public static IEnumerator ForEachFrame(int count, Action<int> action)
    {
        for (int i = 0; i < count; i++)
        {
            action(i);
            yield return null;
        }
    }
}
