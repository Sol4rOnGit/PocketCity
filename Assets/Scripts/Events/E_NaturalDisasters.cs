using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EventManager : MonoBehaviour
{
    [Header("Natural Disasters")]

    [Header("Earthquake")]
    [SerializeField] private AudioClip earthquakeAudioClip;

    [Header("Fire")]
    private readonly float secondsToBurnBuilding = 10f;

    [Header("Health Statistics")]
    private int infectedPopulation;
    public bool isLockdownActive = false;

    [Header("Flood")]
    [SerializeField] private GameObject floodPlanePrefab;
    public bool isFlooded;

    //Natural disasters

    private float minRatio = 0.01f; //1%
    private float maxRatio = 0.05f; //max 5%

    //Earthquake
    private void TriggerEarthquake()
    {
        if (gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        gameManager.UserNotification?.Invoke("Earthquake!", true);

        //Update ratios
        maxRatio = Mathf.Lerp(minRatio, maxRatio, gameManager.daysPassed / 300f);
        float ratio = Random.Range(maxRatio/4, maxRatio);

        int numBuildingsToDestroy = Mathf.CeilToInt(ratio * gridManager.BuildingPositions.Count);

        StartCoroutine(EarthquakeCoroutine(numBuildingsToDestroy));
        GameAudioManager.instance.globalAudioSource.PlayOneShot(earthquakeAudioClip);
        gameManager.disastersSurvived++;
    }

    private IEnumerator EarthquakeCoroutine(int numBuildingsToDestroy)
    {
        int destroyedBuildings = 0;
        float gapBetweenDestructionSeconds = 0.2f;

        while (destroyedBuildings < numBuildingsToDestroy)
        {
            if (gridManager.BuildingPositions.Count == 0) break;

            int randomInt = Random.Range(0, gridManager.BuildingPositions.Count);
            Vector2Int buildingPos = gridManager.BuildingPositions[randomInt];

            destroyedBuildings++;

            var mapGrid = gridManager.GetMapGrid();
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile gridTile))
            {
                if (gridTile.buildingScript == null) continue;

                if (gridTile.buildingScript.isRetrofitted)
                {
                    Debug.Log("Retrofitted! Saved from earthquake");
                    continue;
                }
            }

            gridManager.forceRemoveElement(buildingPos);

            yield return new WaitForSeconds(gapBetweenDestructionSeconds);
        }
        yield break;
    }

    //Fire
    private void TriggerBuildingOnFire()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }
        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

        if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
        {
            if (!tile.buildingScript.isOnFire)
            {
                tile.buildingScript.IgniteFire();

                //Timer
                StartCoroutine(BurnBuilding(randomPos, tile.buildingScript));

                //Call fire services
                if (ServiceManager.instance != null)
                {
                    ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
                }
                else { Debug.LogError("Service Manager not found!"); }
            }
        }
    }

    public void CheckForFires()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        var mapGrid = gridManager.GetMapGrid();

        foreach (Vector2Int buildingPos in gridManager.BuildingPositions) //Optimistaion -> hashmap for infected buildings and houses auto append? O(1)?
        {
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript)
            {
                if (!tile.buildingScript.isOnFire) continue;
                if (tile.buildingScript.isSpreadingFire) continue;

                tile.buildingScript.isSpreadingFire = true;

                StartCoroutine(SpreadFire(buildingPos, mapGrid));

                //Send fire truck if one not already on the way
                if (!tile.buildingScript.isFiretruckOnRoute)
                {
                    if (ServiceManager.instance != null)
                    {
                        ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
                    }
                    else { Debug.LogError("Service Manager not found!"); }
                }
            }
        }

        return;
    }

    public IEnumerator BurnBuilding(Vector2Int pos, Building buildingScript)
    {
        yield return new WaitForSeconds(secondsToBurnBuilding);

        if (buildingScript != null && buildingScript.isOnFire)
        {
            gameManager.UserNotification?.Invoke($"{pos.x}, {pos.y} burned down!", true);
            gridManager.forceRemoveElement(pos);
        }
    }

    private IEnumerator SpreadFire(Vector2Int pos, Dictionary<Vector2Int, GridManager.GridTile> mapGrid)
    {
        yield return new WaitForSeconds(Random.Range(3f, 9f));

        //Return if no longer on fire/existant
        if (!mapGrid.TryGetValue(pos, out GridManager.GridTile sourceTile) || sourceTile.buildingScript == null) yield break;

        if (!sourceTile.buildingScript.isOnFire)
        {
            sourceTile.buildingScript.isSpreadingFire = false;
            yield break;
        }

        List<GridManager.GridTile> validTargets = new List<GridManager.GridTile>();

        foreach (Vector2Int dir in gameManager.directions)
        {
            Vector2Int checkPos = pos + dir;
            if (mapGrid.TryGetValue(checkPos, out GridManager.GridTile tile) && tile.buildingScript != null && !tile.buildingScript.isOnFire)
            {
                validTargets.Add(tile);
            }
        }

        if (validTargets.Count > 0)
        {
            if (Random.value > 0.3f)
            {
                sourceTile.buildingScript.isSpreadingFire = false;
                yield break;
            }

            int randomIndex = Random.Range(0, validTargets.Count);
            GridManager.GridTile tile = validTargets[randomIndex];

            tile.buildingScript.IgniteFire();

            //Call services

            if (ServiceManager.instance != null)
            {
                ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
            }
            else { Debug.LogError("Service Manager not found!"); }

            StartCoroutine(BurnBuilding(tile.buildingScript.gridPos, tile.buildingScript));

            sourceTile.buildingScript.isSpreadingFire = false;

            yield break; //only one building!
        }

        sourceTile.buildingScript.isSpreadingFire = false;
    }

    //HEALTH
    private void TriggerVirusOutbreak(bool newVirus = true)
    {
        if (gridManager.BuildingPositions.Count == 0 || GameManager.instance.isImmuneToViruses) return;

        bool foundInfected = false;
        int tries = 0;

        while (!foundInfected && tries < (newVirus ? 5 : 50))
        {
            Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
            Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

            if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
            {
                if (tile.buildingScript is House houseScript && !houseScript.isInfected)
                {
                    foundInfected = true;

                    houseScript.Infect();

                    infectedPopulation += houseScript.residents;
                    if (newVirus) { gameManager.UserNotification?.Invoke("A virus outbreak has occured!", true); }
                    else { gameManager.UserNotification?.Invoke("Another house has been infected!", true); }

                    //Timer
                    StartCoroutine(KillBuilding(randomPos, houseScript));

                    //Call ambulances
                    if (ServiceManager.instance != null)
                    {
                        bool served = ServiceManager.instance.DispatchAmbulance(tile.buildingScript);
                        if (served) houseScript.isAmbulanceOnRoute = true;
                    }
                    else { Debug.LogError("Service Manager not found!"); }
                }
            }
        }
    }
    private IEnumerator KillBuilding(Vector2Int randomPos, House houseScript)
    {
        yield return new WaitForSeconds(10f);

        if (houseScript == null || !houseScript.isInfected || GameManager.instance.isImmuneToViruses) yield break;

        int spreadNum = (houseScript.residents < 2) ? 2 : Random.Range(1, 5);

        for (int i = 1; i < spreadNum; i++)
        {
            yield return new WaitForSeconds(0.1f);
            TriggerVirusOutbreak(false); //Spread!
        }

        gridManager.forceRemoveElement(randomPos);
        gameManager.UserNotification?.Invoke("A building has been destroyed as it has been overriden with viruses!", true);
    }

    public void CheckForInfections()
    {
        if (gridManager.BuildingPositions.Count == 0 || GameManager.instance.isImmuneToViruses) return;

        var serviceManager = ServiceManager.instance;
        if (serviceManager == null)
        {
            Debug.LogError("Service manager not found!");
            return;
        }

        var mapGrid = gridManager.GetMapGrid();

        foreach (Vector2Int buildingPos in gridManager.BuildingPositions) //Optimistaion -> hashmap for infected buildings and houses auto append? O(1)?
        {
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript is House houseScript)
            {
                if (!houseScript.isInfected) continue;

                if (houseScript.isAmbulanceOnRoute) continue;

                bool served = serviceManager.DispatchAmbulance(houseScript);
                if (served) houseScript.isAmbulanceOnRoute = true;
            }
        }

        return;
    }

    //Flooding  

    public void TriggerFlood()
    {
        GameObject floodObj = Instantiate(floodPlanePrefab, new Vector3(0, 0.1f, 0), Quaternion.identity, transform);
        float seed = Random.Range(0, 1);
        float floodTime = Mathf.Lerp(3, 2, Mathf.Clamp01(GameManager.instance.daysPassed/200)) + seed;

        var mapGrid = GridManager.instance.GetMapGrid();

        foreach (Vector2Int building in GridManager.instance.BuildingPositions)
        {
            if (mapGrid.TryGetValue(building, out var gridTile))
            {
                if (gridTile.buildingScript == null) return;

                if (gridTile.buildingScript.isOnFire)
                {
                    gridTile.buildingScript.ExtinguishFire();
                }
            }
        }

        StartCoroutine(Flood(floodObj, floodTime));
    }

    private IEnumerator Flood(GameObject floodObject, float floodTime)
    {
        isFlooded = true;
        AudioSource floodAudioSource = floodObject.GetComponentInChildren<AudioSource>();
        floodAudioSource.Play();
        floodAudioSource.volume = 0;

        while (floodObject.transform.position.y < 1)
        {
            floodObject.transform.position = floodObject.transform.position + new Vector3(0f, 0.1f * Time.deltaTime, 0f);
            if (floodAudioSource.volume < 0.692f) floodAudioSource.volume = Mathf.Clamp(floodAudioSource.volume + (0.2f * Time.deltaTime), 0, 0.692f);
            yield return null;
        }

        floodAudioSource.volume = 0.692f;
        yield return new WaitForSeconds(floodTime);

        while (floodObject.transform.position.y > 0.1)
        {
            floodObject.transform.position = floodObject.transform.position + new Vector3(0f, -0.2f * Time.deltaTime, 0f);
            if (floodAudioSource.volume > 0) floodAudioSource.volume = Mathf.Clamp(floodAudioSource.volume - (0.4f * Time.deltaTime), 0, 0.692f);
            yield return null;
        }

        isFlooded = false;
        Destroy(floodObject);
    }

}
