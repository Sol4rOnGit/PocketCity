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
    [SerializeField] private E_RareEvents rareEventsScript;

    [Header("Fire")]
    private readonly float secondsToBurnBuilding = 10f;

    [Header("Health Statistics")]
    private int infectedPopulation;
    public bool isLockdownActive = false;

    [Header("Event Rolling")]
    [SerializeField] private int gracePeriodDays = 3;
    [SerializeField] private int minIntervalDays = 3;
    [SerializeField] private int maxIntervalDays = 6; 
    private int daysLeft;
    private int currentPhase = -1;
    private readonly float chanceForDoubleEvent = 0.75f;

    private float rareEventMultiplier = 1f;
    private int crimeWeightingIncrease = 0;

    //Weighted Events
    List<WeightedEvent> weightedEvents = new List<WeightedEvent>();
    Dictionary<EventType, int> phaseWeights = new Dictionary<EventType, int>();
    private int totalWeight;

    public enum EventType
    {
        Nothing,
        PoliticalQuestion,
        DIS_Earthquake,
        DIS_Fire,
        DIS_Virus,
        CRIME_Arson,
        CRIME_Robbery,
        RARE_AsteroidStrike,
        RARE_AlienInvasion,
        RARE_AttackHelicopter
    }

    [Serializable]
    public class WeightedEvent
    {
        public EventType eventType;
        public int weight;
        public Action weightedEvent;

        public WeightedEvent(EventType eventType, int weight, Action weightedEvent)
        {
            this.eventType = eventType;
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

    [Header("Rare Events")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private GameObject _UFOPrefab;

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
        if(gameEffects == null) { Debug.LogError("Game Effects not found!"); }

        //Subscribe
        gameManager.OnDayEnd += Clock;
        gameManager.OnDayEnd += UpdateWeights;

        //Set grace period
        daysLeft = gracePeriodDays + minIntervalDays;

        //Ad the weights
        InitialiseWeights();

        //Populate good/bad features for political
        InitialisePoliticalQuestions();
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
        float lerp = Mathf.Lerp(0f, 1f, Mathf.Clamp01(gameManager.daysPassed / 200f));
        float chanceOfDouble = chanceForDoubleEvent * lerp;

        gameManager.disastersSurvived++;

        EventType currentEventType = playRandomEvent();

        if (UnityEngine.Random.value < chanceOfDouble) { 
            yield return new WaitForSeconds(2.5f); 
            playRandomEvent(currentEventType);
            gameManager.disastersSurvived++;
        }
    }
    private EventType playRandomEvent(EventType prevEventName = EventType.Nothing)
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
                    if (_event.eventType != prevEventName)
                    {
                        selectedEvent = _event;
                    }
                    break;
                }
            }
            safety++;
        }

        //Rejection handling
        if (selectedEvent == null) { 
            Debug.Log("Couldn't find a valid event to play in 5 iterations"); 
            return EventType.Nothing; 
        }

        selectedEvent.weightedEvent?.Invoke();
        return selectedEvent.eventType;
    }

    //Natural disasters

    private float minRatio = 0.01f; //1%
    private float maxRatio = 0.05f; //max 5%

    private void Earthquake()
    {
        Debug.Log("Earthquake!!");

        if (gridManager.BuildingPositions == null || gridManager.BuildingPositions.Count == 0)
        {
            Debug.LogWarning("Failed Earthquake - building pos is null or no count");
            return;
        }

        gameManager.UserNotification?.Invoke("Earthquake!", true);

        //Update ratios
        maxRatio = Mathf.Lerp(minRatio, maxRatio, gameManager.daysPassed / 300f);
        float ratio = UnityEngine.Random.Range(0f, maxRatio);

        int numBuildingsToDestroy = (int)(ratio * gridManager.BuildingPositions.Count);

        for (int i = 0; i < numBuildingsToDestroy + 1; i++)
        {
            if (gridManager.BuildingPositions.Count == 0) break;

            int randomInt = UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count);
            Vector2Int buildingPos = gridManager.BuildingPositions[randomInt];

            var mapGrid = gridManager.GetMapGrid();
            if (mapGrid.TryGetValue(buildingPos, out GridManager.GridTile gridTile))
            {
                if (gridTile.buildingScript.isRetrofitted)
                {
                    Debug.Log("Retrofitted! Saved from earthquake");
                    continue;
                }
            }

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
                if (ServiceManager.instance != null) { 
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

        while (!foundInfected && tries < 50)
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

        int spreadNum = (houseScript.residents < 2) ? 1 : UnityEngine.Random.Range(1, 4);

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

    //CRIME

    //ARSON

    private void TriggerArson()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        var mapGrid = gridManager.GetMapGrid();

        if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile gridTile))
        {
            if (!gridTile.buildingScript.isOnFire)
            {
                gameManager.UserNotification?.Invoke($"Arson Threat Declared at {randomPos.x}, {randomPos.y}", true);

                StartCoroutine(ArsonTimer(randomPos, gridTile.buildingScript));

                if (ServiceManager.instance == null) { return; }
                ServiceManager.instance.DispatchPolice(gridTile.buildingScript);
            }
        }
    }

    private IEnumerator ArsonTimer(Vector2Int gridPos, Building buildingScript)
    {
        buildingScript.isCrimeScene = true;
        GameObject timerCube = CreateTimerCube(gridPos, new Color(0.01f, 4f, 4f, 0.67f));

        float totalTime = 25f;
        float timeRemaining = totalTime;

        while (timeRemaining > 0)
        {
            if (buildingScript == null || !buildingScript.isCrimeScene)
            {
                Destroy(timerCube);
                yield break;
            }

            float currentSize = timeRemaining / totalTime;
            timerCube.transform.localScale = new Vector3(currentSize, currentSize, currentSize);

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        Destroy(timerCube);

        if(buildingScript != null && buildingScript.isCrimeScene)
        {
            buildingScript.isCrimeScene = false;
            gameManager.UserNotification?.Invoke($"Arsonist set fire to building at {gridPos.x} {gridPos.y}", true);

            buildingScript.IgniteFire();
            StartCoroutine(BurnBuilding(gridPos, buildingScript));

            if (ServiceManager.instance != null) { ServiceManager.instance.DispatchFiretruck(buildingScript); }
        }

    }

    // ROBBERY

    private void TriggerRobbery()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int randomPos = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];
        var mapGrid = gridManager.GetMapGrid();

        if (mapGrid.TryGetValue(randomPos, out GridManager.GridTile gridTile))
        {
            if (!gridTile.buildingScript.isOnFire)
            {
                gameManager.UserNotification?.Invoke($"Robbery Threat Declared at {randomPos.x}, {randomPos.y}", true);

                StartCoroutine(RobberyTimer(randomPos, gridTile.buildingScript));

                if (ServiceManager.instance == null) { return; }
                ServiceManager.instance.DispatchPolice(gridTile.buildingScript);
            }
        }
    }

    private IEnumerator RobberyTimer(Vector2Int gridPos, Building buildingScript)
    {
        buildingScript.isCrimeScene = true;
        GameObject timerCube = CreateTimerCube(gridPos, new Color(12f, 0f, 0f, 0.3f));

        float totalTime = 20f;
        float timeRemaining = totalTime;

        while (timeRemaining > 0)
        {
            if (buildingScript == null || !buildingScript.isCrimeScene)
            {
                Destroy(timerCube);
                yield break;
            }

            float currentSize = timeRemaining / totalTime;
            timerCube.transform.localScale = new Vector3(currentSize, currentSize, currentSize);

            timeRemaining -= Time.deltaTime;
            yield return null;
        }

        Destroy(timerCube);

        if (buildingScript != null && buildingScript.isCrimeScene)
        {
            buildingScript.isCrimeScene = false;
            int payout = buildingScript is Employer employerScript ? (int)(employerScript.GetTaxRevenue() * 400f) : Mathf.Min((int)(FinanceManager.instance.currentMoney / 5f), 150_000); 
            gameManager.UserNotification?.Invoke($"Bank robbery succesful! Insurance payout of {payout}. Affected Pos: ({gridPos.x}, {gridPos.y})", true);

            buildingScript.IgniteFire();
            StartCoroutine(BurnBuilding(gridPos, buildingScript));

            if (ServiceManager.instance != null) { ServiceManager.instance.DispatchFiretruck(buildingScript); }
        }
    }

    // Helper [CRIME]
    private GameObject CreateTimerCube(Vector2Int gridPos, Color color)
    {
        float scale = gridManager.getGridScale();

        Vector3 spawnPos = new Vector3(gridPos.x * scale, 6f, gridPos.y * scale);
        GameObject timerCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        timerCube.transform.position = spawnPos;

        Destroy(timerCube.GetComponent<BoxCollider>());

        //Making the cube texture
        Renderer cubeRenderer = timerCube.GetComponent<Renderer>();

        cubeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        cubeRenderer.material.color = color;

        return timerCube;
    }

    //"POLITICAL" QUESTIONS

    private void InitialisePoliticalQuestions()
    {
        goodFeatures = new PoliticalScenario[]
{
            new PoliticalScenario("Tax revenue increases by 5%", () => { gameEffects.IncreaseTaxes(); }),
            new PoliticalScenario("Public happiness improves", () => { gameEffects.IncreaseHappiness(); }),
            new PoliticalScenario("City grows faster", () => { gameEffects.IncreaseCityGrowthSpeed(2); }),
            new PoliticalScenario("City grows a lot faster", () => { gameEffects.IncreaseCityGrowthSpeed(3); }),
            new PoliticalScenario("City grows MUCH faster", () => { gameEffects.IncreaseCityGrowthSpeed(4); }),
            new PoliticalScenario("Get 10k", () => { FinanceManager.instance.Gain(10_000); }),
            new PoliticalScenario("Get 100k", () => { FinanceManager.instance.Gain(100_000); })
};

        badFeatures = new PoliticalScenario[]
        {
            new PoliticalScenario("corruption steals 20% of your net worth", () => { gameEffects.Take20PercentNetWorth(); }),
            new PoliticalScenario("sudden power surge", () => { gameEffects.SuddenPowerSurge(); }),
            new PoliticalScenario("increase in crime", () => { crimeWeightingIncrease += 3; UpdateWeights(); }),
            new PoliticalScenario("50% increase in rare events", () => { rareEventMultiplier += 0.5f; UpdateWeights(); }),
            new PoliticalScenario("get an asteroid bombing", () => { gameEffects.AsteroidBombing(); }),
            new PoliticalScenario("get an alien invasion", () => { TriggerAlienInvasion(); }), 
            new PoliticalScenario("lose 200k", () => { FinanceManager.instance.ForcePurchase(50_000); })
        };
    }

    public async Task TriggerUserPoliticalEvent()
    {
        if (goodFeatures == null || goodFeatures.Length == 0 || badFeatures == null || badFeatures.Length == 0) { Debug.LogError("No good/bad feautres!"); return; }

        //Select random good/bad feature
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
        //Debug.Log($"User Choice Received: {userChoice}");

        //Pop
        PendingQuestions.Remove(currentQuestion);

        //Process request
        if (userChoice) { currentQuestion.onAccept?.Invoke(); }
        //else { Debug.Log("Not invoked!"); }
    }

    //Helper functions for Weights
    private void InitialiseWeights()
    {
        RegisterEvent(EventType.Nothing, () => { });
        RegisterEvent(EventType.PoliticalQuestion, () => { _ = TriggerUserPoliticalEvent(); });

        //Disasters
        RegisterEvent(EventType.DIS_Earthquake, Earthquake);
        RegisterEvent(EventType.DIS_Fire, SetBuildingOnFire);
        RegisterEvent(EventType.DIS_Virus, () => { TriggerVirusOutbreak(); });

        //Crime
        RegisterEvent(EventType.CRIME_Arson, TriggerArson);
        RegisterEvent(EventType.CRIME_Robbery, TriggerRobbery);

        //Rare
        RegisterEvent(EventType.RARE_AsteroidStrike, AsteroidStrike);
        RegisterEvent(EventType.RARE_AlienInvasion, TriggerAlienInvasion);
        RegisterEvent(EventType.RARE_AttackHelicopter, () => { rareEventsScript.SummonAttackHelicopter(); });

        UpdateTotalWeight();
    }

    private void RegisterEvent(EventType eventType, Action action)
    {
        weightedEvents.Add(new WeightedEvent(eventType, 0, action));
    }

    private void LoadPhase(int phase)
    {
        phaseWeights.Clear();

        switch (phase)
        {
            default: //Day 0-99
                SetWeight(EventType.Nothing, 20);
                SetWeight(EventType.PoliticalQuestion, 20);

                SetWeight(EventType.DIS_Earthquake, 5);
                SetWeight(EventType.DIS_Fire, 40);
                SetWeight(EventType.DIS_Virus, 20);

                SetWeight(EventType.CRIME_Arson, 20);
                SetWeight(EventType.CRIME_Robbery, 10);

                SetWeight(EventType.RARE_AlienInvasion, 0);
                SetWeight(EventType.RARE_AsteroidStrike, 0);
                SetWeight(EventType.RARE_AttackHelicopter, 0);

                break;
            case 1: //Day 100-199
                SetWeight(EventType.Nothing, 7); 
                SetWeight(EventType.PoliticalQuestion, 15);

                SetWeight(EventType.DIS_Earthquake, 17);
                SetWeight(EventType.DIS_Fire, 35);
                SetWeight(EventType.DIS_Virus, 25);

                SetWeight(EventType.CRIME_Arson, 15);
                SetWeight(EventType.CRIME_Robbery, 15);

                SetWeight(EventType.RARE_AsteroidStrike, 1);
                SetWeight(EventType.RARE_AlienInvasion, 1);
                SetWeight(EventType.RARE_AttackHelicopter, 1);

                break;
            case 2: //Day 200-299
                SetWeight(EventType.Nothing, 6);
                SetWeight(EventType.PoliticalQuestion, 15);

                SetWeight(EventType.DIS_Earthquake, 10);
                SetWeight(EventType.DIS_Fire, 25);
                SetWeight(EventType.DIS_Virus, 21);

                SetWeight(EventType.CRIME_Arson, 10);
                SetWeight(EventType.CRIME_Robbery, 10);

                SetWeight(EventType.RARE_AsteroidStrike, 2);
                SetWeight(EventType.RARE_AlienInvasion, 2);
                SetWeight(EventType.RARE_AttackHelicopter, 1);

                break;
            case 3: //Day 300-399
                SetWeight(EventType.Nothing, 3);
                SetWeight(EventType.PoliticalQuestion, 10);

                SetWeight(EventType.DIS_Earthquake, 18);
                SetWeight(EventType.DIS_Fire, 22);
                SetWeight(EventType.DIS_Virus, 20);

                SetWeight(EventType.CRIME_Arson, 10);
                SetWeight(EventType.CRIME_Robbery, 10);

                SetWeight(EventType.RARE_AsteroidStrike, 3);
                SetWeight(EventType.RARE_AlienInvasion, 3);
                SetWeight(EventType.RARE_AttackHelicopter, 4);

                break;
        }
    }

    private void UpdateWeights()
    {
        int daysPassed = gameManager.daysPassed;
        int newPhase = GetPhaseFromDay(daysPassed);

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            LoadPhase(currentPhase);
        }

        UpdateTotalWeight();
    }

    private void SetWeight(EventType eventType, int baseWeight)
    {
        int finalWeight = baseWeight;

        //Crime
        if (eventType == EventType.CRIME_Arson || eventType == EventType.CRIME_Robbery)
        {
            finalWeight += crimeWeightingIncrease;
        }

        //Rare
        if (eventType == EventType.RARE_AsteroidStrike || eventType == EventType.RARE_AlienInvasion)
        {
            finalWeight = Mathf.RoundToInt(baseWeight * rareEventMultiplier);
        }

        phaseWeights[eventType] = finalWeight;
        var matchingEvent = weightedEvents.Find(e => e.eventType == eventType);
        if (matchingEvent != null) matchingEvent.weight = finalWeight;
    }

    private int GetPhaseFromDay(int daysPassed)
    {
        if (daysPassed >= 300) { return 3; }
        if (daysPassed >= 200) { return 2; }
        if (daysPassed >= 100) { return 1; }
        return 0;
    }

    private void UpdateTotalWeight()
    {
        totalWeight = 0;
        foreach (var weightedEvent in weightedEvents) totalWeight += weightedEvent.weight;
    }

    //Rare Events -----!!

    //Asteroid Strike
    public void AsteroidStrike()
    {
        if (gridManager.BuildingPositions.Count == 0) { return; }

        Vector2Int centre = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];

        StartCoroutine(AsteroidAnimation(centre));
    }

    private IEnumerator AsteroidAnimation(Vector2Int centre)
    {
        float scale = gridManager.getGridScale();
        Vector3 startPos = new Vector3(0, 100, 0);
        Vector3 endPos = new Vector3(centre.x * scale, 0f, centre.y * scale);

        GameObject asteroid = Instantiate(asteroidPrefab, startPos, Quaternion.identity);

        float timeElapsedSeconds = 0f;
        float timeToMoveSeconds = 1.5f;

        while (timeElapsedSeconds < timeToMoveSeconds)
        {
            asteroid.transform.position = Vector3.Lerp(startPos, endPos, timeElapsedSeconds / timeToMoveSeconds);
            timeElapsedSeconds += Time.deltaTime;
            yield return null;
        }

        Destroy(asteroid);
        StartCoroutine(DoExplosionEffect(endPos));
        BlastDestruction(centre);
    }

    private void BlastDestruction(Vector2Int centre)
    {
        var mapGrid = gridManager.GetMapGrid();

        //Destruction event -> annihilate everything within 3 grid blocks & set everything within radius on fire
        gridManager.forceRemoveElement(centre);

        int radius = 6;
        int innerRadius = 3;

        for (int i = centre.x - radius; i <= centre.x + radius; i++)
        {
            for (int j = centre.y - radius; j <= centre.y + radius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                float distance = Vector2Int.Distance(centre, pos);

                if (distance < innerRadius)
                {
                    gridManager.forceRemoveElement(pos);
                }
                else if (distance < radius)
                {
                    if (mapGrid.TryGetValue(pos, out var tile))
                    {
                        if (tile.isRoad) { gridManager.forceRemoveElement(pos); }
                        if (tile.buildingScript) {

                            //Set building on fire
                            tile.buildingScript.IgniteFire();

                            StartCoroutine(BurnBuilding(pos, tile.buildingScript));

                            //Call fire services
                            if (ServiceManager.instance != null) ServiceManager.instance.DispatchFiretruck(tile.buildingScript);
                            else { Debug.LogError("Service Manager not found!"); }
                        }
                    }
                }

            }
        }
    }

    private IEnumerator DoExplosionEffect(Vector3 centreWorldPos)
    {
        GameObject explosionEffect = Instantiate(explosionEffectPrefab, centreWorldPos, Quaternion.identity, MilitaryManager.instance.transform);

        yield return new WaitForSeconds(3.0f);

        Destroy(explosionEffect);
    }

    //Alien Invastion (UFO)

    private void TriggerAlienInvasion()
    {
        if (gridManager.BuildingPositions.Count == 0) return;

        Vector2Int centre = gridManager.BuildingPositions[UnityEngine.Random.Range(0, gridManager.BuildingPositions.Count)];

        float scale = gridManager.getGridScale();
        Vector3 centrePos = new Vector3(centre.x * scale, 0f, centre.y * scale);

        GameObject _UFOObj = Instantiate(_UFOPrefab, centrePos, Quaternion.identity);
        UFOMain _UFOScript = _UFOObj.GetComponent<UFOMain>();
        _UFOScript.StartInvasion(() => { AlienInvasionConsequences(centre); });
    }

    private void AlienInvasionConsequences(Vector2Int centre)
    {
        var mapGrid = gridManager.GetMapGrid();

        gridManager.forceRemoveElement(centre);

        int radius = 25;
        int innerRadius = 3;

        for (int i = centre.x - radius; i <= centre.x + radius; i++)
        {
            for (int j = centre.y - radius; j <= centre.y + radius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                float distance = Vector2Int.Distance(centre, pos);

                if (distance < innerRadius)
                {
                    //Abduct all residents
                    if (mapGrid.TryGetValue(pos, out var gridTile)){

                        if (gridTile.buildingScript && gridTile.buildingScript is House houseScript)
                        {
                            gameManager.LosePopulation(houseScript.residents);
                            houseScript.residents = 0;
                        }
                    }
                }
                else if (distance < radius)
                {
                    if (mapGrid.TryGetValue(pos, out var tile))
                    {
                        if (tile.buildingScript && tile.buildingScript is House houseScript)
                        {
                            //Aduct 50% of residents
                            int abductedPop = Mathf.RoundToInt(houseScript.residents / 2f);
                            houseScript.residents -= abductedPop;
                            gameManager.LosePopulation(abductedPop);
                        }
                    }
                }

            }
        }
    }

    //RUBBISH

    //Create a: destroyed house, commercial and industrail assets.
    //Create rubbish manager
    //Similar to fire/ambulance after but with a rubbish truck and landfill -> will have to buy!

    //Less bad events

    //

    //-- Tornado

    //-- Nuclear fallout

    //Weather Events

    //Tornado

    //Thunderstorm

    //CRIME

    //-- Terrorism

    //Gang wars

    //Political unrest -> people start rioting (become unemployed, set stuff on fire)

    //Strikes & burnout 

    //-- Country declares war on you 

    //Super rare ones:

    //kaiju invasion

}
