﻿using System;

namespace AndroidZebraPrint
{
    public class GLNLocation : IGLNLocation
    {
        public string Region { get; set; }
        public string Site { get; set; }
        public string Building { get; set; }
        public string Floor { get; set; }
        public string Room { get; set; }
        public string Code { get; set; }
        public string GLN { get; set; }
        public DateTime Date { get; set; }
        public override string ToString() { //return String.Format("{0},{1},{2},{3},{4},{5},{6},{7}", Region, Site, Building, Floor, Room, Code, GLN, Date.ToString()); }
            return String.Format("{0},{1}", Code, GLN);
        }
    }
}
