﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//using SharedMemory;
using System.IO.Pipes;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using SharedMemory;
using log4net;
using MessageLibrary;

namespace TestClient
{
    public partial class Form1 : Form
    {
        private static readonly log4net.ILog log =
  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


       

        //const int BMWidth = 1280;
        //const int BMHeight = 1024;

       
        private int posX = 0;
        private int posY = 0;

        private Socket clientSocket;

        //  private Queue<MouseMessage> _sendEvents; 

        private SharedArray<byte> arr; //= new SharedArray<byte>("MainSharedMem");

        private Bitmap _texture;

        public string memfile= "MainSharedMem";

        public int port = 8885;

        public Form1()
        {
            InitializeComponent();
            this.pictureBox1.MouseWheel += PictureBox1_MouseWheel;

           Init();
        }

        public void Init()
        {



            string args = pictureBox1.Width.ToString() + " " + pictureBox1.Height.ToString()+" ";
            args = args + "http://www.google.ru"+" ";
            Guid memid = Guid.NewGuid();

            memfile = memid.ToString();
            args = args + memfile + " ";
           Random r=new Random();
            port = 8880 + r.Next(10);

         
            args = args + port.ToString();

            //MessageBox.Show(args);

            Process pluginProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WorkingDirectory =
                        @"D:\work\unity\StandaloneConnector\SharedPluginServer\SharedPluginServer\bin\x64\Debug",
                    FileName =
                        @"D:\work\unity\StandaloneConnector\SharedPluginServer\SharedPluginServer\bin\x64\Debug\SharedPluginServer.exe",
                    Arguments = args
                    
                }
            };
            pluginProcess.Start();
            //Thread.Sleep(10000);
            // while (!Process.GetProcesses().Any(p => p.Name == myName)) { Thread.Sleep(100); }

            bool found_proc = false;
            while (!found_proc)
            {
                foreach (Process clsProcess in Process.GetProcesses())
                    if (clsProcess.ProcessName == pluginProcess.ProcessName)
                        found_proc = true;

                Thread.Sleep(1000);
            }

            arr = new SharedArray<byte>(memfile);

            //Connect
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(ip, port));

            _texture = new Bitmap(pictureBox1.Width, pictureBox1.Width);

            Application.Idle += Application_Idle;
        }

       

        private void Application_Idle(object sender, EventArgs e)
        {
            //Render bitmap
           // MessageBox.Show("RENDER");
            byte[] _read = new byte[arr.Length];

            arr.CopyTo(_read, 0);

            
            Rectangle rect = new Rectangle(0, 0, pictureBox1.Width, pictureBox1.Height);
            BitmapData bmpData = _texture.LockBits(rect, ImageLockMode.WriteOnly, _texture.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(_read, 0, ptr, _read.Length);
            _texture.UnlockBits(bmpData);
            pictureBox1.Image = _texture;

        }

        private void button1_Click(object sender, EventArgs e)
        {

           
          /*  byte[] _read = new byte[arr.Length];

            arr.CopyTo(_read, 0);

            Bitmap bmp = new Bitmap(BMWidth, BMHeight);
            Rectangle rect = new Rectangle(0, 0, BMWidth, BMHeight);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(_read, 0, ptr, _read.Length);
            bmp.UnlockBits(bmpData);

            pictureBox1.Image = bmp;*/

            ////




          //  using (StreamWriter sw = new StreamWriter(pipeClient))
           // {
           //     sw.WriteLine("TEST PIPE SEND");
           // }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendShutdownEvent();
            Application.Idle -= Application_Idle;
            clientSocket.Close();
            
        }

        public void SendMouseEvent(MouseMessage msg)
        {
           // if (_MouseDone)
                //_controlMem.Write(ref msg,0);
           // _MouseDone = false;
            EventPacket ep = new EventPacket
            {
                Event = msg,
                Type = EventType.Mouse
            };

           MemoryStream mstr=new MemoryStream();
            BinaryFormatter bf=new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            clientSocket.Send(b);
            //  MessageBox.Show(_sendEvents.Count.ToString());
        }

        public void SendShutdownEvent()
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Shutdown,
                 GenericType = EventType.Generic
            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = EventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            clientSocket.Send(b);

        }

        public void SendNavigateEvent(string url)
        {
            GenericEvent ge = new GenericEvent()
            {
                Type = GenericEventType.Navigate,
                GenericType = EventType.Generic,
                NavigateUrl = url
            };

            EventPacket ep = new EventPacket()
            {
                Event = ge,
                Type = EventType.Generic
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            clientSocket.Send(b);
        }

        public void SendCharEvent(int character,KeyboardEventType type)
        {
            log.Info("____________CHAR EVENT:"+character);
            KeyboardEvent keyboardEvent = new KeyboardEvent()
            {
                Type = type,
                Key = character
            };
            EventPacket ep = new EventPacket()
            {
                Event = keyboardEvent,
                Type = EventType.Keyboard
            };

            MemoryStream mstr = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(mstr, ep);
            byte[] b = mstr.GetBuffer();
            clientSocket.Send(b);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            MouseMessage msg = new MouseMessage
            {
                Type=MouseEventType.ButtonDown,
                X=e.X,
                Y=e.Y,
                GenericType = EventType.Mouse
            };

            SendMouseEvent(msg);
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            //pictureBox1.Focus();
            MouseMessage msg = new MouseMessage
            {
                Type = MouseEventType.Wheel,
                X = e.X,
                Y = e.Y,
                Delta = e.Delta,
                GenericType = EventType.Mouse,
                Button = MouseButton.None
            };

            if(e.Button==MouseButtons.Left)
                msg.Button=MouseButton.Left;
            if (e.Button == MouseButtons.Right)
                msg.Button = MouseButton.Right;
            if (e.Button == MouseButtons.Middle)
                msg.Button = MouseButton.Middle;

            SendMouseEvent(msg);
        }

        //really spammy
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
           // MessageBox.Show("_______MOVE 1");
            if (posX != e.X || posY != e.Y)
            {
                MouseMessage msg = new MouseMessage
                {
                    Type = MouseEventType.Move,
                    X = e.X,
                    Y = e.Y,
                    GenericType = EventType.Mouse,
                     Delta = e.Delta,
                    Button = MouseButton.None
                };

                if (e.Button == MouseButtons.Left)
                    msg.Button = MouseButton.Left;
                if (e.Button == MouseButtons.Right)
                    msg.Button = MouseButton.Right;
                if (e.Button == MouseButtons.Middle)
                    msg.Button = MouseButton.Middle;
                posX = e.X;
                posY = e.Y;

                SendMouseEvent(msg);
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            MouseMessage msg = new MouseMessage
            {
                Type = MouseEventType.ButtonUp,
                X = e.X,
                Y = e.Y,
                GenericType = EventType.Mouse,
                Button = MouseButton.None
            };

            if (e.Button == MouseButtons.Left)
                msg.Button = MouseButton.Left;
            if (e.Button == MouseButtons.Right)
                msg.Button = MouseButton.Right;
            if (e.Button == MouseButtons.Middle)
                msg.Button = MouseButton.Middle;


            SendMouseEvent(msg);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
          
        }

        

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            SendCharEvent((int)e.KeyValue, KeyboardEventType.Down);
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            SendCharEvent((int)e.KeyChar, KeyboardEventType.CharKey);
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            SendCharEvent((int)e.KeyValue, KeyboardEventType.Up);
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            MouseMessage msg = new MouseMessage
            {
                Type = MouseEventType.Leave,
                X = 0,
                Y = 0,
                GenericType = EventType.Mouse
            };

            SendMouseEvent(msg);
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            pictureBox1.Focus();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
           // MessageBox.Show(textBox1.Text);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendNavigateEvent(textBox1.Text);
            }
        }

        //protected override void OnMouseWheel()
    }






    #region Utils
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }
    }

    // Contains the method executed in the context of the impersonated user
    public class ReadFileToStream
    {
        private string fn;
        private StreamString ss;

        public ReadFileToStream(StreamString str, string filename)
        {
            fn = filename;
            ss = str;
        }

        public void Start()
        {
            string contents = File.ReadAllText(fn);
            ss.WriteString(contents);
        }
    }
    #endregion
}
