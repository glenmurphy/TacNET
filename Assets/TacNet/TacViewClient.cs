using System;
using System.Text; // Encoding
using System.IO;
using System.Threading;
using System.Net.Sockets;
using UnityEngine;

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
      Debug.Log(e);
    }
  }

  private void HandleLine(string line) {
    if (line == "[host]")
      Login();
    
    OnMessageEventArgs args = new OnMessageEventArgs();
    args.str = line;
    OnMessage?.Invoke(this, args);
  }

  private void Listen()
  {
    try
    {
      socket = new TcpClient(host, port);
      while (true)
      {
        NetworkStream stream = socket.GetStream();
        StreamReader reader = new StreamReader(stream);
        string line;
        while ((line = reader.ReadLine()) != null) 
        {
          HandleLine(line);
        }
      }
    }
    catch (Exception e)
    {
      Debug.Log(e);
    }
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
                                Crc64.Compute(password) + "\0";
        byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
        stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
      } else {
        Debug.Log("Could not write");
      }
    }
    catch (Exception e)
    {
      Debug.Log(e);
    }
  }
}