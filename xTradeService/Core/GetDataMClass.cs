using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace xTradeService.Core
{
    class GetDataMClass
    {
        private readonly SqlConnection _myConnection;

        private string GetConnectionString()
        {
            return Properties.Settings.Default.xTradeConnectionString;
        }

        public GetDataMClass()
        {
            _myConnection = new SqlConnection(GetConnectionString());
        }

        public MemoryStream GetProdData()
        {
            var settings = new XmlWriterSettings
                               {
                                   ConformanceLevel = ConformanceLevel.Document,
                                   CloseOutput = false,
                                   Encoding = Encoding.UTF8,
                                   Indent = true
                               };


            var strm = new MemoryStream();

            var writer = XmlWriter.Create(strm, settings);

            var tp = new Hashtable();
            var groupht = new SortedList();

            var myCommand = new SqlCommand
                                {
                                    CommandType = CommandType.StoredProcedure,
                                    Connection = _myConnection,
                                    CommandText = "GetTovarsForMob"
                                };

            SqlDataReader myReader = null;

            try
            {
                _myConnection.Open();
                myReader = myCommand.ExecuteReader();

                while (myReader.Read())
                {
                    groupht.Add(myReader.GetInt32(0), new GroupProdClass(myReader.GetString(1)));
                }

                myReader.NextResult();

                while (myReader.Read())
                {
                    var gr = myReader.GetInt32(0);
                    if (!groupht.ContainsKey(gr)) continue;

                    var tmptp = (GroupProdClass) groupht[gr];

                    var tpId = myReader.GetInt32(1);
                    var tmpTpProd = new TpProdClass(myReader.GetString(2));

                    tmptp.Tp.Add(tpId, tmpTpProd);
                    tp.Add(tpId, tmpTpProd);
                }

                writer.WriteStartDocument();
                writer.WriteStartElement("xTovars");

                myReader.NextResult();

                var arrs = new List<BProdClass>();
                var costarrs = new Hashtable();

                while (myReader.Read()) //Товары
                {
                    var xTypeID = myReader.GetInt32(0);
                    var xTvID = myReader.GetInt32(1);
                    var xCodeTv = myReader.GetInt32(2);
                    var xName = myReader.GetString(3);
                    var xNimP = myReader.GetInt32(4);
                    var xRemains = myReader.GetInt32(5);

                    var tmpBProdClass = new BProdClass { TypeID = xTypeID, TvID = xTvID, CodeTv = xCodeTv, Name = xName, NimP = xNimP, Remains = xRemains };

                    arrs.Add(tmpBProdClass);

                    costarrs.Add(xTvID, tmpBProdClass.CostP);
                }

                myReader.NextResult();

                while (myReader.Read()) // Цены товаров
                {
                    var xTvID = myReader.GetInt32(0);
                    var xCost = myReader.GetSqlDouble(2);

                    var tmpc = (List<double>) costarrs[xTvID];
                    tmpc.Add(xCost.IsNull ? 0 : xCost.Value);
                }

                var grByCategory = from arr in arrs
                                   group arr by arr.TypeID into c
                                   orderby c.Key
                                   select new PublicGrouping<int, BProdClass>(c);


                foreach (var variable in grByCategory)
                {
                    var tmpTrod = (TpProdClass)tp[variable.Key];

                    foreach (var bProdClass in variable)
                    {
                        tmpTrod.Tv.Add(bProdClass);
                    }
                }

                foreach (GroupProdClass grprcl in groupht.Values)
                {
                    writer.WriteStartElement("Gr");
                    writer.WriteAttributeString("GrNm", grprcl.GroupName);

                    foreach (TpProdClass tpprcl in grprcl.Tp.Values)
                    {
                        writer.WriteStartElement("Tp");
                        writer.WriteAttributeString("TpNm", tpprcl.TpName);

                        foreach (BProdClass bProdClass in tpprcl.Tv)
                        {
                            writer.WriteStartElement("Item");

                            writer.WriteAttributeString("TvID", bProdClass.TvID.ToString(CultureInfo.InvariantCulture));
                            writer.WriteAttributeString("Code", bProdClass.CodeTv.ToString(CultureInfo.InvariantCulture));
                            writer.WriteAttributeString("Desc", bProdClass.Name);

                            var prcnt = 0;

                            foreach (var dtmp in bProdClass.CostP)
                            {
                                prcnt++;
                                var atttmpn = string.Format("Price{0}", prcnt);
                                writer.WriteAttributeString(atttmpn, dtmp.ToString(new CultureInfo("en-US", false)));
                            }

                            writer.WriteAttributeString("Rem", bProdClass.Remains.ToString(CultureInfo.InvariantCulture));
                            writer.WriteAttributeString("CntinP", bProdClass.NimP.ToString(CultureInfo.InvariantCulture));

                            writer.WriteEndElement();
                        }

                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }


                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();

                strm.Position = 0;

                writer.Close();

                return strm;
                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new MemoryStream();
            }
            finally
            {
                if (myReader != null)
                    myReader.Close();

                _myConnection.Close();
            }   
        }

        private void AddReqToXml(ref XmlWriter writer, ReqClass rc)
        {
            writer.WriteStartElement("Request");
            writer.WriteAttributeString("ReqTvID", rc.ReqTvID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("IDClient", rc.IDClient.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("DateCreation", rc.DateCreation.ToString(new CultureInfo("en-US", false)));
            writer.WriteAttributeString("DateDelivery", rc.DateDelivery.ToString(new CultureInfo("en-US", false)));
            writer.WriteAttributeString("UserID", rc.UserID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Discount", rc.Discount.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("IDClientPoint", rc.IDClientPoint.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("TPaymentID", rc.PaymentID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("PriorityID", rc.PriorityID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("ReqStatusID", rc.ReqStatusID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Number", rc.Number.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("CurrencyID", rc.CurrencyID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("Note", rc.Note.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("WarehouseID", rc.WarehouseID.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("UnqStr", rc.UnqStr.ToString(CultureInfo.InvariantCulture));

            foreach (var reqItem in rc.ReqItemList)
            {
                writer.WriteStartElement("Item");

                writer.WriteAttributeString("TvID", reqItem.TvID.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("CodeTv", reqItem.CodeTv.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("Count", reqItem.Count.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("CostTv", reqItem.CostTv.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("CurrencyID", reqItem.CurrencyID.ToString(CultureInfo.InvariantCulture));

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    
        public void GetReqData(int ussID, ref MemoryStream currreq, ref MemoryStream closedreq, ref MemoryStream arrreq)
        {
            var settings = new XmlWriterSettings
                               {
                                   ConformanceLevel = ConformanceLevel.Document,
                                   CloseOutput = false,
                                   Encoding = Encoding.UTF8,
                                   Indent = true
                               };


            var reqht = new Hashtable();

            var myCommand = new SqlCommand
                                {
                                    CommandType = CommandType.StoredProcedure,
                                    Connection = _myConnection,
                                    CommandText = "GetAllReqByUser"
                                };

            myCommand.Parameters.Add("@UssID", SqlDbType.Int, 4);
            myCommand.Parameters["@UssID"].Value = ussID;

            SqlDataReader myReader = null;

            try
            {
                _myConnection.Open();
                myReader = myCommand.ExecuteReader();

                while (myReader.Read())
                {
                    var xReqTvID = myReader.GetInt32(0);
                    var xIDClient = myReader.GetInt32(1);
                    var xDateCreation = myReader.GetDateTime(2);
                    var xDateDelivery = myReader.GetDateTime(3);
                    var xUserID = myReader.GetSqlInt32(4);
                    var xDiscount = myReader.GetDouble(5);
                    var xIDClientPoint = myReader.GetSqlInt32(6);
                    var xTPaymentID = myReader.GetInt32(7);
                    var xPriorityID = myReader.GetInt32(8);
                    var xReqStatusID = myReader.GetInt32(9);
                    var xNumber = myReader.GetString(10);
                    var xCurrencyID = myReader.GetInt32(11);
                    var xNote = myReader.GetString(12);
                    var xWarehouseID = myReader.GetInt32(13);
                    var unqStr = myReader.GetString(14);

                    var rc = new ReqClass
                    {
                        ReqTvID = xReqTvID,
                        IDClient = xIDClient,
                        DateCreation = xDateCreation,
                        DateDelivery = xDateDelivery,
                        UserID = xUserID.IsNull ? -1 : xUserID.Value,
                        Discount = xDiscount,
                        IDClientPoint = xIDClientPoint.IsNull ? -1 : xIDClientPoint.Value,
                        PaymentID = xTPaymentID,
                        PriorityID = xPriorityID,
                        ReqStatusID = xReqStatusID,
                        Number = xNumber,
                        CurrencyID = xCurrencyID,
                        Note = xNote,
                        WarehouseID = xWarehouseID,
                        UnqStr = unqStr
                    };

                    reqht.Add(xReqTvID, rc);
                }

                myReader.NextResult();

                while (myReader.Read())
                {
                    var xRecTvID = myReader.GetInt32(0);
                    var xTvID = myReader.GetInt32(1);
                    var xCodeTv = myReader.GetInt32(2);
                    var xCount = myReader.GetInt32(3);
                    var xCostTv = myReader.GetDouble(4);
                    var xCurrencyID = myReader.GetInt32(5);

                    var tmpReqItem = new ReqItem { TvID = xTvID, CodeTv = xCodeTv, Count = xCount, CostTv = xCostTv, CurrencyID = xCurrencyID };

                    var tmpc = (ReqClass)reqht[xRecTvID];
                    tmpc.ReqItemList.Add(tmpReqItem);
                }

                myReader.NextResult();

                var currreqstrm = new MemoryStream();
                var closedreqstrm = new MemoryStream();
                var arrreqstrm = new MemoryStream();

                var currreqwriter = XmlWriter.Create(currreqstrm, settings);
                currreqwriter.WriteStartDocument();
                currreqwriter.WriteStartElement("Requests");

                var closedreqwriter = XmlWriter.Create(closedreqstrm, settings);
                closedreqwriter.WriteStartDocument();
                closedreqwriter.WriteStartElement("Requests");

                var arrreqwriter = XmlWriter.Create(arrreqstrm, settings);
                arrreqwriter.WriteStartDocument();
                arrreqwriter.WriteStartElement("Requests");

                DateTime dt = DateTime.Today.AddDays(-14);

                foreach (ReqClass rc in reqht.Values)
                {
                    if (rc.ReqStatusID != 5)
                    {
                        AddReqToXml(ref currreqwriter, rc);
                    }
                    else
                    {
                        AddReqToXml(ref closedreqwriter, rc);
                    }

                    if ((dt.CompareTo(rc.DateDelivery) >= 0) && rc.ReqStatusID > 2)
                    {
                        AddReqToXml(ref arrreqwriter, rc);
                    }
                }

                currreqwriter.WriteEndElement();
                currreqwriter.WriteEndDocument();
                currreqwriter.Flush();

                closedreqwriter.WriteEndElement();
                closedreqwriter.WriteEndDocument();
                closedreqwriter.Flush();

                arrreqwriter.WriteEndElement();
                arrreqwriter.WriteEndDocument();
                arrreqwriter.Flush();

                currreqstrm.Position = 0;
                closedreqstrm.Position = 0;
                arrreqstrm.Position = 0;

                currreqwriter.Close();
                closedreqwriter.Close();
                arrreqwriter.Close();

                currreq = currreqstrm;
                closedreq = closedreqstrm;
                arrreq = arrreqstrm;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                if (myReader != null)
                    myReader.Close();

                _myConnection.Close();
            }
        }


        public MemoryStream GetFullInf()
        {
            var settings = new XmlWriterSettings
                               {
                                   ConformanceLevel = ConformanceLevel.Document,
                                   CloseOutput = false,
                                   Encoding = Encoding.UTF8,
                                   Indent = true
                               };

            var strm = new MemoryStream();

            var writer = XmlWriter.Create(strm, settings);

            var myCommand = new SqlCommand
                                {
                                    CommandType = CommandType.StoredProcedure,
                                    Connection = _myConnection,
                                    CommandText = "GetAllInf"
                                };

            SqlDataReader myReader = null;

            try
            {
                _myConnection.Open();
                myReader = myCommand.ExecuteReader();

                writer.WriteStartDocument();
                writer.WriteStartElement("Inf");

                writer.WriteStartElement("ReqStatus");
                while (myReader.Read())
                {
                    writer.WriteStartElement("RS");
                    writer.WriteAttributeString("ID", myReader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("RSN", myReader.GetString(1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("CNew", myReader.GetBoolean(2).ToString(new CultureInfo("en-US", false)));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                myReader.NextResult();

                writer.WriteStartElement("ReqPriority");
                while (myReader.Read())
                {
                    writer.WriteStartElement("RP");
                    writer.WriteAttributeString("ID", myReader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("RPN", myReader.GetString(1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                myReader.NextResult();

                writer.WriteStartElement("Currency");
                while (myReader.Read())
                {
                    writer.WriteStartElement("CR");
                    writer.WriteAttributeString("ID", myReader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("CRN", myReader.GetString(1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                myReader.NextResult();

                writer.WriteStartElement("Warehouse");
                while (myReader.Read())
                {
                    writer.WriteStartElement("WR");
                    writer.WriteAttributeString("ID", myReader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("WRN", myReader.GetString(1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                myReader.NextResult();

                writer.WriteStartElement("TypePayment");
                while (myReader.Read())
                {
                    writer.WriteStartElement("TP");
                    writer.WriteAttributeString("ID", myReader.GetInt32(0).ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("TPN", myReader.GetString(1).ToString(CultureInfo.InvariantCulture));
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                var clnts = new SortedList();

                myReader.NextResult();

                while (myReader.Read())
                {
                    var id = myReader.GetInt32(0);
                    var tel = myReader.GetSqlString(4);
                    clnts.Add(id, new ClientsClass { IDClient = id,
                                                     ClientName = myReader.GetString(1),
                                                     Address = myReader.GetString(2),
                                                     Balance = myReader.GetDouble(3),
                                                     Telephone = tel.IsNull ? string.Empty : tel.Value
                    });
                }

                myReader.NextResult();

                while (myReader.Read())
                {
                    var id = myReader.GetInt32(0);

                    var tmpCln = (ClientsClass)clnts[id];

                    var tel = myReader.GetSqlString(4);

                    tmpCln.PCl.Add(new ClientsPointsClass { IDClientPoints = myReader.GetInt32(1),
                                                            ClientPointsName = myReader.GetString(2),
                                                            Address = myReader.GetString(3),
                                                            Telephone = tel.IsNull ? string.Empty : tel.Value
                    });
                }

                writer.WriteStartElement("Clients");
                foreach (ClientsClass cl in clnts.Values)
                {
                    writer.WriteStartElement("Cl");
                    writer.WriteAttributeString("CLID", cl.IDClient.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("CLN", cl.ClientName);
                    writer.WriteAttributeString("CLAddrr", cl.Address);
                    writer.WriteAttributeString("CLBlns", cl.Balance.ToString(new CultureInfo("en-US", false)));
                    writer.WriteAttributeString("CLTel", cl.Telephone);

                    foreach (var cp in cl.PCl)
                    {
                        writer.WriteStartElement("CP");
                        writer.WriteAttributeString("CPID", cp.IDClientPoints.ToString(CultureInfo.InvariantCulture));
                        writer.WriteAttributeString("CPNm", cp.ClientPointsName);
                        writer.WriteAttributeString("CPAddrr", cp.Address);
                        writer.WriteAttributeString("CPTel", cp.Telephone);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();

                strm.Position = 0;

                writer.Close();

                return strm;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return new MemoryStream();
            }
            finally
            {
                if (myReader != null)
                    myReader.Close();

                _myConnection.Close();
            }
        }
    }
}
