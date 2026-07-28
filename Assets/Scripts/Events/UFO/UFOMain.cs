using System;
using System.Collections;
using UnityEngine;

public class UFOMain : MonoBehaviour
{
    [Header("UFO")]
    [SerializeField] private int startHeight;
    [SerializeField] private float finalHeight = 18f;
    [SerializeField] private float descentSpeed = 3f;
    [SerializeField] private float ascentSpeed = 10f;
    [SerializeField] private GameObject _UFOObj;
    [SerializeField] private GameObject _UFOBeamObj;
    [SerializeField] private GameObject _UFOPulseObj;

    private UFOPulseProjection _UFOPulseProjectionScript;
    private Action lclHasInvasionStarted;

    void Start()
    {
        _UFOObj.transform.position = new Vector3(_UFOBeamObj.transform.position.x, 100f, _UFOBeamObj.transform.position.z);

        _UFOPulseProjectionScript = GetComponent<UFOPulseProjection>();
        if (_UFOPulseProjectionScript == null) Debug.LogError("No UFO Pulse Projection Script Found!");
    }

    public void StartInvasion(Action hasInvasionStarted)
    {
        lclHasInvasionStarted = hasInvasionStarted;

        StartCoroutine(UFOMoveInTransition());
    }

    private void UpdateBeamAndPulseVisbility(bool isEnabled)
    {
        if (_UFOBeamObj) { _UFOBeamObj.SetActive(isEnabled); } else Debug.LogError("No UFO Beam Object Found!");
        if (_UFOPulseObj) { _UFOPulseObj.SetActive(isEnabled); }
    }

    private IEnumerator UFOMoveInTransition()
    {
        Debug.Log("Descending!");

        while (_UFOObj.transform.position.y > finalHeight)
        {
            _UFOObj.transform.Translate(new Vector3(0, 0, -descentSpeed) * Time.deltaTime, Space.Self);
            yield return null;
        }

        UpdateBeamAndPulseVisbility(true);

        if (_UFOPulseProjectionScript != null) _UFOPulseProjectionScript.StartPulseProjections(TriggerUFOMoveOut); 
        else TriggerUFOMoveOut();
    }

    private void TriggerUFOMoveOut()
    {
        lclHasInvasionStarted?.Invoke();
        StartCoroutine(UFOMoveOut());
    }

    private IEnumerator UFOMoveOut()
    {
        Debug.Log("Ascending!");

        yield return null;
        UpdateBeamAndPulseVisbility(false);

        while (_UFOObj.transform.position.y < 1000)
        {
            _UFOObj.transform.Translate(new Vector3(0, 0, ascentSpeed) * Time.deltaTime, Space.Self);
            yield return null;
        }

        Destroy(gameObject);
    }
}
