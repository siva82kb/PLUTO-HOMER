using System;
using System.Data;
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
    public string DATEFORMAT = "dd/MM";
    //Session file header format
    public string DATEFORMAT_INFILE = "yyyy-MM-dd HH:mm:ss";
    public string DATETIME = "DateTime";
    public string MOVETIME = "MoveTime";
    public string STARTTIME = "StartTime";
    public string STOPTIME = "StopTime";
    public string MECHANISM = "Mechanism";

    public SessionDataHandler(string path)
    {
        filePath = path;
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

 
    public void summaryCalculateMovTimePerDayWithLinq()
    {
        // Group by date and calculate the total movement time for each day
        var movTimePerDay = sessionTable.AsEnumerable()
            .GroupBy(row => DateTime.ParseExact(row.Field<string>(DATETIME), DATEFORMAT_INFILE, CultureInfo.InvariantCulture).Date) // Group by date only
            .Select(group => new
            {
                Date = group.Key,
                DayOfWeek = group.Key.DayOfWeek,   
                TotalMovTime = group.Count() * 60
            })
            .ToList();

        summaryElapsedTimeDay = new float[movTimePerDay.Count];
        summaryDate = new string[movTimePerDay.Count];
       
        for (int i = 0; i < movTimePerDay.Count; i++)
        {
            summaryElapsedTimeDay[i] = movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
           
            summaryDate[i] = movTimePerDay[i].Date.ToString(DATEFORMAT);       // Format date as "dd/MM"
           
        }
    }

    
    public void CalculateMovTimeForMechanism(string mechanism)
    {
        // Filter session data for the specified mechanism
        var filteredData = sessionTable.AsEnumerable()
            .Where(row => row.Field<string>(MECHANISM) == mechanism)
            .Select(row => new
            {
                Date = DateTime.ParseExact(row.Field<string>(DATETIME), DATEFORMAT_INFILE , CultureInfo.InvariantCulture).Date
            })
            .GroupBy(entry => entry.Date)
            .Select(group => new
            {
                Date = group.Key,
                TotalMovTime = group.Count() 
            })
            .OrderBy(result => result.Date)
            .ToList();
        int len = filteredData.Count;
        summaryDate = new string[len];
        summaryElapsedTimeDay = new float[len];

        for (int i = 0; i < len; i++)
        {
            summaryDate[i] = filteredData[i].Date.ToString(DATEFORMAT); 
            summaryElapsedTimeDay[i] = (float)filteredData[i].TotalMovTime; // Store movement time in minutes
          
        }
    }
}
