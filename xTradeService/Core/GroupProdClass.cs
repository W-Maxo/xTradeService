using System.Collections;

namespace xTradeService.Core
{
    public class GroupProdClass
    {
        public string GroupName  { get; private set; }
        public SortedList Tp { get; private set; }

        public GroupProdClass(string grnm)
        {
            Tp = new SortedList();
            GroupName = grnm;
        }
    }
}