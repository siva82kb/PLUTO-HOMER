using System;
using System.IO;
using UnityEngine;

public class GamelogHT : MonoBehaviour
{
    public static GameLog instance;
    GameObject Player;
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
        sessionNum = "Session" + AppData.Instance.currentSessionNumber;
    }

    private void CreateLogFile()
    {
        string dir = Path.Combine(DataManager.sessionPath, date, sessionNum);
        Directory.CreateDirectory(dir);

        fileName = Path.Combine(dir, $"{AppData.Instance.selectedMechanism.name}_{AppData.Instance.selectedGame}_{dateTime}.csv");
        AppData.Instance.trialDataFileLocation = fileName;
        AppData.Instance.trialDataFileLocation1 = Path.Combine(dir, $"{AppData.Instance.selectedMechanism.name}_{AppData.Instance.selectedGame}_{dateTime}");
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


