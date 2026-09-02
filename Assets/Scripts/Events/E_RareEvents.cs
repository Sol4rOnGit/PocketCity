using UnityEngine;
using System.Collections;

public partial class EventManager : MonoBehaviour
{

    [Header("Rare Events")]
    [SerializeField] private GameObject asteroidPrefab;
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private GameObject _UFOPrefab;
    private float UFODivisorFactor;

    [Header("Millitary")]
    private Transform spawnTransform;
    [SerializeField] private GameObject attackHelicopterPrefab;
    [SerializeField] private GameObject B2BomberPrefab;


    private void RareEventStart()
    {
        spawnTransform = MilitaryManager.instance.transform;

        //UFO intensity
        switch (GameManager.instance.gameDifficulty)
        {
            case GameSettings.Difficulty.Easy:
                UFODivisorFactor = 4f;
                break;
            case GameSettings.Difficulty.Normal:
                UFODivisorFactor = 3f;
                break;
            case GameSettings.Difficulty.Hard:
                UFODivisorFactor = 2.5f;
                break;
            case GameSettings.Difficulty.Nightmare:
                UFODivisorFactor = 2f;
                break;
            default:
                Debug.LogWarning("UFODivisorFactor set to default value");
                UFODivisorFactor = 3f;
                break;
        }
    }

    //Asteroid Strike
    public void TriggerAsteroidStrike()
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

        BlastDestruction(centre);
        DoExplosionVisualAndAudio(endPos, asteroid);
        Destroy(asteroid);
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
                        if (tile.buildingScript)
                        {

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

    private void DoExplosionVisualAndAudio(Vector3 centreWorldPos, GameObject asteroid)
    {
        GameObject explosionEffect = Instantiate(explosionEffectPrefab, centreWorldPos, Quaternion.identity, MilitaryManager.instance.transform);
        Destroy(explosionEffect, 3);

        AudioSource explosionAudioSource = asteroid.GetComponentInChildren<AudioSource>();
        GameObject explosionSFXGameObj = explosionAudioSource.gameObject;
        explosionSFXGameObj.transform.SetParent(GameManager.instance.transform);
        explosionAudioSource.Play();
        Destroy(explosionSFXGameObj, explosionAudioSource.clip.length);
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

        int radius = 15;

        for (int i = centre.x - radius; i <= centre.x + radius; i++)
        {
            for (int j = centre.y - radius; j <= centre.y + radius; j++)
            {
                Vector2Int pos = new Vector2Int(i, j);
                float distance = Vector2Int.Distance(centre, pos);

                if (distance < radius)
                {
                    if (mapGrid.TryGetValue(pos, out var tile))
                    {
                        if (tile.buildingScript && tile.buildingScript is House houseScript)
                        {
                            //Aduct 50% of residents
                            int abductedPop = Mathf.RoundToInt(houseScript.residents / UFODivisorFactor);
                            houseScript.residents -= abductedPop;
                            gameManager.LosePopulation(abductedPop);
                        }
                    }
                }

            }
        }
    }

    //Military

    public void SummonAttackHelicopter()
    {
        Instantiate(attackHelicopterPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity, spawnTransform);
    }

    public void SummonB2Bomber()
    {
        Instantiate(B2BomberPrefab, new Vector3(200f, 0f, 200f), Quaternion.identity, spawnTransform);
    }

    public void TriggerMilitaryInvasion()
    {
        Instantiate(attackHelicopterPrefab, new Vector3(100f, 10f, 100f), Quaternion.identity, spawnTransform);
        Instantiate(attackHelicopterPrefab, new Vector3(-100f, 10f, -100f), Quaternion.identity, spawnTransform);
        Instantiate(attackHelicopterPrefab, new Vector3(-100f, 10f, 100f), Quaternion.identity, spawnTransform);
        Instantiate(attackHelicopterPrefab, new Vector3(100f, 10f, -100f), Quaternion.identity, spawnTransform);

        float chanceOfCloseHelicopter = 0.3f;
        if (Random.value > chanceOfCloseHelicopter)
        {
            Instantiate(attackHelicopterPrefab, new Vector3(100f, 10f, -100f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(10f, 10f, -10f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(-10f, 10f, 10f), Quaternion.identity, spawnTransform);
            Instantiate(attackHelicopterPrefab, new Vector3(0f, 10f, 0f), Quaternion.identity, spawnTransform);
        }

        if (Random.value > 0.2) SummonB2Bomber(); //80% chance
    }
}
