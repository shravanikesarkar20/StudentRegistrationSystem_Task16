using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;

/// <summary>
/// Lightweight, dependency-free reader for the "Import from File" feature on the
/// Bulk Registration page. Supports plain CSV and modern Excel (.xlsx) workbooks.
///
/// XLSX files are just ZIP archives containing XML, so this reads the first
/// worksheet directly using System.IO.Compression + System.Xml.Linq — no third-party
/// NuGet package (EPPlus/ExcelDataReader/etc.) needs to be installed to build this
/// project. It intentionally supports the common case (a flat header row + data rows,
/// text/number cells) rather than every Excel feature (formulas, multiple sheets,
/// merged cells are not specially handled).
/// </summary>
public static class FileImportHelper
{
    /// <summary>Reads an uploaded .csv or .xlsx file into a DataTable using the first row as headers.</summary>
    public static DataTable ReadFile(Stream fileStream, string fileName)
    {
        string ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();

        if (ext == ".csv")
        {
            return ParseCsv(fileStream);
        }
        if (ext == ".xlsx")
        {
            return ParseXlsx(fileStream);
        }

        throw new NotSupportedException("Unsupported file type \"" + ext + "\". Please upload a .csv or .xlsx file.");
    }

    #region ---- CSV ----

    public static DataTable ParseCsv(Stream stream)
    {
        DataTable dt = new DataTable();

        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
        {
            string headerLine = reader.ReadLine();
            if (headerLine == null) return dt;

            AddColumns(dt, SplitCsvLine(headerLine));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                List<string> values = SplitCsvLine(line);
                DataRow row = dt.NewRow();
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    row[i] = i < values.Count ? values[i] : "";
                }
                dt.Rows.Add(row);
            }
        }

        return dt;
    }

    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    #endregion

    #region ---- XLSX ----

    public static DataTable ParseXlsx(Stream stream)
    {
        DataTable dt = new DataTable();

        // ZipArchive needs a seekable stream; the uploaded HttpPostedFile stream is
        // seekable, but copy defensively in case a non-seekable stream is ever passed in.
        Stream seekableStream = stream;
        MemoryStream bufferedCopy = null;
        if (!stream.CanSeek)
        {
            bufferedCopy = new MemoryStream();
            stream.CopyTo(bufferedCopy);
            bufferedCopy.Position = 0;
            seekableStream = bufferedCopy;
        }

        try
        {
            using (ZipArchive archive = new ZipArchive(seekableStream, ZipArchiveMode.Read, true))
            {
                List<string> sharedStrings = ReadSharedStrings(archive);
                List<List<string>> rows = ReadFirstWorksheetRows(archive, sharedStrings);

                if (rows.Count == 0) return dt;

                AddColumns(dt, rows[0]);

                for (int r = 1; r < rows.Count; r++)
                {
                    List<string> rowValues = rows[r];
                    // Skip fully blank trailing rows some spreadsheet tools emit.
                    if (rowValues.All(string.IsNullOrWhiteSpace)) continue;

                    DataRow newRow = dt.NewRow();
                    for (int c = 0; c < dt.Columns.Count; c++)
                    {
                        newRow[c] = c < rowValues.Count ? rowValues[c] : "";
                    }
                    dt.Rows.Add(newRow);
                }
            }
        }
        finally
        {
            if (bufferedCopy != null) bufferedCopy.Dispose();
        }

        return dt;
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        List<string> sharedStrings = new List<string>();
        ZipArchiveEntry entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return sharedStrings;

        using (Stream s = entry.Open())
        {
            XDocument xdoc = XDocument.Load(s);
            XNamespace ns = xdoc.Root.Name.Namespace;
            foreach (XElement si in xdoc.Root.Elements(ns + "si"))
            {
                string text = string.Concat(si.Descendants(ns + "t").Select(t => t.Value));
                sharedStrings.Add(text);
            }
        }
        return sharedStrings;
    }

    private static List<List<string>> ReadFirstWorksheetRows(ZipArchive archive, List<string> sharedStrings)
    {
        List<List<string>> rows = new List<List<string>>();

        ZipArchiveEntry sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
            ?? archive.Entries.FirstOrDefault(en => en.FullName.StartsWith("xl/worksheets/") && en.FullName.EndsWith(".xml"));

        if (sheetEntry == null)
        {
            throw new InvalidDataException("This Excel file doesn't contain a readable worksheet.");
        }

        using (Stream s = sheetEntry.Open())
        {
            XDocument xdoc = XDocument.Load(s);
            XNamespace ns = xdoc.Root.Name.Namespace;
            XElement sheetData = xdoc.Root.Element(ns + "sheetData");
            if (sheetData == null) return rows;

            foreach (XElement rowEl in sheetData.Elements(ns + "row"))
            {
                Dictionary<int, string> rowValues = new Dictionary<int, string>();
                int maxCol = -1;

                foreach (XElement c in rowEl.Elements(ns + "c"))
                {
                    string cellRef = (string)c.Attribute("r");
                    int colIndex = string.IsNullOrEmpty(cellRef) ? (maxCol + 1) : CellRefToColumnIndex(cellRef);
                    string type = (string)c.Attribute("t");
                    XElement vEl = c.Element(ns + "v");
                    string value = "";

                    if (type == "s" && vEl != null)
                    {
                        int idx;
                        if (int.TryParse(vEl.Value, out idx) && idx >= 0 && idx < sharedStrings.Count)
                        {
                            value = sharedStrings[idx];
                        }
                    }
                    else if (type == "inlineStr")
                    {
                        XElement isEl = c.Element(ns + "is");
                        value = isEl != null ? string.Concat(isEl.Descendants(ns + "t").Select(t => t.Value)) : "";
                    }
                    else if (vEl != null)
                    {
                        value = vEl.Value;
                    }

                    rowValues[colIndex] = value;
                    if (colIndex > maxCol) maxCol = colIndex;
                }

                List<string> list = new List<string>();
                for (int i = 0; i <= maxCol; i++)
                {
                    list.Add(rowValues.ContainsKey(i) ? rowValues[i] : "");
                }
                rows.Add(list);
            }
        }

        return rows;
    }

    /// <summary>Converts a cell reference like "C7" into a 0-based column index (2).</summary>
    private static int CellRefToColumnIndex(string cellRef)
    {
        int col = 0;
        foreach (char ch in cellRef)
        {
            if (char.IsLetter(ch))
            {
                col = col * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            else break;
        }
        return col - 1;
    }

    #endregion

    #region ---- Shared ----

    private static void AddColumns(DataTable dt, List<string> headerCells)
    {
        foreach (string h in headerCells)
        {
            string colName = string.IsNullOrWhiteSpace(h) ? ("Column" + (dt.Columns.Count + 1)) : h.Trim();
            string finalName = colName;
            int suffix = 1;
            while (dt.Columns.Contains(finalName))
            {
                finalName = colName + "_" + suffix++;
            }
            dt.Columns.Add(finalName, typeof(string));
        }
    }

    #endregion
}
