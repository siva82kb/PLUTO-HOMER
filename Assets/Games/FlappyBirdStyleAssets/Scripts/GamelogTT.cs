using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class GamelogTT : MonoBehaviour
{
    public static GameLog instance;
    GameObject Player, Target;
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
            gameData.events = 0;
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

        File.Create(fileName).Dispose();
    }
    void Update()
    {
        if (gameData.isGameLogging)
        {
            Player = GameObject.FindGameObjectWithTag("Player");
            Target = GameObject.FindGameObjectWithTag("Target");
            //if (gameData.game == "COMPENSATION")
            //{
            //    gameData.playerPos = Player.transform.eulerAngles.z.ToString();
            //}
            //else
            //{
            //    if (Player != null)
            //    {
            //        gameData.playerPos = Player.transform.position.y.ToString();
            //        Debug.Log("PlayerPos");
            //    }
            //    else
            //        gameData.playerPos = "\"" + "XXX" + "," + "XXX" + "\"";

            //}
            if (Player != null)
            {
                gameData.playerPos = Player.transform.position.y.ToString();
            }
            else
                gameData.playerPos = "\"" + "XXX" + "," + "XXX" + "\"";

            gameData.LogDataHT();
        }
        time += Time.deltaTime;
    }

    public void SaveData(){
       // gameData.StopLogging();
        }
    public void OnDestroy()
    {
        gameData.StopLogging();
    }
}
