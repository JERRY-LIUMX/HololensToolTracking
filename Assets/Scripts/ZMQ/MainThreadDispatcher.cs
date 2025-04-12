using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<System.Action> ExecutionQueue = new Queue<System.Action>();
    private static MainThreadDispatcher _instance;
    private const int MaxActionsPerFrame = 120;

    public static void Enqueue(System.Action action)
    {
        if (action == null) return;

        lock (ExecutionQueue)
        {
            ExecutionQueue.Enqueue(action);
        }
    }

    void Update()
    {
        int actionsProcessed = 0;

        while (actionsProcessed < MaxActionsPerFrame)
        {
            System.Action action = null;

            lock (ExecutionQueue)
            {
                if (ExecutionQueue.Count == 0)
                    break;

                action = ExecutionQueue.Dequeue();
            }

            action?.Invoke();
            actionsProcessed++;
        }
    }
}
