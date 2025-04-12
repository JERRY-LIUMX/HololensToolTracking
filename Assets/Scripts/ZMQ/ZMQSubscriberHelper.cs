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
        string[] SockerSettings = new string[2] { "192.168.0.104", "5587" };
        // string[] Topics = Topic.text[..^1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        string[] Topics = new string[3] {"Probe","Anatomy","StaticRef"};
/*  
        string[] info = new string[SockerSettings.Length + Topics.Length];
        SockerSettings.CopyTo(info, 0);
        Topics.CopyTo(info, SockerSettings.Length);*/
        return (SockerSettings, Topics);
    }
}
