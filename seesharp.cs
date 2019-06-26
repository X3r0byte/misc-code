
        /// <summary>
        /// This method creates a report using Microsoft's RDLC reporting definition.
        /// You must have the RDLC package installed and a .rdlc report file to render for this method to work.
        /// </summary>
        /// <param name="filePath">The path where the report is to be created</param>
        /// <param name="fileName">The name of the report to be created</param>
        /// <param name="fileExt">use "pdf" or "xls", no dot (.)</param>
        /// <param name="reportName">The name of the report design file (myReport.rdlc)</param>
        /// <param name="sourceTables">Supports one or many DataTables for report data</param>
        /// <param name="parameters">Supports one or many parameters for report data</param>
        /// <returns>"FAILED" if fail, or report file path if success</returns>
        public static string createReport(string filePath, string fileName, string fileExt, string reportName, 
                                          List<DataTable> sourceTables, List<ReportParameter> parameters)
        {
            Warning[] warnings;
            string[] streamIds;
            string mimeType = string.Empty;
            string encoding = string.Empty;
            string extension = string.Empty;
            string renderType = fileExt;
            string file = fileName + "." + fileExt;
            ReportViewer viewer = new ReportViewer();

            if (renderType == "xls")
            {
                renderType = "Excel";
            }

            // create file path if it does not exist
            if (!Directory.Exists(filePath))
            {
                DirectoryInfo di;
                di = Directory.CreateDirectory(filePath);
            }

            // create a placeholder file to indicate the report is processing
            File.WriteAllText(filePath + fileName + "(processing)", "");

            // remove and prior fails if it occurred
            if (File.Exists(filePath + "REPORTFAIL_README.txt"))
            {
                File.Delete(filePath + "REPORTFAIL_README.txt");
            }

            // if this specific report exists, try to delete it
            if (File.Exists(Path.Combine(filePath, file)))
            {
                try
                {
                    System.IO.File.Delete(Path.Combine(filePath, file));
                }
                catch (Exception ex)
                {
                    File.WriteAllText(filePath + "REPORTFAIL_README.txt", fileName + 
                        " Report creation failed, the requested report may be in use: " + fileName);
                    System.IO.File.Delete(Path.Combine(filePath + fileName + "(processing)"));
                    return "FAILED";
                }
                // System.IO.File.Delete(Path.Combine(filePath, file));
            }

            // add data to the report
            foreach (DataTable dt in sourceTables)
            {
                ReportDataSource rds = new ReportDataSource(dt.TableName, dt);
                viewer.LocalReport.DataSources.Add((rds));
            }

            viewer.ProcessingMode = ProcessingMode.Local;
            viewer.LocalReport.ReportPath = @"\\YOUR\PATH\TO\REPORT\DESIGN\FILE\" + reportName + ".rdlc"; 
            viewer.LocalReport.Refresh();

            // add parameters to the report
            foreach (ReportParameter par in parameters)
            {
                viewer.LocalReport.SetParameters(par);
            }

            // start rendering the report
            byte[] bytes = viewer.LocalReport.Render(renderType, null, out mimeType, out encoding, out extension, out streamIds, out warnings);
            try
            {
                using (var fs = new FileStream((Path.Combine(filePath, file)), FileMode.Create, FileAccess.Write))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                // likely happens if the report does not get the data or parameters it requires,
                // or there is something wrong with the rdlc file itself
                return "FAILED";
            }

            // remove the temporary file
            System.IO.File.Delete(Path.Combine(filePath + fileName + "(processing)"));

            // return the new report path
            return Path.Combine(filePath, file);
        }

        /// <summary>
        /// Simple SHA256 encryption method
        /// Can be used for passwords, user names, etc
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Encrypted string based on the parameter "value"</returns>
        public static string SHA256(string value)
        {
            // TODO - salt?
            System.Security.Cryptography.SHA256 sha = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.ASCII.GetBytes(value);
            var hash = sha.ComputeHash(bytes);
            System.Text.StringBuilder stringbuilder = new System.Text.StringBuilder();
            for (var i = 0; i < hash.Length; i++)
            {
                stringbuilder.Append(hash[i].ToString("X2"));
            }
            return stringbuilder.ToString();
        }

        /// <summary>
        /// SELECT method for returning data from an SQL database
        /// </summary>
        /// <returns>DataTable with data, or blank DataTable on fail</returns>
        public DataTable returnRecordSQL(string sSQL)
        {
            DataView view = new DataView();

            try
            {
                SqlDataSource data = new SqlDataSource("LOGIN CREDENTIALS", sSQL);
                view = (DataView)data.Select(DataSourceSelectArguments.Empty);
                DataTable dt = view.ToTable();
                return dt;
            }
            catch (System.Data.SqlClient.SqlException ex)
            {
                return new DataTable();
            }
        }

        /// <summary>
        /// calculates business hours given a lower and upper date constraint
        /// has a commented out loop to include your own holiday table if applicable
        /// </summary>
        /// <param name="startDate">lower date constraint</param>
        /// <param name="endDate">upper date constraint</param>
        /// <returns>Business hours within the given date range</returns>
        public double getBusinessHours(DateTime startDate, DateTime endDate)
        {
            double totalBusinessHours = 0;

            //DataTable holidays; // SELECT holidays from your own database with column "HolidayDate"
            //DataRow[] foundrows = holidays.Select("HolidayDate >= '" + lowerDate.ToString() + "' AND HolidayDate <= '" + upperDate.ToString() + "'");

            //// loop through custom holidays and dock 8 hour work days for each found holiday
            //if (foundrows.Length > 0)
            //{
            //    foreach (DataRow row in foundrows)
            //    {
            //        totalBusinessHours = totalBusinessHours - 8;
            //    }
            //}

            string sSQL = "Declare " +
                             " @startdate datetime = '" + startDate + "', " +
                             " @enddate datetime = '" + endDate + "', " +
                             "@baseHours float " +

                        "SET @baseHours = ( " +
                        "SELECT( " +
                             " (DATEDIFF(dd, @StartDate, @EndDate) + 1) " +
                             " - (DATEDIFF(wk, @StartDate, @EndDate) * 2) " +
                             " - (case datepart(dw, @StartDate) +@@datefirst when 8 then 1 else 0 end) " +
                             " - (case datepart(dw, @EndDate) +@@datefirst when 7 then 1 when 14 then 1 else 0 end))*8) " +
                       " SELECT @baseHours as StandardWorkHours";

            DataRow dr = returnRecordSQL(sSQL).Rows[0];

            totalBusinessHours += double.Parse(dr["StandardWorkHours"].ToString());

            return totalBusinessHours;
        }

        /// <summary>
        /// Creates a QR image based on myValue using Google's charts api
        /// </summary>
        /// <param name="myValue">some value to encode</param>
        /// <returns>base 64 encoded string for easy databasing</returns>
        public string CreateQRCode(string myValue)
        {
            var url = string.Format("http://chart.apis.google.com/chart?cht=qr&chs={1}x{2}&chl={0}", myValue, "300", "300");
            WebResponse response = default(WebResponse);
            Stream remoteStream = default(Stream);
            StreamReader readStream = default(StreamReader);
            WebRequest request = WebRequest.Create(url);
            MemoryStream ms = new MemoryStream();
            byte[] bytes;
            string base64encodedimg = "";

            // get data from google chart api
            response = request.GetResponse();
            remoteStream = response.GetResponseStream();
            readStream = new StreamReader(remoteStream);
            System.Drawing.Image img = System.Drawing.Image.FromStream(remoteStream);

            // convert image data to byte array, base 64 encoded image
            // use this img object if you wish to save the QR code to file
            // else continue and store the image as base64 encoding
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            bytes = ms.ToArray();
            base64encodedimg = Convert.ToBase64String(bytes);
            
            // clean up connections            
            response.Close();
            remoteStream.Close();
            readStream.Close();

            // QR code as base 64 encoded string
            // use First(Fields!Base64Encoding.Value) as a datasource in RDLC to convert back to image
            return base64encodedimg;
        }




