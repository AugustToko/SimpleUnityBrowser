using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using MessageLibrary;
using SharedMemory;

using UnityEngine;

public class BrowserEngine
{
    private TcpClient _clientSocket;

    private SharedArray<byte> _mainTexArray;

    private System.Diagnostics.Process _pluginProcess;

    private static System.Object sPixelLock;

    public Texture2D BrowserTexture=null;
    public bool Initialized = false;

   

    
    //Image buffer
    private byte[] _bufferBytes = null;
    private long _arraySize = 0;

    //TCP buffer
    const int READ_BUFFER_SIZE = 2048;
    private byte[] readBuffer = new byte[READ_BUFFER_SIZE];

    #region Settings
    public int kWidth = 512;
    public int kHeight = 512;
    private string _sharedFileName;
    private int _port;
    private string _initialURL;
    #endregion

    #region Dialogs

    public delegate void JavaScriptDialog(string message, string prompt, DialogEventType type);

    public event JavaScriptDialog OnJavaScriptDialog;

    #endregion

    #region JSQuery

    public delegate void JavaScriptQuery(string message);

    public event JavaScriptQuery OnJavaScriptQuery;
    #endregion



    #region Init
    public void InitPlugin(int width,int height, string sharedfilename,int port,string initialURL)
    {
          
        Debug.Log(System.Reflection.Assembly.GetExecutingAssembly().Location);

#if UNITY_EDITOR_64
        string PluginServerPath = Application.dataPath + @"\PluginServer\x64";
#else
        //HACK
        string s = @"\web_browser_Data\Managed\Assembly-CSharp.dll";
        string AssemblyPath=System.Reflection.Assembly.GetExecutingAssembly().Location;
        AssemblyPath = AssemblyPath.Substring(0, AssemblyPath.Length - s.Length);
        string PluginServerPath=AssemblyPath+@"\PluginServer";
#endif

        Debug.Log("Starting server from:"+PluginServerPath);


        kWidth = width;
        kHeight = height;
        _sharedFileName = sharedfilename;
        _port = port;
        _initialURL = initialURL;

        if(BrowserTexture==null)
        BrowserTexture = new Texture2D(kWidth, kHeight, TextureFormat.BGRA32, false);
        sPixelLock = new object();


        string args = BuildParamsString();

        try
        {
            _pluginProcess = new System.Diagnostics.Process()
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo()
                {
                    WorkingDirectory = PluginServerPath,
                    FileName =PluginServerPath + @"\SharedPluginServer.exe",
                    Arguments = args

                }
            };



            _pluginProcess.Start();
        }
        catch (Exception ex)
        {
            //log the file
            Debug.Log("FAILED TO START SERVER FROM:"+ PluginServerPath + @"\SharedPluginServer.exe");
            throw;
        }
        


    }

    private string BuildParamsString()
    {
        string ret = kWidth.ToString() + " " + kHeight.ToString() + " ";
        ret = ret + _initialURL + " ";
        ret = ret + _sharedFileName + " ";
        ret = ret + _port.ToString();
        return ret;
    }

#endregion



#region SendEvents

    public void SendNavigateEvent(string url, bool back, bool forward)
    {
        GenericEvent ge = new GenericEvent()
        {
            Type = GenericEventType.Navigate,
            GenericType = MessageLibrary.BrowserEventType.Generic,
            NavigateUrl = url
        };

        if (back)
            ge.Type = GenericEventType.GoBack;
        else if (forward)
            ge.Type = GenericEventType.GoForward;

        EventPacket ep = new EventPacket()
        {
            Event = ge,
            Type = MessageLibrary.BrowserEventType.Generic
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }
    }

    public void SendShutdownEvent()
    {
        GenericEvent ge = new GenericEvent()
        {
            Type = GenericEventType.Shutdown,
            GenericType = MessageLibrary.BrowserEventType.Generic
        };

        EventPacket ep = new EventPacket()
        {
            Event = ge,
            Type = MessageLibrary.BrowserEventType.Generic
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }

    }

    public void SendDialogResponse(bool ok,string dinput)
    {
        DialogEvent de = new DialogEvent()
        {
            GenericType = MessageLibrary.BrowserEventType.Dialog,
            success = ok,
            input = dinput
        };

        EventPacket ep = new EventPacket
        {
            Event = de,
            Type = MessageLibrary.BrowserEventType.Dialog
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();

        //
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }

    }

    public void SendQueryResponse(string response)
    {
        GenericEvent ge = new GenericEvent()
        {
            Type = GenericEventType.JSQueryResponse,
            GenericType = BrowserEventType.Generic,
            JsQueryResponse = response
        };

        EventPacket ep = new EventPacket()
        {
            Event = ge,
            Type = BrowserEventType.Generic
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();
        //
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }
    }

    public void SendCharEvent(int character, KeyboardEventType type)
    {
        
        KeyboardEvent keyboardEvent = new KeyboardEvent()
        {
            Type = type,
            Key = character
        };
        EventPacket ep = new EventPacket()
        {
            Event = keyboardEvent,
            Type = MessageLibrary.BrowserEventType.Keyboard
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }
    }

    public void SendMouseEvent(MouseMessage msg)
    {
       
        EventPacket ep = new EventPacket
        {
            Event = msg,
            Type = MessageLibrary.BrowserEventType.Mouse
        };

        MemoryStream mstr = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(mstr, ep);
        byte[] b = mstr.GetBuffer();
        lock (_clientSocket.GetStream())
        {
            _clientSocket.GetStream().Write(b, 0, b.Length);
        }

    }



#endregion

    

    public void UpdateTexture()
    {
        if (Initialized)
        {


            if (_bufferBytes == null)
            {
                long arraySize = _mainTexArray.Length;
                Debug.Log("Memory array size:"+arraySize);
                _bufferBytes = new byte[arraySize];
            }
            _mainTexArray.CopyTo(_bufferBytes, 0);

            lock (sPixelLock)
            {
                BrowserTexture.LoadRawTextureData(_bufferBytes);
                BrowserTexture.Apply();
            }



        }
        else
        {
            try
            {
                string processName = _pluginProcess.ProcessName;//could be InvalidOperationException
                foreach (System.Diagnostics.Process clsProcess in System.Diagnostics.Process.GetProcesses())
                    if (clsProcess.ProcessName == processName) 
                    {
                        Thread.Sleep(200); //give it some time to initialize
                        try
                        {
                            _mainTexArray = new SharedArray<byte>(_sharedFileName);
                            //Connect
                            _clientSocket = new TcpClient("127.0.0.1", _port);
                            //start listen
                            _clientSocket.GetStream().BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
                            Initialized = true;
                        }
                        catch (Exception)
                        {
                            //SharedMem and TCP exceptions
                            
                        }



                    }
            }
            catch (Exception)
            {
                
                //InvalidOperationException
            }
            
        }
    }

    //Receiver
    private void StreamReceiver(IAsyncResult ar)
    {
        int BytesRead;

        try
        {
            // Ensure that no other threads try to use the stream at the same time.
            lock (_clientSocket.GetStream())
            {
                // Finish asynchronous read into readBuffer and get number of bytes read.
                BytesRead = _clientSocket.GetStream().EndRead(ar);
            }
            MemoryStream mstr = new MemoryStream(readBuffer);
            BinaryFormatter bf = new BinaryFormatter();
            EventPacket ep = bf.Deserialize(mstr) as EventPacket;
            if (ep != null)
            {
                //main handlers
                if (ep.Type == MessageLibrary.BrowserEventType.Dialog)
                {
                    DialogEvent dev = ep.Event as DialogEvent;
                    if (dev != null)
                    {
                        if (OnJavaScriptDialog != null)
                            OnJavaScriptDialog(dev.Message, dev.DefaultPrompt, dev.Type);
                    }
                }
                if (ep.Type == BrowserEventType.Generic)
                {
                    GenericEvent ge = ep.Event as GenericEvent;
                    if (ge != null)
                    {
                        if (ge.Type == GenericEventType.JSQuery)
                        {
                            if (OnJavaScriptQuery != null)
                                OnJavaScriptQuery(ge.JsQuery);
                        }
                    }
                }
            }
            lock (_clientSocket.GetStream())
            {
                // Start a new asynchronous read into readBuffer.
                _clientSocket.GetStream()
                    .BeginRead(readBuffer, 0, READ_BUFFER_SIZE, new AsyncCallback(StreamReceiver), null);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error reading from socket,waiting for plugin server to start...");
        }
    }


    public void Shutdown()
    {
        SendShutdownEvent();
        _clientSocket.Close();
    }
}