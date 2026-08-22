using UnityEngine;

public class MiniGameBullet : MonoBehaviour
{
    [SerializeField] private Vector2Int bounds = new(268, 268); //can move +/- 268
    [SerializeField] private float hitDistance = 15f;
    private RectTransform playerRect;
    private RectTransform rectTransform;
    private Vector2 direction;
    private Vector2 centre;
    private float speed;
    private int damage;

    private bool dieAtCentre = false;
    private float prevDistance = float.MaxValue;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Init(Vector2 dir, float bulletSpeed, int bulletDmg, RectTransform playerRectTransform, bool shouldDieAtCentre = false, Vector2? customCentre = null)
    {
        direction = dir;
        speed = bulletSpeed;
        damage = bulletDmg;
        playerRect = playerRectTransform;
        dieAtCentre = shouldDieAtCentre;
        if (customCentre.HasValue) centre = customCentre.Value;
    }

    private void Update()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos += direction * speed * Time.unscaledDeltaTime;
        rectTransform.anchoredPosition = pos;

        if (Mathf.Abs(pos.x) > bounds.x || Mathf.Abs(pos.y) > bounds.y)
        {
            Destroy(gameObject);
        }

        if (playerRect != null)
        {
            float distance = Vector2.Distance(pos, playerRect.anchoredPosition);
            if (distance > hitDistance) return;

            FindAnyObjectByType<BossFight>().TakeDamage(damage);
            Destroy(gameObject);
        }

        if (dieAtCentre)
        {
            float distance = Vector2.Distance(pos, centre);

            if (distance > prevDistance)
            {
                Destroy(gameObject);
            }

            prevDistance = distance;
        }
    }
}
