using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UDPServerAndWebSocketClient
{

    public class UDPSocket
    {
        private Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 8 * 1024;
        private State state = new State();
        private EndPoint epSender = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        byte[] byteData = new byte[bufSize];

        FormMain form1;
        List<byte[]> listDev = new List<byte[]>();
        static string nwKey = "11E6FD2D9AC364C607B02E32049C586D";
        static string appKey = "50AECEA6ADE24F911E3028504B0F6E28";
        Pexo63LorawanEntities db = new Pexo63LorawanEntities();
        Setting settings = new Setting();
        Alarm alarms = new Alarm();
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public UDPSocket(FormMain form1)
        {
            this.form1 = form1;
        }
        UDPSocket()
        {

        }

        public void Server(int port)
        {
            InitAdd();
            //serverSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            //Assign the any IP of the machine and listen on port number 17000
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);
            //Bind this address to the server
            serverSocket.Bind(ipEndPoint);
            //Start receiving data
            serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length,
                SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);
            // Receive();
        }
        void AddDevice(byte[] dev)
        {
            if (dev.Length == 4)
            {
                Array.Reverse(dev);
                listDev.Add(dev);
            }
        }
        void InitAdd()
        {
            byte n1 = 0x26;
            for (int i = 0; i < 59; i++)
            {
                byte[] dev = new byte[] { (byte)(n1 + i), 0x04, 0x1A, 0x39 };
                listDev.Add(dev);
            }
            listDev.Add(new byte[] { 100, 0x04, 0x1A, 0x39 });
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int bytes = serverSocket.EndReceiveFrom(ar, ref epSender);
                //processing data
                string receive = Encoding.ASCII.GetString(byteData, 0, bytes).Replace("\0", "");
                string rec = "RECV: " + epSender.ToString() + " , " + bytes + " , " + receive + " \r\n";
                form1.PrintStatus(rec);
                if (bytes > 50)
                {
                    try
                    {
                        JObject json = JObject.Parse(receive);
                        if (json.ContainsKey("rxpk"))
                        {
                            var data = json["rxpk"][0]["data"];
                            var timeStamp = json["rxpk"][0]["tmst"];
                            // Console.WriteLine("Time stamp= "+ timeStamp);

                            if (data != null)
                            {
                                var phyLoadData = Convert.FromBase64String(data.ToString());
                                var phyPayload = new PHYPayload(phyLoadData, nwKey, appKey);
                                var m = (DataMessageWithKey)phyPayload.Message;
                                var ttt = m.Pirnt();
                                //Console.WriteLine(ttt);
                                EncryptData encrypt = new EncryptData();
                                byte[] devAdd = m.FHDR.DevAddr;
                                foreach (var item in listDev)
                                {
                                    if (item.SequenceEqual(devAdd))
                                    {
                                        string frmPayloadStr = "MACAdd: " + Helper.ToHexString(devAdd);
                                        frmPayloadStr += "  FRMPayload: " + m.GetFRMPayLoadDecryptedString();
                                        Utilities.WriteLogError(frmPayloadStr);
                                        Console.WriteLine("Data tu Loc:" + frmPayloadStr);

                                        form1.PrintData(frmPayloadStr);

                                        //Xu ly data
                                        byte[] dataReceive = m.GetFRMPayLoadDecrypted();
                                        string serialno = (dataReceive[4] * Math.Pow(2, 16) + dataReceive[3] * Math.Pow(2, 8) + dataReceive[2]).ToString("00000");
                                        string serialNumber = (char)(dataReceive[5]) + dataReceive[1].ToString("00") + dataReceive[0].ToString("00") + serialno;
                                        string packetType = Encoding.ASCII.GetString(dataReceive, 6, 2);
                                        // Console.WriteLine("Serial: " + serialNumber + " Type: " + packetType);

                                        switch (packetType)
                                        {
                                            case "DA":///Realtime data
                                                GetRealtimeData(dataReceive, serialNumber);
                                                break;
                                            case "S1": //Setting 1
                                                settings = new Setting();
                                                GetSeting1(dataReceive, serialNumber);
                                                break;
                                            case "S2"://Setting 2
                                                GetSeting2(dataReceive, serialNumber);
                                                break;
                                            case "S3"://Setting 3
                                                GetSeting3(dataReceive, serialNumber);
                                                break;
                                            case "AL"://Alarm packet
                                                alarms = new Alarm();
                                                GetAlarmData(dataReceive, serialNumber);
                                                break;
                                        }

                                        counterUp++;
                                        byte[] payload = { 0x00 };
                                        //  byte[] payload = { };
                                        byte[] payload2 = { 0x55, 0x66, 0x77, 0x88, 0x99 };
                                        //string Trinh = "I love you";
                                        //payload = Encoding.ASCII.GetBytes(Trinh);
                                        var frmPayload = encrypt.encrypt(payload, appKey, devAdd, counterUp);
                                        // Console.WriteLine();
                                        //Console.WriteLine("FrmPayload= " + Helper.ToHexString(frmPayload));
                                        byte[] MacPayload = encrypt.CreatMacPayload(devAdd, counterUp, frmPayload);
                                        //Console.WriteLine("MACPayload= " + Helper.ToHexString(MacPayload));
                                        byte[] MIC = encrypt.CalculateMIC(devAdd, counterUp, MacPayload, nwKey);
                                        // Console.WriteLine("Mic= " + Helper.ToHexString(MIC));
                                        List<byte> dataSend = new List<byte>();
                                        dataSend.Add(0x60);//add MHDR
                                        dataSend.AddRange(MacPayload);
                                        dataSend.AddRange(MIC);
                                        string dataStr = Convert.ToBase64String(dataSend.ToArray());
                                        //string dataStr = Helper.ToHexString(dataSend.ToArray());

                                       // Console.WriteLine("Data encrypted= " + dataStr);
                                        Thread.Sleep(500);
                                        Txpk txpk = new Txpk(false, double.Parse(timeStamp.ToString()) + 2000000, 923.3, 0, 27, "LORA", "SF9BW125", "4/5", true, payload.Length + 13, true, dataStr);
                                        string json_down = " {\"txpk\":" + JsonConvert.SerializeObject(txpk) + "}";
                                        Send2(json_down);
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Utilities.WriteLogError(ex);
                        Utilities.WriteLogError(rec);
                        Console.WriteLine(receive);
                    }
                }



                //continue listening to the message send by the user
                byteData = new byte[8 * 1024];
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender,
                    new AsyncCallback(OnReceive), epSender);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                serverSocket.EndSend(ar);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "UDP Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            try
            {
                Console.WriteLine(epSender);

                serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epSender, (ar) =>
                {
                    State so = (State)ar.AsyncState;
                    int bytes = 0;// _socket.EndSend(ar);
                    string textPrint = "SEND:  " + text.Length + " bytes, " + text + "\r\n";
                    Console.WriteLine("SEND: {0}, {1}, {2}", bytes, text, "IP: " + epSender.ToString());
                    form1.PrintStatus(textPrint);
                }, state);

                // _socket.SendTo(data, epFrom);

            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }
        }
        void Send2(string text)
        {
            try
            {
                //Send the message to user
                byte[] data = Encoding.ASCII.GetBytes(text);
                serverSocket.BeginSendTo(data, 0, data.Length, SocketFlags.None, epSender,
                            new AsyncCallback(OnSend), epSender);
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }
        }

        int counterUp = 0;
        private void Receive()
        {
            serverSocket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epSender, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = serverSocket.EndReceiveFrom(ar, ref epSender);
                //received byte
                serverSocket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epSender, recv, so);
                serverSocket.SendTimeout = 3000;
                Console.WriteLine("Sent time out: " + serverSocket.SendTimeout);
                string receive = Encoding.ASCII.GetString(state.buffer, 0, bytes).Replace("\0", "");
                //Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes, receive);
                string rec = "RECV: " + epSender.ToString() + " , " + bytes + " , " + receive + " \r\n";
                form1.PrintStatus(rec);
                string strSend = "60391A0426200C00769BD84D";
                Send(strSend);
                if (bytes > 50)
                {
                    try
                    {
                        JObject json = JObject.Parse(receive);
                        if (json.ContainsKey("rxpk"))
                        {
                            var data = json["rxpk"][0]["data"];
                            var timeStamp = json["rxpk"][0]["tmst"];
                            // Console.WriteLine("Time stamp= "+ timeStamp);

                            if (data != null)
                            {
                                var phyLoadData = Convert.FromBase64String(data.ToString());
                                var phyPayload = new PHYPayload(phyLoadData, nwKey, appKey);
                                var m = (DataMessageWithKey)phyPayload.Message;
                                var ttt = m.Pirnt();
                                Console.WriteLine(ttt);
                                EncryptData encrypt = new EncryptData();
                                byte[] devAdd = m.FHDR.DevAddr;
                                foreach (var item in listDev)
                                {
                                    if (item.SequenceEqual(devAdd))
                                    {
                                        string frmPayloadStr = "MACAdd: " + Helper.ToHexString(devAdd);
                                        frmPayloadStr += "  FRMPayload: " + m.GetFRMPayLoadDecryptedString();
                                        Utilities.WriteLogError(frmPayloadStr);
                                        //form1.PrintData(frmPayloadStr);

                                        //int counterUp = m.FHDR.FCnt[0]*256 + m.FHDR.FCnt[1] +1 ;//tang sau moi lan gui
                                        counterUp++;
                                        byte[] payload = { 0x00 };
                                        //  byte[] payload = { };
                                        byte[] payload2 = { 0x55, 0x66, 0x77, 0x88, 0x99 };
                                        //string Trinh = "I love you";
                                        //payload = Encoding.ASCII.GetBytes(Trinh);
                                        var frmPayload = encrypt.encrypt(payload, appKey, devAdd, counterUp);
                                        // Console.WriteLine();
                                        //Console.WriteLine("FrmPayload= " + Helper.ToHexString(frmPayload));
                                        byte[] MacPayload = encrypt.CreatMacPayload(devAdd, counterUp, frmPayload);
                                        //Console.WriteLine("MACPayload= " + Helper.ToHexString(MacPayload));
                                        byte[] MIC = encrypt.CalculateMIC(devAdd, counterUp, MacPayload, nwKey);
                                        // Console.WriteLine("Mic= " + Helper.ToHexString(MIC));
                                        List<byte> dataSend = new List<byte>();
                                        dataSend.Add(0x60);//add MHDR
                                        dataSend.AddRange(MacPayload);
                                        dataSend.AddRange(MIC);
                                        string dataStr = Convert.ToBase64String(dataSend.ToArray());
                                        //string dataStr = Helper.ToHexString(dataSend.ToArray());

                                        Console.WriteLine("Data encrypted= " + dataStr);
                                        Thread.Sleep(500);
                                        Txpk txpk = new Txpk(false, double.Parse(timeStamp.ToString()) + 2000000, 923.3, 0, 27, "LORA", "SF9BW125", "4/5", true, payload.Length + 13, true, dataStr);
                                        string json_down = " {\"txpk\":" + JsonConvert.SerializeObject(txpk) + "}";
                                        Send(json_down);
                                    }
                                }
                            }
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Console.WriteLine(ex.Message);
                        Utilities.WriteLogError(ex);
                        Utilities.WriteLogError(rec);
                        Console.WriteLine(receive);
                    }
                }

            }, state);
        }

        public double convertTemFrom15bit(double num, int coefficient)
        {
            if (num > 32767D)
                return num;
            if (num < 0)
                return 0;
            if (num <= 16383D)
                return (num / coefficient);
            return ((num - 32768D) / coefficient);
        }
        void GetRealtimeData(byte[] data, string Serial)
        {
            Data dt = new Data();
            dt.Serial = Serial;
            try
            {
                dt.Time = new DateTime(data[14] + 2000, data[13], data[12], data[11], data[10], data[9]);
            }
            catch
            {
                dt.Time = new DateTime();
            }

            dt.Data1 = convertTemFrom15bit((data[21] + data[22] * 256), 10);

            dt.Data2 = convertTemFrom15bit((data[23] + data[24] * 256), 10);

            //get Runtime 
            string runtime = "";
            TimeSpan tspRuntime;
            try
            {
                tspRuntime = new TimeSpan(data[19] * 256 + data[18], data[17], data[16], data[15]);
            }
            catch
            {
                tspRuntime = new TimeSpan();
            }
            runtime = tspRuntime.ToString();
            //get status 
            string status = GetStatus(data[20]);


            //save Realtime Table

            //var record = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
            try
            {
                var record = db.Realtimes.FirstOrDefault<Realtime>(u => u.Serial == Serial);
                if (record != null) //not exis
                {
                    string unit = record.Setting.Unit == "Celsius" ? "°C " : "°F";
                    record.Runtime = runtime;
                    record.Status = status;
                    record.Data1 = dt.Data1.ToString();// + unit;
                    record.Data2 = dt.Data2.ToString();// + "%";
                    record.TimeUpdated = dt.Time.ToString("MMM d HH:mm:ss");
                }
                else //kho xay ra 
                {
                    db.Realtimes.Add(new Realtime()
                    {

                        Data1 = dt.Data1.ToString(),
                        Data2 = dt.Data2.ToString(),
                        Runtime = runtime,
                        Serial = Serial,
                        Status = status,
                        TimeUpdated = dt.Time.ToString("MMM d HH:mm:ss")
                    });
                }
                db.Data.Add(dt);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
        void GetAlarmData(byte[] data, string Serial)
        {
            try
            {
                alarms.Serial = Serial;
                alarms.TttAlarm1 = new TimeSpan(data[30] * 256 + data[29], data[28], data[27], data[26]).TotalSeconds;
                alarms.TttLowAlarm1 = new TimeSpan(data[35] * 256 + data[34], data[33], data[32], data[31]).TotalSeconds;
                alarms.TttAlarm2 = new TimeSpan(data[40] * 256 + data[39], data[38], data[37], data[36]).TotalSeconds;
                alarms.TttLowAlarm2 = new TimeSpan(data[45] * 256 + data[44], data[43], data[42], data[41]).TotalSeconds;
                alarms.TimeUpdated = new DateTime(data[20], data[19], data[18], data[17], data[16], data[15]).ToString("MMM d HH:mm:ss");//Stop time

                var StartTime = new DateTime(data[14] + 2000, data[13], data[12], data[11], data[10], data[9]);
                //get Runtime 
                string runtime = new TimeSpan(data[25] * 256 + data[24], data[23], data[22], data[21]).ToString();
                var Data1 = convertTemFrom15bit((data[46] + data[47] * 256), 10);
                var Data2 = convertTemFrom15bit((data[48] + data[49] * 256), 10);

                //Add or Update to Realtime Table
                // var realtime = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                var realtime = db.Realtimes.FirstOrDefault<Realtime>(u => u.Serial == Serial);
                if (realtime != null) //not exis
                {
                    string unit = realtime.Setting.Unit == "Celsius" ? "°C " : "°F";
                    //update to db Settings
                    realtime.Runtime = runtime;
                    realtime.Data1 = Data1.ToString();// + unit;
                    realtime.Data2 = Data2.ToString();// + "%";
                    realtime.TimeUpdated = alarms.TimeUpdated;
                }
                else //ko xay ra 
                {
                    db.Realtimes.Add(new Realtime()
                    {
                        Data1 = Data1.ToString(),
                        Data2 = Data2.ToString(),
                        Runtime = runtime,
                        Serial = Serial,
                        Status = "Running",
                        TimeUpdated = alarms.TimeUpdated
                    });
                }
                var Alarmrecord = db.Alarms.Where(s => s.Serial == alarms.Serial).FirstOrDefault<Alarm>();
                if (Alarmrecord == null) //not exis (kho xay ra)
                {
                    // Thêm vào bang Alarm
                    db.Alarms.Add(alarms);
                    SendEmail(alarms);
                }
                else
                {
                    //
                    if (Alarmrecord.TttAlarm1 < alarms.TttAlarm1 || Alarmrecord.TttAlarm2 < alarms.TttAlarm2)
                    {
                        //send email
                        SendEmail(alarms);
                        //Cap nhat DB
                        Alarmrecord.TttAlarm1 = alarms.TttAlarm1;
                        Alarmrecord.TttAlarm2 = alarms.TttAlarm2;
                        Alarmrecord.TttLowAlarm1 = alarms.TttLowAlarm1;
                        Alarmrecord.TttLowAlarm2 = alarms.TttLowAlarm2;
                        Alarmrecord.TimeUpdated = alarms.TimeUpdated;
                    }
                }

                db.SaveChanges();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine("Last update error: " + ex.Message);
            }
        }
        void GetSeting1(byte[] data, string Serial)
        {
            string deviceType = GetDeviceType(data[8]);
            settings.Serial = Serial;
            settings.Unit = data[9] == 0xAC ? "Celsius" : "Farenheit";
            //thieu continue menory, stopke
            try
            {
                settings.Settingtime = new DateTime(data[34] + 2000, data[33], data[32], data[31], data[30], data[29]).ToString();
                settings.Starttime = new DateTime(data[16] + 2000, data[15], data[14], data[13], data[12], data[11]).ToString();
                settings.Stoptime = new DateTime(data[22] + 2000, data[21], data[20], data[19], data[18], data[17]).ToString();
            }
            catch
            {

            }

            string Runtime = new TimeSpan(data[27] * 256 + data[26], data[25], data[24], data[23]).ToString();
            string Status = GetStatus(data[28]);

            settings.Delay = data[35];
            settings.Duration = (data[36] + data[37] * 256).ToString() + " D  " + data[38].ToString() + " H";
            settings.Interval = data[39].ToString() + " h  " + data[40].ToString() + " m " + data[41].ToString() + " s";
            settings.HighAlarmTemp = (data[42] + data[43] * 256) / 10.0;
            settings.LowAlarmTemp = (data[44] + data[45] * 256) / 10.0;
            settings.HighAlarmHumid = (data[46] + data[47] * 256) / 10.0;
            settings.LowAlarmHumid = (data[48] + data[49] * 256) / 10.0;
            settings.FirmwareVer = "v " + data[50] + "." + data[51];

            //Update to tbl  REaltime and Alarms
            var realtime = db.Realtimes.FirstOrDefault<Realtime>(u => u.Serial == Serial);
            if (realtime != null) //upadte or insert
            {
                realtime.Serial = Serial;
                realtime.Runtime = Runtime;
                realtime.Status = Status;
                realtime.Data1 = null;
                realtime.Data2 = null;
                realtime.TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss");
            }
            else
            {
                db.Realtimes.Add(new Realtime()
                {
                    Runtime = Runtime,
                    Serial = Serial,
                    Status = Status,
                    Data1 = null,
                    Data2 = null,
                    TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss")
                });
            }

            var alarm = db.Alarms.FirstOrDefault<Alarm>(u => u.Serial == Serial);
            if (alarm != null) //update
            {
                alarm.Serial = Serial;
                alarm.TttAlarm1 = 0;
                alarm.TttAlarm2 = 0;
                alarm.TttLowAlarm1 = 0;
                alarm.TttLowAlarm2 = 0;
                alarm.TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss");
            }
            else //add new record
            {
                db.Alarms.Add(new Alarm()
                {
                    Serial = Serial,
                    TttAlarm1 = 0,
                    TttAlarm2 = 0,
                    TttLowAlarm1 = 0,
                    TttLowAlarm2 = 0,
                    TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss")
                });
            }
        }
        void GetSeting2(byte[] data, string Serial)
        {
            //Get timezone 40byte
            string timezone = Encoding.ASCII.GetString(data, 8, 40);
            Console.WriteLine("Timezone: " + timezone);
        }
        void GetSeting3(byte[] data, string Serial)
        {
            //Get Description
            settings.Description = Encoding.ASCII.GetString(data, 8, 20).Replace("\0", "");
            //Console.WriteLine("Descrtion: " + descrtiptin);
            //update or insert to database
            var record = db.Settings.Where(s => s.Serial == settings.Serial).FirstOrDefault<Setting>();
            if (record == null) //not exis
            {
                // Thêm vào database
                db.Settings.Add(settings);
            }
            else
            {
                //Cap nhat
                record.Description = settings.Description;
                record.Duration = settings.Duration;
                record.FirmwareVer = settings.FirmwareVer;
                record.HighAlarmHumid = settings.HighAlarmHumid;
                record.LowAlarmTemp = settings.LowAlarmTemp;
                record.HighAlarmTemp = settings.HighAlarmTemp;
                record.Interval = settings.Interval;
                record.LowAlarmHumid = settings.LowAlarmHumid;
                record.Settingtime = settings.Settingtime;
                record.Starttime = settings.Starttime;
                record.Stoptime = settings.Stoptime;
                record.Unit = settings.Unit;
            }
            db.SaveChanges();
        }
        string GetStatus(byte dt)
        {
            string status = "";
            switch (dt)
            {
                case 0xff:
                    status = "no setting & no run";
                    break;
                case 0x11:
                    status = "setting & no run";
                    break;
                case 0x44:
                    status = "running";
                    break;
                case 0xAA:
                    status = "stop";
                    break;
            }
            return status;
        }
        string GetDeviceType(byte dt)
        {
            string deviceType = "";
            switch (dt)
            {
                case 0x11:
                    deviceType = "Humidity, Room temperature";
                    break;
                case 0x22:
                    deviceType = "Humidity, LN2";
                    break;
                case 0x33:
                    deviceType = "Humidity, RTD2";
                    break;
                case 0x44:
                    deviceType = "LN2, Room Temperature";
                    break;
                case 0x55:
                    deviceType = "RTD2, Room temperature";
                    break;
            }
            return deviceType;
        }

        void SendEmail(Alarm alarm)
        {
            var myemail = "marathonlorawan@gmail.com";
            const string fromPassword = "MarathonDgs";
            string subject = "Alarm occurs from logger: " + alarm.Serial;
            var db = new Pexo63LorawanEntities();
            // var email = db.Settings.Include(u => u.Realtime).FirstOrDefault<Setting>(s => s.Serial == alarm.Serial);
            var email = db.Settings.FirstOrDefault<Setting>(s => s.Serial == alarm.Serial);
            if (email == null) //not exis
            {
                return;
            }
            string unit = "";
            if (email.Unit == "Celsius")
            {
                unit = "°C";
            }
            else
            {
                unit = "°F";
            }
            if (email.Email != null)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                    mail.From = new MailAddress(myemail);
                    mail.To.Add(email.Email);
                    mail.Subject = subject;
                    mail.Body = "This is a warning email sent automatically from Lorawan server when logger: \"" + alarm.Serial + "\" exceeds the limit.\r\n";
                    mail.Body += "- High alarm Temperature: " + email.HighAlarmTemp + " " + email.Unit + "\r\n";
                    mail.Body += "- Low alarm Temperature: " + email.LowAlarmTemp + " " + email.Unit + "\r\n";
                    mail.Body += "- High alarm Humidity: " + email.HighAlarmHumid + "%" + "\r\n";
                    mail.Body += "- Low alarm Humidity: " + email.LowAlarmHumid + "%" + "\r\n";
                    mail.Body += "- Current Temperature: " + email.Realtime.Data1 + " " + email.Unit + "\r\n";
                    mail.Body += "- Current Humidity: " + email.Realtime.Data2 + " " + email.Unit + "\r\n";
                    mail.Body += "- Total time high alarm temp: " + TimeSpan.FromSeconds(alarm.TttAlarm1 - alarm.TttLowAlarm1).ToString() + "\r\n";
                    mail.Body += "- Total time low alarm temp: " + TimeSpan.FromSeconds(alarm.TttLowAlarm1).ToString() + "\r\n";
                    mail.Body += "- Total time high alarm humid: " + TimeSpan.FromSeconds(alarm.TttAlarm1 - alarm.TttLowAlarm2).ToString() + "\r\n";
                    mail.Body += "- Total time low alarm humid: " + TimeSpan.FromSeconds(alarm.TttLowAlarm2).ToString() + "\r\n";
                    mail.Body += "For more infomation please visit website: http://113.161.71.163/";
                    mail.Priority = MailPriority.High;
                    SmtpServer.Port = 587;
                    SmtpServer.Credentials = new System.Net.NetworkCredential(myemail, fromPassword);
                    SmtpServer.EnableSsl = true;
                    SmtpServer.Send(mail);
                    //MessageBox.Show("mail Send");
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                }
            }


        }
        public byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }

}
