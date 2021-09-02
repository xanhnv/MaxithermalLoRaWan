
using MaxithermalWebApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.Reporting.WebForms;
using System.IO;
using System.Text;

namespace MaxithermalWebApplication.Controllers
{
    public class GraphController : Controller
    {
        Pexo63LorawanEntities db = new Pexo63LorawanEntities();
        public ActionResult Index(string id)
        {
            //Include.
            Setting setting = db.Settings.Include(u => u.Alarm).FirstOrDefault(a => a.Serial == id);
            ViewBag.ID = id;
            if (setting.TimezoneId != null)
            {
                ViewBag.Timezone = TimeZoneInfo.FindSystemTimeZoneById(setting.TimezoneId).DisplayName.Substring(0, 11);

            }
            else
            {
                ViewBag.Timezone = TimeZoneInfo.Local.DisplayName;
            }

            return View(setting);
        }
        public JsonResult Chart(string id)
        {

            var data1 = db.Data1
                            .Where(u => u.Serial == id)
                            .OrderByDescending(u => u.Time)
                            .Take(60000)
                            .OrderBy(u => u.Time)
                            .Select(u => u.Data1)
                            .ToList();
            var data2 = db.Data1
                            .Where(u => u.Serial == id)                            
                            .OrderByDescending(u => u.Time)
                            .Take(60000)
                            .OrderBy(u => u.Time)
                            .Select(u => u.Data2)
                            .ToList();
            var date = db.Data1
                            .Where(u => u.Serial == id)
                             .OrderByDescending(u => u.Time)
                             .Take(60000)
                             .OrderBy(u => u.Time)
                            .Select(u => u.Time)
                            .ToList();
            //var Data1 = db.Data1
            //               .Where(u => u.Serial == id)
            //               .OrderBy(u=>u.Time)
            //               .ToList();
            //date.RemoveRange(0, 32000);
            //data1.RemoveRange(0, 32000);
            //data2.RemoveRange(0, 32000);
            //var dateUnix = new List<double>();

            //foreach (var item in Data1)
            //{
            //    dateUnix.Add(Helper.ToJsonTicks(item));
            //}
            var dateStr = new List<string>();
            foreach (var item in date)
            {
                dateStr.Add(item.ToString("MMM d HH:mm:ss"));
            }

            List<object> iData = new List<object>();
            iData.Add(data1);
            iData.Add(data2);
            iData.Add(dateStr);
            return Json(iData, JsonRequestBehavior.AllowGet);
        }

        // GET: Graph/Getnewdata/5
        public JsonResult GetNewData(string id)
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
                Time = newest.Time.ToString("MMM d HH:mm:ss")
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Export(string type, string id)
        {
            LocalReport localReport = new LocalReport();
            localReport.EnableExternalImages = true;

            string FilePath = @"file:\" + AppDomain.CurrentDomain.BaseDirectory + "\\" + "Reports\\reportGraph.png";

            localReport.ReportPath = Server.MapPath("~/Reports/ReportSum.rdlc");
            ReportDataSource reportDataSource = new ReportDataSource();
            reportDataSource.Name = "AllData";
            reportDataSource.Value = db.Data1
                                              .Where(u => u.Serial == id)
                                              .OrderBy(x => x.Time)
                                              .ToList();

            localReport.DataSources.Add(reportDataSource);
            ReportDataSource SettingDataSource = new ReportDataSource();
            SettingDataSource.Name = "SettingDataSet";
            var settings = db.Settings.Where(u => u.Serial == id).ToList();
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
            string unit1 = settings[0].Celsius ? " (°C)" : " (°F)";
            string unit2 = "";
            string title1 = "";
            string title2 = "";

            switch (settings[0].Devicetype)
            {
                case 0x11:
                    title2 = "Humidity";
                    title1 = " Room temperature";

                    break;
                case 0x22:
                    title2 = "Humidity";
                    title1 = "LN2";
                    break;
                case 0x33:
                    title2 = "Humidity";
                    title1 = "RTD2";
                    break;
                case 0x44:
                    title2 = "LN2";
                    title1 = "Room Temperature";
                    break;
                case 0x55:
                    title2 = "RTD2";
                    title1 = "Room temperature";
                    break;
                case 0x66:
                    title2 = "Humidity";
                    title1 = "Thermal couple";
                    break;
                case 0x77:
                    title2 = "Thermal couple";
                    title1 = "Room temperature";
                    break;
            }
            devType = title1 + "," + title2;
            if (title2 == "Humidity")
            {
                unit2 = "%";
            }
            else
            {
                unit2 = unit1;
            }
            if (settings[0].TimezoneId != null)
            {
                TimeZoneInfo zoneInfo = TimeZoneInfo.FindSystemTimeZoneById(settings[0].TimezoneId);
                displayTimezone = zoneInfo.DisplayName;
            }

            ReportParameter[] para = new ReportParameter[8];
            para[0] = new ReportParameter("Serial", id);
            para[1] = new ReportParameter("Timezone", displayTimezone);
            para[2] = new ReportParameter("Loggersensor", devType);
            para[3] = new ReportParameter("unit1", unit1);
            para[4] = new ReportParameter("unit2", unit2);
            para[5] = new ReportParameter("channel1Name", title1);
            para[6] = new ReportParameter("channel2Name", title2);
            para[7] = new ReportParameter("ImagePath", FilePath);

            localReport.SetParameters(para);
            string reportType = type;
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
        [HttpPost]
        public ActionResult SaveImage(string imageData)
        {
            //Server.MapPath
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Reports\\reportGraph.png";

            FileStream fs = new FileStream(path, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            byte[] data = Convert.FromBase64String(imageData);

            bw.Write(data);
            bw.Close();
            return View();
        }

        public ActionResult ExportCSV(string id)
        {
            var lstStudents = db.Data1.Where(u => u.Serial == id)
                                     .OrderBy(x => x.Time)
                                     .ToList();
            StringBuilder sb = new StringBuilder();
            foreach (var item in lstStudents)
            {
                //Append data with comma(,) separator.
                sb.Append(item.Time.ToString() + ',' + item.Data1 + "," + item.Data2);
                //Append new line character.
                sb.Append("\r\n");
            }
            return File(Encoding.ASCII.GetBytes(sb.ToString()), "text/csv", id + ".csv");
        }
    }
}