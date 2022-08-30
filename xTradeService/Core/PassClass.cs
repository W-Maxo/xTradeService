using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace xTradeService.Core
{
    class PassClass
    {
        public struct UssStr
        {
            public string UssName;
            public string UssPHash;
            public int UssID;
            public bool AllowLogin;
        }

        private string GetConnectionString()
        {
            return Properties.Settings.Default.xTradeConnectionString;
        }

        private readonly SqlConnection _myConnection;

        public PassClass()
        {
            _myConnection = new SqlConnection(GetConnectionString());
        }

        public IEnumerable<UssStr> GetUserList()
        {
            #region SLQ Init

                SqlDataReader drd = null;
                SqlCommand getUsers = _myConnection.CreateCommand();
                getUsers.CommandText = "GetUssPass";
                getUsers.CommandType = CommandType.StoredProcedure;

            #endregion

            #region GetUserList Dr

                try
                {
                    _myConnection.Open();
                    drd = getUsers.ExecuteReader();

                    if (drd.HasRows)
                    {
                        while (drd.Read())
                        {
                            string xUssName = drd.GetString(0);
                            string xUsspHash = drd.GetString(1);
                            int xUsspID = drd.GetInt32(2);
                            bool xAllowLogin = drd.GetBoolean(3);

                            var mc = new UssStr
                                            {
                                                UssName = xUssName,
                                                UssPHash = xUsspHash,
                                                UssID = xUsspID,
                                                AllowLogin = xAllowLogin
                                            };

                            yield return mc;
                        }
                    }
                }
                finally
                {
                    if (drd != null)
                        drd.Close();
                    _myConnection.Close();
                }

            #endregion 
        }
    }
}
