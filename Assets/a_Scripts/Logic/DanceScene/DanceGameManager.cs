using UnityEngine;

/// <summary>
/// 音游总控 —— 加载谱面、启动时间轴，并串联生成/判定/计分/UI 各系统。
/// </summary>
public class DanceGameManager : MonoBehaviour
{
    public static DanceGameManager Instance { get; private set; }

    [Header("谱面")]
    [SerializeField] private string _chartResourcePath = "Charts/Song1";

    [Header("系统引用")]
    [SerializeField] private Conductor _conductor;
    [SerializeField] private NoteSpawner _noteSpawner;
    [SerializeField] private JudgeManager _judgeManager;
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private DanceGameUI _ui;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        DanceChart chart = ChartLoader.Load(_chartResourcePath);
        if (chart == null) return;

        if (_noteSpawner == null || _conductor == null)
        {
            Debug.LogError("[DanceGameManager] 系统引用未完整配置（NoteSpawner / Conductor 必填）");
            return;
        }

        _noteSpawner.LoadChart(chart);

        if (_judgeManager != null)
            _judgeManager.OnJudged += HandleJudged;
        if (_scoreManager != null && _ui != null)
            _scoreManager.OnScoreChanged += _ui.OnScoreChanged;

        _conductor.Begin();
    }

    private void HandleJudged(NoteType type, JudgeResult result)
    {
        if (_scoreManager != null) _scoreManager.RegisterJudge(result);
        if (_ui != null) _ui.OnJudged(result);
    }
}
