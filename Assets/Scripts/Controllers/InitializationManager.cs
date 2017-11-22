#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InitializationManager
{
    private static Queue<Action> unityQueue = new Queue<Action>();
    private static int unityTotalCallbacks = 0;

    private static Queue<Action> threadQueue = new Queue<Action>();
    private static int threadTotalCallbacks = 0;

    private static object lockToken = new object();

    public static void EnqueueWork(Action work, bool canBeThreaded = false)
    {
        //work.Invoke();
        lock (lockToken)
        {
            if (canBeThreaded)
                threadQueue.Enqueue(work);
            else
                unityQueue.Enqueue(work);
        }
    }

    private static Action DequeueUnityWork()
    {
        lock (lockToken)
        {
            if (unityQueue.Count > 0)
                return unityQueue.Dequeue();
            else
                return null;
        }
    }

    public static void Initialize()
    {
        new Thread(() => StartThreads()).Start();
        new GameObject().AddComponent<UnityCallbackManager>();
    }

    private static void StartThreads()
    {
        while (threadQueue.Count > 0)
        {
            Action threadWork = threadQueue.Dequeue();
            ThreadPool.QueueUserWorkItem((object o) => threadWork());
        }
    }

    /// <summary>
    /// TODO: delete me
    /// </summary>
    public static void Profile(Action action, string name)
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        action.Invoke();
        UnityDebugger.Debugger.LogWarning("Function " + name + " performed in " + sw.ElapsedMilliseconds + " milliseconds");
    }

    private class UnityCallbackManager : MonoBehaviour
    {
        public void Start()
        {
            StartCoroutine(DoWork());
        }

        public IEnumerator DoWork()
        {
            // Busy waiting
            //for (int i = 0; i < 10; i++)
            //{
            //    yield return null;
            //}

            Action work = InitializationManager.DequeueUnityWork();
            while (work != null)
            {
                work.Invoke();
                yield return null;
                work = InitializationManager.DequeueUnityWork();
            }

            // Busy waiting
            for (int i = 0; i < 1000; i++)
            {
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
