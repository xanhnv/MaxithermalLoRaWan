using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MaxithermalWebApplication.Models
{
    public class PointChart
    {
        public PointChart(double x, string y)
        {
            this.x = x;
            this.y = y;
        }

        double x { get; set; }
        string y { get; set; }

    }
}