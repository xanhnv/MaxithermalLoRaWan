using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PagedList;
using MaxithermalWebApplication.Models;
using Microsoft.Reporting.WebForms;

namespace MaxithermalWebApplication.Controllers
{
    public class DataController : Controller
    {
        private Pexo63LorawanEntities db = new Pexo63LorawanEntities();

        // GET: Data
        public ActionResult Index(string id, int? page)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ViewBag.ID = id;
            var data = db.Data1
                          .Where(u => u.Serial == id)
                          .OrderBy(x => x.Time)
                          
                          .ToList();
            var setting = db.Settings.FirstOrDefault(u => u.Serial == id);
            ViewBag.devType = setting.Devicetype;
            ViewBag.UnitTemp = setting.Unit == "Celsius" ? " (°C)" : " (°F)";
            if (setting.TimezoneId != null)
            {
                ViewBag.Timezone = TimeZoneInfo.FindSystemTimeZoneById(setting.TimezoneId).DisplayName.Substring(0, 11);

            }
            else
            {
                ViewBag.Timezone = TimeZoneInfo.Local.DisplayName.Substring(0, 11);
            }

            if (data.Count == 0)
            {
                ViewBag.lastRecordID = 0;


                if (page == null) page = 1;
                return View(data.ToPagedList(1, 1));
            }

            ViewBag.lastRecordID = data.LastOrDefault().ID;

            if (page == null) page = 1;
            int pageSize = 100;
            // Toán tử ?? trong C# mô tả nếu page khác null thì lấy giá trị page, còn
            // nếu page = null thì lấy giá trị 1 cho biến pageNumber.
            int pageNumber = (page ?? 1);
            return View(data.ToPagedList(pageNumber, pageSize));
        }

        //Json get newest Data
        public JsonResult NewestData(string id)
        {
            var newest = db.Data1.OrderByDescending(u => u.ID).Where(u => u.Serial == id).FirstOrDefault();
            if (newest == null)
            {
                return Json(new EmptyResult(), JsonRequestBehavior.AllowGet);
            }
            return Json(new
            {
                ID = newest.ID,
                Serial = newest.Serial,
                Data1 = newest.Data1,
                Data2 = newest.Data2,
                Time = Helper.ToJsonTicks(newest.Time)
            }, JsonRequestBehavior.AllowGet);
        }
        // GET: Data/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Data data = db.Data1.Find(id);
            if (data == null)
            {
                return HttpNotFound();
            }
            return View(data);
        }


        // GET: Data/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Data/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Serial,Data1,Data2,Time")] Data data)
        {
            if (ModelState.IsValid)
            {
                db.Data1.Add(data);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(data);
        }

        // GET: Data/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Data data = db.Data1.Find(id);
            if (data == null)
            {
                return HttpNotFound();
            }
            return View(data);
        }

        // POST: Data/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Serial,Data1,Data2,Time")] Data data)
        {
            if (ModelState.IsValid)
            {
                db.Entry(data).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(data);
        }

        // GET: Data/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Data data = db.Data1.Find(id);
            if (data == null)
            {
                return HttpNotFound();
            }
            return View(data);
        }

        // POST: Data/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Data data = db.Data1.Find(id);
            db.Data1.Remove(data);
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

        public ActionResult Reports(string ReportType, string id)
        {
            LocalReport localReport = new LocalReport();
            localReport.ReportPath = Server.MapPath("~/Reports/ReportData.rdlc");
            ReportDataSource reportDataSource = new ReportDataSource();
            reportDataSource.Name = "AllData";
            reportDataSource.Value = db.Data1
                                              .Where(u => u.Serial == id)
                                              .OrderBy(x => x.ID)
                                              .ToList();
            localReport.DataSources.Add(reportDataSource);
            string reportType = ReportType;
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
            Response.AddHeader("content-disposition", "attachment;filename=Alldata_logger_" + 
                id + "." + fileNameExtention);
            return File(renderedbytes, fileNameExtention);
        }
    }
}
