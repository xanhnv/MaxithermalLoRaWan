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
using System.Data.Entity;
using System.Timers;
using UDPServerAndWebSocketClient.Model;

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
        //static string nwKey = "11E6FD2D9AC364C607B02E32049C586D";
        static string nwKey = "21E6FD2D9AC364C607B02E32049C586D";
        static string appKey = "50AECEA6ADE24F911E3028504B0F6E28";
        DataProcessing dataProcessing = new DataProcessing();
        /// <summary>
        /// timer to check status of node every after 5 minute
        /// </summary>
        System.Timers.Timer aTimer = new System.Timers.Timer();
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public UDPSocket(FormMain form1)
        {
            this.form1 = form1;
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 1000 * 60;//1 minute
            aTimer.Enabled = true;
        }
        UDPSocket()
        {

        }
        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            using (var db = new Pexo63LorawanEntities())
            {
                //Get all record
                var allRecord = db.Realtimes.Include(a => a.Setting);
                //check if record is running
                foreach (var item in allRecord.ToList())
                {
                    if (item.Status == "running")
                    {
                        //get last data record
                        var lastDataRec = db.Data1.OrderByDescending(p => p.Time).FirstOrDefault(dt => dt.Serial == item.Serial);
                        if (lastDataRec != null)
                        {
                            //get timezone in Logger
                            //change current time to Logger time
                            DateTime now = DateTime.Now;
                            if (item.Setting.TimezoneId != null)
                            {
                                now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, TimeZoneInfo.Local.Id, item.Setting.TimezoneId);
                            }
                            int totalminute = (int)(now - lastDataRec.Time).TotalMinutes;
                            if (totalminute > item.Setting.IntervalSendLoraMin + 1)
                            {

                                //The time has passed without updating the new data
                                //update status to table Realtime : "Not availible"
                                var realtimeRec = db.Realtimes.First(dt => dt.Serial == item.Serial);

                                if (realtimeRec != null)
                                {
                                    realtimeRec.Status = "Not availible";
                                    try
                                    {
                                        //db.Entry(realtimeRec).Property(x => x.Status).IsModified = true;
                                        db.SaveChanges();
                                        Console.WriteLine("updated logger status");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }


                                }
                            }
                        }
                    }

                }
            }
        }

        public void Server(int port)
        {
            //InitAdd();
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

        byte[] GetDataSetting(string Serial, string packetype)
        {
            using (var db = new Pexo63LorawanEntities())
            {
                var rcst = db.Settings.Include(a => a.Alarm).Where(s => s.Serial == Serial).FirstOrDefault();
                if (rcst != null)
                {
                    if (rcst.SettingByLora)
                    {
                        int interval = rcst.IntervalSec + rcst.IntervalMin * 60 + rcst.IntervalHour * 3600;//Total second of interval time
                        int dataPerPacket = ((rcst.IntervalSendLoraMin + rcst.IntervalSendLoraHour * 60 + rcst.IntervalSendLoraDay * 24 * 60) * 60) / (interval);//num of data per package must sent to server
                        //if (packetype == "D0" || packetype == "A0")
                        //{
                        //    if (dataPerPacket > 21)
                        //    {
                        //        return new byte[] { 0 };
                        //    }
                        //}
                        //else if (packetype == "D1" || packetype == "A1")
                        //{
                        //    if (dataPerPacket > 42)
                        //    {
                        //        return new byte[] { 0 };
                        //    }
                        //}

                        byte[] setting = new byte[30];
                        Array.Copy(Encoding.ASCII.GetBytes("SE"), setting, 2);//[0-1]
                        if (rcst.Celsius)
                        {
                            setting[2] = 0xAC;
                        }
                        else
                        {
                            setting[2] = 0xAF;
                        }
                        string stopkeyContinue = "";
                        if (rcst.ContinueMem)
                        {
                            stopkeyContinue = "1";
                        }
                        else
                        {
                            stopkeyContinue = "0";
                        }
                        if (rcst.Stopkey)
                            stopkeyContinue += "0";
                        else
                            stopkeyContinue += "1";

                        setting[3] = Convert.ToByte(stopkeyContinue, 2);
                        if (rcst.Alarm.AlarmStatus1)
                        {
                            setting[4] = (byte)((rcst.Alarm.HighAlarmTemp * 10) % 256);
                            setting[5] = (byte)((rcst.Alarm.HighAlarmTemp * 10) / 256);
                            setting[6] = (byte)((rcst.Alarm.LowAlarmTemp * 10) % 256);
                            setting[7] = (byte)((rcst.Alarm.LowAlarmTemp * 10) / 256);
                        }
                        else
                        {
                            setting[4] = 10000 % 256;
                            setting[5] = 10000 / 256;
                            setting[6] = 10000 % 256;
                            setting[7] = 10000 / 256;
                        }
                        if (rcst.Alarm.AlarmStatus2)
                        {
                            setting[8] = (byte)((rcst.Alarm.HighAlarmHumid * 10) % 256);
                            setting[9] = (byte)((rcst.Alarm.HighAlarmHumid * 10) / 256);
                            setting[10] = (byte)((rcst.Alarm.LowAlarmHumid * 10) % 256);
                            setting[11] = (byte)((rcst.Alarm.LowAlarmHumid * 10) / 256);
                        }
                        else
                        {
                            setting[8] = 10000 % 256;
                            setting[9] = 10000 / 256;
                            setting[10] = 10000 % 256;
                            setting[11] = 10000 / 256;
                        }
                        setting[12] = (byte)(rcst.DurationDay % 256);
                        setting[13] = (byte)(rcst.DurationDay / 256);
                        setting[14] = rcst.DurationHour;
                        setting[15] = rcst.IntervalHour;
                        setting[16] = rcst.IntervalMin;
                        setting[17] = rcst.IntervalSec;
                        if (rcst.AutoStart)
                        {
                            setting[18] = 0xAE;
                        }
                        else
                        {
                            setting[18] = 0xAD;
                        }
                        //Auto start
                        setting[19] = 0;//sec
                        setting[20] = 0;//Min
                        setting[21] = 0;//Hour
                        setting[22] = 0;//day
                        setting[23] = 0;//month
                        setting[24] = 0;//year
                        setting[25] = rcst.IntervalSendLoraDay;//interval send lora day
                        setting[26] = rcst.IntervalSendLoraHour;//interval send lora hour
                        setting[27] = rcst.IntervalSendLoraMin;//interval send lora Min
                        setting[28] = rcst.Delay;//delay start
                        return setting;
                    }
                    return new byte[] { 0 };
                }
            }
            return new byte[] { 0 };
        }
        void InitAdd()
        {
            //byte n1 = 0x26;
            //for (int i = 0; i < 59; i++)
            //{
            //    byte[] dev = new byte[] { (byte)(n1 + i), 0x04, 0x1A, 0x39 };
            //    listDev.Add(dev);
            //}
            //listDev.Add(new byte[] { 100, 0x04, 0x1A, 0x39 });
            using (var db = new Pexo63LorawanEntities())
            {
                byte n1 = 0x26;
                for (int i = 0; i < 59; i++)
                {
                    var dev = new Device();
                    dev.DeviceAddress = new byte[] { (byte)(n1 + i), 0x04, 0x1A, 0x39 };
                    db.Devices.Add(dev);

                }
                db.SaveChanges();
                //dev.DeviceAddress= new byte[] { 100, 0x04, 0x1A, 0x39 };
                //db.Devices.Add(dev);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {


                int bytes = serverSocket.EndReceiveFrom(ar, ref epSender);
                //processing data
                string receive = Encoding.ASCII.GetString(byteData, 0, bytes).Replace("\0", "");
                string rec = "RECV: " + epSender.ToString() + " , " + bytes + " , " + receive + " \r\n";
                Console.WriteLine(rec);
                if (bytes > 50)
                {
                    try
                    {                                                         
                        JObject json = JObject.Parse(receive);
                        if (json.ContainsKey("rxpk"))
                        {
                            var data = json["rxpk"][0]["data"];
                            var timeStamp = json["rxpk"][0]["tmst"];

                            if (data != null)
                            {
                                var phyLoadData = Convert.FromBase64String(data.ToString());
                                var phyPayload = new PHYPayload(phyLoadData, nwKey, appKey);
                                var m = (DataMessageWithKey)phyPayload.Message;
                                var ttt = m.Pirnt();
                                //Console.WriteLine(ttt);
                                EncryptData encrypt = new EncryptData();
                                byte[] devAdd = m.FHDR.DevAddr;
                                //SendTestACK(encrypt, devAdd, double.Parse(timeStamp.ToString()));
                                if (dataProcessing.CheckExistDevice(devAdd))
                                {
                                    string frmPayloadStr = "MACAdd: " + Helper.ToHexString(devAdd);
                                    frmPayloadStr += "  FRMPayload: " + m.GetFRMPayLoadDecryptedString();
                                    //Console.WriteLine(frmPayloadStr);
                                    if (m.GetFRMPayLoadDecryptedString() != frmPayloadOld)
                                    {

                                        frmPayloadOld = m.GetFRMPayLoadDecryptedString();
                                        byte[] dataReceive = m.GetFRMPayLoadDecrypted();
                                        //if (dataReceive != null)
                                        //{
                                        //    return;
                                        //}
                                        //if (dataReceive.Length < 40)
                                        //{
                                        //    return;
                                        //}
                                        string serialno = (dataReceive[4] * Math.Pow(2, 16) + dataReceive[3] * Math.Pow(2, 8) + dataReceive[2]).ToString("00000");
                                        string serialNumber = (char)(dataReceive[5]) + dataReceive[1].ToString("00") + dataReceive[0].ToString("00") + serialno;
                                        string packetType = Encoding.ASCII.GetString(dataReceive, 6, 2);
                                        Console.WriteLine("Serial: " + serialNumber + " packetType: " + packetType);

                                        //Utilities.WriteLog("S/N: " + serialNumber + " Type: " + packetType + "Data: " + Helper.ToHexString(dataReceive));
                                        Utilities.WriteLogPackType(DateTime.Now.ToString("g") + ", " + serialNumber + "," +dataReceive[6].ToString("X")+dataReceive[7].ToString("X") + "," + Helper.ToHexString(dataReceive));

                                        if (dataReceive[6] == 0xd0 || dataReceive[6] == 0xa0)
                                        {
                                            packetType = "D0";
                                        }
                                        else if (dataReceive[6] == 0xd1 || dataReceive[6] == 0xa1)
                                        {
                                            packetType = "D1";
                                        }
                                        else if (dataReceive[6] == 0xd2 || dataReceive[6] == 0xa2)
                                            packetType = "D2";
                                       

                                        //Send data to node
                                        counterUp++;
                                        byte[] payload = { 0 }; //data to send to node (may be setting data...)
                                        if (packetType != "S1" || packetType != "S2" || packetType != "S3")
                                        {
                                            payload = GetDataSetting(serialNumber, packetType);
                                        }

                                        //Console.WriteLine("seting length:" + payload.Length);
                                        var frmPayload = encrypt.encrypt(payload, appKey, devAdd, counterUp);
                                        byte[] MacPayload = encrypt.CreatMacPayload(devAdd, counterUp, frmPayload);
                                        byte[] MIC = encrypt.CalculateMIC(devAdd, counterUp, MacPayload, nwKey);
                                        List<byte> dataSend = new List<byte>();
                                        dataSend.Add(0x60);//add MHDR
                                        dataSend.AddRange(MacPayload);//Add mac payload
                                        dataSend.AddRange(MIC); //Add mic
                                        string dataStr = Convert.ToBase64String(dataSend.ToArray());
                                        Thread.Sleep(100);
                                        Txpk txpk = new Txpk(false, double.Parse(timeStamp.ToString()) + 2000000, 923.3, 0, 27, "LORA", "SF9BW125", "4/5", true, payload.Length + 13, true, dataStr);
                                        string json_down = " {\"txpk\":" + JsonConvert.SerializeObject(txpk) + "}";
                                        Send2(json_down);
                                        //processcing data
                                        switch (packetType)
                                        {
                                            case "A0":
                                            case "D0"://Realtime data
                                                dataProcessing.GetRealtimeD0(dataReceive, serialNumber);//
                                                //new Thread(() =>
                                                //{
                                                //    dataProcessing.GetRealtimeD0(dataReceive, serialNumber);
                                                //}
                                                //).Start();
                                                break;
                                            case "A1":
                                            case "D1":
                                                //new Thread(() => { dataProcessing.GetRealtimeD1(dataReceive, serialNumber); });
                                                dataProcessing.GetRealtimeD1(dataReceive, serialNumber);//
                                                break;
                                            case "A2":
                                            case "D2":
                                               // new Thread(()=> { dataProcessing.GetRealtimeD2(dataReceive, serialNumber); });
                                                dataProcessing.GetRealtimeD2(dataReceive, serialNumber);//
                                                break;

                                            case "S1": //Setting 1
                                                //new Thread(()=> { dataProcessing.GetSeting1(dataReceive, serialNumber); });
                                                dataProcessing.GetSeting1(dataReceive, serialNumber);
                                                break;
                                            case "S2"://Setting 2
                                                //new Thread(() => { dataProcessing.GetSeting2(dataReceive, serialNumber); });
                                                dataProcessing.GetSeting2(dataReceive, serialNumber);
                                                break;
                                            case "S3"://Setting 3
                                                //new Thread(() => { dataProcessing.GetSeting3(dataReceive, serialNumber); });
                                                dataProcessing.GetSeting3(dataReceive, serialNumber);

                                                break;
                                            case "AL"://Alarm packet
                                                //new Thread(() => { dataProcessing.GetAlarmData(dataReceive, serialNumber); });
                                               dataProcessing.GetAlarmData(dataReceive, serialNumber);

                                                break;
                                            case "EN"://End setting
                                                //new Thread(() => { dataProcessing.GetEndSetting(dataReceive, serialNumber); });
                                                dataProcessing.GetEndSetting(dataReceive, serialNumber);

                                                break;
                                        }
                                        frmPayloadOld = serialNumber;
                                        form1.RefreshGridView();
                                       

                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Not Exit device address: " + Helper.ToHexString(devAdd));
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
        void SendTestACK(EncryptData encrypt, byte[]devAdd, double timeStamp )
        {
            byte[] payload = { 0, 1, 2, 3, 4, 5 };
            var frmPayload = encrypt.encrypt(payload, appKey, devAdd, counterUp);
            byte[] MacPayload = encrypt.CreatMacPayload(devAdd, counterUp, frmPayload);
            byte[] MIC = encrypt.CalculateMIC(devAdd, counterUp, MacPayload, nwKey);
            List<byte> dataSend = new List<byte>();
            dataSend.Add(0x60);//add MHDR
            dataSend.AddRange(MacPayload);//Add mac payload
            dataSend.AddRange(MIC); //Add mic
            string dataStr = Convert.ToBase64String(dataSend.ToArray());
            Thread.Sleep(700);
            Txpk txpk = new Txpk(false, timeStamp + 2000000, 923.3, 0, 27, "LORA", "SF9BW125", "4/5", true, payload.Length + 13, true, dataStr);
            string json_down = " {\"txpk\":" + JsonConvert.SerializeObject(txpk) + "}";
            Send2(json_down);
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
                    //form1.PrintStatus(textPrint);
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

                 Console.WriteLine("SEND: {0}, {1}", text, "IP: " + epSender.ToString());
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }
        }

        int counterUp = 0;
        private string frmPayloadOld;
    }

}
