using MaxithermalWebApplication.Models;
using Microsoft.Reporting.WebForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace MaxithermalWebApplication.Controllers
{
    public class HomeController : Controller
    {

        Pexo63LorawanEntities db = new Pexo63LorawanEntities();
        public ActionResult Index()
        {
            var db = new Pexo63LorawanEntities();

            return View(db.Settings.Include(s => s.Realtime).Include(s=>s.Alarm));

        }
        [HttpGet]
        public JsonResult GetData()
        {
            db.Configuration.ProxyCreationEnabled = false;
            var json = db.Settings.Include(u => u.Realtime).Include(u => u.Alarm).Select(i =>
                new
                {
                    
                    i.Serial,
                    i.Description,
                    i.Location,
                    i.Realtime.Runtime,
                    i.Realtime.Status,
                    i.Alarm.HighAlarmTemp,
                    i.Alarm.LowAlarmTemp,
                    i.Alarm.AlarmStatus1,
                    i.Unit,
                    i.Devicetype,
                    i.Realtime.Data1,
                    i.Realtime.Data2,
                    i.Realtime.TimeUpdated
                });

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        // GET: Setting/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting datum = db.Settings.Include(u => u.Realtime).FirstOrDefault(u => u.Serial == id);
            if (datum == null)
            {
                return HttpNotFound();
            }
            return View(datum);
        }
        // GET: Home/Alarm/ID
        public ActionResult Alarm(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Alarm alarm = db.Alarms.Include(a => a.Setting).FirstOrDefault(u => u.Serial == id);
            //if (alarm == null)
            //{
            //    return HttpNotFound();
            //}
            ViewBag.ID = id;
            return View(alarm);
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        // GET: Home/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting data = db.Settings.Find(id);
            if (data == null)
            {
                return HttpNotFound();
            }
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Serial, Description, Location, Email")
            ] Setting data)
        {
            if (ModelState.IsValid)
            {
                db.Entry(data).State = EntityState.Modified;
                db.Entry(data).Property(x => x.Unit).IsModified = false;
                db.Entry(data).Property(x => x.Starttime).IsModified = false;
                db.Entry(data).Property(x => x.Stoptime).IsModified = false;
                db.Entry(data).Property(x => x.Settingtime).IsModified = false;
                db.Entry(data).Property(x => x.FirmwareVer).IsModified = false;
                db.Entry(data).Property(x => x.DurationDay).IsModified = false;
                db.Entry(data).Property(x => x.DurationHour).IsModified = false;
                db.Entry(data).Property(x => x.IntervalHour).IsModified = false;
                db.Entry(data).Property(x => x.IntervalMin).IsModified = false;
                db.Entry(data).Property(x => x.IntervalSec).IsModified = false;
                db.Entry(data).Property(x => x.IntervalSendLoraMin).IsModified = false;
                db.Entry(data).Property(x => x.IntervalSendLoraHour).IsModified = false;
                db.Entry(data).Property(x => x.IntervalSendLoraDay).IsModified = false;
                db.Entry(data).Property(x => x.Devicetype).IsModified = false;
                db.Entry(data).Property(x => x.TimezoneId).IsModified = false;
                db.Entry(data).Property(x => x.Celsius).IsModified = false;
                db.Entry(data).Property(x => x.Delay).IsModified = false;
                db.Entry(data).Property(x => x.AutoStart).IsModified = false;
                db.Entry(data).Property(x => x.ContinueMem).IsModified = false;
                db.Entry(data).Property(x => x.Stopkey).IsModified = false;
                db.Entry(data).Property(x => x.SettingByLora).IsModified = false;
                //db.Entry(data).Property(x => x.Delay).IsModified = false;

                db.SaveChanges();
                return RedirectToAction("Index");
            }


            return View(data);
        }
        public ActionResult SettingLogger(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting data = db.Settings.Include(a => a.Alarm).FirstOrDefault(u => u.Serial == id);

            return View(data);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SettingLogger(Setting data)
        {
            //[Bind(Include = "Serial,Celsius,ContinueMem,StopKey,AutoStart,Delay," +
            // "AutoStarttime,DurationDay,DurationHour,IntervalSec,IntervalMin,IntervalHour,IntervalSendLoraMin,IntervalSendLoraHour," +
            //"IntervalSendLoraDay" + ",Alarm.HighAlarmTemp,Alarm.LowAlarmTemp,Alarm.HighAlarmHumid,Alarm.LowAlarmHumid,Alarm.AlarmStatus1,Alarm.AlarmStatus2"
            // )]
            if (ModelState.IsValid)
            {
                db.Entry(data).State = EntityState.Modified;

                db.Entry(data).Property(x => x.Unit).IsModified = false;
                db.Entry(data).Property(x => x.Description).IsModified = false;
                db.Entry(data).Property(x => x.Email).IsModified = false;
                db.Entry(data).Property(x => x.Location).IsModified = false;
                db.Entry(data).Property(x => x.Starttime).IsModified = false;
                db.Entry(data).Property(x => x.Stoptime).IsModified = false;
                db.Entry(data).Property(x => x.Settingtime).IsModified = false;
                db.Entry(data).Property(x => x.FirmwareVer).IsModified = false;
                db.Entry(data).Property(x => x.Devicetype).IsModified = false;
                db.Entry(data).Property(x => x.TimezoneId).IsModified = false;

                db.Entry(data).Property(x => x.SettingByLora).CurrentValue = true;
                //alarm

                var alarmRec = db.Alarms.Where(a => a.Serial == data.Serial).FirstOrDefault();
                db.Entry(alarmRec).State = EntityState.Modified;
                db.Entry(alarmRec).Property(x => x.AlarmStatus1).CurrentValue = data.Alarm.AlarmStatus1;
                db.Entry(alarmRec).Property(x => x.AlarmStatus2).CurrentValue = data.Alarm.AlarmStatus2;
                if (data.Alarm.AlarmStatus1)
                {
                    db.Entry(alarmRec).Property(x => x.HighAlarmTemp).CurrentValue = data.Alarm.HighAlarmTemp;
                    db.Entry(alarmRec).Property(x => x.LowAlarmTemp).CurrentValue = data.Alarm.LowAlarmTemp;
                }
                else
                {
                    db.Entry(alarmRec).Property(x => x.HighAlarmTemp).CurrentValue = 1000;
                    db.Entry(alarmRec).Property(x => x.LowAlarmTemp).CurrentValue = 1000;
                }
                if (data.Alarm.AlarmStatus2)
                {
                    db.Entry(alarmRec).Property(x => x.HighAlarmHumid).CurrentValue = data.Alarm.HighAlarmHumid;
                    db.Entry(alarmRec).Property(x => x.LowAlarmHumid).CurrentValue = data.Alarm.LowAlarmHumid;
                }
                else
                {
                    db.Entry(alarmRec).Property(x => x.HighAlarmHumid).CurrentValue = 1000;
                    db.Entry(alarmRec).Property(x => x.LowAlarmHumid).CurrentValue = 1000;
                }

                db.Realtimes.Where(s => s.Serial == data.Serial).FirstOrDefault().Status = "has set";
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(data);
        }

        public ActionResult Export(string id)
        {
            LocalReport localReport = new LocalReport();
            localReport.ReportPath = Server.MapPath("~/Reports/ReportSum.rdlc");
            ReportDataSource reportDataSource = new ReportDataSource();
            reportDataSource.Name = "AllData";
            reportDataSource.Value = db.Data1
                                              .Where(u => u.Serial == id)
                                              .OrderBy(x => x.ID)
                                              .ToList();

            localReport.DataSources.Add(reportDataSource);
            ReportDataSource SettingDataSource = new ReportDataSource();
            SettingDataSource.Name = "SettingDataSet";
            var settings= db.Settings.Where(u => u.Serial == id).ToList();
            SettingDataSource.Value = settings;

            localReport.DataSources.Add(SettingDataSource);
            ReportDataSource alarmDataSource = new ReportDataSource();
            alarmDataSource.Name = "AlarmDataSet";
            alarmDataSource.Value = db.Alarms
                                              .Where(u => u.Serial == id)
                                              .ToList();

            localReport.DataSources.Add(alarmDataSource);
            string devType = "";
            string displayTimezone = "";
            switch (settings[0].Devicetype)
            {
                case 0x11:
                    devType = "Humidity, Room temperature";
                    break;
                case 0x22:
                    devType = "Humidity, LN2";
                    break;
                case 0x33:
                    devType = "Humidity, RTD2";
                    break;
                case 0x44:
                    devType = "LN2, Room Temperature";
                    break;
                case 0x55:
                    devType = "RTD2, Room temperature";
                    break;
                case 0x66:
                    devType = "Thermal couple, Humidity";
                    break;
                case 0x77:
                    devType = "Room temperature, Thermal couple";
                    break;
            }
            if (settings[0].TimezoneId != null)
            {
                TimeZoneInfo zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(settings[0].TimezoneId);
                displayTimezone = zoneInfo.DisplayName;
            }
            ReportParameter[] para = new ReportParameter[3];
            para[0] = new ReportParameter("Serial",id);
            para[1] = new ReportParameter("Timezone", displayTimezone);
            para[2] = new ReportParameter("Loggersensor",devType);
            localReport.SetParameters(para);
            string reportType = "Pdf";
            string mimeType;
            string encoding;
            string fileNameExtention;
            if (reportType == "Excel")
            {
                fileNameExtention = "xlsx";
            }
            else if (reportType == "Word")
            {
                fileNameExtention = "docx";
            }
            else
            {
                fileNameExtention = "pdf";
            }
            string[] streams;
            byte[] renderedbytes;
            Warning[] warnings;
            renderedbytes = localReport.Render(reportType, "", out mimeType, out encoding,
                out fileNameExtention, out streams, out warnings);
            Response.AddHeader("content-disposition", "attachment;filename=data_logger_" + id + "." + fileNameExtention);
            return File(renderedbytes, fileNameExtention);
        }
        
        // POST: DataToSends/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Serial,Description,Unit,ContinueMem,StopKey,AutoStart,Delay,AutoStarttime,DurationDay,DurationHour,IntervalSec,IntervalMin,IntervalHour,IntervalSendLora,TimezoneId,HighAlarmTemp,LowAlarmTemp,HighAlarmHumid,LowAlarmHumid,AlarmStatus1,AlarmStatus2")] DataToSend dataToSend)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(dataToSend).State = EntityState.Modified;

        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    ViewBag.Serial = new SelectList(db.Settings, "Serial", "Description", dataToSend.Serial);
        //    return View(dataToSend);
        //}

    }
}