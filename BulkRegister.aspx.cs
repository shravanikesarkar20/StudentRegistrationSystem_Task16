using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Services;

namespace StudentRegistrationSystem
{
    /// <summary>One row of a file being imported on the "Import from Excel / CSV" tab, after validation.</summary>
    public class ImportPreviewRow
    {
        public int RowNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Gender { get; set; }
        public DateTime? DOB { get; set; }
        public string DOBDisplay { get { return DOB.HasValue ? DOB.Value.ToString("dd MMM yyyy") : "—"; } }
        public int CountryID { get; set; }
        public int StateID { get; set; }
        public int DistrictID { get; set; }
        public string LocationDisplay { get; set; }
        public string Address { get; set; }
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }

    public partial class BulkRegister : Page
    {
        private const string SESSION_TEMP_TABLE = "BulkStudentsTable";
        private const string SESSION_IMPORT_PREVIEW = "BulkImportPreviewRows";

        private static readonly string[] SupportedExtensions = { ".csv", ".xlsx" };

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                LoadCountries();
                LoadStates(-1);
                LoadDistricts(-1);
                BindTempGrid();
                Session.Remove(SESSION_IMPORT_PREVIEW);
            }
        }

        #region ---- Task 6: Duplicate Email Prevention ----

        [WebMethod]
        public static bool CheckEmailExists(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;

            object result = DBHelper.ExecuteScalar(
                "SELECT COUNT(1) FROM Students WHERE Email = @Email",
                new SqlParameter("@Email", email.Trim()));

            return Convert.ToInt32(result) > 0;
        }

        private static bool EmailAlreadyRegistered(string email)
        {
            object result = DBHelper.ExecuteScalar(
                "SELECT COUNT(1) FROM Students WHERE Email = @Email",
                new SqlParameter("@Email", email.Trim()));
            return Convert.ToInt32(result) > 0;
        }

        #endregion

        #region ---- Cascading Dropdowns (Country -> State -> District) ----

        private void LoadCountries()
        {
            DataTable dt = DBHelper.ExecuteQuery("SELECT CountryID, CountryName FROM Countries ORDER BY CountryName");
            ddlCountry.DataSource = dt;
            ddlCountry.DataTextField = "CountryName";
            ddlCountry.DataValueField = "CountryID";
            ddlCountry.DataBind();
            ddlCountry.Items.Insert(0, new ListItem("Select Country", ""));
        }

        private void LoadStates(int countryId)
        {
            ddlState.Items.Clear();
            ddlState.Items.Add(new ListItem("Select State", ""));

            if (countryId > 0)
            {
                DataTable dt = DBHelper.ExecuteQuery(
                    "SELECT StateID, StateName FROM States WHERE CountryID = @CountryID ORDER BY StateName",
                    new SqlParameter("@CountryID", countryId));

                foreach (DataRow row in dt.Rows)
                {
                    ddlState.Items.Add(new ListItem(row["StateName"].ToString(), row["StateID"].ToString()));
                }
            }
        }

        private void LoadDistricts(int stateId)
        {
            ddlDistrict.Items.Clear();
            ddlDistrict.Items.Add(new ListItem("Select District", ""));

            if (stateId > 0)
            {
                DataTable dt = DBHelper.ExecuteQuery(
                    "SELECT DistrictID, DistrictName FROM Districts WHERE StateID = @StateID ORDER BY DistrictName",
                    new SqlParameter("@StateID", stateId));

                foreach (DataRow row in dt.Rows)
                {
                    ddlDistrict.Items.Add(new ListItem(row["DistrictName"].ToString(), row["DistrictID"].ToString()));
                }
            }
        }

        protected void ddlCountry_SelectedIndexChanged(object sender, EventArgs e)
        {
            int countryId;
            int.TryParse(ddlCountry.SelectedValue, out countryId);
            LoadStates(countryId);
            LoadDistricts(-1);
        }

        protected void ddlState_SelectedIndexChanged(object sender, EventArgs e)
        {
            int stateId;
            int.TryParse(ddlState.SelectedValue, out stateId);
            LoadDistricts(stateId);
        }

        #endregion

        #region ---- Temporary (staged) list, held in Session ----

        private DataTable GetTempTable()
        {
            DataTable dt = Session[SESSION_TEMP_TABLE] as DataTable;
            if (dt == null)
            {
                dt = new DataTable();
                dt.Columns.Add("RowKey", typeof(string));
                dt.Columns.Add("FullName", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Mobile", typeof(string));
                dt.Columns.Add("Gender", typeof(string));
                dt.Columns.Add("DOB", typeof(DateTime));
                dt.Columns.Add("CountryID", typeof(int));
                dt.Columns.Add("StateID", typeof(int));
                dt.Columns.Add("DistrictID", typeof(int));
                dt.Columns.Add("Location", typeof(string));
                dt.Columns.Add("Address", typeof(string));
                Session[SESSION_TEMP_TABLE] = dt;
            }
            return dt;
        }

        private void SaveTempTable(DataTable dt)
        {
            Session[SESSION_TEMP_TABLE] = dt;
        }

        private void BindTempGrid()
        {
            DataTable dt = GetTempTable();
            gvTemp.DataSource = dt;
            gvTemp.DataBind();
            lblTempCount.Text = dt.Rows.Count + " pending";
        }

        #endregion

        #region ---- Add / Remove / Clear (manual entry) ----

        protected void btnAddRecord_Click(object sender, EventArgs e)
        {
            HideMessages();

            if (!Page.IsValid) return;

            if (ddlCountry.SelectedValue == "" || ddlState.SelectedValue == "" || ddlDistrict.SelectedValue == "")
            {
                ShowError("Please select Country, State and District.");
                return;
            }

            string email = txtEmail.Text.Trim();
            DataTable dt = GetTempTable();

            bool duplicateInTempList = dt.AsEnumerable()
                .Any(r => string.Equals(r.Field<string>("Email"), email, StringComparison.OrdinalIgnoreCase));
            if (duplicateInTempList)
            {
                ShowError("This email is already in the pending list below.");
                return;
            }

            if (EmailAlreadyRegistered(email))
            {
                ShowError("A student is already registered with this Email Address.");
                return;
            }

            string mobile = string.IsNullOrEmpty(hdnFullMobile.Value) ? txtMobile.Text.Trim() : hdnFullMobile.Value;
            int countryId = int.Parse(ddlCountry.SelectedValue);
            int stateId = int.Parse(ddlState.SelectedValue);
            int districtId = int.Parse(ddlDistrict.SelectedValue);
            string location = ddlDistrict.SelectedItem.Text + ", " + ddlState.SelectedItem.Text + ", " + ddlCountry.SelectedItem.Text;

            AddRowToTempTable(dt, txtFullName.Text.Trim(), email, mobile, ddlGender.SelectedValue,
                Convert.ToDateTime(txtDOB.Text), countryId, stateId, districtId, location, txtAddress.Text.Trim());

            SaveTempTable(dt);
            BindTempGrid();
            ShowSuccess("Record added to the pending list. Add more students or click Save All when you're ready.");
            ClearForm();
        }

        private void AddRowToTempTable(DataTable dt, string fullName, string email, string mobile, string gender,
            DateTime dob, int countryId, int stateId, int districtId, string location, string address)
        {
            DataRow newRow = dt.NewRow();
            newRow["RowKey"] = Guid.NewGuid().ToString("N");
            newRow["FullName"] = fullName;
            newRow["Email"] = email;
            newRow["Mobile"] = mobile;
            newRow["Gender"] = gender;
            newRow["DOB"] = dob;
            newRow["CountryID"] = countryId;
            newRow["StateID"] = stateId;
            newRow["DistrictID"] = districtId;
            newRow["Location"] = location;
            newRow["Address"] = address ?? "";
            dt.Rows.Add(newRow);
        }

        protected void gvTemp_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "RemoveRow") return;

            HideMessages();

            string rowKey = e.CommandArgument.ToString();
            DataTable dt = GetTempTable();

            DataRow[] matches = dt.Select("RowKey = '" + rowKey.Replace("'", "''") + "'");
            if (matches.Length > 0)
            {
                dt.Rows.Remove(matches[0]);
                SaveTempTable(dt);
            }

            BindTempGrid();
        }

        protected void btnClearAll_Click(object sender, EventArgs e)
        {
            HideMessages();
            Session.Remove(SESSION_TEMP_TABLE);
            BindTempGrid();
            ShowSuccess("All pending records were cleared.");
        }

        #endregion

        #region ---- Import from Excel / CSV ----

        protected void btnDownloadTemplate_Click(object sender, EventArgs e)
        {
            const string csv =
                "FullName,Email,Mobile,Gender,DOB,Country,State,District,Address\r\n" +
                "Aarav Sharma,aarav.sharma@example.com,9876543210,Male,2004-08-15,India,Maharashtra,Kolhapur,\"123 MG Road\"\r\n" +
                "Priya Patel,priya.patel@example.com,9123456780,Female,2005-01-22,India,Maharashtra,Pune,\"45 Baner Road\"\r\n";

            Response.Clear();
            Response.ContentType = "text/csv";
            Response.AddHeader("Content-Disposition", "attachment; filename=BulkStudentImportTemplate.csv");
            Response.Write(csv);
            Response.End();
        }

        protected void btnPreviewFile_Click(object sender, EventArgs e)
        {
            HideMessages();
            HideImportPreview();

            if (!fuBulkFile.HasFile)
            {
                ShowError("Please choose a .csv or .xlsx file first.");
                return;
            }

            string fileName = fuBulkFile.FileName;
            string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();

            if (!SupportedExtensions.Contains(ext))
            {
                ShowError("Unsupported file type \"" + ext + "\". Please upload a .csv or .xlsx file.");
                return;
            }

            DataTable rawData;
            try
            {
                using (Stream stream = fuBulkFile.PostedFile.InputStream)
                {
                    rawData = FileImportHelper.ReadFile(stream, fileName);
                }
            }
            catch (Exception ex)
            {
                ShowError("Couldn't read that file: " + ex.Message);
                return;
            }

            if (rawData.Rows.Count == 0)
            {
                ShowWarning("The file was read successfully but contains no data rows below the header.");
                return;
            }

            List<ImportPreviewRow> preview = ValidateAndBuildPreview(rawData);
            Session[SESSION_IMPORT_PREVIEW] = preview;

            gvImportPreview.DataSource = preview;
            gvImportPreview.DataBind();

            int validCount = preview.Count(r => r.IsValid);
            int invalidCount = preview.Count - validCount;

            if (invalidCount == 0)
            {
                pnlImportSummary.CssClass = "alert alert-success";
                pnlImportSummary.Controls.Clear();
                pnlImportSummary.Controls.Add(new LiteralControl(
                    "<i class='bi bi-check-circle-fill me-1'></i>All " + validCount + " row(s) look good and are ready to import."));
            }
            else
            {
                pnlImportSummary.CssClass = "alert alert-warning";
                pnlImportSummary.Controls.Clear();
                pnlImportSummary.Controls.Add(new LiteralControl(
                    "<i class='bi bi-exclamation-triangle-fill me-1'></i>" + validCount + " row(s) are ready to import. " +
                    invalidCount + " row(s) will be skipped — see the reasons below. You can fix the file and re-upload, or import just the valid rows."));
            }

            pnlImportPreview.CssClass = "mt-4";
        }

        protected void btnCancelImport_Click(object sender, EventArgs e)
        {
            Session.Remove(SESSION_IMPORT_PREVIEW);
            HideImportPreview();
            HideMessages();
        }

        protected void btnImportValidRows_Click(object sender, EventArgs e)
        {
            HideMessages();

            List<ImportPreviewRow> preview = Session[SESSION_IMPORT_PREVIEW] as List<ImportPreviewRow>;
            if (preview == null || preview.Count == 0)
            {
                ShowError("There's no previewed file to import. Choose a file and click Preview File first.");
                return;
            }

            List<ImportPreviewRow> candidates = preview.Where(r => r.IsValid).ToList();
            if (candidates.Count == 0)
            {
                ShowError("None of the rows in that file were valid, so nothing was imported.");
                return;
            }

            DataTable dt = GetTempTable();
            HashSet<string> pendingEmails = new HashSet<string>(
                dt.AsEnumerable().Select(r => r.Field<string>("Email").ToLowerInvariant()));

            // Re-check against the database in case something changed since the preview was generated.
            HashSet<string> alreadyInDb = GetExistingEmails(candidates.Select(r => r.Email).ToList());

            int imported = 0;
            int skippedDuplicate = 0;
            HashSet<string> seenInThisBatch = new HashSet<string>();

            foreach (ImportPreviewRow row in candidates)
            {
                string emailKey = row.Email.ToLowerInvariant();

                if (pendingEmails.Contains(emailKey) || alreadyInDb.Contains(emailKey) || seenInThisBatch.Contains(emailKey))
                {
                    skippedDuplicate++;
                    continue;
                }

                AddRowToTempTable(dt, row.FullName, row.Email, row.Mobile, row.Gender, row.DOB.Value,
                    row.CountryID, row.StateID, row.DistrictID, row.LocationDisplay, row.Address);

                seenInThisBatch.Add(emailKey);
                imported++;
            }

            SaveTempTable(dt);
            BindTempGrid();

            Session.Remove(SESSION_IMPORT_PREVIEW);
            HideImportPreview();

            if (imported > 0 && skippedDuplicate == 0)
            {
                ShowSuccess(imported + " student(s) from the file were added to the pending list. Click Save All when you're ready.");
            }
            else if (imported > 0)
            {
                ShowSuccess(imported + " student(s) from the file were added to the pending list.");
                ShowWarning(skippedDuplicate + " row(s) were skipped because that email was already pending or registered.");
            }
            else
            {
                ShowWarning("All valid rows were already pending or registered, so nothing new was added.");
            }
        }

        /// <summary>Maps raw file columns (by flexible header name) onto Student fields and validates each row.</summary>
        private List<ImportPreviewRow> ValidateAndBuildPreview(DataTable rawData)
        {
            Dictionary<string, string> columnMap = BuildColumnMap(rawData.Columns);

            DataTable countries = DBHelper.ExecuteQuery("SELECT CountryID, CountryName FROM Countries");
            DataTable states = DBHelper.ExecuteQuery("SELECT StateID, StateName, CountryID FROM States");
            DataTable districts = DBHelper.ExecuteQuery("SELECT DistrictID, DistrictName, StateID FROM Districts");

            List<ImportPreviewRow> results = new List<ImportPreviewRow>();
            HashSet<string> emailsSeenInFile = new HashSet<string>();
            int rowNumber = 1;

            foreach (DataRow raw in rawData.Rows)
            {
                rowNumber++; // row 1 is the header, so first data row is file row 2

                ImportPreviewRow item = new ImportPreviewRow { RowNumber = rowNumber };
                List<string> errors = new List<string>();

                string fullName = GetCell(raw, columnMap, "FullName");
                string email = GetCell(raw, columnMap, "Email");
                string mobile = GetCell(raw, columnMap, "Mobile");
                string genderRaw = GetCell(raw, columnMap, "Gender");
                string dobRaw = GetCell(raw, columnMap, "DOB");
                string countryRaw = GetCell(raw, columnMap, "Country");
                string stateRaw = GetCell(raw, columnMap, "State");
                string districtRaw = GetCell(raw, columnMap, "District");
                string address = GetCell(raw, columnMap, "Address");

                item.FullName = fullName;
                item.Mobile = mobile;
                item.Address = address;

                if (string.IsNullOrWhiteSpace(fullName)) errors.Add("Full name is missing.");

                if (string.IsNullOrWhiteSpace(email))
                {
                    errors.Add("Email is missing.");
                }
                else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    errors.Add("Email format looks invalid.");
                }
                else
                {
                    string emailKey = email.Trim().ToLowerInvariant();
                    if (!emailsSeenInFile.Add(emailKey))
                    {
                        errors.Add("Duplicate email — already appears earlier in this file.");
                    }
                    else if (EmailAlreadyRegistered(email.Trim()))
                    {
                        errors.Add("A student is already registered with this email.");
                    }
                }
                item.Email = string.IsNullOrWhiteSpace(email) ? "" : email.Trim();

                if (string.IsNullOrWhiteSpace(mobile)) errors.Add("Mobile number is missing.");

                string gender = NormalizeGender(genderRaw);
                item.Gender = gender ?? genderRaw;
                if (gender == null) errors.Add("Gender must be Male, Female or Other.");

                DateTime dob;
                if (TryParseDob(dobRaw, out dob))
                {
                    item.DOB = dob;
                }
                else
                {
                    errors.Add("Date of birth is missing or not a recognizable date.");
                }

                int countryId = LookupId(countries.Select(), "CountryName", "CountryID", countryRaw);
                int stateId = -1, districtId = -1;
                if (countryId <= 0)
                {
                    errors.Add("Country \"" + countryRaw + "\" was not found.");
                }
                else
                {
                    DataRow[] matchingStates = states.Select("CountryID = " + countryId);
                    stateId = LookupId(matchingStates, "StateName", "StateID", stateRaw);
                    if (stateId <= 0)
                    {
                        errors.Add("State \"" + stateRaw + "\" was not found under " + countryRaw + ".");
                    }
                    else
                    {
                        DataRow[] matchingDistricts = districts.Select("StateID = " + stateId);
                        districtId = LookupId(matchingDistricts, "DistrictName", "DistrictID", districtRaw);
                        if (districtId <= 0)
                        {
                            errors.Add("District \"" + districtRaw + "\" was not found under " + stateRaw + ".");
                        }
                    }
                }

                item.CountryID = countryId > 0 ? countryId : 0;
                item.StateID = stateId > 0 ? stateId : 0;
                item.DistrictID = districtId > 0 ? districtId : 0;
                item.LocationDisplay = (districtId > 0 && stateId > 0 && countryId > 0)
                    ? (districtRaw + ", " + stateRaw + ", " + countryRaw)
                    : string.Join(" / ", new[] { countryRaw, stateRaw, districtRaw }.Where(s => !string.IsNullOrWhiteSpace(s)));

                item.IsValid = errors.Count == 0;
                item.ErrorMessage = string.Join(" ", errors);

                results.Add(item);
            }

            return results;
        }

        private static Dictionary<string, string> BuildColumnMap(DataColumnCollection columns)
        {
            // Maps our canonical field name -> the actual column name found in the uploaded file.
            var aliases = new Dictionary<string, string[]>
            {
                { "FullName", new[] { "fullname", "full name", "name", "studentname" } },
                { "Email", new[] { "email", "emailaddress", "email address" } },
                { "Mobile", new[] { "mobile", "mobilenumber", "phone", "phonenumber", "contact" } },
                { "Gender", new[] { "gender", "sex" } },
                { "DOB", new[] { "dob", "dateofbirth", "date of birth", "birthdate" } },
                { "Country", new[] { "country" } },
                { "State", new[] { "state", "province" } },
                { "District", new[] { "district", "city" } },
                { "Address", new[] { "address", "fulladdress", "full address" } }
            };

            Dictionary<string, string> normalizedColumns = columns.Cast<DataColumn>()
                .ToDictionary(c => Normalize(c.ColumnName), c => c.ColumnName);

            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (var kvp in aliases)
            {
                foreach (string alias in kvp.Value)
                {
                    string normalizedAlias = Normalize(alias);
                    if (normalizedColumns.ContainsKey(normalizedAlias))
                    {
                        map[kvp.Key] = normalizedColumns[normalizedAlias];
                        break;
                    }
                }
            }
            return map;
        }

        private static string Normalize(string s)
        {
            return Regex.Replace((s ?? "").ToLowerInvariant(), @"[\s_\-]", "");
        }

        private static string GetCell(DataRow row, Dictionary<string, string> columnMap, string canonicalField)
        {
            string actualColumn;
            if (!columnMap.TryGetValue(canonicalField, out actualColumn)) return "";
            object value = row[actualColumn];
            return value == null || value == DBNull.Value ? "" : value.ToString().Trim();
        }

        private static string NormalizeGender(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string g = raw.Trim().ToLowerInvariant();
            if (g == "male" || g == "m") return "Male";
            if (g == "female" || g == "f") return "Female";
            if (g == "other" || g == "o") return "Other";
            return null;
        }

        private static bool TryParseDob(string raw, out DateTime dob)
        {
            dob = default(DateTime);
            if (string.IsNullOrWhiteSpace(raw)) return false;

            string[] formats = { "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "d-M-yyyy", "d/M/yyyy" };
            foreach (string fmt in formats)
            {
                if (DateTime.TryParseExact(raw.Trim(), fmt, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out dob))
                {
                    return true;
                }
            }

            // Excel sometimes stores dates as a serial day-count number.
            double serial;
            if (double.TryParse(raw.Trim(), out serial) && serial > 0)
            {
                try
                {
                    dob = new DateTime(1899, 12, 30).AddDays(serial);
                    return true;
                }
                catch { /* fall through */ }
            }

            return DateTime.TryParse(raw.Trim(), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out dob);
        }

        private static int LookupId(DataRow[] rows, string nameColumn, string idColumn, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            string target = name.Trim();
            foreach (DataRow row in rows)
            {
                if (string.Equals(row[nameColumn].ToString(), target, StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToInt32(row[idColumn]);
                }
            }
            return -1;
        }

        private void HideImportPreview()
        {
            pnlImportPreview.CssClass = "mt-4 d-none";
        }

        #endregion

        #region ---- Save All (single transactional bulk insert) ----

        protected void btnSaveAll_Click(object sender, EventArgs e)
        {
            HideMessages();

            DataTable dt = GetTempTable();
            if (dt.Rows.Count == 0)
            {
                ShowError("There are no pending records to save. Add at least one student first.");
                return;
            }

            List<string> emails = dt.AsEnumerable().Select(r => r.Field<string>("Email")).ToList();
            HashSet<string> alreadyInDb = GetExistingEmails(emails);

            List<DataRow> duplicateRows = new List<DataRow>();
            List<DataRow> insertableRows = new List<DataRow>();

            foreach (DataRow row in dt.Rows)
            {
                if (alreadyInDb.Contains(row.Field<string>("Email").ToLowerInvariant()))
                    duplicateRows.Add(row);
                else
                    insertableRows.Add(row);
            }

            int savedCount = 0;
            if (insertableRows.Count > 0)
            {
                const string insertSql = @"INSERT INTO Students
                    (FullName, Email, Mobile, CountryID, StateID, DistrictID, Address, Gender, DOB, PhotoPath, IsEmailVerified, RegistrationDate)
                    VALUES
                    (@FullName, @Email, @Mobile, @CountryID, @StateID, @DistrictID, @Address, @Gender, @DOB, NULL, 0, GETDATE())";

                List<SqlParameter[]> parameterSets = insertableRows.Select(row => new[]
                {
                    new SqlParameter("@FullName", row["FullName"]),
                    new SqlParameter("@Email", row["Email"]),
                    new SqlParameter("@Mobile", row["Mobile"]),
                    new SqlParameter("@CountryID", row["CountryID"]),
                    new SqlParameter("@StateID", row["StateID"]),
                    new SqlParameter("@DistrictID", row["DistrictID"]),
                    new SqlParameter("@Address", string.IsNullOrEmpty(row["Address"].ToString()) ? (object)DBNull.Value : row["Address"]),
                    new SqlParameter("@Gender", row["Gender"]),
                    new SqlParameter("@DOB", row["DOB"])
                }).ToList();

                try
                {
                    savedCount = DBHelper.ExecuteTransactionalBatch(insertSql, parameterSets);
                }
                catch (Exception ex)
                {
                    ShowError("Save All failed — no records were saved (all-or-nothing transaction). Details: " + ex.Message);
                    return;
                }
            }

            DataTable remaining = dt.Clone();
            foreach (DataRow row in duplicateRows)
            {
                remaining.ImportRow(row);
            }
            SaveTempTable(remaining);
            BindTempGrid();

            if (savedCount > 0 && duplicateRows.Count == 0)
            {
                ShowSuccess(savedCount + " student(s) registered successfully.");
            }
            else if (savedCount > 0 && duplicateRows.Count > 0)
            {
                ShowSuccess(savedCount + " student(s) registered successfully.");
                ShowWarning(duplicateRows.Count + " record(s) were skipped and kept in the pending list because that email is already registered: " +
                    string.Join(", ", duplicateRows.Select(r => r.Field<string>("Email"))));
            }
            else
            {
                ShowWarning("All " + duplicateRows.Count + " pending record(s) were skipped because those emails are already registered. Update or remove them and try again.");
            }
        }

        private HashSet<string> GetExistingEmails(List<string> emails)
        {
            var result = new HashSet<string>();
            if (emails.Count == 0) return result;

            var parameters = new List<SqlParameter>();
            var placeholders = new List<string>();
            for (int i = 0; i < emails.Count; i++)
            {
                string paramName = "@e" + i;
                placeholders.Add(paramName);
                parameters.Add(new SqlParameter(paramName, emails[i]));
            }

            string sql = "SELECT Email FROM Students WHERE Email IN (" + string.Join(",", placeholders) + ")";
            DataTable dt = DBHelper.ExecuteQuery(sql, parameters.ToArray());

            foreach (DataRow row in dt.Rows)
            {
                result.Add(row["Email"].ToString().ToLowerInvariant());
            }
            return result;
        }

        #endregion

        #region ---- Helpers ----

        private void ShowSuccess(string message)
        {
            pnlSuccessMsg.Controls.Clear();
            pnlSuccessMsg.Controls.Add(new LiteralControl(message));
            pnlSuccessMsg.CssClass = "alert alert-success";
        }

        private void ShowWarning(string message)
        {
            pnlWarningMsg.Controls.Clear();
            pnlWarningMsg.Controls.Add(new LiteralControl(message));
            pnlWarningMsg.CssClass = "alert alert-warning";
        }

        private void ShowError(string message)
        {
            pnlErrorMsg.Controls.Clear();
            pnlErrorMsg.Controls.Add(new LiteralControl(message));
            pnlErrorMsg.CssClass = "alert alert-danger";
        }

        private void HideMessages()
        {
            pnlSuccessMsg.CssClass = "alert alert-success d-none";
            pnlErrorMsg.CssClass = "alert alert-danger d-none";
            pnlWarningMsg.CssClass = "alert alert-warning d-none";
        }

        private void ClearForm()
        {
            txtFullName.Text = "";
            txtEmail.Text = "";
            txtMobile.Text = "";
            hdnFullMobile.Value = "";
            txtAddress.Text = "";
            txtDOB.Text = "";
            ddlGender.SelectedIndex = 0;
            ddlCountry.SelectedIndex = 0;
            LoadStates(-1);
            LoadDistricts(-1);
        }

        #endregion
    }
}
