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


    [Header("Event Rolling")]
    [SerializeField] private int gracePeriodDays = 3;
    [SerializeField] private int minIntervalDays = 3;
    [SerializeField] private int maxIntervalDays = 6; 
    private int daysLeft;
    private float chanceForDoubleEvent = 0.75f;

    private float rareEventMultiplier = 1f;
    private int crimeWeightingIncrease = 0;

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

    [Header("Rare Events")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;

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

        string currentEventType = playRandomEvent();

        if (UnityEngine.Random.value < chanceOfDouble) { 
            yield return new WaitForSeconds(2.5f); 
            playRandomEvent(currentEventType);
            gameManager.disastersSurvived++;
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
                    }
                    break;
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

        if (houseScript.residents < 2) {
            //Spread atleast Once
            yield return null;
            TriggerVirusOutbreak(false); 
        } 
        else
        {
            int random = UnityEngine.Random.Range(1, 5); //anywhere between 1 to 5 residents
            for (int i = 1; i < random; i++)
            {
                yield return null; 
                TriggerVirusOutbreak(false); //Spread!
            }
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
};

        badFeatures = new PoliticalScenario[]
        {
            new PoliticalScenario("corruption steals 20% of your net worth", () => { gameEffects.Take20PercentNetWorth(); }),
            new PoliticalScenario("sudden power surge", () => { gameEffects.SuddenPowerSurge(); }),
            new PoliticalScenario("increase in crime", () => { crimeWeightingIncrease += 3; }),
            new PoliticalScenario("50% increase in rare events", () => { rareEventMultiplier += 0.5f; }),
            new PoliticalScenario("get an asteroid bombing", () => { gameEffects.AsteroidBombing(); })
        };
    }

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
        weightedEvents.Add(new WeightedEvent("Nothing", 20, () => { }));
        weightedEvents.Add(new WeightedEvent("Earthquake", 5, Earthquake));
        weightedEvents.Add(new WeightedEvent("Fire", 40, SetBuildingOnFire));
        weightedEvents.Add(new WeightedEvent("Virus", 20, () => { TriggerVirusOutbreak(); }));
        weightedEvents.Add(new WeightedEvent("Arson", 15, TriggerArson));
        weightedEvents.Add(new WeightedEvent("Robbery", 10, TriggerRobbery));
        weightedEvents.Add(new WeightedEvent("PoliticalQuestion", 30, () => { _ = TriggerUserPoliticalEvent(); }));
        weightedEvents.Add(new WeightedEvent("AsteroidStrike", 0, AsteroidStrike));

        UpdateTotalWeight();
    }
    
    private void UpdateWeights()
    {
        int daysPassed = gameManager.daysPassed;

        //SetWeights(nothing, earthquake, fire, virus, arson, robbery, polquest, asteroidStrike);

        if (daysPassed >= 300) { SetWeights(0, 10, 20, 40, 15, 6, 20, 2); return; }
        if (daysPassed >= 200) { SetWeights(5, 9, 10, 30, 20, 8, 15, 1); return; }
        if (daysPassed >= 100) { SetWeights(10, 7, 10, 20, 40, 10, 25, 1); return; }
        SetWeights(20, 5, 40, 20, 20, 10, 30, 0);

        UpdateTotalWeight();
    }

    private void SetWeights(int nothing, int earthquake, int fire, int virus, int arson, int robbery, int polquest, int asteroidStrike)
    {
        UpdateWeight("Nothing", nothing);

        //"Natural" disasters
        UpdateWeight("Earthquake", earthquake);
        UpdateWeight("Fire", fire);
        UpdateWeight("Virus", virus);

        //Crime
        UpdateWeight("Arson", arson + crimeWeightingIncrease);
        UpdateWeight("Robbery", robbery + crimeWeightingIncrease);

        //Other
        UpdateWeight("PoliticalQuestion", polquest);

        //Rare events
        UpdateWeight("AsteroidStrike", (int)(asteroidStrike * rareEventMultiplier));
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
        GameObject explosionEffect = Instantiate(explosionEffectPrefab, centreWorldPos, Quaternion.identity);

        yield return new WaitForSeconds(3.0f);

        Destroy(explosionEffect);
    }

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
