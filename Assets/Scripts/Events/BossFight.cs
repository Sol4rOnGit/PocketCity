using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossFight : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TMPro.TextMeshProUGUI bossFightText;
    [SerializeField] private TMPro.TextMeshProUGUI healthText;

    [SerializeField] private GameObject miniGame;
    [SerializeField] private GameObject textPanel;
    [SerializeField] private GameObject darkeningPanel;

    [SerializeField] private RectTransform boardTransform;
    [SerializeField] private RectTransform playerRectTransform;

    [SerializeField] private GameObject VictoryTuneGameObj;
    [SerializeField] private GameObject DefeatTuneGameObj;

    [Header("Prefabs")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject blastPrefab;

    [Header("Settings")]
    [SerializeField] private float textTypeSpeed = 0.05f;
    [SerializeField] private float textReadTime = 1.5f;
    [SerializeField] private int startHealthMoney = 40_000_000;
    private int currentHealth = 40_000_000;

    [Header("Attack Settings")]
    public int bulletDmg = 100_000;
    public int blastDmg = 2_000_000;
    [SerializeField] private float blastWarningSeconds = 0.5f;
    [SerializeField] private float blastActiveSeconds = 0.7f;
    [SerializeField] private Color blastWarningColor = Color.red;

    //Timeline vars
    private float timeElapsed = 0;
    private bool phase1entered = false;
    private bool phase2entered = false;
    private bool phase3entered = false;
    private bool phase4entered = false;
    private bool completed = false;

    private int daysPassedToTrigger = 1;

    private void OnEnable()
    {
        //SetDaysPassedToTrigger();
        GameManager.instance.OnDayEnd += OnDayEnd;
    }

    private void OnDisable()
    {
        GameManager.instance.OnDayEnd -= OnDayEnd;
    }

    private void OnDayEnd()
    {
        if (GameManager.instance.daysPassed == (daysPassedToTrigger - 10))
        {
            EventManager.instance.StopEvents();
        }

        if (GameManager.instance.daysPassed == daysPassedToTrigger)
        {
            OpenGame();
            StartCoroutine(Clock());
            StartCoroutine(BossFightStart());
        }
    }

    private IEnumerator Clock()
    {
        while (true)
        {
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator BossFightStart()
    {
        completed = false;
        currentHealth = startHealthMoney;
        timeElapsed = 0;

        GetComponent<AudioSource>().Play();

        yield return StartCoroutine(TypeText("Is this too easy for you?"));
        yield return new WaitForSecondsRealtime(textReadTime);
        yield return StartCoroutine(TypeText("Suffer."));
        yield return new WaitForSecondsRealtime(textReadTime);

        yield return StartCoroutine(MiniGame());

        if (currentHealth <= 0) yield return StartCoroutine(OnLose());
        else yield return OnWin();
    }

    private IEnumerator OnWin()
    {
        VictoryTuneGameObj.GetComponent<AudioSource>().Play();

        yield return StartCoroutine(TypeText("WHAT"));

        yield return new WaitForSecondsRealtime(1.8f);

        yield return StartCoroutine(TypeText("How.."));

        yield return new WaitForSecondsRealtime(0.8f);

        yield return StartCoroutine(TypeText("I'll get you one day."));

        yield return DoRandomEasyBulletAttack();

        yield return StartCoroutine(TypeText("I needa go find more bullets"));

        EventManager.instance.TriggerFlood();
        FinanceManager.instance.Gain(currentHealth);

        StartCoroutine(CloseGame());

        yield return new WaitForSecondsRealtime(VictoryTuneGameObj.GetComponent<AudioSource>().clip.length);
        EventManager.instance.StartEvents();
    }

    private IEnumerator OnLose()
    {
        DefeatTuneGameObj.GetComponent<AudioSource>().Play();

        StartCoroutine(TypeText("Pathetic"));

        yield return new WaitForSecondsRealtime(0.8f);

        StartCoroutine(TypeText("You're no match for me."));

        EventManager.instance.TriggerAsteroidStrike();
        EventManager.instance.TriggerMilitaryInvasion();
        EventManager.instance.TriggerMilitaryInvasion();

        StartCoroutine(CloseGame());
        EventManager.instance.StartEvents();
    }

    private void OpenGame()
    {
        textPanel.SetActive(true);
        darkeningPanel.SetActive(true);
        miniGame.SetActive(true);
        bossFightText.enabled = true;

        Time.timeScale = 0f;

        GameAudioManager.instance.natureSoundsAudioSource.Stop();
        EventManager.instance.StopEvents();
        CityGenerator.instance.StopGeneration();
        GameManager.instance.updateMovementPermissions?.Invoke(false);
        GameManager.instance.OnSetUIVisbility?.Invoke(false);
    }
    private IEnumerator CloseGame()
    {
        textPanel.SetActive(false);
        darkeningPanel.SetActive(false);
        miniGame.SetActive(false);
        bossFightText.enabled = false;

        Time.timeScale = 1f;

        if (TryGetComponent<AudioSource>(out AudioSource audioSource))
        {
            audioSource.Stop();
        }
        GameAudioManager.instance.natureSoundsAudioSource.Play();
        CityGenerator.instance.StartGeneration();
        GameManager.instance.updateMovementPermissions?.Invoke(true);
        GameManager.instance.OnSetUIVisbility?.Invoke(true);

        yield return null;
    }

    //MiniGame functions
    enum CurrentMiniGameDifficulty
    {
        None,
        Chill,
        Normal,
        BulletHell,
        BlastHell,
        Brutal
    }

    private IEnumerator MiniGame()
    {
        CurrentMiniGameDifficulty currentMiniGameDifficulty = CurrentMiniGameDifficulty.None;

        //First hit
        yield return StartCoroutine(DoHardestAttack(false));

        yield return TypeText("You like that?");
        yield return new WaitForSecondsRealtime(textReadTime);

        while (currentHealth > 0 && completed == false)
        {
            currentMiniGameDifficulty = SetCurrentDifficulty();
            float rand = UnityEngine.Random.value;

            switch (currentMiniGameDifficulty){
                case CurrentMiniGameDifficulty.Chill:
                    if (!phase1entered)
                    {
                        phase1entered = true;
                        yield return StartCoroutine(TypeText("I'll let you settle in first"));

                        yield return new WaitForSecondsRealtime(textReadTime);

                        yield return StartCoroutine(TypeText("That was quite rude of me."));

                        yield return StartCoroutine(DoExpandingBulletsAttack(true));

                        yield return StartCoroutine(TypeText("Oops"));
                        yield return new WaitForSecondsRealtime(textReadTime);
                        StartCoroutine(TypeText("..."));
                    }

                    if (rand < 0.02) yield return StartCoroutine(DoRandomBulletAttack());
                    else if (rand < 0.1) yield return StartCoroutine(DoMultipleBlastAttacks());
                    else yield return StartCoroutine(DoRandomEasyBulletAttack());
                    break;

                case CurrentMiniGameDifficulty.None:
                    if (!phase2entered)
                    {
                        phase2entered = true;
                        yield return StartCoroutine(TypeText("Not bad eh?"));
                        yield return new WaitForSecondsRealtime(textReadTime);
                        yield return StartCoroutine(TypeText("Look at you all proud of yourself"));
                        yield return new WaitForSecondsRealtime(textReadTime);
                        yield return StartCoroutine(TypeText("Got more where that came from"));
                        yield return new WaitForSecondsRealtime(textReadTime);
                    }

                    yield return null;
                    break;

                case CurrentMiniGameDifficulty.Normal:
                    if (!phase4entered)
                    {
                        phase4entered = true;
                        yield return StartCoroutine(TypeText("Giving me a real workout"));
                        yield return StartCoroutine(DoNestedSpiralAttack());
                    }

                    if (rand < 0.2) yield return StartCoroutine(DoRandomEasyBulletAttack());
                    else if (rand < 0.6) yield return StartCoroutine(DoRandomBulletAttack());
                    else if (rand < 0.9) yield return StartCoroutine(DoMultipleBlastAttacks());
                    else yield return StartCoroutine(DoRandomBlastAttack());
                    break;

                case CurrentMiniGameDifficulty.BulletHell:
                    if (rand < 0.6) yield return StartCoroutine(DoExpandingBulletsAttack());
                    else if (rand < 0.9) yield return StartCoroutine(DoExpandingBulletsAttack(true));
                    else yield return StartCoroutine(DoMultipleBlastAttacks(false));
                    break;

                case CurrentMiniGameDifficulty.BlastHell:
                    if (rand < 0.9) yield return StartCoroutine(DoMultipleBlastAttacks());
                    else yield return StartCoroutine(DoHardestAttack());
                    break;

                case CurrentMiniGameDifficulty.Brutal:
                    if (!phase3entered)
                    {
                        phase3entered = true;
                        yield return StartCoroutine(TypeText("Good luck."));
                    }
                    if (rand < 0.2) yield return StartCoroutine(DoNestedSpiralAttack());
                    else if (rand < 0.4) yield return StartCoroutine(DoSpiralBulletAttack());
                    else if (rand < 0.5) yield return StartCoroutine(DoExpandingBulletsAttack(true));
                    else if (rand < 0.6) yield return StartCoroutine(DoExpandingBulletsAttack(false));
                    else if (rand < 0.8) yield return StartCoroutine(DoMultipleBlastAttacks());
                    else yield return StartCoroutine(DoHardestAttack());
                    break;
            }

            yield return null;
        }

        yield break;
    }

    private CurrentMiniGameDifficulty SetCurrentDifficulty()
    {
        if (timeElapsed < 41) { return CurrentMiniGameDifficulty.Chill; }
        if (timeElapsed < 48) { return CurrentMiniGameDifficulty.BulletHell; }
        if (timeElapsed < 55) { return CurrentMiniGameDifficulty.None; }
        if (timeElapsed < 89) { return CurrentMiniGameDifficulty.Brutal; }
        if (timeElapsed < 110) { return CurrentMiniGameDifficulty.Normal; }
        if (timeElapsed < 117) { return CurrentMiniGameDifficulty.BlastHell; }

        completed = true;
        return CurrentMiniGameDifficulty.None;
    }

    private IEnumerator DoRandomEasyBulletAttack()
    {
        int numBullets = UnityEngine.Random.Range(3, 6);
        float bulletSpeed = Mathf.Lerp(250, 350, timeElapsed / 48f);

        for (int i = 0; i < numBullets; i++)
        {
            Vector2 spawnPos = GetRandomEdgePosition();
            SpawnBulletTargetedToPlayer(spawnPos, bulletSpeed);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(2f);
    }

    private IEnumerator DoRandomBulletAttack()
    {
        float bulletSpeed = Mathf.Lerp(250, 500, timeElapsed/117f);
        int waves = 5;
        int bulletsPerWave = 10;

        for (int wave = 0; wave < waves; wave++)
        {
            for (int i = 0; i < bulletsPerWave; i++)
            {
                Vector2 spawnPos = GetRandomEdgePosition();
                SpawnBulletTargetedToPlayer(spawnPos, bulletSpeed);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(1.5f);
        }

        yield return new WaitForSecondsRealtime(3f);
    }

    private IEnumerator DoSpiralBulletAttack()
    {
        float bound = 268f;
        float stepSize = 40f;
        float bulletSpeed = Mathf.Lerp(200, 350, timeElapsed / 117f);
        float timeBetweenBullets = 0.02f;

        List<Vector2> perimeterPoints = new List<Vector2>();

        for (float x = -bound; x <= bound; x += stepSize)
        {
            perimeterPoints.Add(new Vector2(x, bound));
        }

        for (float y = -bound; y <= bound; y += stepSize)
        {
            perimeterPoints.Add(new Vector2(bound, y));
        }

        for (float x = bound; x >= -bound; x -= stepSize)
        {
            perimeterPoints.Add(new Vector2(x, -bound));
        }

        for (float y = bound; y >= -bound; y -= stepSize)
        {
            perimeterPoints.Add(new Vector2(-bound, y));
        }

        foreach(Vector2 spawnPos in perimeterPoints)
        {
            Vector2 fireDir = (Vector2.zero - spawnPos).normalized;
            SpawnBullet(spawnPos, fireDir, bulletSpeed, true);
            yield return new WaitForSecondsRealtime(timeBetweenBullets);
        }

        yield return new WaitForSecondsRealtime(3f);
    }
    
    private IEnumerator DoNestedSpiralAttack()
    {
        float bulletSpeed = 250f;
        int totalBursts = 4;
        int bulletsPerBurst = UnityEngine.Random.Range(14, 18);

        for (int burst = 0; burst < totalBursts; burst++)
        {
            float angleStep = 360f / bulletsPerBurst;
            int gapStartIdx = UnityEngine.Random.Range(0, bulletsPerBurst);
            int gapSize = 4;

            for (int i = 0; i < bulletsPerBurst; i++)
            {
                int currentIndex = (gapStartIdx + i) % bulletsPerBurst;
                if (currentIndex < gapSize) continue;

                float angle = i * angleStep * Mathf.Deg2Rad;
                float spawnRadius = 268f;

                Vector2 spawnPos = new Vector2(Mathf.Cos(angle) * spawnRadius, Mathf.Sin(angle) * spawnRadius);

                Vector2 fireDir = -spawnPos.normalized;

                SpawnBullet(spawnPos, fireDir, bulletSpeed, true, playerRectTransform.anchoredPosition);
            }

            yield return new WaitForSecondsRealtime(0.8f);
        }

        yield return new WaitForSecondsRealtime(0.6f);
    }

    private IEnumerator DoExpandingBulletsAttack(bool ring = false)
    {
        float bulletSpeed = Mathf.Lerp(300, 500, timeElapsed / 117f);
        int bulletsToShoot = UnityEngine.Random.Range(12, 14);

        float angleStep = 360f / bulletsToShoot;

        for (int i = 0; i < bulletsToShoot; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;

            Vector2 fireDir = new(Mathf.Cos(angle), Mathf.Sin(angle));

            SpawnBullet(Vector2.zero, fireDir, bulletSpeed);

            if (ring) yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return new WaitForSecondsRealtime(1.4f);
    }

    private IEnumerator DoRandomBlastAttack(bool wait = true)
    {
        float boardHalfWidth = 268f;
        float blastWidth = (boardHalfWidth * 2f) / 5f;

        float randomX = UnityEngine.Random.Range(-boardHalfWidth + (blastWidth / 2f), boardHalfWidth - (blastWidth / 2f));

        GameObject blastObj = Instantiate(blastPrefab, boardTransform);
        RectTransform blastRect = blastObj.GetComponent<RectTransform>();

        blastRect.anchoredPosition = new(randomX, 0f);
        blastRect.sizeDelta = new(blastWidth, boardHalfWidth * 2.4f);

        RawImage blastImg = blastObj.GetComponent<RawImage>();

        //Warning ting
        float warningElapsed = 0f;
        while (warningElapsed < blastWarningSeconds)
        {
            warningElapsed += Time.unscaledDeltaTime;
            float t = warningElapsed / blastWarningSeconds;

            Color pulseCol = blastWarningColor;
            pulseCol.a = Mathf.Lerp(0.15f, 0.3f, Mathf.Sin(t * Mathf.PI * 4) * 0.5f);
            blastImg.color = pulseCol;

            float currentWidth = Mathf.Lerp(blastWidth * 0.8f, blastWidth, Mathf.Sin(t * Mathf.PI * 4) * 0.5f + 0.5f);
            blastRect.sizeDelta = new(currentWidth, boardHalfWidth * 2.4f);

            yield return null;
        }

        blastRect.sizeDelta = new(blastWidth, boardHalfWidth * 2.4f);

        Color blastImgColor = blastImg.color;
        blastImgColor = Color.white;
        blastImgColor.a = 0.8f;
        blastImg.color = blastImgColor;

        //Blast

        float activeElapsed = 0f;
        bool damageDealt = false;

        while (activeElapsed < blastActiveSeconds)
        {
            activeElapsed += Time.unscaledDeltaTime;

            if (!damageDealt)
            {
                float playerX = playerRectTransform.anchoredPosition.x;
                float leftBound = randomX - (blastWidth / 2f);
                float rightBound = randomX + (blastWidth / 2f);

                if (playerX >= leftBound && playerX <= rightBound)
                {
                    TakeDamage(blastDmg);
                    damageDealt = true;
                }
            }

            yield return null;
        }

        float fadeDuration = 0.35f;
        float fadeElapsed = 0f;
        Vector2 startSize = blastRect.sizeDelta;
        Vector2 targetSize = new(blastWidth * 1.8f, boardHalfWidth * 3f);

        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = fadeElapsed / fadeDuration;

            blastRect.sizeDelta = Vector2.Lerp(startSize, targetSize, t);

            Color fadeCol = Color.white;
            fadeCol.a = Mathf.Lerp(0.8f, 0f, t);
            blastImg.color = fadeCol;

            yield return null;
        }

        Destroy(blastObj);
        if (wait) yield return new WaitForSecondsRealtime(1.0f);
    }

    private IEnumerator DoMultipleBlastAttacks(bool wait = true)
    {
        float waitDur = Mathf.Lerp(0.8f, 0.02f, timeElapsed / 117f);
        int repetitions = 5;

        for (int i = 0; i < repetitions; i++)
        {
            StartCoroutine(DoRandomBlastAttack());
            yield return new WaitForSecondsRealtime(waitDur);
        }

        if (wait) yield return new WaitForSecondsRealtime(2f);

    }

    private IEnumerator DoHardestAttack(bool finalWait = true)
    {
        float bulletSpeed = 500f;
        int totalBursts = 4;
        int bulletsPerBurst = 12;

        for (int burst = 0; burst < totalBursts; burst++)
        {
            float angleStep = 360f / bulletsPerBurst;
            int gapStartIdx = UnityEngine.Random.Range(0, bulletsPerBurst);
            int gapSize = 3;

            for (int i = 0; i < bulletsPerBurst; i++)
            {
                int currentIndex = (gapStartIdx + i) % bulletsPerBurst;
                if (currentIndex < gapSize) continue;

                float angle = i * angleStep * Mathf.Deg2Rad;
                float spawnRadius = 268f;

                Vector2 spawnPos = new Vector2(Mathf.Cos(angle) * spawnRadius, Mathf.Sin(angle) * spawnRadius);

                Vector2 fireDir = -spawnPos.normalized;

                SpawnBulletTargetedToPlayer(spawnPos, bulletSpeed);
            }

            yield return new WaitForSecondsRealtime(0.5f);

            StartCoroutine(DoRandomBlastAttack(false));

            yield return new WaitForSecondsRealtime(0.7f);
        }

        yield return StartCoroutine(DoMultipleBlastAttacks());

        if (finalWait) yield return new WaitForSecondsRealtime(1.2f);
    }

    private void SpawnBulletTargetedToPlayer(Vector2 spawnPos, float bulletSpeed, 
        bool dieAtCentre = false, Vector2? customCentre = null)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, boardTransform);
        RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
        bulletRect.anchoredPosition = spawnPos;

        Vector2 targetPos = playerRectTransform.anchoredPosition;
        Vector2 fireDir = (targetPos - spawnPos).normalized;

        MiniGameBullet bulletScript = bulletObj.GetComponent<MiniGameBullet>();
        bulletScript.Init(fireDir, bulletSpeed, bulletDmg, playerRectTransform, 
            dieAtCentre, customCentre);
    }

    private void SpawnBullet(Vector2 spawnPos, Vector2 fireDir,  float bulletSpeed, 
        bool dieAtCentre = false, Vector2? customCentre = null)
    {
        GameObject bulletObj = Instantiate(bulletPrefab, boardTransform);
        RectTransform bulletRect = bulletObj.GetComponent<RectTransform>();
        bulletRect.anchoredPosition = spawnPos;

        MiniGameBullet bulletScript = bulletObj.GetComponent<MiniGameBullet>();
        bulletScript.Init(fireDir, bulletSpeed, bulletDmg, playerRectTransform, 
            dieAtCentre, customCentre);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        healthText.text = $"{currentHealth:N0}";
    }

    //Helper functions

    private void SetDaysPassedToTrigger()
    {
        daysPassedToTrigger = (GameManager.instance.gameDifficulty) switch
        {
            GameSettings.Difficulty.Easy => 700,
            GameSettings.Difficulty.Normal => 600,
            GameSettings.Difficulty.Hard => 500,
            GameSettings.Difficulty.Nightmare => 400,
            _ => 1500
        };

    }

    private IEnumerator TypeText(string text)
    {
        bossFightText.text = "";

        for (int i = 0; i < text.Length; i++)
        {
            bossFightText.text += text[i];

            if (text[i] != ' ')
            {
                //Audio here later
                yield return new WaitForSecondsRealtime(textTypeSpeed);
            }
        }

        yield break;
    }

    private Vector2 GetRandomEdgePosition()
    {
        float boundX = 268f;
        float boundY = 268f;

        int side = UnityEngine.Random.Range(0, 4);

        switch (side)
        {
            case 0: return new(UnityEngine.Random.Range(-boundX, boundX), boundY);
            case 1: return new(UnityEngine.Random.Range(-boundX, boundX), -boundY);
            case 2: return new(-boundX, UnityEngine.Random.Range(-boundY, boundY));
            case 3: return new(boundX, UnityEngine.Random.Range(-boundY, boundY));
            default: return Vector2.zero;
        }
    }
}
