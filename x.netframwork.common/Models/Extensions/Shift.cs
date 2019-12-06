using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Shift
    {
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public double Relax { get; set; } = 0;
    }
}
