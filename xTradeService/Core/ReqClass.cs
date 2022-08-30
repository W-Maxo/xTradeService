using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Serialization;

namespace xTradeService.Core
{
    internal class DataReq
    {
        private readonly SqlConnection _myConnection;

        public DataReq(SqlConnection myConnection)
        {
            _myConnection = myConnection;
        }

        public int RecTvID { private get; set; }
        public int TvID { private get; set; }
        public int Count { private get; set; }
        public int CurrencyID { private get; set; }

        public bool Insert()
        {
            try
            {
                var insertCommand = new SqlCommand("INSERT INTO [dbo].[DataReq] (" +
                                                   "RecTvID, " +
                                                   "TvID, " +
                                                   "Count, " +
                                                   "CurrencyID) VALUES (" +
                                                   "@xRecTvID, " +
                                                   "@xTvID, " +
                                                   "@xCount, " +
                                                   "@xCurrencyID)", _myConnection);


                insertCommand.Parameters.Add(new SqlParameter("@xRecTvID", typeof (int))).Value = RecTvID;
                insertCommand.Parameters.Add(new SqlParameter("@xTvID", typeof (int))).Value = TvID;
                insertCommand.Parameters.Add(new SqlParameter("@xCount", typeof (float))).Value = Count;
                insertCommand.Parameters.Add(new SqlParameter("@xCurrencyID", typeof (float))).Value = CurrencyID;

                insertCommand.ExecuteNonQuery();

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    [XmlRoot("Req")]
    public class ReqClass
    {
        [XmlIgnore]
        public string Title { get; set; }

        [XmlIgnore]
        public string Description { get; set; }

        [XmlIgnore]
        public DateTime Category { get; set; }

        [XmlAttribute("ReqTvID")]
        public int ReqTvID { get; set; }

        [XmlAttribute("IDCl")]
        public int IDClient { get; set; }

        [XmlAttribute("DateCr")]
        public DateTime DateCreation { get; set; }

        [XmlAttribute("DateDel")]
        public DateTime DateDelivery { get; set; }

        [XmlAttribute("UrID")]
        public int UserID { get; set; }

        [XmlAttribute("Disc")]
        public double Discount { get; set; }

        [XmlAttribute("IDClP")]
        public int IDClientPoint { get; set; }

        [XmlAttribute("PaymentID")]
        public int PaymentID { get; set; }

        [XmlAttribute("PrID")]
        public int PriorityID { get; set; }

        [XmlAttribute("ReqStID")]
        public int ReqStatusID { get; set; }

        [XmlAttribute("Num")]
        public string Number { get; set; }

        [XmlAttribute("CurrID")]
        public int CurrencyID { get; set; }

        [XmlAttribute("Note")]
        public string Note { get; set; }

        [XmlAttribute("WID")]
        public int WarehouseID { get; set; }

        [XmlArray("ReqItemList")]
        public List<ReqItem> ReqItemList { get; set; }

        [XmlAttribute("Pst")]
        public bool Posted { get; set; }

        [XmlAttribute("UnqStr")]
        public string UnqStr { get; set; }

        private readonly SqlConnection _myConnection;

        private string GetConnectionString()
        {
            return Properties.Settings.Default.xTradeConnectionString;
        }

        public ReqClass()
        {
            ReqItemList = new List<ReqItem>();
            _myConnection = new SqlConnection(GetConnectionString());
        }

        public int GetLastItem()
        {
            var selectCommand = new SqlCommand("SELECT DISTINCT TOP 1 RecTvID FROM dbo.Requests ORDER BY RecTvID DESC", _myConnection);

            SqlDataReader myReader = selectCommand.ExecuteReader();

            int xTypeID = 0;

            while (myReader.Read())
            {
                xTypeID = myReader.GetInt32(0);
            }

            myReader.Close();
            return xTypeID;
        }

        public bool Insert()
        {
            try
            {
                #region Insert

                var insertCommand = new SqlCommand("INSERT INTO [dbo].[Requests] (" +
                                                   "IDClient, " +
                                                   "DateCreation, " +
                                                   "DateDelivery, " +
                                                   "UserID, " +
                                                   "Discount, " +
                                                   (IDClientPoint != -1 ? "IDClientPoint, " : string.Empty) +
                                                   "TPaymentID, " +
                                                   "PriorityID, " +
                                                   "ReqStatusID, " +
                                                   "Number, " +
                                                   "CurrencyID, " +
                                                   "Note, " +
                                                   "WarehouseID," +
                                                   "UnqStr) VALUES" +
                                                   "(" +
                                                   "@IDClient, " +
                                                   "@DateCreation, " +
                                                   "@DateDelivery, " +
                                                   "@UserID, " +
                                                   "@Discount, " +
                                                   (IDClientPoint != -1 ? "@IDClientPoint, " : string.Empty) +
                                                   "@TPaymentID, " +
                                                   "@PriorityID, " +
                                                   "@ReqStatusID, " +
                                                   "@Number, " +
                                                   "@CurrencyID, " +
                                                   "@Note, " +
                                                   "@WarehouseID," +
                                                   "@UnqStr)", _myConnection);

                #endregion

                #region Add Parameters

                insertCommand.Parameters.Add(new SqlParameter("@IDClient", typeof (int))).Value = IDClient;
                insertCommand.Parameters.Add(new SqlParameter("@DateCreation", typeof (DateTime))).Value = DateCreation;
                insertCommand.Parameters.Add(new SqlParameter("@DateDelivery", typeof (DateTime))).Value = DateDelivery;
                insertCommand.Parameters.Add(new SqlParameter("@UserID", typeof (int))).Value = UserID;
                insertCommand.Parameters.Add(new SqlParameter("@Discount", typeof (float))).Value = Discount;

                if (IDClientPoint != -1)
                    insertCommand.Parameters.Add(new SqlParameter("@IDClientPoint", typeof (int))).Value = IDClientPoint;

                insertCommand.Parameters.Add(new SqlParameter("@TPaymentID", typeof (int))).Value = PaymentID;
                insertCommand.Parameters.Add(new SqlParameter("@PriorityID", typeof (int))).Value = PriorityID;
                insertCommand.Parameters.Add(new SqlParameter("@ReqStatusID", typeof (int))).Value = ReqStatusID;

                if (null == Number) Number = string.Empty;

                insertCommand.Parameters.Add(new SqlParameter("@Number", typeof (string))).Value = Number;
                insertCommand.Parameters.Add(new SqlParameter("@CurrencyID", typeof (int))).Value = CurrencyID;
                insertCommand.Parameters.Add(new SqlParameter("@Note", typeof (string))).Value = Note;
                insertCommand.Parameters.Add(new SqlParameter("@WarehouseID", typeof (int))).Value = WarehouseID;
                insertCommand.Parameters.Add(new SqlParameter("@UnqStr", typeof(string))).Value = UnqStr;

                #endregion

                _myConnection.Open();

                insertCommand.ExecuteNonQuery();

                int numReq = GetLastItem();

                if (ReqItemList.Select(ri => new DataReq(_myConnection)
                                                 {
                                                     Count = ri.Count,
                                                     CurrencyID = ri.CurrencyID,
                                                     RecTvID = numReq,
                                                     TvID = ri.TvID
                                                 }).Any(dr => !dr.Insert()))
                {
                    return false;
                }

                _myConnection.Close();

                return true;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
           
                return false;
            }
        }
    }
}