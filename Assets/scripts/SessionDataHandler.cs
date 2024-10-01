using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class SessionDataHandler
{
    private DataTable sessionTable;
    private string filePath;

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
