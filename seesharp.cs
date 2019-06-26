
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
        public static string createReport(string filePath, string fileName, string fileExt, string reportName, List<DataTable> sourceTables, List<ReportParameter> parameters)
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
