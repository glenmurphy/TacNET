using System;
using System.Collections.Generic;
using UnityEngine;

public class Speech : MonoBehaviour
{
  // config
  public static float overlap = 0.42f;

  // internal
  AudioSource[] sources;
  int currentSource = 0;

  public enum Call
  {
    NONE,
    BANDIT,
    CLOSE,
    COLD,
    FLANKING,
    HIGH,
    HOT,
    INCOMING,
    INCOMING_MISSILE,
    LOW,
    MILES5,
    MILES10,
    MILES15,
    MILES20,
    MISSILE,
    OCLOCK12,
    OCLOCK1,
    OCLOCK2,
    OCLOCK3,
    OCLOCK4,
    OCLOCK5,
    OCLOCK6,
    OCLOCK7,
    OCLOCK8,
    OCLOCK9,
    OCLOCK10,
    OCLOCK11,
  };
  Dictionary<Call, AudioClip> clips = new Dictionary<Call, AudioClip>();
  List<Call> queue = new List<Call>();

  void Awake()
  {
    Debug.Log("Starting");
    clips[Call.BANDIT] = Resources.Load<AudioClip>("speech-bandit");
    clips[Call.COLD] = Resources.Load<AudioClip>("speech-cold");
    clips[Call.FLANKING] = Resources.Load<AudioClip>("speech-flanking");
    clips[Call.HIGH] = Resources.Load<AudioClip>("speech-high");
    clips[Call.HOT] = Resources.Load<AudioClip>("speech-hot");
    clips[Call.INCOMING] = Resources.Load<AudioClip>("speech-incoming");
    clips[Call.INCOMING_MISSILE] = Resources.Load<AudioClip>("speech-incoming missile");
    clips[Call.LOW] = Resources.Load<AudioClip>("speech-low");
    clips[Call.MISSILE] = Resources.Load<AudioClip>("speech-missile");

    clips[Call.CLOSE] = Resources.Load<AudioClip>("speech-close");
    clips[Call.MILES5] = Resources.Load<AudioClip>("speech-five miles");
    clips[Call.MILES10] = Resources.Load<AudioClip>("speech-ten miles");
    clips[Call.MILES15] = Resources.Load<AudioClip>("speech-fifteen miles");
    clips[Call.MILES20] = Resources.Load<AudioClip>("speech-twenty miles");

    clips[Call.OCLOCK1] = Resources.Load<AudioClip>("speech-one oclock");
    clips[Call.OCLOCK2] = Resources.Load<AudioClip>("speech-two oclock");
    clips[Call.OCLOCK3] = Resources.Load<AudioClip>("speech-three oclock");
    clips[Call.OCLOCK4] = Resources.Load<AudioClip>("speech-four oclock");
    clips[Call.OCLOCK5] = Resources.Load<AudioClip>("speech-five oclock");
    clips[Call.OCLOCK6] = Resources.Load<AudioClip>("speech-six oclock");
    clips[Call.OCLOCK7] = Resources.Load<AudioClip>("speech-seven oclock");
    clips[Call.OCLOCK8] = Resources.Load<AudioClip>("speech-eight oclock");
    clips[Call.OCLOCK9] = Resources.Load<AudioClip>("speech-nine oclock");
    clips[Call.OCLOCK10] = Resources.Load<AudioClip>("speech-ten oclock");
    clips[Call.OCLOCK11] = Resources.Load<AudioClip>("speech-eleven oclock");
    clips[Call.OCLOCK12] = Resources.Load<AudioClip>("speech-twelve oclock");
    
    sources = GetComponents<AudioSource>();
  }

  void Update()
  {
    if (queue.Count == 0) {
      return;
    }

    if (sources[currentSource].isPlaying == false)
    {
      PlayNext();
    }
    else if (sources[currentSource].time > sources[currentSource].clip.length - overlap)
    {
      currentSource = ( currentSource + 1 ) % sources.Length;
      PlayNext();
    }
  }

  void PlayNext() {
    sources[currentSource].clip = clips[queue[0]];
    sources[currentSource].Play();
    queue.RemoveAt(0);
  }

  public Call GetDistanceCall(float distance) {
    if (distance < 5) return Call.CLOSE;
    if (distance < 8) return Call.MILES5;
    if (distance < 13) return Call.MILES10;
    if (distance < 18) return Call.MILES15;
    if (distance < 26) return Call.MILES20;
    return Call.NONE;
  }

  public Call GetBearingCall(float bearing) {
    bearing = bearing % 360;
    if (bearing <= 15 || bearing >= 345) return Call.OCLOCK12;
    if (bearing < 45) return Call.OCLOCK1;
    if (bearing < 75) return Call.OCLOCK2;
    if (bearing < 105) return Call.OCLOCK3;
    if (bearing < 135) return Call.OCLOCK4;
    if (bearing < 165) return Call.OCLOCK5;
    if (bearing < 195) return Call.OCLOCK6;
    if (bearing < 225) return Call.OCLOCK7;
    if (bearing < 255) return Call.OCLOCK8;
    if (bearing < 285) return Call.OCLOCK9;
    if (bearing < 315) return Call.OCLOCK10;
    if (bearing < 345) return Call.OCLOCK11;
    return Call.NONE;
  }

  // Really need to check this!
  public Call GetAspectCall(float aspect)
  {
    if (aspect < 25 || aspect > 335) return Call.HOT;
    if (aspect > 120 && aspect < 220) return Call.COLD;
    return Call.FLANKING;
  }

  public void Say(Call[] calls)
  {
    Debug.Log(calls);
    sources[currentSource].Stop();
    queue.Clear();
    for (int i = 0; i < calls.Length; i++) {
      if (clips.ContainsKey(calls[i]))
        queue.Add(calls[i]);
      else
        Debug.Log("Invalid call " + calls[i]);
    }
  }
}