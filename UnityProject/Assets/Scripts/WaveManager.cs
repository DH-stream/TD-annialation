using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public delegate void WaveStartHandler();
    public static event WaveStartHandler OnWaveStart;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [ContextMenu("Start Wave")]
    public void StartWave()
    {
        Debug.Log("Wave Startad!");
        OnWaveStart?.Invoke();
    }
}
