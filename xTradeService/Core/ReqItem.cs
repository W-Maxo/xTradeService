using System.Xml.Serialization;

namespace xTradeService.Core
{
    public class ReqItem
    {
        [XmlAttribute("TvID")]
        public int TvID { get; set; }

        [XmlAttribute("CodeTv")]
        public int CodeTv { get; set; }

        [XmlAttribute("Count")]
        public int Count { get; set; }

        [XmlAttribute("CostTv")]
        public double CostTv { get; set; }

        [XmlAttribute("CurrID")]
        public int CurrencyID { get; set; }

        [XmlIgnore]
        public string ItemName { get; set; }

        [XmlIgnore]
        public bool btnen { get; set; }

    }
}