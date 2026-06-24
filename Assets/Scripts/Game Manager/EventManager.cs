using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager instance { get; private set; }
    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    [Header("Dependencies")]
    private GameManager gameManager;
    private GridManager gridManager;


    [Header("Disasters")]
    [SerializeField] private int gracePeriodDays = 3;
    [SerializeField] private int minIntervalDays = 3;
    [SerializeField] private int maxIntervalDays = 6; 
    private int daysLeft;
    private float chanceForDoubleEvent;

    private float secondsToBurnBuilding = 10f;

    [Header("Health Statistics")]
    private int infectedPopulation;
    public bool isLockdownActive = false; //Let user use later

    //Weighted Events
    [Serializable]
    public class WeightedEvent
    {
        public string name;
        public int weight;
        public Action weightedEvent;

        public WeightedEvent(string name, int weight, Action weightedEvent)
        {
            this.name = name;
            this.weight = weight;
            this.weightedEvent = weightedEvent;
        }
    }

    //Helper functions for Weights
    private void UpdateWeights()
    {
        int daysPassed = gameManager.daysPassed;

        if (daysPassed >= 300) { SetWeights(0, 30, 20, 40); return; }
        if (daysPassed >= 200) { SetWeights(0, 25, 10, 30); return; }
        if (daysPassed >= 100) { SetWeights(5, 20, 10, 20); return; }
        SetWeights(20, 5, 40, 20);
    }

    private void SetWeights(int nothing, int earthquake, int fire, int virus)
    {
        UpdateWeight("Nothing", nothing);
        UpdateWeight("Earthquake", earthquake);
        UpdateWeight("Fire", fire);
        UpdateWeight("Virus", virus);
    }

    private void UpdateWeight(string name, int newWeight)
    {
        var evnt = weightedEvents.Find(e => e.name == name);
        if (evnt != null) evnt.weight = newWeight;

        UpdateTotalWeight();
    }

    private void UpdateTotalWeight()
    {
        totalWeight = 0;
        foreach (var weightedEvent in weightedEvents) totalWeight += weightedEvent.weight;
    }

    List<WeightedEvent> weightedEvents = new List<WeightedEvent>();
    private int totalWeight;

    void Start()
    {
        //Dependencies
        gameManager = GameManager.instance;
        gridManager = GridManager.instance;

        //Subscribe
        gameManager.OnDayEnd += Clock;
        gameManager.OnDayEnd += UpdateWeights;

        //Set grace period
        daysLeft = gracePeriodDays + minIntervalDays;

        //Ad the weights
        weightedEvents.Add(new WeightedEvent("Nothing", 20, () => { }));
        weightedEvents.Add(new WeightedEvent("Earthquake", 5, Earthquake));
        weightedEvents.Add(new WeightedEvent("Fire", 40, SetBuildingOnFire));
        weightedEvents.Add(new WeightedEvent("Virus", 20, () => { TriggerVirusOutbreak(); } ));

        UpdateTotalWeight();
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnDayEnd -= Clock;
            gameManager.OnDayEnd -= UpdateWeights;
        }
    }

    //Event clock

    private void Clock()
    {
        daysLeft--;

        if (daysLeft <= 0)
        {
            StartCoroutine(rollEvents());
            daysLeft = UnityEngine.Random.Range(minIntervalDays, maxIntervalDays);
        }
    }

    //Event player

    private IEnumerator rollEvents()
    {
        float lerp = Mathf.Lerp(0f, 1f, gameManager.daysPassed / 200f);
        float chanceOfDouble = 5f * lerp;

        playRandomEvent();

        if (UnityEngine.Random.value < chanceOfDouble) { yield return new WaitForSeconds(2.5f); playRandomEvent(); }
    }
    private void playRandomEvent()
    {
        int randInt = UnityEngine.Random.Range(0, totalWeight);
        int cursor = 0;

        foreach (var _event in weightedEvents)
        {
            cursor+= _event.weight;
            if (cursor >= randInt)
            {
                _event?.weightedEvent.Invoke();
                return;
            }
        }
    }

    //Natural disasters

    float minRatio = 0.01f;
    float maxRatio = 0.3f;

    private void Earthquake()
    {
        Debug.Log("Earthquake!!");

        if (gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        gameManager.UserNotification?.Invoke("Earthquake!", false);

        //Update ratios
        float ratio = Mathf.Lerp(minRatio, maxRatio, gameManager.daysPassed / gameManager.daysUntilFinal);

        int numBuildingsToDestroy = (int)(ratio * gridManager.BuildingPositions.Count);

        for (int i = 0; i < numBuildingsToDestroy + 1; i++)
        {
            if (gridManager.BuildingPositions.Count == 0) break;

            int randomInt = UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count);
            Vector2Int buildingPos = gridManager.BuildingPositions[randomInt];

            gridManager.forceRemoveElement(buildingPos);
        }

        gameManager.disastersSurvived++;
    }

    //Fire
    private void SetBuildingOnFire()
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
                if (ServiceManager.instance != null) ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
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
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript is House houseScript)
            {
                if (!houseScript.isOnFire) continue;

                StartCoroutine(SpreadFire(buildingPos, mapGrid));
            }
        }

        return;
    }

    private IEnumerator BurnBuilding(Vector2Int pos, Building buildingScript)
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
        yield return new WaitForSeconds(UnityEngine.Random.Range(3f, 9f));

        if (!mapGrid.TryGetValue(pos, out GridManager.GridTile sourceTile) || sourceTile.buildingScript == null || !sourceTile.buildingScript.isOnFire)
        {
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

        if (validTargets.Count > 0 && UnityEngine.Random.Range(0, 2) != 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
            GridManager.GridTile tile = validTargets[randomIndex];

            tile.buildingScript.IgniteFire();

            StartCoroutine(BurnBuilding(tile.buildingScript.gridPos, tile.buildingScript));
            yield break; //only one building -> remove this if decide not to.
        }
    }

    //HEALTH
    private void TriggerVirusOutbreak(bool newVirus = true)
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        Dictionary<Vector2Int, GridManager.GridTile> mapGrid = gridManager.GetMapGrid();

        if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile tile) && tile.buildingScript != null)
        {
            if (tile.buildingScript is House houseScript && !houseScript.isInfected)
            {
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

    private IEnumerator KillBuilding(Vector2Int randomPos, House houseScript)
    {
        yield return new WaitForSeconds(10f);

        if (houseScript == null || !houseScript.isInfected) yield break;

        for (int i = 0; i < houseScript.residents; i++)
        {
            TriggerVirusOutbreak(false);
        }

        gridManager.forceRemoveElement(randomPos);
        gameManager.UserNotification?.Invoke("A building has been destroyed as it has been overriden with viruses!", true);
    }

    public void CheckForInfections()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

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
                houseScript.isAmbulanceOnRoute = true;

                serviceManager.DispatchAmbulance(houseScript);
            }
        }

        return;
    }

    //Political questions


    //RUBBISH

    //Create a: destroyed house, commercial and industrail assets.
    //Create rubbish manager
    //Similar to fire/ambulance after but with a rubbish truck and landfill -> will have to buy!

    //-- Tornado

    //-- Nuclear fallout

    //CRIME

    //Robberies

    //-- Terrorism

    //Gang wars

    //Police


    //POLITICS

    //Blackout ???

    //Video game choices issues -> e.g. raise taxes but you nuke half the city

    //Political unrest -> people start rioting (become unemployed, set stuff on fire)

    //Strikes & burnout 

    //-- Country declares war on you 

    //HEALTH!!

    //Viruses

    //Lockdowns

    //Hospitals


    //Super rare ones:

    //asteroid attack

    //Alien invasion
}
