using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Data.SqlClient;
using System.Dynamic;
using System.Net.Http;
using Newtonsoft.Json;

using System.Threading;
using System.Globalization;
using System.IO;
using System.Net.Mail;

using Stream;
using System.Reflection;

using OdooRpcWrapper;
using System.Text.RegularExpressions;
using ADGBravoImport.ADGSAP;
using System.ComponentModel;

//using Tranbros.Sport;

//using SAP.Middleware.Connector.Examples;

namespace ADGBravoImport
{

    public partial class Form1 : DevExpress.XtraBars.Ribbon.RibbonForm
    {
        //public Form1 _form1 { get; set; };
        //public static Form1 Form {
        //    get { return _form1; }
        //}
        

        string[] IgnoreTokens;//
        dynamic _settings;
        //static string _connStrOurSQLServer = "Data Source = 192.168.10.200; Initial Catalog = Bravo; User id = sa; Password=sa1234567890;";

        static string _tableChungTu = "ADG_ChungTuSAP";
        // select * from OrdersX
        static int CurrentStep = 0;

        static HttpClient client = new HttpClient();

        Dictionary<string, Dictionary<string, object>> rows;
        DataTable dataTable = new DataTable();
        DataTable dtCongNo = new DataTable();
        //DataTable dtSAPItems = new DataTable();
        List<ADGSAP.ZstSdItem1> listSAPItems = new List<ZstSdItem1>();

        Dictionary<string, string> dicEmailGetFly = new Dictionary<string, string>();
        List<string> dictionaryEmail = new List<string>();
        private bool localLoad = false;
        private bool IsRunning = false;




        public Form1()
        {
            InitializeComponent();
            //_form1 = this;
            ribbonPage3.Visible = false;
            LoadSettingsFromGoogle();


            //TestSAP2();
            //TestGH();
        }
        private void TestGH()
        {
            Ser_GH.ZfmSdTtcvt input1 = new Ser_GH.ZfmSdTtcvt();
            var cred2 = new System.Net.NetworkCredential();
            cred2.UserName = "adg-dev01";
            cred2.Password = "abc1234";

            Ser_GH.ZSD_TTCVT service1 = new Ser_GH.ZSD_TTCVT();

            service1.PreAuthenticate = true;
            service1.Credentials = cred2;
            service1.EnableDecompression = true;

            //input1.IMacvt = "";
            input1.INgaygiao = "20181210";
            //input1.ITtGiaohang = "";

            service1.Timeout = 1000000;

            var res1 = service1.ZfmSdTtcvt(input1);
            var list1 = res1.TbData.ToList();

            this.dataGridView1.DataSource = list1;

            service1.Abort();
            service1.Dispose();
        }

        private async Task<int> LoadStream()
        {
            var client = new StreamClient("rrr7tqxsknrm", "fv3vpv69zxk6qguwjupc8t7mpq3xp277y4tsuttg5d6j7mdb3gr37byyjjzxydkq");
            var userFeed1 = client.Feed("user", "eric");

            // Create a complex activity

            var activity = new Activity("eric", "add", "picture:10")
            {
                ForeignId = "picture:10"
            };
            activity.SetData("message", "Beautiful bird!");

            await userFeed1.AddActivity(activity);

            return 0;
        }


        public async Task<DataTable> GetDataTableAsync(string connStr, string commandText, CancellationToken cancellationToken, string tableName = null)
        {
            TaskCompletionSource<DataTable> source = new TaskCompletionSource<DataTable>();
            var resultTable = new DataTable(tableName ?? commandText);
            System.Data.Common.DbDataReader dataReader = null;
            System.Data.Common.DbCommand command = null;
            if (cancellationToken.IsCancellationRequested == true)
            {
                source.SetCanceled();

                await source.Task;
            }

            try
            {
                using (var connection = new SqlConnection(connStr))
                {
                    using (command = new SqlCommand(commandText, connection))
                    {
                        await connection.OpenAsync();
                        command.CommandTimeout = 1000;
                        dataReader = await command.ExecuteReaderAsync(CommandBehavior.Default);
                        resultTable.Load(dataReader);
                        source.SetResult(resultTable);
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                source.SetException(ex);
            }
            finally
            {
                if (dataReader != null)
                    dataReader.Close();
                command.Connection.Close();
            }

            return resultTable;
        }

        async Task<int> LocalLoad()
        {
            this.barInfo.Caption = "Loading from local...";
            dataGridView1.DataSource = null;
            this.dataTable = await GetDataTableAsync(this.barEditItemLocalConn.EditValue.ToString(), this.barEditItemLocalSQLQuery.EditValue.ToString(), CancellationToken.None, null);
            dataGridView1.DataSource = dataTable;
            this.barInfo.Caption = "Ready";
            //label1.Text = dataTable.Select().Length.ToString() + " objects found";
            barStaticItem1.Caption = "Load DB success. " + dataTable.Select().Length.ToString() + " objects found";
            LogWriter.Writer("Get local db success. " + label1.Text + "\r\nQuery: " + this.barEditItemLocalSQLQuery.EditValue.ToString() + ". \r\n");
            this.localLoad = true;
            this.textBoxLog.Text += "\r\n3. Local Load completed at " + DateTime.Now;
            return 1;
        }

        // convert Datatable sang Object 2 lop
        int RowCount = 0;


        private Dictionary<string, Dictionary<string, object>> ConvertDataTableToRows(DataTable dtHeader, List<ADGSAP.ZstSdItem1> list)
        {
            RowCount = 0;
            rows = new Dictionary<string, Dictionary<string, object>>();
            Dictionary<string, object> row;
            foreach (DataRow dr in dtHeader.Rows)
            {
                row = new Dictionary<string, object>();
                var columns = dtHeader.Columns;

                for (int i = 1; i < columns.Count; i++)
                {
                    row.Add(columns[i].ColumnName, dr[columns[i]]);
                }

                string fuckThisKey = dr["SoCt"].ToString();
                if (fuckThisKey == "")
                {
                    this.textBoxLog.Text += "\r\nId:" + dr["Id"].ToString() + " has no So_Ct! Please fix";
                    continue;
                }
                string fuckThisKey1 = dr["SoCt"].ToString() //0 So Chung tu
                    + "|" + dr["NgayCt"].ToString()//      1 Ngay Chung tu
                    + "|" + dr["MaCt"].ToString()//    2 Email dai ly  * khong su dung, tam thay bang MaCt
                    + "|" + dr["TenDt"].ToString()//       3 Ten Dai ly
                    + "|" + dr["Tien4"].ToString()//        4 Tien giam tru
                    + "|" + dr["MaDt"].ToString()//        5 Ma Dai ly
                    + "|" + dr["Tongtien"].ToString()//     6 Tong tien
                    + "|" + dr["Documentno"].ToString()//  7 Ma van tai
                    + "|" + dr["MaCt"].ToString()//        8 Loai chung tu
                    ;

                if (rows.ContainsKey(fuckThisKey))
                {
                    //rows[fuckThisKey].Add(dr["increment"].ToString() + dr["Ma_Vt"].ToString(), row);
                    //bo qua
                }
                else
                {
                    rows.Add(fuckThisKey, new Dictionary<string, object>());
                    rows[fuckThisKey].Add("info", fuckThisKey1);

                    var query1 = listSAPItems.Where(item => item.SoCt1 == fuckThisKey);
                    int incre = 0;
                    foreach (ZstSdItem1 item in query1)
                    {
                        incre++;
                        rows[fuckThisKey].Add(item.MaVt + "_" + incre.ToString(), item);
                    }


                    RowCount++;
                }
            }
            return rows;
        }

        async Task<int> ConvertToJS()
        {
            if (localLoad)
            {
                this.barInfo.Caption = "Converting Objects...";
                this.textBoxJSON.Text = await ConvertDataTabletoString(dataTable);
                this.barInfo.Caption = "Ready";
                this.barStaticItem1.Caption = "Converted Objects : " + RowCount.ToString();
                this.textBoxLog.Text += "\r\n4. Convet To JS completed at " + DateTime.Now + ". " + RowCount.ToString() + " objects converted.";
                return 1;
            }
            else
                MessageBox.Show("Please load data from local first");
            return 0;
        }

        public async Task<string> ConvertDataTabletoString(DataTable dt)
        {
            System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            string re = await JsonConvert.SerializeObjectAsync(ConvertDataTableToRows(dt, listSAPItems));
            return re;
        }

        public static string CreateTABLE(string tableName, DataTable table)
        {
            string sqlsc;
            sqlsc = "CREATE TABLE " + tableName + "(";
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sqlsc += "\n [" + table.Columns[i].ColumnName + "] ";
                string columnType = table.Columns[i].DataType.ToString();
                switch (columnType)
                {
                    case "System.Int32":
                        sqlsc += " int ";
                        break;
                    case "System.Int64":
                        sqlsc += " bigint ";
                        break;
                    case "System.Int16":
                        sqlsc += " smallint";
                        break;
                    case "System.Byte":
                        sqlsc += " tinyint";
                        break;
                    case "System.Decimal":
                        sqlsc += " decimal ";
                        break;
                    case "System.DateTime":
                        sqlsc += " datetime ";
                        break;
                    case "System.String":
                    default:
                        sqlsc += string.Format(" nvarchar({0}) ", table.Columns[i].MaxLength == -1 ? "max" : table.Columns[i].MaxLength.ToString());
                        break;
                }
                if (table.Columns[i].AutoIncrement)
                    sqlsc += " IDENTITY(" + table.Columns[i].AutoIncrementSeed.ToString() + "," + table.Columns[i].AutoIncrementStep.ToString() + ") ";
                if (!table.Columns[i].AllowDBNull)
                    sqlsc += " NOT NULL ";
                sqlsc += ",";
            }
            return sqlsc.Substring(0, sqlsc.Length - 1) + "\n)";
        }


        private void btnCopyToDB_Click(object sender, EventArgs e)
        {
            InsertToBulkTable();
        }

        int InsertToBulkTable()
        {
            SqlBulkCopy bulkcopy = new SqlBulkCopy(barEditItemLocalConn.EditValue.ToString());
            bulkcopy.BulkCopyTimeout = 1000;
            bulkcopy.DestinationTableName = _settings.gsxbulktablename.t.ToString();
            try
            {
                bulkcopy.WriteToServer(dataTable);
                barStaticItem1.Caption = "Bulk Copied";
                this.textBoxLog.Text += "\r\n2. Insert To Bulk Table completed at " + DateTime.Now;
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
            finally
            {
                using (var conn = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
                using (var command = new SqlCommand(_settings.gsxexecafterbulk.t.ToString(), conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.ExecuteNonQuery();
                    this.textBoxLog.Text += "\r\n2.1 Exec: " + _settings.gsxexecafterbulk.t.ToString() + " completed";
                }
            }
        }
        private async Task<int> UpdateEmailFromGetFlyBeforePost()
        {
            dicEmailGetFly.Clear();
            this.barInfo.Caption = "Updating Accounts List from GetFly..";
            var client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.Add("X-API-KEY", _settings.gsxgetflyheadervalue.t.ToString());
            var response = await client.GetAsync(_settings.gsxgetflyaccountslink.t.ToString());
            var response2 = await response.Content.ReadAsStringAsync();

            var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();
            dynamic _getFlyAccounts = JsonConvert.DeserializeObject<ExpandoObject>(response2.Replace("$", ""), expConverter);
            foreach (var record in _getFlyAccounts.records)
            {
                if (record.account_code.StartsWith("KH.A002"))
                {

                }
                else if (record.account_code.StartsWith("KH"))
                    continue;
                try
                {
                    dicEmailGetFly.Add(record.account_code.Trim(), record.manager_user_name.Trim());
                    //this.textBoxLog.Text += record.account_code.Trim() + " - " + record.manager_email.Trim() + "\r\n";
                }
                catch (Exception ex)
                {

                }
            }
            this.barInfo.Caption = "Ready";
            return 1;
        }

        private void ExecThisSQL(string connstr, string SQL)
        {
            using (var conn = new SqlConnection(connstr))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(SQL, conn))
                using (SqlDataReader reader = command.ExecuteReader())
                {

                }
                conn.Close();
                this.textBoxLog.Text += "\r\n  >> Run SQL: " + SQL;
            }
        }

        async Task<int> PostToGetFly()
        {
            //string URI = "https://adg.getflycrm.com/api/v3/orders/";
            string URI = _settings.gsxgetflyposturl.t.ToString();

            this.barInfo.Caption = "Preparing..";
            int count = 0;
            int PostSuccess = 0;
            int Ignore = 0;
            int NoEmail = 0;
            int Error = 0;
            try
            {
                //Lay data khach hang moi nhat tu Getly
                int x = await UpdateEmailFromGetFlyBeforePost();
                if (x == 1)
                {
                    var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();
                    dynamic obj = JsonConvert.DeserializeObject<ExpandoObject>(this.textBoxJSON.Text, expConverter);

                    if (checkMulti.Checked)
                    {

                    }
                    else
                    {
                        foreach (var row in obj) // cho chay tat ca cac Orders
                        {
                            bool skip = false;
                            count++;
                            dynamic _order;
                            if (barCheckType.Checked)
                                _order = PrepareOrderForGetFly(row, 1);
                            else
                                _order = PrepareOrderForGetFly(row, 0);
                            if (!barCheckType.Checked)//new 
                                this.barInfo.Caption = "Posting: " + row.Key + " - " + count + " of " + RowCount.ToString();
                            else
                                this.barInfo.Caption = "Updating: " + row.Key + " - " + count + " of " + RowCount.ToString();
                            string orderJS = "";
                            //string orderJS = JsonConvert.SerializeObject(_order);
                            var _order_info = _order.order_info;

                            // bo qua nhung dai ly dac biet
                            foreach (string token in IgnoreTokens)
                            {
                                if (_order_info.account_code.StartsWith(token))
                                {
                                    if (barCheckType.Checked)//update
                                    {
                                        UpdateGetFlyID(-1, _order.order_code);
                                        this.textBoxLog.Text += "\r\n- Skip : " + _order_info.account_code + " " + _order.order_code;
                                        Ignore++;
                                    }
                                    else
                                    {
                                        UpdateGetFlyID(-1, _order_info.order_code);//
                                        this.textBoxLog.Text += "\r\n- Skip : " + _order_info.account_code + " " + _order_info.order_code;
                                        skip = true;
                                        Ignore++;
                                        break;
                                    }
                                }
                            }
                            if (skip) continue;

                            if (!dicEmailGetFly.ContainsKey(_order_info.account_code))
                            {
                                string err = "";
                                if (!barCheckType.Checked)//new 
                                    err = "\r\n- Account: " + _order_info.account_code + ", order: " + _order_info.order_code + " has no email founds in GetFly";
                                else
                                    err = "\r\n- Update: Account: " + _order_info.account_code + ", order: " + _order.order_code + " has no email founds in GetFly";
                                this.textBoxLog.Text += err;
                                NoEmail++;
                                LogWriter.Writer(err);
                                continue;
                            }
                            else
                            {
                                int result = 0;
                                try
                                {
                                    //if (!barCheckType.Checked)
                                    _order_info.assigned_username = dicEmailGetFly[_order_info.account_code];
                                    //else

                                    orderJS = JsonConvert.SerializeObject(_order);
                                    LogWriter.Writer(orderJS);
                                    this.textBoxJSON.Text = orderJS;
                                    if (!barCheckType.Checked)
                                        result = await PostToGetFly(URI, orderJS, 0, 0);//POST len getFly
                                    else
                                        result = await PostToGetFly(URI, orderJS, 1, 0);//PUT len getFly
                                }
                                catch (TaskCanceledException timeoutEx)
                                {
                                    string err = "\r\n- Error: TaskCanceledException:" + _order_info.account_code + " ex:" + timeoutEx.Message;
                                    this.textBoxLog.Text += err;
                                    LogWriter.Writer(err);
                                    //System.Diagnostics.Debug.WriteLine(timeoutEx);
                                    Error++;
                                    continue;
                                }
                                catch (Exception ex)
                                {
                                    string err = "\r\n- Error: Khong ton tai mail cho dai ly:" + _order_info.account_code + " ex:" + ex.Message;
                                    this.textBoxLog.Text += err;
                                    LogWriter.Writer(err);
                                    Error++;
                                    //MessageBox.Show(ex.Message);
                                    continue;
                                }

                                if (result == 0)
                                {
                                    string err = "";
                                    if (!barCheckType.Checked)
                                    {
                                        err = "\r\n- GetFly Post Error: " + _order_info.order_code;
                                    }
                                    else
                                        err = "\r\n- GetFly Update Error: " + _order.order_code;
                                    Error++;
                                    this.textBoxLog.Text += err;
                                    LogWriter.Writer(err);
                                    continue;
                                }
                                else
                                {
                                    try
                                    {
                                        if (!barCheckType.Checked)
                                        {
                                            UpdateGetFlyID(result, _order_info.order_code);
                                            PostSuccess++;
                                            this.barStaticItem1.Caption = _order_info.order_code + "POST success - " + result.ToString();
                                        }
                                        else//update
                                        {
                                            if (barCheckWholeMonth.Checked)
                                                UpdateGetFlyID(result, _order.order_code, "", "");
                                            else
                                                UpdateGetFlyID(result, _order.order_code);

                                            PostSuccess++;
                                            this.barStaticItem1.Caption = _order.order_code + "UPDATE success - " + result.ToString();

                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        string err = "\r\n- DB Error: " + ex.Message;
                                        this.textBoxLog.Text += err;
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
                //done
                string result2 = "\r\n * Post success: " + PostSuccess.ToString()
                     + " / Ignore: " + Ignore.ToString()
                     + " / Error: " + Error.ToString()
                     + " / No Email: " + NoEmail.ToString()
                     + " - Total: " + RowCount.ToString();

                this.textBoxLog.Text += result2;
                this.textBoxLog.Text += "\r\n5. Post/Update to GetFly completed at " + DateTime.Now;
                this.barStaticItem1.Caption = result2;
                using (var conn = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
                using (var command = new SqlCommand(_settings.gsxexecafterpost.t.ToString(), conn)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    conn.Open();
                    command.ExecuteNonQuery();
                }
                this.textBoxLog.Text += "\r\n- Exec: " + _settings.gsxexecafterpost.t.ToString() + " completed";
                this.barInfo.Caption = "Ready";
                Thread.Sleep(10000);
                return 1;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        private dynamic PrepareOrderForGetFly(dynamic row, int type)
        {
            dynamic _order = new ExpandoObject();
            dynamic _order_info = new ExpandoObject();

            string[] tokens = row.Value.info.Split('|');
            DateTime myDate = DateTime.Parse(tokens[1]);

            if (type == 0) // POST tao moi
            {
                _order_info.order_code = row.Key;
            }
            else if (type == 1)
            {
                _order.order_code = row.Key;
            }
            _order_info.order_code = row.Key;
            _order_info.assigned_username = tokens[2];

            _order_info.account_code = tokens[5];
            _order_info.account_name = tokens[3];
            _order_info.account_address = "";
            _order_info.account_email = "";
            _order_info.account_phone = "";

            _order_info.order_date = myDate.ToString("dd/MM/yyyy");
            _order_info.discount = 0;
            decimal TongChietKhau = decimal.Parse(tokens[4], CultureInfo.InvariantCulture);
            _order_info.discount_amount = 0;// decimal.Parse(tokens[4], CultureInfo.InvariantCulture); // tam thoi bo chiet khau
            _order_info.vat = 0;
            _order_info.vat_amount = 0;
            _order_info.transport = 0;
            _order_info.transport_amount = 0;
            _order_info.installation = 0;
            _order_info.installation_amount = 0;
            _order_info.lading_code = tokens[7];
            if (tokens[8] == "TL")
                _order_info.is_repay = 1;
            decimal TongDoanhThu = decimal.Parse(tokens[6], CultureInfo.InvariantCulture) + TongChietKhau;

            _order.order_info = _order_info;
            _order.products = new List<object>();
            decimal TongChietKhauDongBo = 0;
            decimal TongChiPhi = 0;
            decimal TongTienTuTinh = 0;

            foreach (var product in row.Value)
            { // Lay tat ca san pham trong Orders
                var field = product.Value;
                try
                {
                    var x = field.Dvt;
                    // do stuff with x
                }
                catch (Exception ex)
                {
                    continue;
                }

                dynamic _product = new ExpandoObject();
                _product.product_code = field.MaVt.Replace("0000000000", "");
                _product.product_name = field.TenVt;

                _product.quantity = decimal.Parse(field.Soluong9.ToString(), CultureInfo.InvariantCulture);
                _product.price = decimal.Parse(field.Gia2.ToString(), CultureInfo.InvariantCulture);
                decimal thanhtien = (_product.quantity * _product.price);
                decimal ChietKhau = 0;

                if (thanhtien != 0)
                {
                    ChietKhau = decimal.Parse(field.Tien4.ToString(), CultureInfo.InvariantCulture) / thanhtien;
                }
                else
                {
                    Console.Write("Hey");
                }

                _product.price = _product.price + (_product.price * ChietKhau);

                //_product.product_sale_off = ChietKhau;

                if (_product.product_code == "CPKHAC"
                    || _product.product_code == "CPPS01"
                    || _product.product_code == "VC"
                    || _product.product_code == "VTC01"
                    || _product.product_code == "VTKHAC"
                    || _settings.gsxigmavt.t.ToString().Contains(_product.product_code)
                    )
                {
                    TongChiPhi += _product.quantity * _product.price;
                    _product.quantity = 0;
                }
                if (_product.price < 0) // bo chiet khau dong bo voi gia âm 9.1.2019
                {
                    TongChietKhauDongBo += _product.quantity * _product.price;
                    _product.price = 0;
                }
                thanhtien = _product.quantity * _product.price;
                TongTienTuTinh += thanhtien;
                _product.product_sale_off = 0;
                _product.cash_discount = 0;// decimal.Parse(field.Tien4.ToString(), CultureInfo.InvariantCulture);

                if (!_settings.gsxigmavt.t.ToString().Contains(_product.product_code))
                    _order.products.Add(_product);
            }
            _order_info.amount = TongDoanhThu + (TongChietKhauDongBo + TongChiPhi) * -1;
            decimal lechnhau = (_order_info.amount - TongTienTuTinh);
            //if (lechnhau < 1000) dang lam den day


            _order.payments = new List<object>();

            dynamic _payment_info = new ExpandoObject();
            if (barCheckPayment.Checked)
                _payment_info.amount = _order_info.amount;// truoc day
            else
                _payment_info.amount = 0;

            _payment_info.pay_date = _order_info.order_date;
            _payment_info.description = "";
            _order.payments.Add(_payment_info);

            //chuyen sang dinh dang JSON

            return _order;
        }

        private async void UpdateGetFlyID(int DocumentID, string So_Ct)
        {
            try
            {
                using (var connection = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
                {
                    using (var command = new SqlCommand("update " + _tableChungTu + " SET GetFlyID = " + DocumentID.ToString()
                        + " where SoCt = '" + So_Ct + "'", connection))
                    {
                        await connection.OpenAsync();
                        command.CommandTimeout = 10000;

                        int x = await command.ExecuteNonQueryAsync();
                        connection.Close();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async void UpdateGetFlyID(int DocumentID, string So_Ct, string bang)
        {
            using (var connection = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
            {
                using (var command = new SqlCommand("update " + bang + " SET GetFlyID = " + DocumentID.ToString()
                    + " where SoCt = '" + So_Ct + "'", connection))
                {
                    await connection.OpenAsync();
                    command.CommandTimeout = 10000;
                    int x = await command.ExecuteNonQueryAsync();
                    connection.Close();
                }
            }
        }
        private async void UpdateGetFlyID(int DocumentID, string So_Ct, string TongTien, string Tien4)
        {
            using (var connection = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
            {
                using (var command = new SqlCommand(
                    "UPDATE ADG_ChungTuSAP SET ADG_ChungTuSAP.TongTien = B.TongTien, ADG_ChungTuSAP.Tien4 = B.Tien4"
                    + " FROM ADG_ChungTuSAP A INNER JOIN BulkSAP B ON A.SoCt = B.SoCt and A.SoCt = '"
                    + So_Ct + "'", connection))
                {
                    await connection.OpenAsync();
                    command.CommandTimeout = 10000;
                    int x = await command.ExecuteNonQueryAsync();
                    connection.Close();
                    this.textBoxLog.Text += "\r\nUpdate changed record for [" + So_Ct + "]";
                }
            }
        }

        private async Task<int> PostToGetFly(string URI, string JSONText, int type, int posttype)
        {
            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("X-API-KEY", _settings.gsxgetflyheadervalue.t.ToString());
                string content = "";
                if (type == 0)
                {
                    var response = await client.PostAsync(
                        URI,
                         new StringContent(JSONText, Encoding.UTF8, "application/json"));
                    content = await response.Content.ReadAsStringAsync();
                }
                else if (type == 1)
                {
                    var response = await client.PutAsync(
                        URI,
                         new StringContent(JSONText, Encoding.UTF8, "application/json"));
                    content = await response.Content.ReadAsStringAsync();
                }
                content = Regex.Unescape(content);
                var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();
                if (posttype == 0)
                {
                    //this.textBoxLog.Text = "Post success. GetFly item ID:" + content;
                    LogWriter.Writer("Response:" + content + "\r\n");
                    int output = 0;
                    if (content.Contains("message")) //C\u1eadp nh\u1eadt \u0111\u01a1n h\u00e0ng th\u00e0nh c\u00f4ng
                    {
                        //string result = DecodeFromUtf8(content);
                        if (content.Contains("order_id"))
                        {
                            dynamic _getFlyReply = JsonConvert.DeserializeObject<ExpandoObject>(content, expConverter);
                            Int32.TryParse(_getFlyReply.order_id.ToString(), out output);
                            client.Dispose();
                            return output;
                        }
                        this.textBoxLog.Text += "\r\n" + content;
                        client.Dispose();
                        return 0;
                    }
                    else
                    {
                        int.TryParse(content, out output);
                        return output;
                    }
                }
                else if (posttype == 1)
                {
                    //LogWriter.Writer("Response:" + content + "\r\n");
                    dynamic _getFlyReply = JsonConvert.DeserializeObject<ExpandoObject>(content, expConverter);
                    this.textBoxLog.Text += "\r\n >> GetFly response: " + _getFlyReply.message;
                    client.Dispose();
                    return 2;
                }
                else if (posttype == 2)
                {
                    //LogWriter.Writer("Response:" + content + "\r\n");
                    dynamic _getFlyReply = JsonConvert.DeserializeObject<ExpandoObject>(content, expConverter);
                    //this.textBoxLog.Text += "\r\n >> GetFly response: " + _getFlyReply.message;
                    client.Dispose();
                    int output = -3;
                    Int32.TryParse(_getFlyReply.order_id.ToString(), out output);
                    return output;
                }
                else if (posttype == 3)
                {

                }
            }
            return 0;
        }
        public string DecodeFromUtf8(string utf8String)
        {
            // copy the string as UTF-8 bytes.
            byte[] utf8Bytes = new byte[utf8String.Length];
            for (int i = 0; i < utf8String.Length; ++i)
            {
                //Debug.Assert( 0 <= utf8String[i] && utf8String[i] <= 255, "the char must be in byte's range");
                utf8Bytes[i] = (byte)utf8String[i];
            }

            return Encoding.UTF8.GetString(utf8Bytes, 0, utf8Bytes.Length);
        }



        async Task<int> SAPLoad()
        {
            this.localLoad = false;
            this.barInfo.Caption = "Loading from SAP...";
            this.barButtonBravoLoad.Enabled = false;
            dataGridView1.DataSource = null;

            ADGSAP.ZfmSdOrderSer input = new ADGSAP.ZfmSdOrderSer();
            var cred = new System.Net.NetworkCredential();
            cred.UserName = "ADG-BG01";
            cred.Password = "abc1234";

            ADGSAP.ZSD_ORDER1 service = new ADGSAP.ZSD_ORDER1();

            service.PreAuthenticate = true;
            service.Credentials = cred;
            service.EnableDecompression = true;

            input.PFdate = barEditFrom.EditValue.ToString();
            input.PTdate = barEditTo.EditValue.ToString();

            //ADGSAP2.ZfmSdOrderSerResponse res = new ADGSAP2.ZfmSdOrderSerResponse();
            service.Timeout = 1000000;
            var res = service.ZfmSdOrderSer(input);
            var list = res.TbHeader.ToList();

            listSAPItems = res.TbItem.ToList();

            this.dataTable = ToDataTable<ZstSdHeader1>(list);

            DataColumn newColumn = new DataColumn("getFlyID", typeof(Int32));
            newColumn.DefaultValue = 0;
            dataTable.Columns.Add(newColumn);

            this.dataGridView1.DataSource = this.dataTable;

            this.barInfo.Caption = "Ready";
            this.barButtonBravoLoad.Enabled = true;
            barStaticItem1.Caption = "Load SAP success. " + dataTable.Select().Length.ToString() + " objects found";
            LogWriter.Writer("Get SAP database success. " + label1.Text + "\r\nFrom: " + this.barEditItemLocalSQLQuery.EditValue.ToString()
                    + ". \r\n"
                    + "To: "
                    + this.barEditItemLocalConn.EditValue.ToString());
            this.textBoxLog.Text += "\r\n1. Bravo Load completed at " + DateTime.Now + ". Founds: " + dataTable.Select().Length.ToString() + " records";


            service.Abort();
            service.Dispose();
            return 1;
        }
        public DataTable ToDataTable<T>(IList<T> list)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(T));
            DataTable table = new DataTable();
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            }
            object[] values = new object[props.Count];
            foreach (T item in list)
            {
                for (int i = 0; i < values.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                table.Rows.Add(values);
            }
            return table;
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (IsRunning)
                Stop();
            else
                Start();
        }
        private void Start()
        {
            this.IsRunning = true;
            this.barButtonStart.ImageOptions.ImageUri.Uri = "Cancel;Size32x32;Office2013";
            this.barButtonStart.Caption = "Stop";
            this.textBoxLog.Text += "\r\n- System starts at: " + DateTime.Now;
            CreateATask();
        }
        private void Stop()
        {
            this.IsRunning = false;
            this.barButtonStart.ImageOptions.ImageUri.Uri = null;
            this.barButtonStart.ImageOptions.LargeImage = global::ADGBravoImport.Properties.Resources._1692;
            this.barButtonStart.Caption = "Start";
            this.textBoxLog.Text += "\r\n- System stops at: " + DateTime.Now;
            TaskScheduler.Instance.ClearTimer();
        }

        private void CreateATask()
        {
            int hour = Convert.ToInt16(barHour.EditValue);
            int min = Convert.ToInt16(barMin.EditValue);
            this.textBoxLog.Text += "\r\n* Create daily Schedule Task at " + hour.ToString() + ":" + min.ToString();
            TextBox.CheckForIllegalCrossThreadCalls = false;
            TaskScheduler.Instance.ScheduleTask(hour, min, 24, () =>
            {
                if (this.IsRunning)
                {
                    this.textBoxLog.Text = "\r\n0. Wake up at " + DateTime.Now;
                    try
                    {
                        WholeSolutionCombination();
                    }
                    catch (Exception ex)
                    {
                        SendEmailReport("Austdoor - GetFly Error :" + ex.Message);
                    }
                }
            });
            if (barCheckCongNo.Checked)
            {
                double interval = double.Parse(_settings.gsxcongnoperiod.t.ToString());
                this.textBoxLog.Text += "\r\n* Create Công Nợ Schedule Task at now with interval: " + interval.ToString() + " hour";
                TaskScheduler.Instance.ScheduleTask(DateTime.Now.Hour, DateTime.Now.Minute + 1, interval, () =>
                  {
                      if (this.IsRunning && DateTime.Now.Hour >= 8 && DateTime.Now.Hour < 18)
                      {
                          this.textBoxLog.Text += "\r\n0. Wake up for Công Nợ at" + DateTime.Now;
                          try
                          {
                              XulyCongNo();
                          }
                          catch (Exception ex)
                          {
                              SendEmailReport("Austdoor - GetFly Error :" + ex.Message);
                          }
                      }
                  });
            }
        }
        private async void WholeSolutionCombination()
        {
            string sday = "";
            string smonth1 = "";
            string smonth2 = "";
            int day = DateTime.Now.Day;
            bool lastyear = false;

            if (day < 10)
            {
                sday = "0" + day.ToString();
                int month = DateTime.Now.Month - 1;
                if (month == 0) { month = 12; lastyear = true; }

                if (month < 10) smonth1 = "0" + month.ToString();
                else smonth1 = month.ToString();
            }
            else
            {
                sday = day.ToString();
                int month = DateTime.Now.Month;
                if (month == 0) { month = 12; lastyear = true; }
                if (month < 10) smonth1 = "0" + month.ToString();
                else smonth1 = month.ToString();
            }

            int month2 = DateTime.Now.Month;
            if (month2 < 10) smonth2 = "0" + month2.ToString();
            else smonth2 = month2.ToString();

            int year1 = DateTime.Now.Year;
            int year2 = DateTime.Now.Year;
            if (lastyear && barCheckWholeMonth.Checked) year1--;
            string sql = string.Empty;
            if (barCheckWholeMonth.Checked)
            {
                this.barEditFrom.EditValue = year1.ToString() + smonth1 + "01";
                this.barEditTo.EditValue = year2.ToString() + smonth2 + sday;
                //xoa BulkSAP truoc khi day data vao
                this.ExecThisSQL(_settings.gsxlocalsqlconn.t.ToString(), _settings.gsxemptybulk.t.ToString());
            }
            else
            {
                this.barEditFrom.EditValue = year1.ToString() + smonth2 + sday;
                this.barEditTo.EditValue = year2.ToString() + smonth2 + sday;
            }

            //this.barEditTo.EditValue = sql;

            this.barEditItemLocalSQLQuery.EditValue = _settings.gsxlocalsqlquery.t.ToString();


            int x1 = await SAPLoad();
            if (x1 == 1)
            {
                CurrentStep = 1;
                int x2 = InsertToBulkTable();
                if (x2 == 1)
                {
                    CurrentStep = 2;
                    int x3 = await LocalLoad();
                    if (x3 == 1)
                    {
                        CurrentStep = 3;
                        int x4 = await ConvertToJS();
                        if (x4 == 1)
                        {
                            CurrentStep = 4;
                            int x5 = await PostToGetFly();
                            if (x5 == 1)
                            {
                                CurrentStep = 5;
                                int x6 = await ExportToCSV();
                                if (x6 == 1)
                                {
                                    CurrentStep = 6;
                                    int x7 = SendEmailReport("Bravo - GetFly Sync Report on " + DateTime.Now.ToString("yyyyMMdd"));
                                    if (x7 == 1)
                                    {
                                        CurrentStep = 7;
                                        LogWriter.Writer(this.textBoxLog.Text);
                                        if (!barCheckWholeMonth.Checked)
                                            ClearBulk();
                                    }
                                }
                                else
                                {
                                    this.textBoxLog.Text += "\r\n6. Total Completed. All posted to GetFly " + DateTime.Now;
                                    LogWriter.Writer(this.textBoxLog.Text);
                                    int x7 = SendEmailReport("Bravo - GetFly Sync Report on " + DateTime.Now.ToString("yyyyMMdd"));
                                    if (!barCheckWholeMonth.Checked)
                                        ClearBulk();

                                }
                                if (barCheckWholeMonth.Checked)//chay update cho ca thang
                                {
                                    this.textBoxLog.Text += "\r\n10. Starting to find update records..";
                                    this.barEditItemLocalSQLQuery.EditValue = _settings.gsxselectdiff.t.ToString();
                                    this.barCheckType.Checked = true;
                                    x3 = await LocalLoad();
                                    if (x3 == 1)
                                    {
                                        CurrentStep = 3;
                                        x4 = await ConvertToJS();
                                        if (x4 == 1)
                                        {
                                            CurrentStep = 4;
                                            x5 = await PostToGetFly();
                                            if (x5 == 1)
                                            {
                                                this.barCheckType.Checked = false;
                                            }
                                        }
                                    }
                                }
                                //int a = await XulyCongNo();
                            }
                        }
                    }
                }
            }
        }
        private async Task<int> ExportToCSV()
        {
            int x3 = await LocalLoad();
            if (dataTable.Rows.Count > 0)
            {
                var lines = new List<string>();

                string[] columnNames = dataTable.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName).
                                                  ToArray();

                var header = string.Join(",", columnNames);
                lines.Add(header);

                var valueLines = dataTable.AsEnumerable()
                                   .Select(row => string.Join(",", row.ItemArray));
                lines.AddRange(valueLines);

                File.WriteAllLines("Report_" + DateTime.Now.ToString("yyyyMMdd") + ".csv", lines, Encoding.UTF8);
                this.textBoxLog.Text += "\r\n6. Export to CSV completed at " + DateTime.Now;
                return 1;
            }
            return 0;
        }

        private int SendEmailReport(string title)
        {
            MailMessage objeto_mail = new MailMessage();
            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.Host = "mail.austdoor.com";
            client.Timeout = 10000;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("tuantm1@austdoor.com", "adg2018@");
            objeto_mail.From = new MailAddress("tuantm1@austdoor.com");
            objeto_mail.To.Add(new MailAddress("ducdm@austdoor.com"));
            objeto_mail.To.Add(new MailAddress("tunbeo73@gmail.com"));
            objeto_mail.Subject = title;
            objeto_mail.Body = "Do not reply this mail.\r\n"
                + this.textBoxLog.Text;
            if (CurrentStep >= 6)
                objeto_mail.Attachments.Add(new Attachment("Report_" + DateTime.Now.ToString("yyyyMMdd") + ".csv"));
            try
            {
                client.Send(objeto_mail);
                this.textBoxLog.Text += "\r\n7. Send Report Email completed at " + DateTime.Now;
                return 1;
            }
            catch (SmtpException ex)
            {
                this.textBoxLog.Text += "\r\n7. Tried to send Email but failed with SmtpException: " + ex.Message + " at " + DateTime.Now;
            }
            catch (Exception ex)
            {
                this.textBoxLog.Text += "\r\n7. Tried to send Email but failed: " + ex.Message + " at " + DateTime.Now;
            }

            return 0;
        }

        private async void barButtonBravoLoad_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //int x = await BravoLoad();
            int x = await SAPLoad();
        }

        private async void barButtonLocalLoad_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await LocalLoad();
        }

        private void barButtonInsertToBulkTable_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            InsertToBulkTable();
        }

        private async void barButtonConvetToJS_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await ConvertToJS();
        }

        private void barButtonCreateSQLTable_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            textBoxJSON.Text = CreateTABLE("Bulk", dataTable);
        }

        private async void barButtonPostToGetFly_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await PostToGetFly();
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0112) // WM_SYSCOMMAND
            {
                // Check your window state here
                if (m.WParam == new IntPtr(0xF030)) // Maximize event - SC_MAXIMIZE from Winuser.h
                {
                    //splitContainerControl1.SplitterPosition = Screen.PrimaryScreen.Bounds.Width * 3 / 4;
                }
            }
            base.WndProc(ref m);
        }

        private async void barButtonExportToExcel_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await ExportToCSV();
        }

        private void barButtonSendMail_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CurrentStep = 6;
            int x = SendEmailReport("Custom Report");
        }

        private void barButtonClearLog_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.textBoxLog.Text = string.Empty;
        }

        private void barButtonShowSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            string input = Microsoft.VisualBasic.Interaction.InputBox("Password:", "Administrator", "Enter password to unlock", -1, -1);
            if (input == _settings.gsxadminpassword.t.ToString())
            {
                ribbonPage3.Visible = true;
            }
        }

        private async void LoadSettingsFromGoogle()
        {
            var client = new System.Net.Http.HttpClient();
            var response = await client.GetAsync("https://spreadsheets.google.com/feeds/list/1RgIAJngBKiYVOkpOUoo09ZeJejuP7hH4uHGJaMaJKIs/od6/public/values?alt=json");
            var response2 = await response.Content.ReadAsStringAsync();

            var expConverter = new Newtonsoft.Json.Converters.ExpandoObjectConverter();
            dynamic _settingsGoogle = JsonConvert.DeserializeObject<ExpandoObject>(response2.Replace("$", ""), expConverter);
            _settings = _settingsGoogle.feed.entry[0];

            this.barEditFrom.EditValue = _settings.gsxbravoconn.t.ToString();
            this.barEditTo.EditValue = _settings.gsxbravoquery.t.ToString();
            this.barEditItemLocalConn.EditValue = _settings.gsxlocalsqlconn.t.ToString();
            this.barEditItemLocalSQLQuery.EditValue = _settings.gsxlocalsqlquery.t.ToString();
            _tableChungTu = _settings.gsxtablechungtu.t.ToString();
            IgnoreTokens = _settings.gsxignoreaccountstartwith.t.ToString().Replace(" ", "").Split(',');
        }

        private async void barButtonUpdateEmail_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await UpdateEmailFromGetFlyBeforePost();
        }

        private void barButtonTruncateBulk_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ClearBulk();
        }

        private void ClearBulk()
        {
            using (var conn = new SqlConnection(this.barEditItemLocalConn.EditValue.ToString()))
            using (var command = new SqlCommand("Delete " + _settings.gsxbulktablename.t.ToString(), conn)
            {
                CommandType = CommandType.Text
            })
            {
                conn.Open();
                command.ExecuteNonQuery();
                this.textBoxLog.Text += "\r\n- Delete from Bulk: completed";
                conn.Close();
            }
        }

        private async void barButtonLoadStream_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await DeleteOrderFromGetFly();
        }

        private async void UpdateFieldInGetFly()
        {
            string sql = "select Id, So_Ct, Ma_Dt, [document no] as DocNum, Tien4, TongTien from Bulk7 where Sx=1 and getflyid =0 and [document no] <>'' order by id";
            DataTable SoChungTu = await GetDataTableAsync(this.barEditItemLocalConn.EditValue.ToString(), sql, CancellationToken.None, null);
            List<dynamic> dynamicDt = SoChungTu.ToDynamic();
            int allcount = dynamicDt.Count();
            int s = 0;
            int f = 0;
            int x = await UpdateEmailFromGetFlyBeforePost();
            if (x == 1)
            {
                foreach (var c in dynamicDt)
                {
                    string URL = _settings.gsxgetflyposturl.t.ToString();

                    dynamic _order = new ExpandoObject();
                    _order.order_code = c.So_Ct;
                    dynamic _order_info = new ExpandoObject();
                    _order_info.lading_code = c.DocNum;
                    _order.order_info = _order_info;
                    _order_info.account_code = c.Ma_Dt;
                    decimal TongDoanhThu = decimal.Parse(c.Tien4, CultureInfo.InvariantCulture) + decimal.Parse(c.TongTien, CultureInfo.InvariantCulture);
                    //_order_info.amount = decimal.Parse(tokens[6], CultureInfo.InvariantCulture); truoc day
                    _order_info.amount = TongDoanhThu;

                    try
                    {
                        if (!dicEmailGetFly.ContainsKey(_order_info.account_code))
                        {
                            this.textBoxLog.Text += _order_info.account_code + " ";
                            f++;
                        }
                        else
                        {
                            _order_info.assigned_username = dicEmailGetFly[_order_info.account_code];
                            string stringToPost = JsonConvert.SerializeObject(_order);
                            this.barInfo.Caption = _order.order_code;
                            int x1 = await PostToGetFly(URL, stringToPost, 1, 2);
                            if (x1 > 0)
                            {
                                UpdateGetFlyID(x1, c.So_Ct, "Bulk7");
                                s++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        f++;
                        this.barStaticItem1.Caption = s.ToString() + " / " + f.ToString() + " / Total: " + allcount.ToString();
                        continue;
                    }
                    this.barStaticItem1.Caption = s.ToString() + " / " + f.ToString() + " / Total: " + allcount.ToString();
                }
            }
        }

        private void OdooTest()
        {
            OdooConnectionCredentials creds = new OdooConnectionCredentials("http://localhost:8082", "odoo12v1", "tunbeo73@gmail.com", "tintuc");
            OdooAPI api = new OdooAPI(creds);
            api.Login();

            OdooModel productModel = api.GetModel("austdoor.cuacuon");
            OdooModel cuacuonOrderlineModel = api.GetModel("austdoor.cuacuon.orderline");

            productModel.AddField("name");
            productModel.AddField("fullname");
            productModel.AddField("description");
            productModel.AddField("LoCuon");
            productModel.AddField("CaoPhuBi");
            productModel.AddField("RongPhuBi");
            productModel.AddField("ViTriBoToi");
            productModel.AddField("Ray_KichThuoc");
            productModel.AddField("Ray_SoThanh");
            productModel.AddField("DongCua_Id");
            productModel.AddField("LoaiCua_Id");
            productModel.AddField("MauCua_Id");
            productModel.AddField("BoToi_Id");
            productModel.AddField("Ray_Id");
            productModel.AddField("state");

            productModel.AddField("is_show_Truc114");
            productModel.AddField("is_show_GhepMau_Id");
            productModel.AddField("is_show_DoCaoLoThoang");
            productModel.AddField("is_show_GiaDo_Id");
            productModel.AddField("is_show_GiaDo_SoBo");
            productModel.AddField("is_show_LuaChonThem");

            //for (int i = 20; i <= 100; i++)
            //{ 
            OdooRecord record = productModel.CreateNew();
            record.SetValue("fullname", "Cua cuon so x");// + i.ToString());
            record.SetValue("description", "Cua cuon so x");
            //record.SetValue("default_code", "default_code3");
            record.SetValue("LoCuon", "t");
            record.SetValue("CaoPhuBi", 0);
            record.SetValue("RongPhuBi", 0);
            record.SetValue("ViTriBoToi", "t");
            record.SetValue("Ray_KichThuoc", 0);
            record.SetValue("Ray_SoThanh", 1);
            record.SetValue("DongCua_Id", 1);
            record.SetValue("LoaiCua_Id", 1);
            record.SetValue("MauCua_Id", 1);
            record.SetValue("BoToi_Id", 1);
            record.SetValue("Ray_Id", 1);
            record.SetValue("state", "bannhap");
            //record.Save();
            //}

            cuacuonOrderlineModel.AddField("cuacuon_id");
            cuacuonOrderlineModel.AddField("accessories_id");
            cuacuonOrderlineModel.AddField("quantity");

            int[] phukien_01 = { 161, 162, 126, 157, 164, 153, 154, 160 };
            for (int j = 26; j < 100; j++)
            {
                object[] filter = new object[1];
                filter[0] = new object[3] { "fullname", "=", "Cua cuon so " + j.ToString() };
                List<OdooRecord> records = productModel.Search(filter);
                foreach (OdooRecord r in records)
                {
                    this.textBoxJSON.Text += String.Format("[{0}] {1}", r.Id.ToString(), r.GetValue("name"));

                    foreach (int i in phukien_01)
                    {
                        OdooRecord recordOrderLine = cuacuonOrderlineModel.CreateNew();
                        recordOrderLine.SetValue("cuacuon_id", r.Id);
                        recordOrderLine.SetValue("accessories_id", i);
                        recordOrderLine.SetValue("quantity", 1);
                        recordOrderLine.Save();
                    }
                    r.SetValue("is_show_Truc114", true);
                    r.SetValue("is_show_GhepMau_Id", true);
                    r.SetValue("is_show_DoCaoLoThoang", true);
                    r.SetValue("is_show_GiaDo_Id", true);
                    r.SetValue("is_show_GiaDo_SoBo", true);
                    r.SetValue("is_show_LuaChonThem", true);
                    r.Save();
                }
            }
        }


        private async Task<int> DeleteOrderFromGetFly()
        {
            string[] deleteThese = _settings.gsxdeletethese.t.ToString().Replace(" ", "").Split('\n');
            foreach (string id in deleteThese)
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("X-Authorization", _settings.gsxgetflywebcookie.t.ToString());
                    string content = "";

                    var response = await client.PutAsync("https://adg.getflycrm.com/crm/order/cancel",
                             new StringContent("{\"id\":\"" + id + "\",\"order_type\":\"2\"}", Encoding.UTF8, "application/json"));
                    content = await response.Content.ReadAsStringAsync();

                    if (content.Contains("true"))
                    {
                        textBoxLog.Text += "\r\n- Deleted: " + id;
                    }
                    else
                    {
                        textBoxLog.Text += "\r\n- Deleted: " + id + " error";
                    }
                }
            }
            return 1;
        }

        string CongNoStr = "";

        private async void barPostCongNo_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int x = await XulyCongNo();
        }

        async Task<int> XulyCongNo()
        {
            int x = await LoadCongNoDT();
            if (x == 1)
            {
                int x1 = await PostCongNo();
                if (x1 == 1)
                {
                    string URI = _settings.gsxcongnourl.t.ToString();
                    int x2 = await PostToGetFly(URI, CongNoStr, 0, 1);//POST len getFly
                    if (x2 == 2)
                    {
                        this.textBoxLog.Text += "\r\n 9. Update Liabilities to GetFly completed.";
                        return 1;
                    }
                }
            }
            return 0;
        }

        async Task<int> LoadCongNoDT()
        {
            string datenow = DateTime.Now.ToString("yyyyMMdd");
            string sql = _settings.gsxcongnosql.t.ToString().Replace("20181128", datenow);

            this.barInfo.Caption = "Loading Công nợ from Bravo...";
            this.dtCongNo = await GetDataTableAsync(this.barEditFrom.EditValue.ToString(), sql, CancellationToken.None, null);
            this.barInfo.Caption = "Ready";
            //label1.Text = dataTable.Select().Length.ToString() + " objects found";            
            this.textBoxLog.Text += "\r\n8. Get Liabilities completed at " + DateTime.Now;

            return 1;
        }

        async Task<int> PostCongNo()
        {
            string datenow = DateTime.Now.ToString("yyyyMMdd");
            List<dynamic> dynamicDt = this.dtCongNo.ToDynamic();
            //MessageBox.Show(dynamicDt.First().ID.ToString());
            //MessageBox.Show(dynamicDt.First().Name);
            CongNoStr = "[ ";
            foreach (var c in dynamicDt)
            {
                if (!c.Ma_Dt.StartsWith("KL") && !c.Ma_Dt.StartsWith("MNKL"))
                    CongNoStr += "{ \"account_code\":\"" + c.Ma_Dt + "\","
                            + "	\"liabilities_day\": \"" + datenow + "\","
                            + "	\"liabilities_out\": \"" + c.Du_No_Biz2 + "\","
                            + "	\"liabilities_in\": \"" + c.Han_Muc_Cong_No + "\""
                            + " },";

            }
            CongNoStr += "]";
            CongNoStr = CongNoStr.Replace(",]", "]");
            LogWriter.Writer("\r\n" + CongNoStr);

            return 1;
        }

        private async void barLoadDiff_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DoiTen();
        }

        private async void DoiTen()
        {
            string sql = "select getflyid, soct from ADG_ChungTuSAP where getFlyId>0";
            DataTable SoChungTu = await GetDataTableAsync(this.barEditItemLocalConn.EditValue.ToString(), sql, CancellationToken.None, null);
            List<dynamic> dynamicDt = SoChungTu.ToDynamic();
            string URL = "https://adg.getflycrm.com/api/v3/orders/update_by_id";
            foreach (var c in dynamicDt)
            {
                string stringToPost = "{ 'order_id':" + c.getflyid + ",'order_info':{ 'order_code':'" + c.soct + "_deleted1'} }";
                stringToPost = stringToPost.Replace("'", "\"");
                int x1 = await PostToGetFly(URL, stringToPost, 1, 3);
                if (x1 > 0)
                {
                    //UpdateGetFlyID(x1, c.So_Ct, "Bulk7");
                    //s++;
                }
            }
        }
        private async void barButtonPayment_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            //DoPayment();
            LoadBongDaLu();
        }

        private async void DoPayment()
        {
            string sql = _settings.gsxpaymentsql.t.ToString();
            DataTable SoChungTu = await GetDataTableAsync(this.barEditItemLocalConn.EditValue.ToString(), sql, CancellationToken.None, null);
            List<dynamic> dynamicDt = SoChungTu.ToDynamic();
            foreach (var c in dynamicDt)
            {
                string URL = _settings.gsxpaymenturl.t.ToString();
                dynamic _payment = new ExpandoObject();
                dynamic _payment_info = new ExpandoObject();
                _payment_info.amount = decimal.Parse(c.Tien4.ToString(), CultureInfo.InvariantCulture)
                    + decimal.Parse(c.TongTien.ToString(), CultureInfo.InvariantCulture);
                _payment_info.pay_date = c.Ngay_Char.ToString();
                _payment.payment_info = _payment_info;
                _payment.order_code = c.So_Ct;

                string stringToPost = JsonConvert.SerializeObject(_payment);
                this.barInfo.Caption = _payment.order_code;
                int x1 = await PostToGetFly(URL, stringToPost, 0, 3);
                if (x1 > 0)
                {
                    //UpdateGetFlyID(x1, c.So_Ct, "Bulk7");
                    //s++;
                }
            }
        }























        
        private async void LoadBongDaLu()
        {
            //this.textBoxLog.Text = "";

            ////ketqua
            ////http://www.bongdalu.com/data/ft1_vn2.js

            ////live
            ////http://www.bongdalu.com/data/bf_vn.js
            ////string s1 = "http://www.bongdalu.com/data/ft1_vn2.js";
            //string s1 = "http://www.bongdalu.com/data/bf_vn.js";

            //string response = await Utils.WebRequest(s1, "");
            //var matches = Utils.MatchParse(response);            
            //this.dataGridView1.DataSource = matches.Where(m => m.hasLiveOdd);

            //foreach (MatchDTO match in matches.Where(m => m.hasLiveOdd))
            ////foreach (MatchDTO match in matches.Where(m => m.isLive))
            //{
            //    try
            //    {
            //        if (match.isLive)
            //        {
            //            int x = await match.GetLiveOdd();                        
            //            if (x > 0)
            //            {
            //                string s = match.SearchOdd();
            //                if (s != "")
            //                    this.textBoxLog.Text += "\r\n !Live: " + s;
            //            }
            //        }
            //        else
            //        {
            //            int x = await match.GetLiveOdd();
            //            if (x > 0)
            //            {
            //                string s = match.SearchOdd();
            //                if (s != "")
            //                    this.textBoxLog.Text += "\r\n" + s;
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        this.textBoxLog.Text += "\r\n" + match.OddLink + " parse Error";
            //        continue;
            //    }
            //}

            //this.dataGridView1.DataSource = matches.Where(m => m.hasLiveOdd);

            //this.barStaticItem1.Caption = "Won: " + matches.Where(m => m.StrategyStatus).Count().ToString() + "/ Total: " + matches.Where(m => m.RedOU).Count().ToString();
        }
    }
}
