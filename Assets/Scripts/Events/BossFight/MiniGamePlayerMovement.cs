using UnityEngine;
using UnityEngine.InputSystem;

public class MiniGamePlayerMovement : MonoBehaviour
{
    [Header("Dependenices")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Vector2Int bounds = new(268, 268); //can move +/- 268
    [SerializeField] private float moveSpeed = 100f;
    private RectTransform rectTransform;

    private InputAction moveAction;

    void Start()
    {
        InputActionMap playerMap = inputActions.FindActionMap("Player");
        moveAction = playerMap.FindAction("Move");

        moveAction.Enable();

        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        Vector2 inputVector = moveAction.ReadValue<Vector2>().normalized;

        Vector2 currentPos = rectTransform.anchoredPosition;
        currentPos += inputVector * moveSpeed * Time.unscaledDeltaTime;

        currentPos.x = Mathf.Clamp(currentPos.x, -bounds.x, bounds.x);
        currentPos.y = Mathf.Clamp(currentPos.y, -bounds.y, bounds.y);

        rectTransform.anchoredPosition = currentPos;
    }
}
