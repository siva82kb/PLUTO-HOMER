using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GamelogHT : MonoBehaviour
{
    public static GameLog instance;
    GameObject Player, Target, Target1;
    public static string dateTime;
    public static string date;
    public static string sessionNum;

    string fileName;
    float time;

    void Start()
    {
        ResetGameData();
        InitializeSessionDetails();
        CreateLogFile();
        gameData.StartDataLog(fileName);
    }

    private void ResetGameData()
    {
        if (gameData.gameScore != 0)
        {
            gameData.gameScore = 0;
            //  gameData.events = 0;
        }
    }

    private void InitializeSessionDetails()
    {
        dateTime = DateTime.Now.ToString("Dyyyy-MM-ddTHH-mm-ss");
        date = DateTime.Now.ToString("yyyy-MM-dd");
        sessionNum = "Session" + AppData.currentSessionNumber;
    }

    private void CreateLogFile()
    {
        string dir = Path.Combine(DataManager.directoryPathSession, date, sessionNum);
        Directory.CreateDirectory(dir);

        fileName = Path.Combine(dir, $"{AppData.selectedMechanism}_{AppData.selectedGame}_{dateTime}.csv");
        AppData.trialDataFileLocation = fileName;
        AppData.trialDataFileLocation1= Path.Combine(dir, $"{AppData.selectedMechanism}_{AppData.selectedGame}_{dateTime}");
        Debug.Log(fileName);
        File.Create(fileName).Dispose();
    }
    void Update()
    {
        if (gameData.isGameLogging)
        {
            Player = GameObject.FindGameObjectWithTag("Player");

            if (Player != null)
            {
                gameData.playerPos = Player.transform.position.x.ToString();
                //Debug.Log("gameData:" + gameData.playerPos);
            }
            else
            { int x = 0;
                gameData.playerPos = x.ToString();
               // Debug.Log("gameData:"+ gameData.playerPos); 
            }
            gameData.LogDataHT();
        }
        time += Time.deltaTime;
    }

    public void OnDestroy()
    {
        gameData.StopLogging();
    }
}


