using System;

namespace DakotaIntegratedSolutions
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
        public string VariableText { get; set; }
        public override string ToString() { return String.Format("{0},{1}", Code, GLN); }
        public string Value() { return String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", Region, Site, Building, Floor, Room, Code, GLN, Date.ToString(), Printed.ToString()); }
        public bool Printed { get; set; }
    }
}
