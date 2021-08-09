using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UDPServerAndWebSocketClient
{
    public class Txpk
    {
        public Txpk(bool imme, double tmst, double freq, int rfch, int powe, string modu, string datr, string codr, bool ipol, int size, bool ncrc, string data)
        {
            this.imme = imme;
            this.tmst = tmst;
            this.freq = freq;
            this.rfch = rfch;
            this.powe = powe;
            this.modu = modu;
            this.datr = datr;
            this.codr = codr;
            this.ipol = ipol;
            this.size = size;
            this.ncrc = ncrc;
            this.data = data;
        }

        public bool imme { get; set; }
        public double tmst { get; set; }
        public double freq { get; set; }
        public int rfch { get; set; }
        public int powe { get; set; }
        public string modu { get; set; }
        public string datr { get; set; }
        public string codr { get; set; }
        public bool ipol { get; set; }
        public int size { get; set; }
        public bool ncrc { get; set; }
        public string data { get; set; }
    }

}
