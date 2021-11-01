using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NETJDC.Request
{
    public class RequestEntity
    {
        public string Phone { get; set; }
        public int qlkey { get; set; } = 0;

        public string Code { get; set; }
    }
    public class RequestDEL
    {
        public int qlkey { get; set; } = 0;

        public string qlid { get; set; }
    }
    public class Requestremarks
    {
        public int qlkey { get; set; } = 0;

        public string remarks { get; set; }

        public string qlid { get; set; }
    }
}
