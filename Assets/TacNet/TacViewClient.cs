using System;
using System.Text; // Encoding
using System.IO;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;
using DamienG.Security.Cryptography;

public class OnMessageEventArgs : EventArgs
{
    public string str { get; set; }
}

class TacViewClient
{
  private TcpClient socket;
  string host;
  int port;
  string password;
  public event EventHandler<OnMessageEventArgs> OnMessage;
  public event EventHandler<EventArgs> OnError;
  public event EventHandler<EventArgs> OnConnect;

  public TacViewClient(string hostIn, int portIn, string passwordIn)
  {
    host = hostIn;
    port = portIn;
    password = passwordIn;
    Connect();
  }

  public void Connect()
  {
    try
    {
      Thread clientThread = new Thread(new ThreadStart(Listen));
      clientThread.IsBackground = true;
      clientThread.Start();
    }
    catch (Exception e)
    {
      OnError?.Invoke(this, new EventArgs());
      Debug.Log(e);
    }
  }

  OnMessageEventArgs args = new OnMessageEventArgs(); // mem
  private void HandleLine(string line) {
    if (line == "Tacview.RealTimeTelemetry.0")
      Login();
    
    args.str = line;
    OnMessage?.Invoke(this, args);
  }

  private void Listen()
  {
    try
    {
      socket = new TcpClient(host, port);
      OnConnect?.Invoke(this, new EventArgs());
      NetworkStream stream = socket.GetStream();
      StreamReader reader = new StreamReader(stream);
      string line;
      while ((line = reader.ReadLine()) != null)
      {
        HandleLine(line);
      }
      OnError?.Invoke(this, new EventArgs());
    }
    catch (Exception e)
    {
      // this is where we might want to reshow the login panel
      OnError?.Invoke(this, new EventArgs());
      Debug.Log(e);
    }
  }

  private string HashPassword(string pass)
  {
    if (String.IsNullOrEmpty(pass))
      return "0";
    
    Crc32 crc32 = new Crc32();
    byte[] passwordBytes = Encoding.Unicode.GetBytes(pass);
    string hash = String.Empty;
    foreach (byte b in crc32.ComputeHash(passwordBytes))
      hash += b.ToString("x2").ToLower();
    return hash;
  }

  private void Login()
  {
    Debug.Log("Logging in");
    try
    {
      // Get a stream object for writing. 			
      NetworkStream stream = socket.GetStream();
      if (stream.CanWrite)
      {
        string clientMessage = "XtraLib.Stream.0\n" +
                                "Tacview.RealTimeTelemetry.0\n" +
                                "TacDAR\n" + 
                                HashPassword(password) + "\0";
        Debug.Log(clientMessage);
        byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
        stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
      } else {
        Debug.Log("Could not write");
      }
    }
    catch (Exception e)
    {
      OnError?.Invoke(this, new EventArgs());
      Debug.Log(e);
    }
  }
}