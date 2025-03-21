using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DataBuffer : MonoBehaviour
{
    public Dictionary<string, byte[]> topicMsg = new Dictionary<string, byte[]> { };
    private byte[] tmp;

    public void UpdateOrAddMessage(string topic, byte[] msg)
    {
        if (topicMsg.ContainsKey(topic))
        {
            topicMsg[topic] = msg;
        }
        else
        {
            topicMsg.Add(topic, msg);
        }
    }

    public byte[] GetMessage(string topic)
    {
        if (topicMsg.TryGetValue(topic, out byte[] msg))
        {
            return msg;
        }
        return null;
    }

    public byte[] PopMessage(string topic)
    {
        if (topicMsg.TryGetValue(topic, out byte[] msg))
        {
            topicMsg.Remove(topic);
            return msg;
        }
        return null;
    }

    public KeyValuePair<string, byte[]> Pop()
    {
        if (topicMsg.Count > 0)
        {
            var firstItem = topicMsg.First();
            topicMsg.Remove(firstItem.Key);
            return firstItem;
        }
        return new KeyValuePair<string, byte[]>(null, null);
    }
}