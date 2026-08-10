using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public partial class EventManager : MonoBehaviour
{
    public void MiscStart()
    {
        //Populate good/bad features for political
        InitialisePoliticalQuestions();
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

        if (buildingScript != null && buildingScript.isCrimeScene)
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
            new PoliticalScenario("lose 200k", () => { FinanceManager.instance.ForcePurchase(200_000); }),
            new PoliticalScenario("get a flash flood", TriggerFlood)
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
            () => {
                goodFeature.runnable?.Invoke();
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
}
