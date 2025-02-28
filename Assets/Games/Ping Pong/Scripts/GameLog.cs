using UnityEngine;
using System;
using System.IO;

public class GameLog : MonoBehaviour
{
    public static GameLog instance;
    GameObject Player, Target, Enemy;
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
        if (gameData.playerScore != 0 || gameData.enemyScore != 0)
        {
            gameData.playerScore = 0;
            gameData.enemyScore = 0;
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
            Enemy = GameObject.FindGameObjectWithTag("Enemy");
            if (gameData.game == "COMPENSATION")
            {
                gameData.playerPos = Player.transform.eulerAngles.z.ToString();
                gameData.TargetPos = Target.transform.eulerAngles.z.ToString();
                gameData.enemyPos = Enemy.transform.eulerAngles.z.ToString();
            }
            else
            {
                if (Player != null)
                    gameData.playerPos = Player.transform.position.y.ToString();
                else
                    gameData.playerPos = "\"" + "XXX" + "," + "XXX" + "\"";

                if (Target != null)
                    gameData.TargetPos = "\"" + Target.transform.position.x.ToString() + "," + Target.transform.position.y.ToString() + "\"";
                else
                    gameData.TargetPos = "\"" + "XXX" + "," + "XXX" + "\"";
                if (Enemy != null)
                    gameData.enemyPos = Enemy.transform.position.y.ToString();
                else
                    gameData.enemyPos = "\"" + "XXX" + "," + "XXX" + "\"";
            }

            gameData.LogData();
        }
        time += Time.deltaTime;
    }
    
    public void OnDestroy()
    {
        gameData.StopLogging();
    }
}

