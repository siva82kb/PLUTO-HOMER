using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SessionDataHandler
{
    DateTime startDate;
    public int daysPassed;
    private DataTable sessionTable;
    private string filePath;
    public float[] elapsedTimeDay = new float[] { 0, 0, 0, 0, 0, 0, 0 };
    public string[] day = new String[] { "", "", "", "", "", "", "" };
    public string[] date = new String[] { "", "", "", "", "", "", "" };
    private float[] summaryElapsedTimeDay;
    private string[] summaryDate;

    public SessionDataHandler(string path)
    {
        filePath = path;
        LoadSessionData();
    }

    private void LoadSessionData()
    {
        sessionTable = new DataTable();
        if (File.Exists(filePath))
        {
            var lines = File.ReadAllLines(filePath);

            string[] headers = lines[0].Split(',');
            foreach (var header in headers)
            {
                sessionTable.Columns.Add(header.Trim());
            }

            for (int i = 1; i < lines.Length; i++)
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
    public void CalculateMovTimePerDayWithLinq()
    {

        DateTime maxDate = sessionTable.AsEnumerable()
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
            .Max();
        DateTime sevenDaysAgo = maxDate.AddDays(-7);
        var movTimePerDay = sessionTable.AsEnumerable()
             .Where(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date >= sevenDaysAgo) // Filter the last 7 days
            .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date) // Group by date only
            .Select(group => new
            {
                Date = group.Key,      // Format date as "yyyy-MM-dd"
                DayOfWeek = group.Key.DayOfWeek,   // Get the day of the week
                TotalMovTime = group.Sum(row => Convert.ToInt32(row["movTime"]))
            }).OrderByDescending(item => item.Date) // Order by date descending
            .ToList();

        

        for (int i = 0; i < movTimePerDay.Count; i++)
        {
            elapsedTimeDay[i] = movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
            day[i] = GetAbbreviatedDayName(movTimePerDay[i].DayOfWeek);
            date[i] = movTimePerDay[i].Date.ToString("dd/MM");
            //Debug.Log(elapsedTimeDay[i]);
        }

    }
    public void summaryCalculateMovTimePerDayWithLinq()
    {

        DateTime maxDate = sessionTable.AsEnumerable()
            .Select(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture))
            .Max();
        DateTime sevenDaysAgo = maxDate.AddDays(-7);
        var movTimePerDay = sessionTable.AsEnumerable()
           
            .GroupBy(row => DateTime.ParseExact(row.Field<string>("DateTime"), "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture).Date) // Group by date only
            .Select(group => new
            {
                Date = group.Key,      // Format date as "yyyy-MM-dd"
                DayOfWeek = group.Key.DayOfWeek,   // Get the day of the week
                TotalMovTime = group.Sum(row => Convert.ToInt32(row["movTime"]))
            }).OrderByDescending(item => item.Date) // Order by date descending
            .ToList();

        summaryElapsedTimeDay = new float[movTimePerDay.Count];
       
        summaryDate = new string[movTimePerDay.Count];

        for (int i = 0; i < movTimePerDay.Count; i++)
        {
            summaryElapsedTimeDay[i]= movTimePerDay[i].TotalMovTime / 60f; // Convert seconds to minutes
            day[i] = GetAbbreviatedDayName(movTimePerDay[i].DayOfWeek);
            summaryDate[i] = movTimePerDay[i].Date.ToString("dd/MM");
            //Debug.Log(elapsedTimeDay[i]);
        }

    }

    private string GetAbbreviatedDayName(DayOfWeek dayOfWeek)
    {

        string[] abbreviatedDayNames = CultureInfo.CurrentCulture.DateTimeFormat.AbbreviatedDayNames;

        return abbreviatedDayNames[(int)dayOfWeek];
    }
    public int calculateDaypassed()
    {

        TimeSpan duration = DateTime.Now - startDate;
        daysPassed = (int)duration.TotalDays;
        return daysPassed;
    }

    public Dictionary<string, double> CalculateTotalTimeForMechanisms(DateTime currentDate)
    {

        var mechanismTime = new Dictionary<string, double>();
        var rowsForCurrentDate = sessionTable.AsEnumerable()
            .Where(row => DateTime.TryParse(row["DateTime"].ToString(), out DateTime rowDate) && rowDate.Date == currentDate.Date);

        foreach (DataRow row in rowsForCurrentDate)
        {
            string startTimeStr = row["StartTime"].ToString();
            string stopTimeStr = row["StopTime"].ToString();
            string mechanism = row["mech"].ToString();
            if (DateTime.TryParse(startTimeStr, out DateTime startTime) && DateTime.TryParse(stopTimeStr, out DateTime stopTime))
            {
                double sessionMinutes = (stopTime - startTime).TotalMinutes;

                if (mechanismTime.ContainsKey(mechanism))
                {
                    mechanismTime[mechanism] += sessionMinutes;
                }
                else
                {
                    mechanismTime[mechanism] = sessionMinutes;
                }
            }
        }

        return mechanismTime;  // Return the dictionary containing total time per mechanism
    }
}
