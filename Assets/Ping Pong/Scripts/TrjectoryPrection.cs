using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrjectoryPrection : MonoBehaviour
{
    // Start is called before the first frame update

    private Scene _simulationScene;
    private PhysicsScene2D _physicsScene;
    [SerializeField] private Transform[] _obstaclesParent;

    void Start()
    {
        createPhysicsScene();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void createPhysicsScene()
    {
        _simulationScene = SceneManager.CreateScene("Simulation", new CreateSceneParameters(LocalPhysicsMode.Physics2D));
        _physicsScene = _simulationScene.GetPhysicsScene2D();

        foreach (Transform obj in _obstaclesParent)
        {
            Debug.Log("hello");
            var ghostObj = Instantiate(obj.gameObject, obj.position, obj.rotation);
            if (ghostObj.GetComponent<Renderer>())
            {
                ghostObj.GetComponent<Renderer>().enabled = false;
            }


            SceneManager.MoveGameObjectToScene(ghostObj, _simulationScene);
        }
    }


    [SerializeField] private LineRenderer _line;
    [SerializeField] private int _maxPhysicsFrameIterations;
    public void SimulateTrajectory(Vector2 pos, Vector2 velocity)
    {
        Debug.Log("simulating");
        GameObject ball = GameObject.FindGameObjectWithTag("Target");
        Debug.Log(ball.GetComponent<Rigidbody2D>().velocity);
        var ghostObj = Instantiate(ball, pos, Quaternion.identity);
        ghostObj.tag = "";
        ghostObj.GetComponent<Rigidbody2D>().velocity = ball.GetComponent<Rigidbody2D>().velocity;


        if (ghostObj.GetComponent<Renderer>())
        {
            ghostObj.GetComponent<Renderer>().enabled = false;
        }
        SceneManager.MoveGameObjectToScene(ghostObj.gameObject, _simulationScene);

        _line.positionCount = _maxPhysicsFrameIterations;

        for (int i = 0; i < _maxPhysicsFrameIterations; i++)
        {
            _physicsScene.Simulate(Time.fixedDeltaTime);
            _line.SetPosition(i, ghostObj.transform.position);
        }
        Destroy(ghostObj);


    }


}
