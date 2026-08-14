using UnityEngine;

public class PlayerCameraScript : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Camera playerCamera;
    private bool showTrees = true;

    private void Start()
    {
        if (playerCamera == null) playerCamera = GetComponent<Camera>();

        showTrees = PlayerPrefs.GetInt("TreeVisibility", 1) == 1;
        HandleTreeVisibility(showTrees);
    }

    private void OnEnable()
    {
        if (GameManager.instance != null) GameManager.instance.OnTreeVisibilityChanged += HandleTreeVisibility;
    }

    private void OnDisable()
    {
        if (GameManager.instance != null) GameManager.instance.OnTreeVisibilityChanged -= HandleTreeVisibility;
    }

    private void HandleTreeVisibility(bool visible)
    {
        if (playerCamera == null) return;
        if (visible)
        {
            playerCamera.cullingMask |= (1 << LayerMask.NameToLayer("Trees"));
        }
        else
        {
            playerCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Trees"));
        }
    }
}
