using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// My solution to unity threadsafe shenangegans. Not sure how to test
class SyncList<T> { 
  private List<T> _list = new List<T>();
  private object _sync = new object();
  public void Push(T value) {
    lock (_sync) {
      _list.Add(value);
    }
  }
  public bool Pull(ref T output) {
    lock (_sync) {
      if (_list.Count == 0)
        return false;
      output = _list[0];
      _list.RemoveAt(0);
      return true;
    }
  }
}

public class World : MonoBehaviour
{
  // User config
  public GameObject entityPrefab;
  public GameObject sunlightObject;
  public MainUI mainUI;

  // Internal variables
  private DateTime baseTime;
  private DateTime currentTime;
  public Pos basePos; // origin for tacview transforms; we also use it as the 0,0 of the map
  public Dictionary<string, Entity> entities;
  SyncList<string> messageQueue = new SyncList<string>();

  private static string CLIENT_ERROR = "!CLIENTERROR!";
  private static string CLIENT_CONNECTED = "!CLIENTCONNECTED!";

  public void Start()
  {
    basePos = new Pos(0, 0, 0);
    entities = new Dictionary<string, Entity>();
  }

  public bool Login(string host, string port, string password)
  {
    int portInt;
    try {
      portInt = (String.IsNullOrEmpty(port)) ? 42674 : Int32.Parse(port);
    } catch(Exception e) {
      portInt = 42674;
      Debug.Log(e);
    }

    TacViewClient client = new TacViewClient(host, portInt, password);
    client.OnMessage += ClientMessage;
    client.OnError += ClientError;
    client.OnConnect += ClientConnected;
    return true;
  }

  // Called on the network thread!
  public void ClientConnected(object sender, EventArgs e)
  {
    messageQueue.Push(CLIENT_CONNECTED);
  }

  // Called on the network thread!
  public void ClientMessage(object sender, OnMessageEventArgs e)
  {
    messageQueue.Push(e.str);
  }
  
  // Called on the network thread!
  public void ClientError(object sender, EventArgs e)
  {
    messageQueue.Push(CLIENT_ERROR);
  }

  public void Update() {
    string str = "";
    while (messageQueue.Pull(ref str)) {
      Parse(str);
    }
  }

  public void Parse(string str)
  {
    if (str == CLIENT_ERROR) {
      mainUI.HandleClientError();
      return;
    }
    if (str == CLIENT_CONNECTED) {
      mainUI.HandleClientConnected();
      return;
    }

    switch (str[0]) {
      case '#':
        OffsetTime(str.Substring(1));
        return;
      case '-':
        RemoveEntity(str.Substring(1));
        return;
      case '/':
        return;
    }

    string[] c = str.Split(',');

    if (c[0] == "0") {
      // Global update
      string[] d = c[1].Split('=');
      string type = d[0];
      string data = d[1];

      switch(type) {
        case "ReferenceTime":
          SetRefTime(data);
          break;
        case "ReferenceLongitude":
          SetRefLong(data);
          break;
        case "ReferenceLatitude":
          SetRefLat(data);
          break;
      }
    } else {
      // Entity update
      string id = c[0];

      for (int i = 1; i < c.Length; i++) {
        string[] d = c[i].Split('=');
        string type = d[0];
        string[] data = new string[0];
        if (d.Length > 1)
          data = d[1].Split('|');
        UpdateEntity(id, type, data);
      }
    }
  }

  Dictionary<string, float> res = new Dictionary<string, float>(); // mem
  private Dictionary<string, float> ParseTransform(string[] data) {
    // This is a straight port from JS, so probably not as correct as
    // a proper C# way
    res.Clear();
  
    if (!String.IsNullOrEmpty(data[0])) res.Add("lon", basePos.lon + float.Parse(data[0]));
    if (!String.IsNullOrEmpty(data[1])) res.Add("lat", basePos.lat + float.Parse(data[1]));
    if (!String.IsNullOrEmpty(data[2])) res.Add("alt", float.Parse(data[2]));

    if (data.Length == 5)
    {
      if (!String.IsNullOrEmpty(data[3])) res.Add("u", float.Parse(data[3]));
      if (!String.IsNullOrEmpty(data[4])) res.Add("v", float.Parse(data[4]));
    }
    else if (data.Length == 6)
    {
      if (!String.IsNullOrEmpty(data[3])) res.Add("roll", float.Parse(data[3]));
      if (!String.IsNullOrEmpty(data[4])) res.Add("pitch", float.Parse(data[4]));
      if (!String.IsNullOrEmpty(data[5])) res.Add("yaw", float.Parse(data[5]));
    }
    else if (data.Length == 9)
    {
      if (!String.IsNullOrEmpty(data[3])) res.Add("roll", float.Parse(data[3]));
      if (!String.IsNullOrEmpty(data[4])) res.Add("pitch", float.Parse(data[4]));
      if (!String.IsNullOrEmpty(data[5])) res.Add("yaw", float.Parse(data[5]));
      if (!String.IsNullOrEmpty(data[6])) res.Add("u", float.Parse(data[6]));
      if (!String.IsNullOrEmpty(data[7])) res.Add("v", float.Parse(data[7]));
      if (!String.IsNullOrEmpty(data[8])) res.Add("heading", float.Parse(data[8]));
    }

    return res;
  }

  private void CreateEntity(string id)
  {
    GameObject entity = Instantiate(entityPrefab);
    Entity e = entity.GetComponent<Entity>();
    entities.Add(id, e);
    e.SetID(id);
  }

  private void UpdateEntity(string id, string type, string[] data) {
    if (!entities.ContainsKey(id))
      CreateEntity(id);
    
    Entity e = entities[id];

    switch (type) {
      case "T":
        Dictionary<string, float> t = ParseTransform(data);
        e.SetTransform(t, currentTime);
        break;
      case "Type":
        string entityType = data[0];
        if (entityType.Length == 0) return;

        e.SetType(entityType);
        //this.index.add(entityType, id, e);
        break;
      case "Name":
        e.SetName(data[0]);
        break;
      case "Color":
        e.SetColor(data[0]);
        break;
      case "Coalition":
        e.SetCoalition(data[0]);
        break;
      case "Pilot":
        string pilot = data[0];
        e.SetPilot(pilot);
        //this.index.add('pilots', pilot, e);
        break;
    }
  }

  private void SetRefTime(String timeString) {
    baseTime = DateTime.Parse(timeString);
    currentTime = baseTime;
  }

  private void SetRefLong(String lon) {
    basePos.lon = float.Parse(lon);
  }

  private void SetRefLat(String lat) {
    basePos.lat = float.Parse(lat);
  }

  public void OffsetTime(string offsetString) {
    currentTime = baseTime.AddMilliseconds(float.Parse(offsetString) * 1000);
    //Debug.Log(currentTime);
  }
  
  public void RemoveEntity(string id) {
    Destroy(entities[id].gameObject, 0.5f);
    entities.Remove(id);
  }
}
