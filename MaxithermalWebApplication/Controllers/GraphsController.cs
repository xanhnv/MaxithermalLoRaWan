using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using MaxithermalWebApplication.Models;
using Newtonsoft.Json;

namespace MaxithermalWebApplication.Controllers
{
    public class GraphsController : Controller
    {
        private Pexo63LorawanEntities db = new Pexo63LorawanEntities();

        // GET: Graphs
        public ActionResult Index()
        {
            var settings = db.Settings.Include(s => s.Alarm).Include(s => s.Realtime);
            return View(settings.ToList());
        }

        public JsonResult Chart()
        {
            List<List<double>> Data = new List<List<double>>();
            List<List<DateTime>> Date = new List<List<DateTime>>();
            List<List<PointChart>> pointCharts = new List<List<PointChart>>();
            var setting = db.Settings.Select(u => u.Serial).ToList();
            for (int i = 0; i < setting.Count(); i++)
            {
                var serial = setting[i];
                Data.Add(db.Data1.Where(u => u.Serial == serial)
                                     .Select(u => u.Data1)
                                     .ToList());

                Date.Add(db.Data1
                           .Where(u => u.Serial == serial)
                           .Select(u => u.Time)
                           .ToList());

            }

            //var dateUnix = new List<double>();

            //foreach (var item in date)
            //{
            //    dateUnix.Add(Helper.ToJsonTicks(item));
            //}
            //var dateStr = new List<string>();
            for (int j = 0; j < Data.Count; j++)
            {
                pointCharts.Add(new List<PointChart>());
                for (int i = 0; i <100; i++)
                {
                    pointCharts[j].Add(new PointChart( Data[j][i], Date[j][i].ToString("MMM d HH:mm:ss")));
                }
            }
            
            var json =  JsonConvert.SerializeObject(Data, Formatting.None);
            //JsonConvert.SerializeObject()
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        // GET: Graphs/Details/5
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting setting = db.Settings.Find(id);
            if (setting == null)
            {
                return HttpNotFound();
            }
            return View(setting);
        }

        // GET: Graphs/Create
        public ActionResult Create()
        {
            ViewBag.Serial = new SelectList(db.Alarms, "Serial", "TimeUpdated");
            ViewBag.Serial = new SelectList(db.Realtimes, "Serial", "Status");
            return View();
        }

        // POST: Graphs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Serial,Description,Location,Email,Unit,Delay,Starttime,Stoptime,Settingtime,Duration,Interval,HighAlarmTemp,LowAlarmTemp,HighAlarmHumid,LowAlarmHumid,AlarmStatus,FirmwareVer")] Setting setting)
        {
            if (ModelState.IsValid)
            {
                db.Settings.Add(setting);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Serial = new SelectList(db.Alarms, "Serial", "TimeUpdated", setting.Serial);
            ViewBag.Serial = new SelectList(db.Realtimes, "Serial", "Status", setting.Serial);
            return View(setting);
        }

        // GET: Graphs/Edit/5
        public ActionResult Edit(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting setting = db.Settings.Find(id);
            if (setting == null)
            {
                return HttpNotFound();
            }
            ViewBag.Serial = new SelectList(db.Alarms, "Serial", "TimeUpdated", setting.Serial);
            ViewBag.Serial = new SelectList(db.Realtimes, "Serial", "Status", setting.Serial);
            return View(setting);
        }

        // POST: Graphs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Serial,Description,Location,Email,Unit,Delay,Starttime,Stoptime,Settingtime,Duration,Interval,HighAlarmTemp,LowAlarmTemp,HighAlarmHumid,LowAlarmHumid,AlarmStatus,FirmwareVer")] Setting setting)
        {
            if (ModelState.IsValid)
            {
                db.Entry(setting).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Serial = new SelectList(db.Alarms, "Serial", "TimeUpdated", setting.Serial);
            ViewBag.Serial = new SelectList(db.Realtimes, "Serial", "Status", setting.Serial);
            return View(setting);
        }

        // GET: Graphs/Delete/5
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Setting setting = db.Settings.Find(id);
            if (setting == null)
            {
                return HttpNotFound();
            }
            return View(setting);
        }

        // POST: Graphs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            Setting setting = db.Settings.Find(id);
            db.Settings.Remove(setting);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
