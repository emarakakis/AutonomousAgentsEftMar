using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class AgentController : Agent
{
    [SerializeField] Transform _target;
    [SerializeField] GameObject _bomb;
    [SerializeField] private float _moveSpeed;
    [SerializeField] private int bombCount;
    private List<GameObject> _bombList = new List<GameObject>();


    private Rigidbody2D _rb;

    public GameObject food;
    public List<GameObject> spawnedPelletsList = new List<GameObject>();
    [SerializeField] private int pelletCount;

    //Environment Variables
    [SerializeField] private Transform environmentLocation;
    Material envMaterial;
    public GameObject env;

    //Time keeping variables
    [SerializeField] private int timeForEpisode;
    private float timeLeft;


    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody2D>();
        envMaterial = env.GetComponent<Renderer>().material;
    }

    public override void OnEpisodeBegin()
    {

        CreatePellet();
        transform.localPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
        EpisodeTimerNew();
    }

    private void Update()
    {
        CheckRemainingTime();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(_target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveRotate = actions.ContinuousActions[0];
        float moveForward = actions.ContinuousActions[1];


        _rb.MovePosition(transform.position + transform.up * moveForward * _moveSpeed * Time.deltaTime);
        transform.Rotate(0f, 0f, -moveRotate * _moveSpeed, Space.Self);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }

    private bool CheckOverlap(Vector3 objectAvoidOverlap, Vector3 alreadyExistingObj, float minDistanceWanted)
    {
        float DistanceBetweenObjecs = Vector3.Distance(objectAvoidOverlap, alreadyExistingObj);
        if (minDistanceWanted <= DistanceBetweenObjecs)
        {
            return true;
        }
        return false;

    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Pellet")
        {

            spawnedPelletsList.Remove(other.gameObject);
            Destroy(other.gameObject);
            AddReward(10f);
            if (spawnedPelletsList.Count == 0)
            {
                envMaterial.color = Color.green;
                AddReward(5f);
                RemovePellet(spawnedPelletsList);
                EndEpisode();
            }
        }

        if (other.gameObject.tag == "Wall")
        {
            envMaterial.color = Color.yellow;
            AddReward(-15f);
            RemovePellet(spawnedPelletsList);
            RemovePellet(_bombList);
            EndEpisode();
        }

        if (other.gameObject.tag == "Bomb")
        {
            envMaterial.color = Color.magenta;
            AddReward(-15f);
            RemovePellet(spawnedPelletsList);
            RemovePellet(_bombList);
            EndEpisode();
        }
    }

    private void CreatePellet()
    {

        if (spawnedPelletsList.Count != 0)
        {
            RemovePellet(spawnedPelletsList);
        }

        if (_bombList.Count != 0)
        {
            RemovePellet(_bombList);
        }

        for (int i = 0; i < pelletCount; i++)
        {
            int counter = 0;
            bool distanceGood;
            bool alreadyDecremented = false;
            //Spawning
            GameObject newPellet = Instantiate(food);
            newPellet.transform.parent = environmentLocation;
            Vector3 pelletLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);

            if (spawnedPelletsList.Count != 0)
            {
                for (int k = 0; k < spawnedPelletsList.Count; k++)
                {
                    if (counter < 10)
                    {
                        distanceGood = CheckOverlap(pelletLocation, spawnedPelletsList[k].transform.localPosition, 5f);
                        if (distanceGood == false)
                        {
                            pelletLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                            k--;
                            Debug.Log("Too close to other Pellet!");
                            alreadyDecremented = true;
                        }

                        distanceGood = CheckOverlap(pelletLocation, transform.localPosition, 5f);
                        if (distanceGood == false)
                        {
                            pelletLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                            if (alreadyDecremented == false)
                            {
                                k--;
                            }
                            Debug.Log("Too close to Agent!");
                        }
                        counter++;
                    }
                    else
                    {
                        k = spawnedPelletsList.Count;
                    }
                }
            }
            newPellet.transform.localPosition = pelletLocation;
            spawnedPelletsList.Add(newPellet);
        }


        for (int i = 0; i < bombCount; i++)
        {
            bool distanceGood;
            bool alreadyDecremented = false;
            GameObject newBomb = Instantiate(_bomb);
            newBomb.transform.parent = environmentLocation;
            Vector3 bombLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
            for (int k = 0; k < spawnedPelletsList.Count; k++)
            {
                distanceGood = CheckOverlap(bombLocation, spawnedPelletsList[k].transform.localPosition, 5f);
                if (distanceGood == false)
                {
                    bombLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                    k--;
                    Debug.Log("Too close to other Pellet!");
                    alreadyDecremented = true;
                }

                distanceGood = CheckOverlap(bombLocation, transform.localPosition, 5f);
                if (distanceGood == false)
                {
                    bombLocation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
                    if (alreadyDecremented == false)
                    {
                        k--;
                    }
                    Debug.Log("Too close to Agent!");
                }
            }
            _bombList.Add(newBomb);
            newBomb.transform.localPosition = bombLocation;
        }
    }

    private void RemovePellet(List<GameObject> toBeDeletedGameObjectList)
    {
        foreach (GameObject i in toBeDeletedGameObjectList)
        {
            Destroy(i.gameObject);
        }
        toBeDeletedGameObjectList.Clear();
    }

    private void EpisodeTimerNew()
    {
        timeLeft = Time.time + timeForEpisode;
    }

    private void CheckRemainingTime()
    {
        if (Time.time >= timeLeft)
        {
            envMaterial.color = Color.blue;
            AddReward(-15f);
            RemovePellet(spawnedPelletsList);
            EndEpisode();
        }
    }
}
