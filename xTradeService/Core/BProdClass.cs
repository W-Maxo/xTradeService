using System.Collections.Generic;

namespace xTradeService.Core
{
    public class BProdClass
    {
        public int TvID { get; set; }
        public int CodeTv { get; set; }
        public int TypeID { get; set; }
        public string Name { get; set; }
        public int NimP { get; set; }
        public List<double> CostP { get; private set; }
        public int Remains { get; set; }

        public BProdClass()
        {
            CostP = new List<double>();
        }
    }
}