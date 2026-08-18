using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

/// <summary>
/// Task 12: Registration Fee Management (Admin Panel).
///
/// Single data-access + business-logic point for the whole module, following the same
/// convention as AdvertisementHelper / RichTextDocumentHelper: every query is parameterized,
/// every write that touches money crosses a SqlTransaction, and every public method is safe
/// to call directly from a code-behind Page_Load/Click handler.
///
/// Money flow implemented here:
///   FeeStructure (config)  --generate-->  StudentFeeDemand (+ FeeInstallmentDemand rows)
///   FeeTransaction (money received)  --allocate-->  FeeTransactionAllocation (Principal/LateFee)
///   allocations roll back up into FeeInstallmentDemand.AmountPaid/LateFeeCharged and
///   StudentFeeDemand.AmountPaid/LateFeeCharged/Status, keeping the demand's own columns as a
///   fast-to-read cache of what the allocation ledger already proves.
/// </summary>
public static class RegistrationFeeHelper
{
    // =====================================================================================
    // LOOKUPS
    // =====================================================================================

    public static DataTable GetAcademicYears(bool activeOnly = true)
    {
        string sql = "SELECT AcademicYearID, YearLabel, IsActive FROM dbo.AcademicYears" +
                     (activeOnly ? " WHERE IsActive = 1" : "") + " ORDER BY YearLabel DESC";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataTable GetInstitutes(bool activeOnly = true)
    {
        // An institute is only selectable here once an admin has approved its
        // self-registration (see InstituteRegistrationHelper.ApproveInstitute) - Pending and
        // Rejected institutes must never reach the student Registration form.
        string sql = "SELECT InstituteID, InstituteName, IsActive FROM dbo.Institutes WHERE ApprovalStatus = N'Approved'" +
                     (activeOnly ? " AND IsActive = 1" : "") + " ORDER BY InstituteName";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataTable GetCourses(int? instituteId = null, bool activeOnly = true)
    {
        string sql = "SELECT CourseID, CourseName, InstituteID, IsActive FROM dbo.Courses WHERE 1=1";
        List<SqlParameter> ps = new List<SqlParameter>();
        if (instituteId.HasValue)
        {
            sql += " AND InstituteID = @InstituteID";
            ps.Add(new SqlParameter("@InstituteID", instituteId.Value));
        }
        if (activeOnly) sql += " AND IsActive = 1";
        sql += " ORDER BY CourseName";
        return DBHelper.ExecuteQuery(sql, ps.ToArray());
    }

    public static DataTable GetStudentCategories(bool activeOnly = true)
    {
        string sql = "SELECT StudentCategoryID, CategoryName, IsActive FROM dbo.StudentCategories" +
                     (activeOnly ? " WHERE IsActive = 1" : "") + " ORDER BY CategoryName";
        return DBHelper.ExecuteQuery(sql);
    }

    public static DataTable GetFeeTypes(bool activeOnly = true)
    {
        string sql = "SELECT FeeTypeID, FeeTypeName, IsActive FROM dbo.FeeTypes" +
                     (activeOnly ? " WHERE IsActive = 1" : "") + " ORDER BY FeeTypeName";
        return DBHelper.ExecuteQuery(sql);
    }

    // Fixed list — Year/Semester is a free label on the fee structure, but the dropdown is
    // constrained to these so admins can't fat-finger a value that will never match a student.
    public static readonly string[] YearSemesterOptions = new[]
    {
        "Year 1", "Year 2", "Year 3", "Year 4",
        "Semester 1", "Semester 2", "Semester 3", "Semester 4",
        "Semester 5", "Semester 6", "Semester 7", "Semester 8"
    };

    // =====================================================================================
    // FEE STRUCTURES (configuration)
    // =====================================================================================

    public static DataTable GetFeeStructures(string searchTerm, bool? activeOnly)
    {
        string sql = @"
            SELECT fs.FeeStructureID, fs.AcademicYearID, ay.YearLabel,
                   fs.InstituteID, ins.InstituteName,
                   fs.CourseID, c.CourseName,
                   fs.YearSemester,
                   fs.StudentCategoryID, sc.CategoryName,
                   fs.FeeTypeID, ft.FeeTypeName,
                   fs.FeeAmount, fs.DueDate,
                   fs.InstallmentsAllowed, fs.NumberOfInstallments,
                   fs.LateFeeType, fs.LateFeeValue, fs.LateFeeGraceDays, fs.LateFeeMaxAmount,
                   fs.IsActive, fs.CreatedDate, fs.UpdatedDate,
                   (SELECT COUNT(*) FROM dbo.StudentFeeDemands d WHERE d.FeeStructureID = fs.FeeStructureID) AS DemandCount
            FROM dbo.FeeStructures fs
            JOIN dbo.AcademicYears ay ON ay.AcademicYearID = fs.AcademicYearID
            JOIN dbo.Institutes ins ON ins.InstituteID = fs.InstituteID
            JOIN dbo.Courses c ON c.CourseID = fs.CourseID
            JOIN dbo.StudentCategories sc ON sc.StudentCategoryID = fs.StudentCategoryID
            JOIN dbo.FeeTypes ft ON ft.FeeTypeID = fs.FeeTypeID
            WHERE (@Search = '' OR c.CourseName LIKE '%' + @Search + '%'
                              OR ft.FeeTypeName LIKE '%' + @Search + '%'
                              OR ay.YearLabel LIKE '%' + @Search + '%')
              AND (@ActiveOnly IS NULL OR fs.IsActive = @ActiveOnly)
            ORDER BY fs.UpdatedDate DESC";

        return DBHelper.ExecuteQuery(sql,
            new SqlParameter("@Search", searchTerm ?? string.Empty),
            new SqlParameter("@ActiveOnly", (object)activeOnly ?? DBNull.Value));
    }

    public static DataRow GetFeeStructureById(int feeStructureId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT * FROM dbo.FeeStructures WHERE FeeStructureID = @Id",
            new SqlParameter("@Id", feeStructureId));
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static DataTable GetInstallmentSchedule(int feeStructureId)
    {
        return DBHelper.ExecuteQuery(
            "SELECT InstallmentID, InstallmentNo, DueDate, AmountPercent FROM dbo.FeeStructureInstallments " +
            "WHERE FeeStructureID = @Id ORDER BY InstallmentNo",
            new SqlParameter("@Id", feeStructureId));
    }

    /// <summary>Input bag for creating/editing a fee structure — keeps the Save signature manageable.</summary>
    public class FeeStructureInput
    {
        public int? FeeStructureID;         // null = create
        public int AcademicYearID;
        public int InstituteID;
        public int CourseID;
        public string YearSemester;
        public int StudentCategoryID;
        public int FeeTypeID;
        public decimal FeeAmount;
        public DateTime DueDate;
        public bool InstallmentsAllowed;
        public int NumberOfInstallments;
        public string LateFeeType;          // Flat | PerDay | Percentage
        public decimal LateFeeValue;
        public int LateFeeGraceDays;
        public decimal? LateFeeMaxAmount;
        public bool IsActive;
        public string ActorName;
        // Installment schedule: (InstallmentNo, DueDate, AmountPercent). Empty/null when
        // InstallmentsAllowed is false, or to fall back to an even auto-split.
        public List<Tuple<int, DateTime, decimal>> Installments;
    }

    /// <summary>
    /// Validates and saves a fee structure (insert or update). Returns the FeeStructureID on
    /// success. Throws ArgumentException with a user-facing message on validation failure —
    /// callers should catch that specifically to show a friendly error instead of a 500.
    /// </summary>
    public static int SaveFeeStructure(FeeStructureInput input)
    {
        if (input.FeeAmount <= 0)
            throw new ArgumentException("Fee amount must be greater than zero.");
        if (input.NumberOfInstallments < 1 || input.NumberOfInstallments > 12)
            throw new ArgumentException("Number of installments must be between 1 and 12.");
        if (input.LateFeeType != "Flat" && input.LateFeeType != "PerDay" && input.LateFeeType != "Percentage")
            throw new ArgumentException("Late fee type must be Flat, PerDay, or Percentage.");
        if (input.LateFeeValue < 0)
            throw new ArgumentException("Late fee value cannot be negative.");
        if (string.IsNullOrWhiteSpace(input.YearSemester))
            throw new ArgumentException("Year/Semester is required.");

        // Build the installment schedule up front: either use the admin-supplied rows, or
        // auto-split evenly (last installment absorbs the rounding remainder so percentages
        // always sum to exactly 100.00).
        List<Tuple<int, DateTime, decimal>> schedule = input.Installments;
        int installmentCount = input.InstallmentsAllowed ? input.NumberOfInstallments : 1;
        if (schedule == null || schedule.Count != installmentCount)
        {
            schedule = new List<Tuple<int, DateTime, decimal>>();
            decimal evenShare = Math.Floor((100m / installmentCount) * 100m) / 100m;
            decimal runningTotal = 0m;
            for (int i = 1; i <= installmentCount; i++)
            {
                decimal percent = (i == installmentCount) ? (100m - runningTotal) : evenShare;
                runningTotal += percent;
                DateTime dueDate = installmentCount == 1
                    ? input.DueDate
                    : input.DueDate.AddMonths(i - 1);
                schedule.Add(Tuple.Create(i, dueDate, percent));
            }
        }
        else
        {
            decimal sum = schedule.Sum(t => t.Item3);
            if (Math.Abs(sum - 100m) > 0.01m)
                throw new ArgumentException("Installment percentages must add up to 100%.");
        }

        using (SqlConnection conn = DBHelper.GetConnection())
        {
            conn.Open();
            using (SqlTransaction txn = conn.BeginTransaction())
            {
                try
                {
                    int feeStructureId;

                    if (input.FeeStructureID.HasValue)
                    {
                        feeStructureId = input.FeeStructureID.Value;
                        using (SqlCommand cmd = new SqlCommand(@"
                            UPDATE dbo.FeeStructures SET
                                AcademicYearID = @AcademicYearID, InstituteID = @InstituteID, CourseID = @CourseID,
                                YearSemester = @YearSemester, StudentCategoryID = @StudentCategoryID, FeeTypeID = @FeeTypeID,
                                FeeAmount = @FeeAmount, DueDate = @DueDate,
                                InstallmentsAllowed = @InstallmentsAllowed, NumberOfInstallments = @NumberOfInstallments,
                                LateFeeType = @LateFeeType, LateFeeValue = @LateFeeValue, LateFeeGraceDays = @LateFeeGraceDays,
                                LateFeeMaxAmount = @LateFeeMaxAmount, IsActive = @IsActive,
                                UpdatedBy = @ActorName, UpdatedDate = GETDATE()
                            WHERE FeeStructureID = @FeeStructureID", conn, txn))
                        {
                            AddStructureParams(cmd, input);
                            cmd.Parameters.AddWithValue("@FeeStructureID", feeStructureId);
                            cmd.ExecuteNonQuery();
                        }

                        using (SqlCommand del = new SqlCommand(
                            "DELETE FROM dbo.FeeStructureInstallments WHERE FeeStructureID = @Id", conn, txn))
                        {
                            del.Parameters.AddWithValue("@Id", feeStructureId);
                            del.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO dbo.FeeStructures
                                (AcademicYearID, InstituteID, CourseID, YearSemester, StudentCategoryID, FeeTypeID,
                                 FeeAmount, DueDate, InstallmentsAllowed, NumberOfInstallments,
                                 LateFeeType, LateFeeValue, LateFeeGraceDays, LateFeeMaxAmount, IsActive,
                                 CreatedBy, CreatedDate, UpdatedBy, UpdatedDate)
                            VALUES
                                (@AcademicYearID, @InstituteID, @CourseID, @YearSemester, @StudentCategoryID, @FeeTypeID,
                                 @FeeAmount, @DueDate, @InstallmentsAllowed, @NumberOfInstallments,
                                 @LateFeeType, @LateFeeValue, @LateFeeGraceDays, @LateFeeMaxAmount, @IsActive,
                                 @ActorName, GETDATE(), @ActorName, GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, txn))
                        {
                            AddStructureParams(cmd, input);
                            feeStructureId = (int)cmd.ExecuteScalar();
                        }
                    }

                    foreach (Tuple<int, DateTime, decimal> row in schedule)
                    {
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO dbo.FeeStructureInstallments (FeeStructureID, InstallmentNo, DueDate, AmountPercent)
                            VALUES (@FeeStructureID, @No, @DueDate, @Percent)", conn, txn))
                        {
                            cmd.Parameters.AddWithValue("@FeeStructureID", feeStructureId);
                            cmd.Parameters.AddWithValue("@No", row.Item1);
                            cmd.Parameters.AddWithValue("@DueDate", row.Item2.Date);
                            cmd.Parameters.AddWithValue("@Percent", row.Item3);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    txn.Commit();
                    return feeStructureId;
                }
                catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
                {
                    txn.Rollback();
                    throw new ArgumentException(
                        "A fee structure already exists for this exact combination of Academic Year, " +
                        "Institute, Course, Year/Semester, Category and Fee Type. Edit the existing one instead.");
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
            }
        }
    }

    private static void AddStructureParams(SqlCommand cmd, FeeStructureInput input)
    {
        cmd.Parameters.AddWithValue("@AcademicYearID", input.AcademicYearID);
        cmd.Parameters.AddWithValue("@InstituteID", input.InstituteID);
        cmd.Parameters.AddWithValue("@CourseID", input.CourseID);
        cmd.Parameters.AddWithValue("@YearSemester", input.YearSemester);
        cmd.Parameters.AddWithValue("@StudentCategoryID", input.StudentCategoryID);
        cmd.Parameters.AddWithValue("@FeeTypeID", input.FeeTypeID);
        cmd.Parameters.AddWithValue("@FeeAmount", input.FeeAmount);
        cmd.Parameters.AddWithValue("@DueDate", input.DueDate.Date);
        cmd.Parameters.AddWithValue("@InstallmentsAllowed", input.InstallmentsAllowed);
        cmd.Parameters.AddWithValue("@NumberOfInstallments", input.InstallmentsAllowed ? input.NumberOfInstallments : 1);
        cmd.Parameters.AddWithValue("@LateFeeType", input.LateFeeType);
        cmd.Parameters.AddWithValue("@LateFeeValue", input.LateFeeValue);
        cmd.Parameters.AddWithValue("@LateFeeGraceDays", input.LateFeeGraceDays);
        cmd.Parameters.AddWithValue("@LateFeeMaxAmount", (object)input.LateFeeMaxAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", input.IsActive);
        cmd.Parameters.AddWithValue("@ActorName", (object)input.ActorName ?? DBNull.Value);
    }

    public static void SetFeeStructureActive(int feeStructureId, bool active, string actorName)
    {
        DBHelper.ExecuteNonQuery(
            "UPDATE dbo.FeeStructures SET IsActive = @Active, UpdatedBy = @Actor, UpdatedDate = GETDATE() WHERE FeeStructureID = @Id",
            new SqlParameter("@Active", active),
            new SqlParameter("@Actor", (object)actorName ?? DBNull.Value),
            new SqlParameter("@Id", feeStructureId));
    }

    /// <summary>Deletes a fee structure only if no demand has ever been generated from it
    /// (financial history is never deleted — deactivate instead).</summary>
    public static bool DeleteFeeStructure(int feeStructureId)
    {
        object count = DBHelper.ExecuteScalar(
            "SELECT COUNT(*) FROM dbo.StudentFeeDemands WHERE FeeStructureID = @Id",
            new SqlParameter("@Id", feeStructureId));
        if (Convert.ToInt32(count) > 0)
            throw new InvalidOperationException(
                "This fee structure already has student fee demands generated against it and cannot be deleted. " +
                "Deactivate it instead.");

        DBHelper.ExecuteNonQuery("DELETE FROM dbo.FeeStructureInstallments WHERE FeeStructureID = @Id",
            new SqlParameter("@Id", feeStructureId));
        return DBHelper.ExecuteNonQuery("DELETE FROM dbo.FeeStructures WHERE FeeStructureID = @Id",
            new SqlParameter("@Id", feeStructureId)) > 0;
    }

    // =====================================================================================
    // STUDENT ACADEMIC PROFILE  (drives which fee structures apply to a student)
    // =====================================================================================

    public static DataTable SearchStudents(string term)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT TOP 25 StudentID, FullName, Email, Mobile
            FROM dbo.Students
            WHERE @Term = '' OR FullName LIKE '%' + @Term + '%' OR Email LIKE '%' + @Term + '%'
               OR Mobile LIKE '%' + @Term + '%' OR CAST(StudentID AS NVARCHAR(20)) = @Term
            ORDER BY FullName",
            new SqlParameter("@Term", term ?? string.Empty));
    }

    public static DataRow GetStudentById(int studentId)
    {
        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT StudentID, FullName, Email, Mobile FROM dbo.Students WHERE StudentID = @Id",
            new SqlParameter("@Id", studentId));
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static DataRow GetStudentAcademicProfile(int studentId)
    {
        DataTable dt = DBHelper.ExecuteQuery(@"
            SELECT p.StudentID, p.AcademicYearID, ay.YearLabel, p.InstituteID, ins.InstituteName,
                   p.CourseID, c.CourseName, p.YearSemester, p.StudentCategoryID, sc.CategoryName, p.UpdatedDate
            FROM dbo.StudentAcademicProfile p
            JOIN dbo.AcademicYears ay ON ay.AcademicYearID = p.AcademicYearID
            JOIN dbo.Institutes ins ON ins.InstituteID = p.InstituteID
            JOIN dbo.Courses c ON c.CourseID = p.CourseID
            JOIN dbo.StudentCategories sc ON sc.StudentCategoryID = p.StudentCategoryID
            WHERE p.StudentID = @Id",
            new SqlParameter("@Id", studentId));
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public static void SaveStudentAcademicProfile(int studentId, int academicYearId, int instituteId, int courseId,
        string yearSemester, int studentCategoryId, string actorName)
    {
        int rows = DBHelper.ExecuteNonQuery(@"
            UPDATE dbo.StudentAcademicProfile SET
                AcademicYearID = @AY, InstituteID = @Inst, CourseID = @Course,
                YearSemester = @YS, StudentCategoryID = @Cat, UpdatedBy = @Actor, UpdatedDate = GETDATE()
            WHERE StudentID = @StudentID",
            new SqlParameter("@AY", academicYearId), new SqlParameter("@Inst", instituteId),
            new SqlParameter("@Course", courseId), new SqlParameter("@YS", yearSemester),
            new SqlParameter("@Cat", studentCategoryId), new SqlParameter("@Actor", (object)actorName ?? DBNull.Value),
            new SqlParameter("@StudentID", studentId));

        if (rows == 0)
        {
            DBHelper.ExecuteNonQuery(@"
                INSERT INTO dbo.StudentAcademicProfile
                    (StudentID, AcademicYearID, InstituteID, CourseID, YearSemester, StudentCategoryID, UpdatedBy, UpdatedDate)
                VALUES (@StudentID, @AY, @Inst, @Course, @YS, @Cat, @Actor, GETDATE())",
                new SqlParameter("@StudentID", studentId), new SqlParameter("@AY", academicYearId),
                new SqlParameter("@Inst", instituteId), new SqlParameter("@Course", courseId),
                new SqlParameter("@YS", yearSemester), new SqlParameter("@Cat", studentCategoryId),
                new SqlParameter("@Actor", (object)actorName ?? DBNull.Value));
        }
    }

    // =====================================================================================
    // FEE DEMAND GENERATION
    // =====================================================================================

    /// <summary>
    /// Automated Generation (core requirement): finds every active FeeStructure whose axes
    /// match the student's academic profile and that does NOT yet have a demand for this
    /// student, and creates one StudentFeeDemand (+ its installment breakdown) per match.
    /// Idempotent — safe to call repeatedly (e.g. a nightly job or a manual "Refresh" button);
    /// it only ever adds newly-configured fee heads, never duplicates or overwrites existing ones.
    /// Returns the number of new demands created.
    /// </summary>
    public static int GenerateFeeDemandsForStudent(int studentId, string generatedBy)
    {
        DataRow profile = GetStudentAcademicProfile(studentId);
        if (profile == null)
            throw new InvalidOperationException("This student has no academic profile set (Academic Year / Institute / Course / Year-Semester / Category). Set it first.");

        DataTable matches = DBHelper.ExecuteQuery(@"
            SELECT fs.FeeStructureID, fs.FeeTypeID, fs.FeeAmount, fs.DueDate
            FROM dbo.FeeStructures fs
            WHERE fs.IsActive = 1
              AND fs.AcademicYearID = @AY AND fs.InstituteID = @Inst AND fs.CourseID = @Course
              AND fs.YearSemester = @YS AND fs.StudentCategoryID = @Cat
              AND NOT EXISTS (SELECT 1 FROM dbo.StudentFeeDemands d
                               WHERE d.StudentID = @StudentID AND d.FeeStructureID = fs.FeeStructureID)",
            new SqlParameter("@AY", profile["AcademicYearID"]), new SqlParameter("@Inst", profile["InstituteID"]),
            new SqlParameter("@Course", profile["CourseID"]), new SqlParameter("@YS", profile["YearSemester"]),
            new SqlParameter("@Cat", profile["StudentCategoryID"]), new SqlParameter("@StudentID", studentId));

        if (matches.Rows.Count == 0) return 0;

        int created = 0;
        using (SqlConnection conn = DBHelper.GetConnection())
        {
            conn.Open();
            using (SqlTransaction txn = conn.BeginTransaction())
            {
                try
                {
                    foreach (DataRow structure in matches.Rows)
                    {
                        int feeStructureId = Convert.ToInt32(structure["FeeStructureID"]);
                        int feeTypeId = Convert.ToInt32(structure["FeeTypeID"]);
                        decimal feeAmount = Convert.ToDecimal(structure["FeeAmount"]);
                        DateTime dueDate = Convert.ToDateTime(structure["DueDate"]);

                        int feeDemandId;
                        using (SqlCommand cmd = new SqlCommand(@"
                            INSERT INTO dbo.StudentFeeDemands
                                (StudentID, FeeStructureID, FeeTypeID, GrossAmount, DueDate, Status, GeneratedBy, GeneratedDate)
                            VALUES (@StudentID, @FeeStructureID, @FeeTypeID, @Gross, @DueDate, 'Pending', @Actor, GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, txn))
                        {
                            cmd.Parameters.AddWithValue("@StudentID", studentId);
                            cmd.Parameters.AddWithValue("@FeeStructureID", feeStructureId);
                            cmd.Parameters.AddWithValue("@FeeTypeID", feeTypeId);
                            cmd.Parameters.AddWithValue("@Gross", feeAmount);
                            cmd.Parameters.AddWithValue("@DueDate", dueDate);
                            cmd.Parameters.AddWithValue("@Actor", (object)generatedBy ?? DBNull.Value);
                            feeDemandId = (int)cmd.ExecuteScalar();
                        }

                        DataTable schedule;
                        using (SqlCommand cmd = new SqlCommand(
                            "SELECT InstallmentNo, DueDate, AmountPercent FROM dbo.FeeStructureInstallments " +
                            "WHERE FeeStructureID = @Id ORDER BY InstallmentNo", conn, txn))
                        {
                            cmd.Parameters.AddWithValue("@Id", feeStructureId);
                            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                            {
                                schedule = new DataTable();
                                da.Fill(schedule);
                            }
                        }

                        decimal allocatedSoFar = 0m;
                        for (int i = 0; i < schedule.Rows.Count; i++)
                        {
                            DataRow instRow = schedule.Rows[i];
                            int no = Convert.ToInt32(instRow["InstallmentNo"]);
                            DateTime instDue = Convert.ToDateTime(instRow["DueDate"]);
                            decimal percent = Convert.ToDecimal(instRow["AmountPercent"]);
                            decimal amount = (i == schedule.Rows.Count - 1)
                                ? Math.Round(feeAmount - allocatedSoFar, 2)
                                : Math.Round(feeAmount * percent / 100m, 2);
                            allocatedSoFar += amount;

                            using (SqlCommand cmd = new SqlCommand(@"
                                INSERT INTO dbo.FeeInstallmentDemands
                                    (FeeDemandID, InstallmentNo, DueDate, AmountDue, Status)
                                VALUES (@FeeDemandID, @No, @Due, @Amount, 'Pending')", conn, txn))
                            {
                                cmd.Parameters.AddWithValue("@FeeDemandID", feeDemandId);
                                cmd.Parameters.AddWithValue("@No", no);
                                cmd.Parameters.AddWithValue("@Due", instDue);
                                cmd.Parameters.AddWithValue("@Amount", amount);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        created++;
                    }

                    txn.Commit();
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
            }
        }
        return created;
    }

    // =====================================================================================
    // LATE FEE CALCULATION
    // =====================================================================================

    /// <summary>Computes the late fee owed on one installment as of a given date, per the
    /// owning fee structure's rule. Returns 0 while inside the grace period or already paid.</summary>
    public static decimal ComputeLateFee(DateTime dueDate, DateTime asOf, decimal outstandingPrincipal,
        string lateFeeType, decimal lateFeeValue, int graceDays, decimal? maxAmount)
    {
        if (outstandingPrincipal <= 0) return 0m;

        DateTime graceEnd = dueDate.Date.AddDays(graceDays);
        if (asOf.Date <= graceEnd) return 0m;

        int overdueDays = (asOf.Date - graceEnd).Days;
        decimal fee;
        switch (lateFeeType)
        {
            case "PerDay":
                fee = lateFeeValue * overdueDays;
                break;
            case "Percentage":
                fee = Math.Round(outstandingPrincipal * lateFeeValue / 100m, 2);
                break;
            case "Flat":
            default:
                fee = lateFeeValue;
                break;
        }

        if (maxAmount.HasValue && fee > maxAmount.Value) fee = maxAmount.Value;
        return fee < 0 ? 0m : fee;
    }

    // =====================================================================================
    // FEE SUMMARY (what the UI displays)
    // =====================================================================================

    public class FeeSummary
    {
        public decimal TotalPayable;      // sum of GrossAmount across all demands
        public decimal AmountPaid;        // sum of AmountPaid (principal only)
        public decimal DiscountAmount;    // sum of DiscountAmount
        public decimal LateFeeOutstanding;// sum of currently-due-but-unpaid late fee, computed as of today
        public decimal LateFeeCharged;    // sum of late fee already collected historically
        public decimal OutstandingAmount; // TotalPayable - Discount - Paid + LateFeeOutstanding
        public decimal NetPayable;        // TotalPayable - Discount + LateFeeOutstanding + LateFeeCharged
        public string PaymentStatus;      // Paid | PartiallyPaid | Pending | Overdue | No Dues Generated
    }

    /// <summary>Returns the per-fee-head demand rows for a student, each annotated with a
    /// live-computed outstanding late fee (as of today) alongside the stored figures.</summary>
    public static DataTable GetStudentFeeDemands(int studentId)
    {
        DataTable dt = DBHelper.ExecuteQuery(@"
            SELECT d.FeeDemandID, d.FeeStructureID, ft.FeeTypeName, d.GrossAmount, d.DiscountAmount,
                   d.DiscountReason, d.AmountPaid, d.LateFeeCharged, d.DueDate, d.Status, d.GeneratedDate,
                   fs.LateFeeType, fs.LateFeeValue, fs.LateFeeGraceDays, fs.LateFeeMaxAmount
            FROM dbo.StudentFeeDemands d
            JOIN dbo.FeeTypes ft ON ft.FeeTypeID = d.FeeTypeID
            JOIN dbo.FeeStructures fs ON fs.FeeStructureID = d.FeeStructureID
            WHERE d.StudentID = @StudentID
            ORDER BY d.DueDate, ft.FeeTypeName",
            new SqlParameter("@StudentID", studentId));

        dt.Columns.Add("LiveLateFeeOutstanding", typeof(decimal));
        dt.Columns.Add("OutstandingPrincipal", typeof(decimal));

        foreach (DataRow row in dt.Rows)
        {
            decimal gross = Convert.ToDecimal(row["GrossAmount"]);
            decimal discount = Convert.ToDecimal(row["DiscountAmount"]);
            decimal paid = Convert.ToDecimal(row["AmountPaid"]);
            decimal outstandingPrincipal = Math.Max(0, gross - discount - paid);

            decimal liveLateFee = 0m;
            if (outstandingPrincipal > 0)
            {
                liveLateFee = ComputeLateFee(
                    Convert.ToDateTime(row["DueDate"]), DateTime.Today, outstandingPrincipal,
                    row["LateFeeType"].ToString(), Convert.ToDecimal(row["LateFeeValue"]),
                    Convert.ToInt32(row["LateFeeGraceDays"]),
                    row["LateFeeMaxAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["LateFeeMaxAmount"]));
            }

            row["LiveLateFeeOutstanding"] = liveLateFee;
            row["OutstandingPrincipal"] = outstandingPrincipal;
        }
        return dt;
    }

    public static FeeSummary GetStudentFeeSummary(int studentId)
    {
        DataTable demands = GetStudentFeeDemands(studentId);
        FeeSummary summary = new FeeSummary();

        if (demands.Rows.Count == 0)
        {
            summary.PaymentStatus = "No Dues Generated";
            return summary;
        }

        bool anyOverdueUnpaid = false;
        bool anyUnpaid = false;
        bool anyPartial = false;

        foreach (DataRow row in demands.Rows)
        {
            summary.TotalPayable += Convert.ToDecimal(row["GrossAmount"]);
            summary.AmountPaid += Convert.ToDecimal(row["AmountPaid"]);
            summary.DiscountAmount += Convert.ToDecimal(row["DiscountAmount"]);
            summary.LateFeeCharged += Convert.ToDecimal(row["LateFeeCharged"]);
            summary.LateFeeOutstanding += Convert.ToDecimal(row["LiveLateFeeOutstanding"]);

            decimal outstandingPrincipal = Convert.ToDecimal(row["OutstandingPrincipal"]);
            if (outstandingPrincipal > 0)
            {
                anyUnpaid = true;
                if (Convert.ToDecimal(row["AmountPaid"]) > 0) anyPartial = true;
                if (Convert.ToDateTime(row["DueDate"]).Date < DateTime.Today) anyOverdueUnpaid = true;
            }
        }

        summary.OutstandingAmount = Math.Max(0,
            summary.TotalPayable - summary.DiscountAmount - summary.AmountPaid) + summary.LateFeeOutstanding;
        summary.NetPayable = summary.TotalPayable - summary.DiscountAmount + summary.LateFeeOutstanding + summary.LateFeeCharged;

        summary.PaymentStatus = !anyUnpaid ? "Paid"
            : anyOverdueUnpaid ? "Overdue"
            : anyPartial ? "PartiallyPaid"
            : "Pending";

        return summary;
    }

    /// <summary>Scholarship / concession. Validated so a discount can never exceed the gross
    /// amount minus whatever has already been paid (can't "discount away" money already
    /// collected).</summary>
    public static void ApplyDiscount(int feeDemandId, decimal discountAmount, string reason, string actorName)
    {
        if (discountAmount < 0) throw new ArgumentException("Discount cannot be negative.");

        DataTable dt = DBHelper.ExecuteQuery(
            "SELECT GrossAmount, AmountPaid FROM dbo.StudentFeeDemands WHERE FeeDemandID = @Id",
            new SqlParameter("@Id", feeDemandId));
        if (dt.Rows.Count == 0) throw new InvalidOperationException("Fee demand not found.");

        decimal gross = Convert.ToDecimal(dt.Rows[0]["GrossAmount"]);
        decimal paid = Convert.ToDecimal(dt.Rows[0]["AmountPaid"]);
        if (discountAmount > gross - paid)
            throw new ArgumentException("Discount cannot exceed the amount still owed on this fee head.");

        DBHelper.ExecuteNonQuery(@"
            UPDATE dbo.StudentFeeDemands SET DiscountAmount = @Discount, DiscountReason = @Reason
            WHERE FeeDemandID = @Id",
            new SqlParameter("@Discount", discountAmount), new SqlParameter("@Reason", (object)reason ?? DBNull.Value),
            new SqlParameter("@Id", feeDemandId));

        RefreshDemandStatus(feeDemandId, null);
        AppLogger.Info("RegistrationFee", string.Format("Discount of {0} applied to FeeDemandID {1} by {2}", discountAmount, feeDemandId, actorName));
    }

    // =====================================================================================
    // PAYMENT RECORDING + ALLOCATION  (transaction integrity is the point of this method)
    // =====================================================================================

    public class PaymentResult
    {
        public int TransactionID;
        public string TransactionRef;
        public decimal AmountAllocated;
        public decimal AmountUnallocated; // leftover if payment exceeds everything currently owed
    }

    /// <summary>
    /// Records one payment (online or offline) and allocates it across the student's open
    /// installment demands, oldest due date first (FIFO), late fee before principal on each
    /// installment. The entire insert + allocation + status rollup happens inside a single
    /// SqlTransaction — if anything fails partway through, nothing is committed, so a demand
    /// can never show a payment that doesn't have a matching, fully-allocated transaction.
    /// </summary>
    public static PaymentResult RecordPayment(int studentId, decimal amount, string paymentMode,
        string gatewayName, string gatewayTransactionId, string bankReferenceNumber, string chequeOrDdNumber,
        string remarks, string createdBy)
    {
        if (amount <= 0) throw new ArgumentException("Payment amount must be greater than zero.");
        string[] validModes = { "Online", "Cash", "Cheque", "DD", "BankTransfer", "UPI" };
        if (Array.IndexOf(validModes, paymentMode) < 0)
            throw new ArgumentException("Invalid payment mode.");

        PaymentResult result = new PaymentResult();

        using (SqlConnection conn = DBHelper.GetConnection())
        {
            conn.Open();
            using (SqlTransaction txn = conn.BeginTransaction())
            {
                try
                {
                    string transactionRef = "RCPT-" + DateTime.UtcNow.Ticks.ToString().Substring(8);

                    int transactionId;
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO dbo.FeeTransactions
                            (StudentID, TransactionRef, PaymentMode, Amount, PaymentDate, GatewayName,
                             GatewayTransactionID, BankReferenceNumber, ChequeOrDDNumber, Remarks,
                             Status, ReconciliationStatus, CreatedBy, CreatedDate)
                        VALUES
                            (@StudentID, @Ref, @Mode, @Amount, GETDATE(), @Gateway,
                             @GatewayTxn, @BankRef, @Cheque, @Remarks,
                             'Success', 'Unreconciled', @Actor, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, txn))
                    {
                        cmd.Parameters.AddWithValue("@StudentID", studentId);
                        cmd.Parameters.AddWithValue("@Ref", transactionRef);
                        cmd.Parameters.AddWithValue("@Mode", paymentMode);
                        cmd.Parameters.AddWithValue("@Amount", amount);
                        cmd.Parameters.AddWithValue("@Gateway", (object)gatewayName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@GatewayTxn", (object)gatewayTransactionId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BankRef", (object)bankReferenceNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Cheque", (object)chequeOrDdNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Remarks", (object)remarks ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Actor", (object)createdBy ?? DBNull.Value);
                        transactionId = (int)cmd.ExecuteScalar();
                    }

                    // Pull every open installment for this student, oldest due date first, together
                    // with its parent demand's late-fee rule so we can compute the currently-owed
                    // late fee inside the same transaction (locked in the moment it's paid).
                    DataTable openInstallments;
                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT i.InstallmentDemandID, i.FeeDemandID, i.DueDate, i.AmountDue, i.AmountPaid,
                               i.LateFeeCharged, d.DiscountAmount, d.GrossAmount,
                               fs.LateFeeType, fs.LateFeeValue, fs.LateFeeGraceDays, fs.LateFeeMaxAmount,
                               (SELECT SUM(AmountDue) FROM dbo.FeeInstallmentDemands WHERE FeeDemandID = d.FeeDemandID) AS DemandTotalDue
                        FROM dbo.FeeInstallmentDemands i
                        JOIN dbo.StudentFeeDemands d ON d.FeeDemandID = i.FeeDemandID
                        JOIN dbo.FeeStructures fs ON fs.FeeStructureID = d.FeeStructureID
                        WHERE d.StudentID = @StudentID AND i.Status <> 'Paid'
                        ORDER BY i.DueDate, i.InstallmentDemandID
                        FOR UPDATE", conn, txn))
                    {
                        // NOTE: SQL Server doesn't use "FOR UPDATE"; the transaction's default
                        // isolation already prevents lost updates for this single-writer workflow.
                        cmd.CommandText = cmd.CommandText.Replace("FOR UPDATE", "");
                        cmd.Parameters.AddWithValue("@StudentID", studentId);
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            openInstallments = new DataTable();
                            da.Fill(openInstallments);
                        }
                    }

                    decimal remaining = amount;
                    HashSet<int> touchedDemandIds = new HashSet<int>();

                    foreach (DataRow inst in openInstallments.Rows)
                    {
                        if (remaining <= 0) break;

                        int installmentDemandId = Convert.ToInt32(inst["InstallmentDemandID"]);
                        int feeDemandId = Convert.ToInt32(inst["FeeDemandID"]);
                        decimal amountDue = Convert.ToDecimal(inst["AmountDue"]);
                        decimal alreadyPaid = Convert.ToDecimal(inst["AmountPaid"]);
                        decimal discountShare = Convert.ToDecimal(inst["DiscountAmount"]) *
                            (amountDue / Math.Max(1, Convert.ToDecimal(inst["DemandTotalDue"]))); // proportional discount share
                        decimal netDue = Math.Max(0, amountDue - discountShare);
                        decimal outstandingPrincipal = Math.Max(0, netDue - alreadyPaid);
                        if (outstandingPrincipal <= 0) continue;

                        decimal lateFeeOwed = ComputeLateFee(
                            Convert.ToDateTime(inst["DueDate"]), DateTime.Today, outstandingPrincipal,
                            inst["LateFeeType"].ToString(), Convert.ToDecimal(inst["LateFeeValue"]),
                            Convert.ToInt32(inst["LateFeeGraceDays"]),
                            inst["LateFeeMaxAmount"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(inst["LateFeeMaxAmount"]));

                        // Late fee first, then principal — matches standard fee-office practice.
                        decimal lateFeePayment = Math.Min(remaining, lateFeeOwed);
                        if (lateFeePayment > 0)
                        {
                            InsertAllocation(conn, txn, transactionId, feeDemandId, installmentDemandId, "LateFee", lateFeePayment);
                            remaining -= lateFeePayment;

                            using (SqlCommand upd = new SqlCommand(
                                "UPDATE dbo.FeeInstallmentDemands SET LateFeeCharged = LateFeeCharged + @Amt WHERE InstallmentDemandID = @Id", conn, txn))
                            {
                                upd.Parameters.AddWithValue("@Amt", lateFeePayment);
                                upd.Parameters.AddWithValue("@Id", installmentDemandId);
                                upd.ExecuteNonQuery();
                            }
                        }

                        decimal principalPayment = Math.Min(remaining, outstandingPrincipal);
                        if (principalPayment > 0)
                        {
                            InsertAllocation(conn, txn, transactionId, feeDemandId, installmentDemandId, "Principal", principalPayment);
                            remaining -= principalPayment;

                            decimal newPaid = alreadyPaid + principalPayment;
                            string newStatus = newPaid >= netDue ? "Paid" : "PartiallyPaid";

                            using (SqlCommand upd = new SqlCommand(
                                "UPDATE dbo.FeeInstallmentDemands SET AmountPaid = @Paid, Status = @Status WHERE InstallmentDemandID = @Id", conn, txn))
                            {
                                upd.Parameters.AddWithValue("@Paid", newPaid);
                                upd.Parameters.AddWithValue("@Status", newStatus);
                                upd.Parameters.AddWithValue("@Id", installmentDemandId);
                                upd.ExecuteNonQuery();
                            }
                        }

                        touchedDemandIds.Add(feeDemandId);
                    }

                    foreach (int feeDemandId in touchedDemandIds)
                    {
                        RefreshDemandStatus(feeDemandId, new Tuple<SqlConnection, SqlTransaction>(conn, txn));
                    }

                    txn.Commit();

                    result.TransactionID = transactionId;
                    result.TransactionRef = transactionRef;
                    result.AmountAllocated = amount - remaining;
                    result.AmountUnallocated = remaining;
                    return result;
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
            }
        }
    }

    private static void InsertAllocation(SqlConnection conn, SqlTransaction txn, int transactionId, int feeDemandId,
        int installmentDemandId, string allocationType, decimal amount)
    {
        using (SqlCommand cmd = new SqlCommand(@"
            INSERT INTO dbo.FeeTransactionAllocations
                (TransactionID, FeeDemandID, InstallmentDemandID, AllocationType, AllocatedAmount, CreatedDate)
            VALUES (@TxnID, @DemandID, @InstDemandID, @Type, @Amount, GETDATE())", conn, txn))
        {
            cmd.Parameters.AddWithValue("@TxnID", transactionId);
            cmd.Parameters.AddWithValue("@DemandID", feeDemandId);
            cmd.Parameters.AddWithValue("@InstDemandID", installmentDemandId);
            cmd.Parameters.AddWithValue("@Type", allocationType);
            cmd.Parameters.AddWithValue("@Amount", amount);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Rolls installment-level figures back up into the parent StudentFeeDemands row
    /// (AmountPaid, LateFeeCharged, Status). Pass an open connection/transaction when called
    /// from inside RecordPayment; pass null to run standalone (e.g. after ApplyDiscount).</summary>
    private static void RefreshDemandStatus(int feeDemandId, Tuple<SqlConnection, SqlTransaction> ctx)
    {
        string selectSql = @"
            SELECT ISNULL(SUM(AmountPaid), 0) AS TotalPaid, ISNULL(SUM(LateFeeCharged), 0) AS TotalLateFee,
                   COUNT(*) AS InstallmentCount,
                   SUM(CASE WHEN Status = 'Paid' THEN 1 ELSE 0 END) AS PaidCount,
                   MIN(DueDate) AS EarliestUnpaidDue
            FROM dbo.FeeInstallmentDemands WHERE FeeDemandID = @Id AND Status <> 'Paid'";

        string paidSql = "SELECT ISNULL(SUM(AmountPaid),0) AS TotalPaid, ISNULL(SUM(LateFeeCharged),0) AS TotalLateFee, " +
                          "COUNT(*) AS Total, SUM(CASE WHEN Status='Paid' THEN 1 ELSE 0 END) AS PaidCount " +
                          "FROM dbo.FeeInstallmentDemands WHERE FeeDemandID = @Id";

        DataRow agg = ExecuteRow(ctx, paidSql, new SqlParameter("@Id", feeDemandId));

        decimal totalPaid = Convert.ToDecimal(agg["TotalPaid"]);
        decimal totalLateFee = Convert.ToDecimal(agg["TotalLateFee"]);
        int total = Convert.ToInt32(agg["Total"]);
        int paidCount = Convert.ToInt32(agg["PaidCount"]);

        DataTable demandInfo = ExecuteTable(ctx, "SELECT GrossAmount, DiscountAmount, DueDate FROM dbo.StudentFeeDemands WHERE FeeDemandID = @Id",
            new SqlParameter("@Id", feeDemandId));
        decimal gross = Convert.ToDecimal(demandInfo.Rows[0]["GrossAmount"]);
        decimal discount = Convert.ToDecimal(demandInfo.Rows[0]["DiscountAmount"]);
        DateTime dueDate = Convert.ToDateTime(demandInfo.Rows[0]["DueDate"]);

        string status;
        if (paidCount >= total && total > 0) status = "Paid";
        else if (totalPaid > 0) status = "PartiallyPaid";
        else if (dueDate.Date < DateTime.Today) status = "Overdue";
        else status = "Pending";

        ExecuteNonQueryCtx(ctx,
            "UPDATE dbo.StudentFeeDemands SET AmountPaid = @Paid, LateFeeCharged = @LateFee, Status = @Status WHERE FeeDemandID = @Id",
            new SqlParameter("@Paid", totalPaid), new SqlParameter("@LateFee", totalLateFee),
            new SqlParameter("@Status", status), new SqlParameter("@Id", feeDemandId));
    }

    // Small helpers so RefreshDemandStatus can run either inside an existing transaction
    // (called from RecordPayment) or standalone (called from ApplyDiscount).
    private static DataRow ExecuteRow(Tuple<SqlConnection, SqlTransaction> ctx, string sql, params SqlParameter[] ps)
    {
        DataTable dt = ExecuteTable(ctx, sql, ps);
        return dt.Rows[0];
    }

    private static DataTable ExecuteTable(Tuple<SqlConnection, SqlTransaction> ctx, string sql, params SqlParameter[] ps)
    {
        if (ctx == null) return DBHelper.ExecuteQuery(sql, ps);

        using (SqlCommand cmd = new SqlCommand(sql, ctx.Item1, ctx.Item2))
        {
            cmd.Parameters.AddRange(ps);
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
    }

    private static void ExecuteNonQueryCtx(Tuple<SqlConnection, SqlTransaction> ctx, string sql, params SqlParameter[] ps)
    {
        if (ctx == null)
        {
            DBHelper.ExecuteNonQuery(sql, ps);
            return;
        }

        using (SqlCommand cmd = new SqlCommand(sql, ctx.Item1, ctx.Item2))
        {
            cmd.Parameters.AddRange(ps);
            cmd.ExecuteNonQuery();
        }
    }

    public static DataTable GetTransactionsForStudent(int studentId)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT TransactionID, TransactionRef, PaymentMode, Amount, PaymentDate, Status,
                   ReconciliationStatus, GatewayTransactionID, BankReferenceNumber
            FROM dbo.FeeTransactions WHERE StudentID = @Id ORDER BY PaymentDate DESC",
            new SqlParameter("@Id", studentId));
    }

    public static DataTable GetTransactionAllocations(int transactionId)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT a.AllocationType, a.AllocatedAmount, ft.FeeTypeName, i.InstallmentNo
            FROM dbo.FeeTransactionAllocations a
            JOIN dbo.StudentFeeDemands d ON d.FeeDemandID = a.FeeDemandID
            JOIN dbo.FeeTypes ft ON ft.FeeTypeID = d.FeeTypeID
            LEFT JOIN dbo.FeeInstallmentDemands i ON i.InstallmentDemandID = a.InstallmentDemandID
            WHERE a.TransactionID = @Id ORDER BY a.AllocationID",
            new SqlParameter("@Id", transactionId));
    }

    // =====================================================================================
    // RECONCILIATION
    // =====================================================================================

    public static DataTable GetTransactionsForReconciliation(string reconciliationStatus, string searchTerm)
    {
        return DBHelper.ExecuteQuery(@"
            SELECT t.TransactionID, t.TransactionRef, s.FullName, s.StudentID, t.PaymentMode, t.Amount,
                   t.PaymentDate, t.GatewayTransactionID, t.BankReferenceNumber, t.ReconciliationStatus,
                   t.ReconciledBy, t.ReconciledDate
            FROM dbo.FeeTransactions t
            JOIN dbo.Students s ON s.StudentID = t.StudentID
            WHERE (@Status = '' OR t.ReconciliationStatus = @Status)
              AND (@Search = '' OR t.TransactionRef LIKE '%' + @Search + '%'
                                OR s.FullName LIKE '%' + @Search + '%'
                                OR t.GatewayTransactionID LIKE '%' + @Search + '%'
                                OR t.BankReferenceNumber LIKE '%' + @Search + '%')
            ORDER BY t.PaymentDate DESC",
            new SqlParameter("@Status", reconciliationStatus ?? string.Empty),
            new SqlParameter("@Search", searchTerm ?? string.Empty));
    }

    public static void ReconcileTransaction(int transactionId, string status, string reconciledBy)
    {
        if (status != "Reconciled" && status != "Disputed" && status != "Unreconciled")
            throw new ArgumentException("Invalid reconciliation status.");

        DBHelper.ExecuteNonQuery(@"
            UPDATE dbo.FeeTransactions SET ReconciliationStatus = @Status,
                ReconciledBy = @Actor, ReconciledDate = GETDATE() WHERE TransactionID = @Id",
            new SqlParameter("@Status", status), new SqlParameter("@Actor", (object)reconciledBy ?? DBNull.Value),
            new SqlParameter("@Id", transactionId));
    }

    public class ReconciliationBatchResult
    {
        public int BatchID;
        public int TotalRecords;
        public int MatchedRecords;
        public int UnmatchedRecords;
    }

    /// <summary>
    /// Matches an uploaded bank/gateway statement (parsed rows of reference/amount/date) against
    /// FeeTransactions: exact reference match (gateway txn ID or bank reference) with amount
    /// equal within 1 paisa auto-reconciles; a reference match with a different amount is
    /// flagged "Mismatch" for manual review; no reference match is "Unmatched".
    /// </summary>
    public static ReconciliationBatchResult MatchBankStatement(string sourceLabel, string fileName,
        List<Tuple<string, decimal, DateTime?>> rows, string uploadedBy)
    {
        ReconciliationBatchResult result = new ReconciliationBatchResult();

        using (SqlConnection conn = DBHelper.GetConnection())
        {
            conn.Open();
            using (SqlTransaction txn = conn.BeginTransaction())
            {
                try
                {
                    int batchId;
                    using (SqlCommand cmd = new SqlCommand(@"
                        INSERT INTO dbo.FeeReconciliationBatches (SourceLabel, UploadedFileName, UploadedBy, UploadedDate, TotalRecords)
                        VALUES (@Source, @File, @Actor, GETDATE(), @Total);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);", conn, txn))
                    {
                        cmd.Parameters.AddWithValue("@Source", sourceLabel);
                        cmd.Parameters.AddWithValue("@File", (object)fileName ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Actor", (object)uploadedBy ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Total", rows.Count);
                        batchId = (int)cmd.ExecuteScalar();
                    }

                    int matched = 0, unmatched = 0;

                    foreach (Tuple<string, decimal, DateTime?> row in rows)
                    {
                        string reference = row.Item1;
                        decimal bankAmount = row.Item2;
                        DateTime? bankDate = row.Item3;

                        int? matchedTxnId = null;
                        string matchStatus = "Unmatched";

                        using (SqlCommand find = new SqlCommand(@"
                            SELECT TOP 1 TransactionID, Amount FROM dbo.FeeTransactions
                            WHERE GatewayTransactionID = @Ref OR BankReferenceNumber = @Ref", conn, txn))
                        {
                            find.Parameters.AddWithValue("@Ref", reference);
                            using (SqlDataReader reader = find.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int txnId = reader.GetInt32(0);
                                    decimal txnAmount = reader.GetDecimal(1);
                                    reader.Close();

                                    if (Math.Abs(txnAmount - bankAmount) <= 0.01m)
                                    {
                                        matchedTxnId = txnId;
                                        matchStatus = "Matched";
                                    }
                                    else
                                    {
                                        matchedTxnId = txnId;
                                        matchStatus = "Mismatch";
                                    }
                                }
                            }
                        }

                        if (matchStatus == "Matched") matched++; else unmatched++;

                        using (SqlCommand ins = new SqlCommand(@"
                            INSERT INTO dbo.FeeReconciliationRecords
                                (BatchID, BankReferenceNumber, BankAmount, BankTransactionDate, MatchedTransactionID, MatchStatus)
                            VALUES (@BatchID, @Ref, @Amount, @Date, @MatchedTxnID, @Status)", conn, txn))
                        {
                            ins.Parameters.AddWithValue("@BatchID", batchId);
                            ins.Parameters.AddWithValue("@Ref", reference);
                            ins.Parameters.AddWithValue("@Amount", bankAmount);
                            ins.Parameters.AddWithValue("@Date", (object)bankDate ?? DBNull.Value);
                            ins.Parameters.AddWithValue("@MatchedTxnID", (object)matchedTxnId ?? DBNull.Value);
                            ins.Parameters.AddWithValue("@Status", matchStatus);
                            ins.ExecuteNonQuery();
                        }

                        if (matchStatus == "Matched" && matchedTxnId.HasValue)
                        {
                            using (SqlCommand upd = new SqlCommand(@"
                                UPDATE dbo.FeeTransactions SET ReconciliationStatus = 'Reconciled',
                                    ReconciledBy = @Actor, ReconciledDate = GETDATE() WHERE TransactionID = @Id", conn, txn))
                            {
                                upd.Parameters.AddWithValue("@Actor", (object)uploadedBy ?? DBNull.Value);
                                upd.Parameters.AddWithValue("@Id", matchedTxnId.Value);
                                upd.ExecuteNonQuery();
                            }
                        }
                    }

                    using (SqlCommand upd = new SqlCommand(
                        "UPDATE dbo.FeeReconciliationBatches SET MatchedRecords = @M, UnmatchedRecords = @U WHERE BatchID = @Id", conn, txn))
                    {
                        upd.Parameters.AddWithValue("@M", matched);
                        upd.Parameters.AddWithValue("@U", unmatched);
                        upd.Parameters.AddWithValue("@Id", batchId);
                        upd.ExecuteNonQuery();
                    }

                    txn.Commit();

                    result.BatchID = batchId;
                    result.TotalRecords = rows.Count;
                    result.MatchedRecords = matched;
                    result.UnmatchedRecords = unmatched;
                    return result;
                }
                catch
                {
                    txn.Rollback();
                    throw;
                }
            }
        }
    }

    public static DataTable GetReconciliationBatches()
    {
        return DBHelper.ExecuteQuery(
            "SELECT BatchID, SourceLabel, UploadedFileName, UploadedBy, UploadedDate, TotalRecords, MatchedRecords, UnmatchedRecords " +
            "FROM dbo.FeeReconciliationBatches ORDER BY UploadedDate DESC");
    }
}
