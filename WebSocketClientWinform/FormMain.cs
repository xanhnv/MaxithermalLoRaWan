using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UDPServerAndWebSocketClient.Model;
using WebSocketSharp;

namespace UDPServerAndWebSocketClient
{
    public partial class FormMain : Form
    {
        
        public FormMain()
        {
            InitializeComponent();
        }
        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        
        DataProcessing processingData = new DataProcessing();
        //string url = "";
        string url = "wss://ap1.loriot.io/app?token=vnoDsAAAAA1hcDEubG9yaW90LmlvGfvbc8XPSeYshxoIT-RsUg==";
        //string url = "wss://echo.websocket.org";
       // WebSocket wsClient;
        private void Form1_Load(object sender, EventArgs e)
        {
           Utilities.WriteLog(DateTime.Now.ToString() + ": On form load \r\n");
            #region Create web socket

            
            ////Create web socket
            //wsClient = new WebSocket(url);
            ////wsClient.Send("Hi I am websocket client.");

            //wsClient.OnOpen += WsClient_OnOpen;

            //wsClient.OnMessage += WsClient_OnMessage;
            //wsClient.OnClose += WsClient_OnClose;

            //wsClient.OnError += WsClient_OnError;
            //wsClient.Connect();
            #endregion
            //init UDP Socket
            UDPSocket s = new UDPSocket(this);
            s.Server(17000);
            LoadDB();
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (registryKey.GetValue("MaxithermalLoraWebSocket")==null)
            {
                registryKey.SetValue("MaxithermalLoraWebSocket", Application.ExecutablePath.ToString());
            }
            
        }

        void LoadDB()
        {
            var db = new Pexo63LorawanEntities();
            var data = (from d in db.Realtimes select d);
            dataGridView1.DataSource = data.ToList();
        }
        public void RefreshGridView()
        {
            if (dataGridView1.InvokeRequired)
            {
                dataGridView1.Invoke((MethodInvoker)delegate ()
                {
                    RefreshGridView();
                });
            }
            else
            {
                dataGridView1.DataSource = null;
                LoadDB();
            }
              
        }

        private void WsClient_OnError(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine("On Error");
            Utilities.WriteLog(DateTime.Now.ToString() + ": On Error \r\n");
           // (sender as WebSocket).Connect();
            Application.Restart();
            Environment.Exit(0);

        }

        private void WsClient_OnClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine("On close");
            Utilities.WriteLog(DateTime.Now.ToString() + ": On close \r\n");
            Application.Restart();
            Environment.Exit(0);
        }

        private void WsClient_OnMessage(object sender, MessageEventArgs e)
        {

            JObject json = JObject.Parse(e.Data);
            if (json["cmd"].ToString() == "rx")
            {
                if (json.ContainsKey("data"))
                {
                    //Console.WriteLine("On Message RX: " + e.Data);
                    // Console.WriteLine(json["data"].ToString());
                    byte[] data = processingData.StringToByteArray(json["data"].ToString());
                    string serialno = (data[4] * Math.Pow(2, 16) + data[3] * Math.Pow(2, 8) + data[2]).ToString("00000");
                    string serialNumber = (char)(data[5]) + data[1].ToString("00") + data[0].ToString("00") + serialno;
                    string packetType = Encoding.ASCII.GetString(data, 6, 2);
                    // Console.WriteLine("Serial: " + serialNumber + " Type: " + packetType);

                    switch (packetType)
                    {
                        case "D1":///Realtime data
                            processingData.GetRealtimeD0(data, serialNumber);
                            break;
                        case "S1": //Setting 1
                            processingData.GetSeting1(data, serialNumber);
                            break;
                        case "S2"://Setting 2
                            processingData.GetSeting2(data, serialNumber);
                            break;
                        case "S3"://Setting 3
                            processingData.GetSeting3(data, serialNumber);
                            break;
                        case "AL"://Alarm packet
                            processingData.GetAlarmData(data, serialNumber);
                            break;
                    }
                    RefreshGridView();
                }
            }
        }
       
        private void WsClient_OnOpen(object sender, EventArgs e)
        {
            Console.WriteLine("On Open");
            Utilities.WriteLog(DateTime.Now.ToString() + ": On open \r\n");

        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void FormMain_MinimumSizeChanged(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(3000);
            this.ShowInTaskbar = false;
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //this.WindowState = FormWindowState.Minimized;
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(2000);
                this.ShowInTaskbar = false;
            }
        }
    }
}
