using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerControler : MonoBehaviour
{
    public GameObject[] spawnItems;
    public int timeToWait;

    private void Awake()
    {
        StartCoroutine("SpawnObject", 0);
    }

    // Start is called before the first frame update
    void Start()
    {
        /*
         GameObject randA = spawnItems[Random.Range(0, spawnItems.Length)];
         Instantiate(randA, gameObject.transform);
         */
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.childCount == 0)
        {
            StartCoroutine("SpawnObject", timeToWait);
        }
    }

    public IEnumerator SpawnObject(int timeToWait)
    {
        yield return new WaitForSeconds(timeToWait);

        GameObject randA = spawnItems[Random.Range(0, spawnItems.Length)];
        Instantiate(randA, gameObject.transform);
        StopCoroutine("SpawnObject");
    }
}
