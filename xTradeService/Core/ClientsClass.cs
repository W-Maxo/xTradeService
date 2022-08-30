using System.Collections.Generic;

namespace xTradeService.Core
{
    public class ClientsClass   
    {
        public int IDClient { get; set; }
        public string ClientName { get; set; }
        public string Address { get; set; }
        public double Balance { get; set; }
        public string Telephone { get; set; }

        public List<ClientsPointsClass> PCl { get; set; }

        public ClientsClass()
        {
            PCl = new List<ClientsPointsClass>();
        }
    }
}