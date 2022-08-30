using System.Collections.Generic;

namespace xTradeService.Core
{
    public class TpProdClass
    {
        public string TpName { get; private set; }
        public List<BProdClass> Tv { get; private set; }

        public TpProdClass(string tpnm)
        {
            Tv = new List<BProdClass>();
            TpName = tpnm;
        }
    }
}