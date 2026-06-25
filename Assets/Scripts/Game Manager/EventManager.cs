using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private GameEffects gameEffects;


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

    //Political Question stuff
    public class PoliticalQuestion
    {
        public string Question;
        public TaskCompletionSource<bool> TaskCompletionSource = new TaskCompletionSource<bool>();
        public Action onAccept;

        public PoliticalQuestion(string question, Action onAccept)
        {
            Question = question;
            this.onAccept = onAccept;
        }
    }

    public class PoliticalScenario
    {
        public string description;
        public Action runnable;

        public PoliticalScenario(string featureDescription, Action functionality)
        {
            description = featureDescription;
            runnable = functionality;
        }
    }

    [Header("Political Question Variables")]
    public List<PoliticalQuestion> PendingQuestions = new List<PoliticalQuestion>();
    public event Action onQueueChanged;

    private PoliticalScenario[] goodFeatures;

    private PoliticalScenario[] badFeatures;

    private static void TemporaryFx()
    {
        Debug.Log("Something would've happened!");
        return;
    }

    void Start()
    {
        //Dependencies
        gameManager = GameManager.instance;
        gridManager = GridManager.instance;
        gameEffects = GameEffects.instance;

        if(gameManager == null) { Debug.LogError("Game Manager not found!"); }
        if(gridManager == null) { Debug.LogError("Grid Manager not found!"); }

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
        weightedEvents.Add(new WeightedEvent("PoliticalQuestion", 30, () => { _ = TriggerUserPoliticalEvent(); }));

        UpdateTotalWeight();

        //Populate good/bad features for political

        goodFeatures = new PoliticalScenario[]
        {
            new PoliticalScenario("Tax revenue increases", () => { gameEffects.IncreaseTaxes(); }),
            new PoliticalScenario("Public happiness improves", () => { gameEffects.IncreaseHappiness(); }),
            new PoliticalScenario("City grows faster", () => { gameEffects.IncreaseCityGrowthSpeed(); }),
        };

        badFeatures = new PoliticalScenario[]
        {
            new PoliticalScenario("corruption steals 20% of your net worth", () => { gameEffects.Take20PercentNetWorth(); }),
            new PoliticalScenario("sudden power surge", () => { gameEffects.SuddenPowerSurge(); }),
            new PoliticalScenario("increase in crime (currently does nothing)", () => { TemporaryFx(); }),
        };
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

        string currentEventType = playRandomEvent();

        if (UnityEngine.Random.value < chanceOfDouble) { 
            yield return new WaitForSeconds(2.5f); 
            playRandomEvent(currentEventType); 
        }
    }
    private string playRandomEvent(string prevEventName = "non-existant-event")
    {
        WeightedEvent selectedEvent = null;


        int safety = 0;
        while (selectedEvent == null && safety < 5)
        {
            int randInt = UnityEngine.Random.Range(0, totalWeight);
            int cursor = 0;

            foreach (var _event in weightedEvents)
            {
                cursor += _event.weight;
                if (cursor >= randInt)
                {
                    if (_event.name != prevEventName)
                    {
                        selectedEvent = _event;
                        break;
                    }
                }
            }
            safety++;
        }

        //Rejection handling
        if (selectedEvent == null) { 
            Debug.Log("Couldn't find a valid event to play in 5 iterations"); 
            return "non-existant-event"; 
        }

        selectedEvent.weightedEvent?.Invoke();
        return selectedEvent.name;
    }

    //Natural disasters

    private float minRatio = 0.01f; //1%
    private float maxRatio = 0.1f; //max 10%

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
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile tile) && tile.buildingScript)
            {
                if (!tile.buildingScript.isOnFire) continue;
                if (tile.buildingScript.isSpreadingFire) continue;

                tile.buildingScript.isSpreadingFire = true;

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

        if (validTargets.Count > 0 && UnityEngine.Random.Range(0, 2) != 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTargets.Count);
            GridManager.GridTile tile = validTargets[randomIndex];

            tile.buildingScript.IgniteFire();

            StartCoroutine(BurnBuilding(tile.buildingScript.gridPos, tile.buildingScript));

            sourceTile.buildingScript.isSpreadingFire = false;

            yield break; //only one building!
        }

        sourceTile.buildingScript.isSpreadingFire = false;
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

    //"POLITICAL" QUESTIONS

    public async Task TriggerUserPoliticalEvent()
    {
        if (goodFeatures == null || goodFeatures.Length == 0 || badFeatures == null || badFeatures.Length == 0) { Debug.LogError("No good/bad feautres!"); return; }

        //Select raddom good/bad feature
        PoliticalScenario goodFeature = goodFeatures[UnityEngine.Random.Range(0, goodFeatures.Length)];
        PoliticalScenario badFeature = badFeatures[UnityEngine.Random.Range(0, badFeatures.Length)];

        string questionString = $"{goodFeature.description} but {badFeature.description}.";

        //generate question strng
        //then calls the trigger political question with that

        PoliticalQuestion currentQuestion = new PoliticalQuestion(questionString, 
            ()=> { goodFeature.runnable?.Invoke(); 
                badFeature.runnable?.Invoke(); 
            }
        );

        PendingQuestions.Add(currentQuestion);
        onQueueChanged?.Invoke();

        bool userChoice = await currentQuestion.TaskCompletionSource.Task;
        Debug.Log($"User Choice Received: {userChoice}");

        //Pop
        PendingQuestions.Remove(currentQuestion);

        //Process request
        if (userChoice) { currentQuestion.onAccept?.Invoke(); }
        else { Debug.Log("Not invoked!"); }
    }

    //Helper functions for Weights
    private void UpdateWeights()
    {
        int daysPassed = gameManager.daysPassed;

        //SetWeights(nothing, earthquake, fire, virus, polquest);

        if (daysPassed >= 300) { SetWeights(0, 10, 20, 40, 20); return; }
        if (daysPassed >= 200) { SetWeights(5, 9, 10, 30, 15); return; }
        if (daysPassed >= 100) { SetWeights(10, 7, 10, 20, 25); return; }
        SetWeights(20, 5, 40, 20, 30);

    }

    private void SetWeights(int nothing, int earthquake, int fire, int virus, int polquest)
    {
        UpdateWeight("Nothing", nothing);
        UpdateWeight("Earthquake", earthquake);
        UpdateWeight("Fire", fire);
        UpdateWeight("Virus", virus);
        UpdateWeight("PoliticalQuestion", polquest);
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
