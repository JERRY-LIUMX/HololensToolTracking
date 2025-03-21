using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;
using System;

public class ZMQSubscriberHelper : MonoBehaviour
{
    public TMP_Text IP, Port, Topic;
    public float Frequency = 3;

    public (string[],string[]) GetSubscriberInfo()
    {
        string[] SockerSettings = new string[2] { "10.0.0.41", "5589" };
        string[] Topics = Topic.text[..^1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
/*  
        string[] info = new string[SockerSettings.Length + Topics.Length];
        SockerSettings.CopyTo(info, 0);
        Topics.CopyTo(info, SockerSettings.Length);*/
        return (SockerSettings, Topics);
    }
}
