using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using UDPServerAndWebSocketClient.Model;

namespace UDPServerAndWebSocketClient
{

    /// <summary>
    /// Processing data and save to DB
    /// </summary>
    public class DataProcessing
    {
        //Pexo63LorawanEntities db = new Pexo63LorawanEntities();
        Setting setting = new Setting();
        private Alarm alarmSet;
        private string Status;

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
                case 0xDD:
                    status = "delaying";
                    break;
            }
            return status;
        }

        public bool CheckExistDevice(byte[] devAdd)
        {
            using (var db = new Pexo63LorawanEntities())
            {
                foreach (var item in db.Devices)
                {
                    if (item.DeviceAddress.SequenceEqual(devAdd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool CheckExistPacketD0(byte[] devAdd)
        {
            using (var db = new Pexo63LorawanEntities())
            {
                foreach (var item in db.Devices)
                {
                    if (item.DeviceAddress.SequenceEqual(devAdd))
                    {
                        return true;
                    }
                }
            }
            return false;
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
                case 0x66:
                    deviceType = "Thermal couple, Humidity (%)";
                    break;
                case 0x77:
                    deviceType = "Room temperature, Thermal couple";
                    break;
            }
            return deviceType;
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
        public DateTime UnixTimeStampToDateTime(double unixTimeStamp, DateTime dateTime)
        {
            // Unix timestamp is seconds past epoch
            //DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);
            return dateTime;
        }
        public double Delta(byte data)
        {
            if (data > 127)
            {
                return (data - 256) / 10.0;
            }
            else
                return data / 10.0;
        }
        /// <summary>
        /// Get realTime D0 and MissData A0 packet 1
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Serial"></param>
        public void GetRealtimeD0(byte[] data, string Serial)
        {
            try
            {
                using (var db = new Pexo63LorawanEntities())
                {
                    var rcst = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                    if (rcst == null)
                    {
                        return;
                    }
                    var datalist = new List<Datum>();

                    int interval = rcst.IntervalSec + rcst.IntervalMin * 60 + rcst.IntervalHour * 3600;//Total second of interval time
                    int numOfDt = (rcst.DurationHour + rcst.DurationDay * 24) * 3600 / interval;//all data

                    int dataPerPacket = ((rcst.IntervalSendLoraMin + rcst.IntervalSendLoraHour * 60 + rcst.IntervalSendLoraDay * 24 * 60) * 60) / (interval);//num of data per package must sent to server
                    int package = 0;


                    if (double.Parse(rcst.FirmwareVer.Substring(2)) < 0.10d)
                    {
                        if (rcst.ContinueMem)
                        {
                            rcst.ContinueMemoryCount = (short)(db.Data.Where(u => u.Serial == Serial).ToList().Count / numOfDt);
                        }
                        package = data[7] + data[8] * 256;//number of the package
                        double data1 = convertTemFrom15bit((data[9] + data[10] * 256), 10);
                        double data2 = convertTemFrom15bit((data[11] + data[12] * 256), 10);
                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;
                        startTime = startTime.AddSeconds((rcst.ContinueMemoryCount.GetValueOrDefault() * numOfDt + dataPerPacket * package) * interval);
                        //Console.WriteLine("a Packet: " + package + ", Data 1: " + data1 + " Data2: " + data2 + " Start time: " + startTime + ", Continue Mem Count: " + rcst.ContinueMemoryCount);
                        string mesLog = Serial + "," + startTime.ToString() + "," + "D0" + "," + rcst.ContinueMemoryCount + "," + package + "," + data1 + "," + data2;
                        Utilities.WriteLogDebug(mesLog);
                        dtFirt.Time = startTime;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 13; i < data.Length - 1; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTime.AddSeconds(j * interval);
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            //startTime = dt.Time;
                            j++;
                        }
                    }
                    else
                    {
                        int packetAndNumOfMeas = data[7] + (data[8] << 8) + (data[9] << 16) + (data[10] << 24);//in version 1.12 packet replaced = unix time 
                        //packet= time stamp (second)
                        package = packetAndNumOfMeas & 0x7FFFFFF;
                        int numberOfMeas = packetAndNumOfMeas >> 27;
                        double data1 = convertTemFrom15bit((data[12] + data[13] * 256), 10);
                        double data2 = convertTemFrom15bit((data[14] + data[15] * 256), 10);
                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        DateTime startTimePacket = UnixTimeStampToDateTime(package, startTime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;
                        //startTimePacket = startTimePacket.AddSeconds((dataPerPacket * package) * interval);
                        // Console.WriteLine("D0: " + package + ", Data 1: " + data1 + " Data2: " + data2 + " Start time: " + startTime + ", Continue Mem Count: " + rcst.ContinueMemoryCount);
                        string mesLog = Serial + "," + startTimePacket.ToString() + "," + "D0" + "," + rcst.ContinueMemoryCount + "," + package + "," + data1 + "," + data2;
                        Utilities.WriteLogDebug(mesLog);
                        dtFirt.Time = startTimePacket;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 16; i < 16 + (data[11]-1) * 2; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTimePacket.AddSeconds(j * interval);
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            //startTime = dt.Time;
                            j++;
                        }
                    }


                    int lastIndex = datalist.Count - 1;
                    //Console.WriteLine("last index: " + lastIndex + "Last time: " + datalist[lastIndex].Time.ToString());
                    //save Realtime Table
                    var record = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                    if (datalist.Count < 1)
                    {
                        return;
                    }
                    if (db.PacketD0.Any(p => p.Serial == Serial && p.Packet == package))
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd-MMM hh:mm:ss") + ", " + Serial + ", D0: " + package + " Existing packet");
                        return;
                    }
                    db.PacketD0.Add(new PacketD0() { Serial = Serial, Packet = package });
                    string runtime = (datalist[lastIndex].Time - DateTime.Parse(rcst.Starttime)).ToString();
                    if (record != null) //update
                    {
                        string unit = record.Setting.Unit == "Celsius" ? "°C " : "°F";

                        record.Runtime = runtime;
                        record.Status = "running";
                        record.Data1 = datalist[lastIndex].Data1.ToString();// + unit;
                        record.Data2 = datalist[lastIndex].Data2.ToString();// + "%";
                        record.TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
                    }
                    else //add new
                    {
                        db.Realtimes.Add(new Realtime()
                        {
                            Data1 = datalist[lastIndex].Data1.ToString(),
                            Data2 = datalist[lastIndex].Data2.ToString(),
                            Runtime = runtime,
                            Serial = Serial,
                            Status = "running",
                            TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial)
                        });
                    }


                    db.Data.AddRange(datalist);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }
        }
        /// <summary>
        /// Get Realtime and missData Packet 2
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Serial"></param>
        public void GetRealtimeD1(byte[] data, string Serial)
        {
            try
            {
                using (var db = new Pexo63LorawanEntities())
                {
                    var rcst = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                    if (rcst == null)
                    {
                        return;
                    }
                    int interval = rcst.IntervalSec + rcst.IntervalMin * 60 + rcst.IntervalHour * 3600;//Total second of interval time
                    int numOfDt = (rcst.DurationHour + rcst.DurationDay * 24) * 3600 / interval;//all data


                    int dataPerPacket = ((rcst.IntervalSendLoraMin + rcst.IntervalSendLoraHour * 60 + rcst.IntervalSendLoraDay * 24 * 60) * 60) / (interval);//num of data per package must sent to server
                    var datalist = new List<Datum>();
                    int package = data[7] + data[8] * 256;//number of the package
                    if (double.Parse(rcst.FirmwareVer.Substring(2)) < 0.10d)
                    {
                        double data1 = convertTemFrom15bit((data[9] + data[10] * 256), 10);
                        double data2 = convertTemFrom15bit((data[11] + data[12] * 256), 10);
                        if (rcst.ContinueMem)
                        {
                            rcst.ContinueMemoryCount = (short)(db.Data.Where(u => u.Serial == Serial).ToList().Count / numOfDt);
                        }
                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;
                        startTime = startTime.AddSeconds((rcst.ContinueMemoryCount.GetValueOrDefault() * numOfDt + dataPerPacket * package + 21) * interval);
                        //Console.WriteLine("Packet: " + package + ", Data 1: " + data1 + " Data2: " + data2 + " Start time: " + startTime);
                        // Console.WriteLine("D1: " + package + ", Data 1: " + data1 + " Data2: " + data2 + " Start time: " + startTime + ", Continue Mem Count: " + rcst.ContinueMemoryCount);
                        string mesLog = Serial + "," + startTime.ToString() + "," + "D1" + "," + rcst.ContinueMemoryCount + "," + package + "," + data1 + "," + data2;
                        Utilities.WriteLogDebug(mesLog);
                        dtFirt.Time = startTime;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 13; i < data.Length - 1; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTime.AddSeconds(j * interval);
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            j++;
                            //startTime = dt.Time;
                        }
                    }
                    else
                    {
                        package = data[7] + (data[8] << 8) + (data[9] << 16) + (data[10] << 24);
                        double data1 = convertTemFrom15bit((data[11] + data[12] * 256), 10);
                        double data2 = convertTemFrom15bit((data[13] + data[14] * 256), 10);

                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;
                        //Console.WriteLine("D1: " + package + ", Data 1: " + data1 + " Data2: " + data2 + " Start time: " + startTime + ", Continue Mem Count: " + rcst.ContinueMemoryCount);
                        string mesLog = Serial + "," + startTime.ToString() + "," + "D1" + "," + rcst.ContinueMemoryCount + "," + package + "," + data1 + "," + data2;
                        Utilities.WriteLogDebug(mesLog);
                        dtFirt.Time = startTime;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 15; i < data.Length - 1; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTime.AddSeconds(j * interval);
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            j++;
                            //startTime = dt.Time;
                        }
                    }


                    //save Realtime Table
                    var recordRealtime = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                    if (datalist.Count < 1)
                    {
                        return;
                    }
                    if (db.PacketD1.Any(p => p.Serial == Serial && p.Packet == package))
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd-MMM hh:mm:ss") + ", " + Serial + ", D1: " + package + " Existing packet");
                        return;
                    }
                    db.PacketD1.Add(new PacketD1() { Serial = Serial, Packet = package });
                    int lastIndex = datalist.Count - 1;
                    if (db.Data.Any(d => d.Serial == Serial))
                    {
                        Datum newestData = db.Data.OrderByDescending(u => u.ID).Where(u => u.Serial == Serial).FirstOrDefault();
                        // Console.WriteLine("Time in DB: " + newestData.Time.ToString());
                        if (newestData.Time == datalist[lastIndex].Time)
                        {
                            //already exist in DB
                            return;
                        }
                    }
                    // Console.WriteLine("last index: " + lastIndex + "Last time: " + datalist[lastIndex].Time);
                    string runtime = (datalist[lastIndex].Time - DateTime.Parse(rcst.Starttime)).ToString();
                    if (recordRealtime != null && datalist.Count > 0) //update
                    {
                        string unit = recordRealtime.Setting.Unit == "Celsius" ? "°C " : "°F";
                        recordRealtime.Runtime = runtime;
                        recordRealtime.Status = "running";
                        recordRealtime.Data1 = datalist[lastIndex].Data1.ToString();// + unit;
                        recordRealtime.Data2 = datalist[lastIndex].Data2.ToString();// + "%";
                        recordRealtime.TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
                    }
                    else //add new
                    {
                        db.Realtimes.Add(new Realtime()
                        {
                            Data1 = datalist[lastIndex].Data1.ToString(),
                            Data2 = datalist[lastIndex].Data2.ToString(),
                            Runtime = runtime,
                            Serial = Serial,
                            Status = "running",
                            TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial)
                        });
                    }
                    db.Data.AddRange(datalist);
                    db.SaveChanges();

                }
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }

        }
        /// <summary>
        /// Get Realtime and missdata Paclet 3
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Serial"></param>
        public void GetRealtimeD2(byte[] data, string Serial)
        {
            try
            {
                using (var db = new Pexo63LorawanEntities())
                {

                    var rcst = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                    if (rcst == null)
                    {
                        return;
                    }
                    var datalist = new List<Datum>();
                    int package = data[7] + data[8] * 256;//number of the package

                    int interval = rcst.IntervalSec + rcst.IntervalMin * 60 + rcst.IntervalHour * 3600;//Total second of interval time
                    int numOfDt = (rcst.DurationHour + rcst.DurationDay * 24) * 3600 / interval;//all data
                    int dataPerPacket = ((rcst.IntervalSendLoraMin + rcst.IntervalSendLoraHour * 60 + rcst.IntervalSendLoraDay * 24 * 60) * 60) / (interval);//num of data per package must sent to server
                    if (double.Parse(rcst.FirmwareVer.Substring(2)) < 0.10d)
                    {
                        if (rcst.ContinueMem)
                        {
                            rcst.ContinueMemoryCount = (short)(db.Data.Where(u => u.Serial == Serial).ToList().Count / numOfDt);
                        }
                        double data1 = convertTemFrom15bit((data[9] + data[10] * 256), 10);
                        double data2 = convertTemFrom15bit((data[11] + data[12] * 256), 10);
                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;
                        startTime = startTime.AddSeconds((rcst.ContinueMemoryCount.GetValueOrDefault() * numOfDt + dataPerPacket * package + 42) * interval);
                        dtFirt.Time = startTime;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 13; i < data.Length - 1; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTime.AddSeconds(j * interval);
                            j++;
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            // startTime = dt.Time;
                        }
                    }
                    else
                    {
                        package = data[7] + (data[8] << 8) + (data[9] << 16);
                        double data1 = convertTemFrom15bit((data[11] + data[12] * 256), 10);
                        double data2 = convertTemFrom15bit((data[13] + data[14] * 256), 10);
                        double delta1 = 0, delta2 = 0;
                        DateTime startTime = DateTime.Parse(rcst.Starttime);
                        //first data of packet
                        Datum dtFirt = new Datum();
                        dtFirt.Serial = Serial;
                        dtFirt.Data1 = data1;
                        dtFirt.Data2 = data2;

                        dtFirt.Time = startTime;
                        datalist.Add(dtFirt);
                        int j = 1;
                        for (int i = 15; i < data.Length - 1; i += 2)
                        {
                            Datum dt = new Datum();
                            dt.Serial = Serial;
                            delta1 = Delta(data[i]);
                            delta2 = Delta(data[i + 1]);
                            dt.Data1 = data1 + delta1;
                            dt.Data2 = data2 + delta2;
                            dt.Time = startTime.AddSeconds(j * interval);
                            j++;
                            datalist.Add(dt);
                            data1 = dt.Data1;
                            data2 = dt.Data2;
                            // startTime = dt.Time;
                        }
                    }

                    //save Realtime Table
                    var record = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                    if (datalist.Count < 1)
                    {
                        return;
                    }
                    if (db.PacketD2.Any(p => p.Serial == Serial && p.Packet == package))
                    {
                        Console.WriteLine(DateTime.Now.ToString("dd-MMM hh:mm:ss") + ", " + Serial + ", D2: " + package + " Existing packet");
                        return;
                    }
                    db.PacketD2.Add(new PacketD2() { Serial = Serial, Packet = package });
                    int lastIndex = datalist.Count - 1;

                    string runtime = (datalist[lastIndex].Time - DateTime.Parse(rcst.Starttime)).ToString();
                    if (record != null && datalist.Count > 0) //update
                    {
                        string unit = record.Setting.Unit == "Celsius" ? "°C " : "°F";
                        record.Runtime = runtime;
                        record.Status = "running";
                        record.Data1 = datalist[lastIndex].Data1.ToString();// + unit;
                        record.Data2 = datalist[lastIndex].Data2.ToString();// + "%";
                        record.TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
                    }
                    else //add new
                    {
                        db.Realtimes.Add(new Realtime()
                        {
                            Data1 = datalist[lastIndex].Data1.ToString(),
                            Data2 = datalist[lastIndex].Data2.ToString(),
                            Runtime = runtime,
                            Serial = Serial,
                            Status = "running",
                            TimeUpdated = datalist[lastIndex].Time.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial)
                        });
                    }
                    db.Data.AddRange(datalist);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex);
            }

        }
        /// <summary>
        /// get alarm data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="Serial"></param>
        public void GetAlarmData(byte[] data, string Serial)
        {
            try
            {
                Alarm alarms = new Alarm();
                alarms.Serial = Serial;
                alarms.TttAlarm1 = new TimeSpan(data[30] * 256 + data[29], data[28], data[27], data[26]).TotalSeconds;
                alarms.TttLowAlarm1 = new TimeSpan(data[35] * 256 + data[34], data[33], data[32], data[31]).TotalSeconds;
                alarms.TttAlarm2 = new TimeSpan(data[40] * 256 + data[39], data[38], data[37], data[36]).TotalSeconds;
                alarms.TttLowAlarm2 = new TimeSpan(data[45] * 256 + data[44], data[43], data[42], data[41]).TotalSeconds;
                alarms.TimeUpdated = new DateTime(data[20], data[19], data[18], data[17], data[16], data[15]).ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);//Stop time

                var StartTime = new DateTime(data[14] + 2000, data[13], data[12], data[11], data[10], data[9]);
                //get Runtime 
                string runtime = new TimeSpan(data[25] * 256 + data[24], data[23], data[22], data[21]).ToString();
                var Data1 = convertTemFrom15bit((data[46] + data[47] * 256), 10);
                var Data2 = convertTemFrom15bit((data[48] + data[49] * 256), 10);

                //Add or Update to Realtime Table
                using (var db = new Pexo63LorawanEntities())
                {
                    #region Check record in DB
                    var alarmDB = db.Alarms.FirstOrDefault<Alarm>(u => u.Serial == Serial);
                    var recordSt = db.Settings.Where(s => s.Serial == setting.Serial).FirstOrDefault<Setting>();
                    if (recordSt == null)
                    {
                        return;
                    }
                    if (alarmDB.TimeUpdated == alarms.TimeUpdated)
                    {
                        return;
                    }
                    #endregion
                    #region Update realtime table
                    var realtime = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                    if (realtime != null) //not exis
                    {
                        string unit = realtime.Setting.Unit == "Celsius" ? "°C " : "°F";
                        //update to db Settings
                        realtime.Runtime = runtime;
                        realtime.Data1 = Data1.ToString();// + unit;
                        realtime.Data2 = Data2.ToString();// + "%";
                        realtime.Status = "running";
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
                            Status = "running",
                            TimeUpdated = alarms.TimeUpdated
                        });
                    }
                    #endregion
                    var Alarmrecord = db.Alarms.Where(s => s.Serial == alarms.Serial).FirstOrDefault<Alarm>();
                    if (Alarmrecord == null) //not exis (kho xay ra)
                    {
                        // Thêm vào bang Alarm
                        db.Alarms.Add(alarms);
                        SendEmail(alarms);
                    }
                    else
                    {

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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Last update error: " + ex.Message);
                Utilities.WriteLogError(ex);
            }
        }
        public void GetSeting1(byte[] data, string Serial)
        {
            //string deviceType = GetDeviceType(data[8]);
            setting = new Setting();
            setting.SettingByLora = false;
            setting.Devicetype = data[8];
            setting.Serial = Serial;
            setting.ContinueMemoryCount = 0;
            if (data[9] == 0xAC)
            {
                setting.Unit = "Celsius";
                setting.Celsius = true;
            }
            else
            {
                setting.Celsius = false;
                setting.Unit = "Farenheit";
            }
            // continue menory, stopkey
            switch (data[10])
            {
                case 0:
                    setting.ContinueMem = false;
                    setting.Stopkey = true;
                    break;
                case 1:
                    setting.ContinueMem = false;
                    setting.Stopkey = false;
                    break;
                case 2:
                    setting.ContinueMem = true;
                    setting.Stopkey = true;
                    break;
                case 3:
                    setting.ContinueMem = true;
                    setting.Stopkey = false;
                    break;
                default:
                    setting.ContinueMem = false;
                    setting.Stopkey = false;
                    break;
            }
            Status = GetStatus(data[28]);
            try
            {
                setting.Settingtime = new DateTime(data[34] + 2000, data[33], data[32], data[31], data[30], data[29]).ToString();
                setting.Starttime = new DateTime(data[16] + 2000, data[15], data[14], data[13], data[12], data[11]).ToString();
                if (data[17] != 255 & data[17] < 60)
                {
                    setting.Stoptime = new DateTime(data[22] + 2000, data[21], data[20], data[19], data[18], data[17]).ToString();
                }
            }
            catch
            {
                Console.WriteLine("Parse datetime error");
            }

            setting.Delay = data[35];
            setting.DurationDay = (data[36] + data[37] * 256);
            setting.DurationHour = data[38];
            setting.IntervalHour = data[39];
            setting.IntervalMin = data[40];
            setting.IntervalSec = data[41];
            setting.FirmwareVer = "v " + data[50] + "." + data[51];

            //Alarm
            alarmSet = new Alarm();
            alarmSet.Serial = Serial;
            alarmSet.HighAlarmTemp = (data[42] + data[43] * 256) / 10.0;
            alarmSet.LowAlarmTemp = (data[44] + data[45] * 256) / 10.0;
            alarmSet.HighAlarmHumid = (data[46] + data[47] * 256) / 10.0;
            alarmSet.LowAlarmHumid = (data[48] + data[49] * 256) / 10.0;
            if (alarmSet.HighAlarmTemp == 1000.0 && alarmSet.LowAlarmTemp == 1000.0)
            {
                alarmSet.AlarmStatus1 = false;
            }
            else
            {
                alarmSet.AlarmStatus1 = true;
            }

            if (alarmSet.HighAlarmHumid == 1000.0 && alarmSet.LowAlarmHumid == 1000.0)
            {
                alarmSet.AlarmStatus2 = false;
            }
            else
            {
                alarmSet.AlarmStatus2 = true;
            }
            alarmSet.TttAlarm1 = 0;
            alarmSet.TttAlarm2 = 0;
            alarmSet.TttLowAlarm1 = 0;
            alarmSet.TttLowAlarm2 = 0;
            alarmSet.TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
            //Update to tbl  REaltime and Alarms
            using (var db = new Pexo63LorawanEntities())
            {



                db.SaveChanges();
            }
        }
        string GetTimezoneUTC(string Serial)
        {
            using (var db = new Pexo63LorawanEntities())
            {
                var record = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                if (record != null)
                {
                    if (record.TimezoneId != null)
                    {
                        TimeZoneInfo infoTimezone = TimeZoneInfo.FindSystemTimeZoneById(record.TimezoneId);
                        return infoTimezone.DisplayName.Substring(0, 11);
                    }
                }
            }
            return null;
        }
        public void GetSeting2(byte[] data, string Serial)
        {
            //Get timezone 40byte
            if (setting == null)
            {
                return;
            }
            string timezone = Encoding.ASCII.GetString(data, 9, 40).Replace("\0", "").Trim();
            System.Collections.ObjectModel.ReadOnlyCollection<TimeZoneInfo> zones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo zone in zones)
            {
                if (zone.Id.Contains(timezone))
                {
                    setting.TimezoneId = zone.Id;
                    break;
                }
            }
            if (setting.TimezoneId == null)
            {
                setting.TimezoneId = TimeZoneInfo.Local.Id;
            }

            //Console.WriteLine("Timezone ID: " + setting.TimezoneId);
        }
        public void GetSeting3(byte[] data, string Serial)
        {
            if (setting == null)//Check this if not receive packet Setting 1
            {
                return;
            }
            if (alarmSet == null) //Check this if not receive packet Setting 1
            {
                return;
            }
            //Get Description
            setting.Description = Encoding.ASCII.GetString(data, 9, 20).Replace("\0", "");
            setting.IntervalSendLoraMin = data[31];
            setting.IntervalSendLoraHour = data[30];
            setting.IntervalSendLoraDay = data[29];
            //Console.WriteLine("Descrtion: " + descrtiptin);
            //update or insert to database
            using (var db = new Pexo63LorawanEntities())
            {

                var record = db.Settings.Where(s => s.Serial == setting.Serial).FirstOrDefault<Setting>();
                var itemDelete = db.Data.Where(dt => dt.Serial == Serial);
                if (itemDelete != null)
                {
                    db.Data.RemoveRange(itemDelete);
                }
                //delete packet table
                var packetTb1 = db.PacketD0.Where(p => p.Serial == Serial);
                var packetTb2 = db.PacketD1.Where(p => p.Serial == Serial);
                var packetTb3 = db.PacketD2.Where(p => p.Serial == Serial);
                if (packetTb1 != null)
                {
                    db.PacketD0.RemoveRange(packetTb1);
                }
                if (packetTb2 != null)
                {
                    db.PacketD1.RemoveRange(packetTb2);
                }
                if (packetTb3 != null)
                {
                    db.PacketD2.RemoveRange(packetTb3);
                }

                if (record == null) //not exis
                {
                    // Thêm vào database
                    db.Settings.Add(setting);
                }
                else
                {


                    //Cap nhat

                    record.Description = setting.Description;
                    record.DurationDay = setting.DurationDay;
                    record.DurationHour = setting.DurationHour;
                    record.FirmwareVer = setting.FirmwareVer;
                    record.IntervalSec = setting.IntervalSec;
                    record.IntervalMin = setting.IntervalMin;
                    record.IntervalHour = setting.IntervalHour;

                    record.Settingtime = setting.Settingtime;
                    record.Starttime = setting.Starttime;
                    record.Stoptime = setting.Stoptime;
                    record.Unit = setting.Unit;
                    record.Devicetype = setting.Devicetype;
                    record.TimezoneId = setting.TimezoneId;
                    record.IntervalSendLoraMin = setting.IntervalSendLoraMin;
                    record.IntervalSendLoraDay = setting.IntervalSendLoraDay;
                    record.IntervalSendLoraHour = setting.IntervalSendLoraHour;
                    record.SettingByLora = setting.SettingByLora;
                    record.ContinueMem = setting.ContinueMem;
                    record.Delay = setting.Delay;
                    record.Stopkey = setting.Stopkey;
                    record.AutoStart = setting.AutoStart;
                    record.ContinueMemoryCount = setting.ContinueMemoryCount;
                }

                //real time
                string Runtime = new TimeSpan().ToString();// new TimeSpan(data[27] * 256 + data[26], data[25], data[24], data[23]).ToString();

                var realtime = db.Realtimes.FirstOrDefault<Realtime>(u => u.Serial == Serial);
                if (realtime != null) //upadte or insert
                {
                    realtime.Serial = Serial;
                    realtime.Runtime = Runtime;
                    realtime.Status = Status;
                    realtime.Data1 = null;
                    realtime.Data2 = null;
                    realtime.TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
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
                        TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial)
                    });
                }
                //alarm
                var alarm = db.Alarms.FirstOrDefault<Alarm>(u => u.Serial == Serial);
                if (alarm != null) //update
                {
                    //alarm = alarmSet;
                    alarm.Serial = Serial;
                    alarm.HighAlarmTemp = alarmSet.HighAlarmTemp;
                    alarm.LowAlarmTemp = alarmSet.LowAlarmTemp;
                    alarm.HighAlarmHumid = alarmSet.HighAlarmHumid;
                    alarm.LowAlarmHumid = alarmSet.LowAlarmHumid;
                    alarm.TttAlarm1 = 0;
                    alarm.TttAlarm2 = 0;
                    alarm.TttLowAlarm1 = 0;
                    alarm.TttLowAlarm2 = 0;
                    alarm.AlarmStatus1 = alarmSet.AlarmStatus1;
                    alarm.AlarmStatus2 = alarmSet.AlarmStatus2;
                    alarm.TimeUpdated = DateTime.Now.ToString("MMM d HH:mm:ss") + GetTimezoneUTC(Serial);
                }
                else //add new record
                {
                    db.Alarms.Add(alarmSet);

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Utilities.WriteLogError(ex);
                }
                setting = null;
                alarmSet = null;
            }

        }
        public void GetEndSetting(byte[] data, string Serial)
        {
            try
            {
                //Start time
                DateTime StartTime = new DateTime();
                try
                {
                    StartTime = new DateTime(data[14] + 2000, data[13], data[12], data[11], data[10], data[9]);
                }
                catch
                {

                    StartTime = new DateTime();
                }

                //Stop time
                DateTime StopTime = new DateTime();
                try
                {
                    StopTime = new DateTime(data[20] + 2000, data[19], data[18], data[17], data[16], data[15]);
                }
                catch
                {
                    StopTime = new DateTime();

                }

                //get Runtime 

                string runtime = new TimeSpan(data[25] * 256 + data[24], data[23], data[22], data[21]).ToString();
                //alarm time
                Alarm alarms = new Alarm();
                alarms.Serial = Serial;
                alarms.TttAlarm1 = new TimeSpan(data[30] * 256 + data[29], data[28], data[27], data[26]).TotalSeconds;
                alarms.TttLowAlarm1 = new TimeSpan(data[35] * 256 + data[34], data[33], data[32], data[31]).TotalSeconds;
                alarms.TttAlarm2 = new TimeSpan(data[40] * 256 + data[39], data[38], data[37], data[36]).TotalSeconds;
                alarms.TttLowAlarm2 = new TimeSpan(data[45] * 256 + data[44], data[43], data[42], data[41]).TotalSeconds;
                alarms.TimeUpdated = StopTime.ToString("MMM d HH:mm:ss");//Stop time
                                                                         //Data realtime
                var Data1 = convertTemFrom15bit((data[46] + data[47] * 256), 10);
                var Data2 = convertTemFrom15bit((data[48] + data[49] * 256), 10);

                //Add or Update to Realtime Table
                using (var db = new Pexo63LorawanEntities())
                {
                    var recordSt = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                    if (recordSt == null)
                    {
                        return;
                    }
                    var realtime = db.Realtimes.Include(a => a.Setting).FirstOrDefault<Realtime>(u => u.Serial == Serial);
                    if (realtime != null) //not exis
                    {
                        string unit = realtime.Setting.Unit == "Celsius" ? "°C " : "°F";
                        //update to db Settings
                        realtime.Runtime = runtime;
                        realtime.Data1 = Data1.ToString();// + unit;
                        realtime.Data2 = Data2.ToString();// + "%";
                        realtime.Status = "stop";
                        realtime.TimeUpdated = alarms.TimeUpdated;
                    }
                    //update alarm table
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
                            // SendEmail(alarms);
                            //Cap nhat DB
                            Alarmrecord.TttAlarm1 = alarms.TttAlarm1;
                            Alarmrecord.TttAlarm2 = alarms.TttAlarm2;
                            Alarmrecord.TttLowAlarm1 = alarms.TttLowAlarm1;
                            Alarmrecord.TttLowAlarm2 = alarms.TttLowAlarm2;
                            Alarmrecord.TimeUpdated = alarms.TimeUpdated;
                        }
                    }
                    //update settings table
                    var settingRecord = db.Settings.Where(s => s.Serial == Serial).FirstOrDefault<Setting>();
                    if (settingRecord != null) //not exis (kho xay ra)
                    {
                        setting.Stoptime = StopTime.ToString();
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Last update error: " + ex.Message);
                Utilities.WriteLogError(ex);
            }
        }
        public void SendEmail(Alarm alarm)
        {
            var myemail = "marathonlorawan@gmail.com";
            const string fromPassword = "MarathonDgs";
            string subject = "Alarm occurs from logger: " + alarm.Serial;
            var db = new Pexo63LorawanEntities();
            var settingRecord = db.Settings.Include(u => u.Realtime).FirstOrDefault<Setting>(s => s.Serial == alarm.Serial);
            if (settingRecord == null) //not exis
            {
                return;
            }
            string unit2 = "", channel2name = "", channel1name = "";
            if (settingRecord.Devicetype == 0x44 || settingRecord.Devicetype == 0x55 || settingRecord.Devicetype == 0x77)
            {
                unit2 = settingRecord.Unit;

            }
            else
            {
                unit2 = "%";

            }
            string[] deviceType = GetDeviceType(settingRecord.Devicetype).Split(',');
            channel1name = deviceType[1].TrimStart();
            channel2name = deviceType[0];
            if (settingRecord.Email != null)
            {
                try
                {
                    MailMessage mail = new MailMessage();
                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                    mail.From = new MailAddress(myemail);
                    mail.To.Add(settingRecord.Email);
                    mail.Subject = subject;
                    mail.Body = "This is a warning email sent automatically from Lorawan server when logger: \"" + alarm.Serial + "\" exceeds the limit.\r\n";

                    mail.Body += channel1name + " enable alarm: " + alarm.AlarmStatus1 + "\r\n";
                    if (alarm.AlarmStatus1)
                    {
                        mail.Body += "- High alarm: " + alarm.HighAlarmTemp + " " + settingRecord.Unit + "\r\n";
                        mail.Body += "- Low alarm: " + alarm.LowAlarmTemp + " " + settingRecord.Unit + "\r\n";
                    }
                    mail.Body += channel2name + " enable alarm: " + alarm.AlarmStatus2 + "\r\n";
                    if (alarm.AlarmStatus2)
                    {
                        mail.Body += "- High alarm: " + alarm.HighAlarmHumid + " " + unit2 + "\r\n";
                        mail.Body += "- Low alarm: " + alarm.LowAlarmHumid + " " + unit2 + "\r\n";
                    }
                    else
                    {
                        mail.Body += "- High alarm: " + "No alarm" + "\r\n";
                        mail.Body += "- Low alarm: " + "No  alarm" + "\r\n";
                    }
                    mail.Body += "- Current " + channel1name + ": " + settingRecord.Realtime.Data1 + " " + settingRecord.Unit + "\r\n";
                    mail.Body += "- Current " + channel2name + ": " + settingRecord.Realtime.Data2 + " " + settingRecord.Unit + "\r\n";
                    mail.Body += "- Total time high alarm " + channel1name + ": " + TimeSpan.FromSeconds(alarm.TttAlarm1 - alarm.TttLowAlarm1).ToString() + "\r\n";
                    mail.Body += "- Total time low alarm " + channel1name + ": " + TimeSpan.FromSeconds(alarm.TttLowAlarm1).ToString() + "\r\n";
                    mail.Body += "- Total time high alarm " + channel2name + ": " + TimeSpan.FromSeconds(alarm.TttAlarm1 - alarm.TttLowAlarm2).ToString() + "\r\n";
                    mail.Body += "- Total time low alarm " + channel2name + ": " + TimeSpan.FromSeconds(alarm.TttLowAlarm2).ToString() + "\r\n";
                    mail.Body += "- Start time: " + settingRecord.Starttime + "\r\n";
                    mail.Body += "- Run time: " + settingRecord.Realtime.Runtime + "\r\n";
                    mail.Body += "- Time updated: " + alarm.TimeUpdated + "\r\n";

                    mail.Body += "For more information please visit the website: http://113.161.71.163/";
                    mail.Priority = MailPriority.High;
                    SmtpServer.Port = 587;
                    SmtpServer.Credentials = new System.Net.NetworkCredential(myemail, fromPassword);
                    SmtpServer.UseDefaultCredentials = false;
                    SmtpServer.EnableSsl = true;
                    SmtpServer.Send(mail);
                    //MessageBox.Show("mail Send");
                }
                catch (Exception ex)
                {
                    Utilities.WriteLogError(ex);
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
