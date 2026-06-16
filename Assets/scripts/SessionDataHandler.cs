using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

public class SessionDataHandler
{
    public int daysPassed;
    private DataTable sessionTable;
    private string filePath;
    public float[] summaryElapsedTimeDay;
    public string[] summaryDate;
    public string DATEFORMAT = "dd/MM/yyyy";
    //Session file header format
    public string DATEFORMAT_INFILE = "yyyy-MM-dd HH:mm:ss";
    public string DATETIME = "DateTime";
    public string MOVETIME = "MoveTime";
    public string STARTTIME = "StartTime";
    public string STOPTIME = "StopTime";
    public string MECHANISM = "Mechanism";
    public DateTime StartDate;
    public DateTime EndDate;




    public SessionDataHandler(string path)
    {
        filePath = path;
        LoadConfigDates(AppData.Instance.userData.dTableConfig);
        LoadSessionData();

    }
    //session file into dataTable
    private void LoadSessionData()
    {
        sessionTable = new DataTable();
        if (File.Exists(filePath))
        {
            var lines = File.ReadAllLines(filePath);

            int headerLineIndex = -1;
            // Find the first line that does NOT start with ':'
            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].TrimStart().StartsWith(":"))
                {
                    headerLineIndex = i;
                    break;
                }
            }

            // Read headers
            string[] headers = lines[headerLineIndex].Split(',');
            foreach (var header in headers)
            {
                sessionTable.Columns.Add(header.Trim());
            }

            // Read the data rows after the header
            for (int i = headerLineIndex + 1; i < lines.Length; i++)
            {
                string[] rowData = lines[i].Split(',');
                sessionTable.Rows.Add(rowData);
            }
        }
        else
        {
            UnityEngine.Debug.Log("CSV file not found at: " + filePath);
        }
    }
    public void LoadConfigDates(DataTable configTable)
    {
        if (configTable.Rows.Count > 0)
        {
            // var row = configTable.Rows[configTable.Rows.Count - 1];
            var row = configTable.Rows[0];


            StartDate = DateTime.ParseExact(row["StartDate"].ToString(), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            EndDate = DateTime.ParseExact(row["EndDate"].ToString(), "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);

            
        }
    }

 
    public void summaryCalculateMovTimePerDayWithLinq()
    {
        if (sessionTable == null || sessionTable.Rows.Count == 0)
        {
            summaryElapsedTimeDay = new float[0];
            summaryDate = new string[0];
            return;
        }

        // Step 1: Group existing data by date
        var actualData = sessionTable.AsEnumerable()
            .Where(row =>
                !string.IsNullOrWhiteSpace(row.Field<string>(DATETIME)) &&
                !string.IsNullOrWhiteSpace(row.Field<string>(MOVETIME)) &&
                double.TryParse(row[MOVETIME].ToString(), out _))
            .GroupBy(row =>
            {
                return DateTime.ParseExact(row.Field<string>(DATETIME),
                                        DATEFORMAT_INFILE,
                                        CultureInfo.InvariantCulture).Date;
            })
            .ToDictionary(
                g => g.Key,
                g => g.Sum(r => Convert.ToDouble(r[MOVETIME])) / 60.0   // seconds -> minutes
            );

        int totalDays = (EndDate - StartDate).Days + 1;
        summaryDate = new string[totalDays];
        summaryElapsedTimeDay = new float[totalDays];

        for (int i = 0; i < totalDays; i++)
        {
            DateTime current = StartDate.AddDays(i).Date;

            summaryDate[i] = current.ToString(DATEFORMAT);

            summaryElapsedTimeDay[i] = actualData.ContainsKey(current)
                ? (float)actualData[current]
                :0;
        }
    }

    
    public void CalculateMovTimeForMechanism(string mechanism)
    {
        UnityEngine.Debug.Log(mechanism);
        if (sessionTable == null || sessionTable.Rows.Count == 0)
        {
            AppLogger.LogWarning("Session table is null or empty.");
            summaryDate = new string[0];
            summaryElapsedTimeDay = new float[0];
            return;
        }

        var filteredData = sessionTable.AsEnumerable()
            .Where(row =>
                !string.IsNullOrWhiteSpace(row.Field<string>(MECHANISM)) &&
                row.Field<string>(MECHANISM) == mechanism &&
                !string.IsNullOrWhiteSpace(row.Field<string>(DATETIME)) &&
                !string.IsNullOrWhiteSpace(row.Field<string>(MOVETIME)) &&
                double.TryParse(row[MOVETIME].ToString(), out _))
            .Select(row =>
            {
                var date = DateTime.ParseExact(row.Field<string>(DATETIME), DATEFORMAT_INFILE, CultureInfo.InvariantCulture).Date;
                var movTime = Convert.ToDouble(row[MOVETIME]);
                return new { Date = date, MovTime = movTime };
            })
            .GroupBy(entry => entry.Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalMovTime = group.Sum(entry => entry.MovTime) / 60.0 // Convert seconds to minutes
            })
            .OrderBy(result => result.Date)
            .ToList();

        if (filteredData.Count == 0)
        {
            AppLogger.LogWarning($"No valid data found for mechanism: {mechanism}");
            summaryDate = new string[0];
            summaryElapsedTimeDay = new float[0];
            return;
        }

        int len = filteredData.Count;
        summaryDate = new string[len];
        summaryElapsedTimeDay = new float[len];

        for (int i = 0; i < len; i++)
        {
            summaryDate[i] = filteredData[i].Date.ToString(DATEFORMAT);
            summaryElapsedTimeDay[i] = (float)filteredData[i].TotalMovTime;
        }
    }

}
