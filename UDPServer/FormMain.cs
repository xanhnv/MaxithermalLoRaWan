using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using Microsoft.Win32;

namespace UDPServerAndWebSocketClient
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //code 0
            UDPSocket s = new UDPSocket(this);
            s.Server(17000);
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            registryKey.SetValue("UDP Server", Application.ExecutablePath.ToString());
        }
        private void TsGeneralInfomation_Click(object sender, EventArgs e)
        {
            DataMessageWithKey();
        }
        public void DataMessageWithKey()
        {
            var phyLoadData = Convert.FromBase64String("QG8UASYAKAAC7J3iMMFbFD/yefYg");
            var NwkSKey = "5ED438E5C86EDD00CE0ED6222A99E684";
            var AppSkey = "29CBD05A4CB9FBC5166A89E671C0EFCE";

            var phyPayload = new PHYPayload(phyLoadData, NwkSKey, AppSkey);
            var m = (DataMessageWithKey)phyPayload.Message;
            var ttt = m.Pirnt();
            Console.WriteLine(ttt);
        }
        public void JoinRequestMessage()
        {
            var phyLoadData = Convert.FromBase64String("ANwAANB+1bNwHm/t9XzurwDIhgMK8sk=");
            var phyPayload = new PHYPayload(phyLoadData);
            var m = (JoinRequestMessage)phyPayload.Message;
            m.Pirnt();
        }
        public void JoinAcceptMessage()
        {
            var phyLoadData = Convert.FromBase64String("IIE/R/UI/6JnC24j4B+EueJdnEEV8C7qCz3T4gs+ypLa");
            var phyPayload = new PHYPayload(phyLoadData);
            var m = (JoinAcceptMessage)phyPayload.Message;
            var ttt = m.Pirnt();
        }
        public void DataMessage()
        {
            var phyLoadData = Convert.FromBase64String("QCkuASaAAAAByFaF53Iu+vzmwQ==");
            var phyPayload = new PHYPayload(phyLoadData);
            var m = (DataMessage)phyPayload.Message;
            var ttt = m.Pirnt();
        }

        private void TsReadData_Click(object sender, EventArgs e)
        {
            LoadDB();
        }
        void LoadDB()
        {
            string dbPath = AppDomain.CurrentDomain.BaseDirectory;
            string sql = "";
            try
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source =" + dbPath + "\\MyDB.db");
                //SQLiteConnection conn = new SQLiteConnection("Data Source ="+dbPath);
                try
                {
                    // Mở CSDL
                    conn.Open();

                    sql = "create table highscores (name varchar(20), score int)";

                    SQLiteCommand command = new SQLiteCommand(sql, conn);
                    command.ExecuteNonQuery();

                    sql = "insert into highscores (name, score) values ('Me', 9001)";

                    command = new SQLiteCommand(sql, conn);
                    command.ExecuteNonQuery();
                }
                catch (SQLiteException ex)
                {
                    // thường có 2 trường hợp lỗi ở đây:
                    // 1. Tập tin CSDL không truy cập được.
                    // 2. Mật mã không đúng.
                    Utilities.WriteLogError(ex.Message);
                }


                // Tạo bộ đọc dữ liệu
                SQLiteDataAdapter da = new SQLiteDataAdapter(sql, conn);
                // Nạp dữ liệu
                DataTable dt = new DataTable();
                da.Fill(dt);
                dataGridView1.DataSource = dt;
                // Trả bộ nhớ
                da.Dispose();
                dt.Dispose();
            }
            catch (Exception ex)
            {
                Utilities.WriteLogError(ex.Message);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void ReturnJsonStringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Txpk txpk = new Txpk(false, 6000000, 866.55, 0, 27, "LORA", "SF10BW125", "4/5", true, 12, true, "YDkaBCYgAQAw+gCy");
            string json = JsonConvert.SerializeObject(txpk);
            MessageBox.Show(json);
            string tx = " \"txpk\":";
            Console.WriteLine(tx + json);
        }

        private void testEncryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EncryptData encrypt = new EncryptData();
            string nwKey = "11E6FD2D9AC364C607B02E32049C586D";
            string appKey = "50AECEA6ADE24F911E3028504B0F6E28";
            string devAddress = "26041A39";
            byte[] devAdd = Helper.StringToByteArray(devAddress);
            int counterUp = 3;//tang sau moi lan gui
            byte[] payload = { 0x22, 0x34, 0x46 };
            var frmPayload = encrypt.encrypt(payload, appKey, devAdd, counterUp);
            Console.WriteLine();
            Console.WriteLine("FrmPayload= " + Helper.ToHexString(frmPayload));
            byte[] MacPayload = encrypt.CreatMacPayload(devAdd, counterUp, frmPayload);
            Console.WriteLine("MACPayload= " + Helper.ToHexString(MacPayload));
            byte[] MIC = encrypt.CalculateMIC(devAdd, counterUp, MacPayload, nwKey);
            Console.WriteLine("Mic= " + Helper.ToHexString(MIC));
            List<byte> dataSend = new List<byte>();
            dataSend.Add(0x60);//add MHDR
            dataSend.AddRange(MacPayload);
            dataSend.AddRange(MIC);
            Console.WriteLine("Data encrypted= " + Helper.ToHexString(dataSend.ToArray()));
        }
        delegate void SetTextCallback(string text);
        public void PrintStatus(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(PrintStatus);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.richTextBox1.Text += "- " + DateTime.Now.ToLongTimeString() + ": " + text + "\r\n";
                //this.richTextBox1.ScrollToCaret();
            }

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // set the current caret position to the end
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            // scroll it automatically
            richTextBox1.ScrollToCaret();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Utilities.WriteLogError("Tắt chương trình");
        }

        private void Form1_MinimumSizeChanged(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            notifyIcon1.Visible = true;
            notifyIcon1.ShowBalloonTip(3000);
            this.ShowInTaskbar = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                //this.WindowState = FormWindowState.Minimized;
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(2000);
                this.ShowInTaskbar = false;
            }
        }

        private void addNodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Add_Node add_Node = new Add_Node();
            add_Node.ShowDialog();
        }

        internal void PrintData(string frmPayloadStr)
        {
            if (InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(PrintData);
                this.Invoke(d, new object[] { frmPayloadStr });
            }
            else
            {
                this.richTextBox2.Text += "- " + DateTime.Now.ToLongTimeString() + ": " + frmPayloadStr + "\r\n";
                //this.richTextBox1.ScrollToCaret();
            }
        }
    }

}
