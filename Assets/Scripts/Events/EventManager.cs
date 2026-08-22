using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EventManager : MonoBehaviour
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
    [SerializeField] private int maxIntervalDays = 4; 
    private int daysLeft;
    private int currentPhase = -1;
    private readonly float chanceForDoubleEvent = 0.75f;

    private bool stopped = false;
    public void StartEvents()
    {
        stopped = false;
    }
    public void StopEvents()
    {
        stopped = true;
    }

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
        DIS_Flood,

        CRIME_Arson,
        CRIME_Robbery,

        RARE_AsteroidStrike,
        RARE_AlienInvasion,
        RARE_AttackHelicopter,
        RARE_MilitaryInvasion
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
        daysLeft = gracePeriodDays;

        //Ad the weights
        InitialiseWeights();

        //Start from other partials
        MiscStart();
        RareEventStart();
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
            if (stopped) return;
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
            int randInt = UnityEngine.Random.Range(1, totalWeight);
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

    //Helper functions for Weights
    private void InitialiseWeights()
    {
        RegisterEvent(EventType.Nothing, () => { });
        RegisterEvent(EventType.PoliticalQuestion, () => { _ = TriggerUserPoliticalEvent(); });

        //Disasters
        RegisterEvent(EventType.DIS_Earthquake, TriggerEarthquake);
        RegisterEvent(EventType.DIS_Fire, TriggerBuildingOnFire);
        RegisterEvent(EventType.DIS_Virus, () => { TriggerVirusOutbreak(); });
        RegisterEvent(EventType.DIS_Flood, TriggerFlood);

        //Crime
        RegisterEvent(EventType.CRIME_Arson, TriggerArson);
        RegisterEvent(EventType.CRIME_Robbery, TriggerRobbery);

        //Rare
        RegisterEvent(EventType.RARE_AsteroidStrike, TriggerAsteroidStrike);
        RegisterEvent(EventType.RARE_AlienInvasion, TriggerAlienInvasion);
        RegisterEvent(EventType.RARE_AttackHelicopter, () => { SummonAttackHelicopter(); });
        RegisterEvent(EventType.RARE_MilitaryInvasion, () => { TriggerMilitaryInvasion(); });

        UpdateTotalWeight();
    }

    private void RegisterEvent(EventType eventType, Action action)
    {
        weightedEvents.Add(new WeightedEvent(eventType, 0, action));
    }
    
    private int GetPhaseFromDay(int daysPassed)
    {
        if (daysPassed >= 400) { return 4; }
        if (daysPassed >= 300) { return 3; }
        if (daysPassed >= 200) { return 2; }
        if (daysPassed >= 100) { return 1; }
        return 0;
    }

    private void LoadPhase(int phase)
    {
        phaseWeights.Clear();

        switch (phase)
        {
            default: //Day 0-99
                SetWeight(EventType.Nothing, 20);
                SetWeight(EventType.PoliticalQuestion, 30);

                SetWeight(EventType.DIS_Earthquake, 5);
                SetWeight(EventType.DIS_Fire, 40);
                SetWeight(EventType.DIS_Virus, 20);
                SetWeight(EventType.DIS_Flood, 0);

                SetWeight(EventType.CRIME_Arson, 20);
                SetWeight(EventType.CRIME_Robbery, 0);

                SetWeight(EventType.RARE_AlienInvasion, 0);
                SetWeight(EventType.RARE_AsteroidStrike, 0);
                SetWeight(EventType.RARE_AttackHelicopter, 0);
                SetWeight(EventType.RARE_MilitaryInvasion, 0);

                break;
            case 1: //Day 100-199
                SetWeight(EventType.Nothing, 7); 
                SetWeight(EventType.PoliticalQuestion, 25);

                SetWeight(EventType.DIS_Earthquake, 17);
                SetWeight(EventType.DIS_Fire, 35);
                SetWeight(EventType.DIS_Virus, 25);
                SetWeight(EventType.DIS_Flood, 3);

                SetWeight(EventType.CRIME_Arson, 15);
                SetWeight(EventType.CRIME_Robbery, 15);

                SetWeight(EventType.RARE_AsteroidStrike, 0);
                SetWeight(EventType.RARE_AlienInvasion, 3);
                SetWeight(EventType.RARE_AttackHelicopter, 4);
                SetWeight(EventType.RARE_MilitaryInvasion, 0);

                break;
            case 2: //Day 200-299
                SetWeight(EventType.Nothing, 6);
                SetWeight(EventType.PoliticalQuestion, 20);

                SetWeight(EventType.DIS_Earthquake, 10);
                SetWeight(EventType.DIS_Fire, 25);
                SetWeight(EventType.DIS_Virus, 21);
                SetWeight(EventType.DIS_Flood, 5);

                SetWeight(EventType.CRIME_Arson, 10);
                SetWeight(EventType.CRIME_Robbery, 10);

                SetWeight(EventType.RARE_AsteroidStrike, 1);
                SetWeight(EventType.RARE_AlienInvasion, 3);
                SetWeight(EventType.RARE_AttackHelicopter, 4);
                SetWeight(EventType.RARE_MilitaryInvasion, 0);

                break;
            case 3: //Day 300-399
                SetWeight(EventType.Nothing, 3);
                SetWeight(EventType.PoliticalQuestion, 10);

                SetWeight(EventType.DIS_Earthquake, 18);
                SetWeight(EventType.DIS_Fire, 22);
                SetWeight(EventType.DIS_Virus, 20);
                SetWeight(EventType.DIS_Flood, 2);

                SetWeight(EventType.CRIME_Arson, 10);
                SetWeight(EventType.CRIME_Robbery, 10);

                SetWeight(EventType.RARE_AsteroidStrike, 2);
                SetWeight(EventType.RARE_AlienInvasion, 3);
                SetWeight(EventType.RARE_AttackHelicopter, 1);
                SetWeight(EventType.RARE_MilitaryInvasion, 3);

                break;
            case 4:
                if (GameManager.instance.gameDifficulty == GameSettings.Difficulty.Easy
                    || GameManager.instance.gameDifficulty == GameSettings.Difficulty.Normal) return;

                SetWeight(EventType.Nothing, 0);
                SetWeight(EventType.PoliticalQuestion, 5);

                SetWeight(EventType.DIS_Earthquake, 20);
                SetWeight(EventType.DIS_Fire, 20);
                SetWeight(EventType.DIS_Virus, 30);
                SetWeight(EventType.DIS_Flood, 1);

                SetWeight(EventType.CRIME_Arson, 1);
                SetWeight(EventType.CRIME_Robbery, 1);

                SetWeight(EventType.RARE_AsteroidStrike, 2);
                SetWeight(EventType.RARE_AlienInvasion, 4);
                SetWeight(EventType.RARE_AttackHelicopter, 10);
                SetWeight(EventType.RARE_AttackHelicopter, 5);

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
        }

        LoadPhase(currentPhase);

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
        if (eventType == EventType.RARE_AsteroidStrike || 
            eventType == EventType.RARE_AlienInvasion || 
            eventType == EventType.RARE_AttackHelicopter || 
            eventType == EventType.RARE_MilitaryInvasion)
        {
            finalWeight = Mathf.RoundToInt(baseWeight * rareEventMultiplier);
        }

        phaseWeights[eventType] = finalWeight;
        var matchingEvent = weightedEvents.Find(e => e.eventType == eventType);
        if (matchingEvent != null) matchingEvent.weight = finalWeight;
    }

    private void UpdateTotalWeight()
    {
        totalWeight = 0;
        foreach (var weightedEvent in weightedEvents) totalWeight += weightedEvent.weight;
    }

    //Rare Events -----!!

    //RUBBISH

    //Create a: destroyed house, commercial and industrail assets.
    //Create rubbish manager
    //Similar to fire/ambulance after but with a rubbish truck and landfill -> will have to buy!

    //Less bad events

    /*
     * Lightning storm sets 8 houses on fire.
     * Flash flood cuts energy generation to 0 and auto triggers lock down for 3 days. 
     * I'll make gas explosion set 4 buildings around it on fire as well as exploding the central building.
     * EMP Burst to disable turrets for 10 seconds.
     */

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
