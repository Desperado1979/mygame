using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// D4表现层壳：直接播放 Justia 动作状态，不再依赖手工连 Animator 参数。
/// 现有战斗/移动逻辑不变，仅把输入和受击状态映射为可见动作。
/// </summary>
[DisallowMultipleComponent]
public class PlayerVisualAnimatorSimple : MonoBehaviour
{
    [Header("References")]
    public Animator targetAnimator;
    public PlayerMoveSimple move;
    public PlayerHealthSimple health;
    public GameObject swordPrefab;

    [Header("Input Mapping")]
    public KeyCode attackKey = KeyCode.Space;
    public KeyCode skillQKey = KeyCode.Q;
    public KeyCode skillRKey = KeyCode.R;
    public KeyCode dashKey = KeyCode.LeftShift;

    [Header("Animator Ownership")]
    [Tooltip("开启后会在运行时清空 Animator Controller，避免控制器自带状态机导致进场自循环；动画只由本脚本的 AnimationClip 驱动。")]
    public bool disableAnimatorControllerAtRuntime = true;

    [Header("Debug")]
    public bool showRuntimeOverlay = true;
    public bool logStateChanges = false;

    [Header("State Names")]
    [Tooltip("若当前 Animator 用的是手工 Player_Runtime，就保留 Idle/Run/AttackState/SkillQState；若切回 Justia 控制器，再填 swordIdleLoop 等。")]
    public string idleStateName = "Idle";
    [Tooltip("当前动作包无 run/walk 时，可继续用 Run 占位；后续补移动动作再换。")]
    public string moveStateName = "Run";
    public string attackStateName = "AttackState";
    public string skillQStateName = "SkillQState";
    public string skillRStateName = "SkillRState";
    public string dashStateName = "DodgeState";
    public string hitStateName = "HitState";
    public string deathStateName = "DeathState";
    public string deathLoopStateName = "DeathLoopState";

    [Header("Animation Clips")]
    [Tooltip("后续找到更自然的站立动作后，直接拖到这里覆盖默认 swordIdleLoop。")]
    public AnimationClip idleClip;
    [Tooltip("后续找到跑步/走路动作后，直接拖到这里覆盖当前占位动作。")]
    public AnimationClip moveClip;
    public AnimationClip attackClip;
    public AnimationClip skillQClip;
    public AnimationClip skillRClip;
    public AnimationClip dashClip;
    public AnimationClip hitClip;
    public AnimationClip deathClip;
    public AnimationClip deathLoopClip;

    [Header("Action Durations")]
    [Tooltip("开启后，攻击/技能/闪避/受击/死亡优先使用对应 AnimationClip 的时长作为锁定时间。")]
    public bool useClipLengthForActionLocks = true;
    public float attackDuration = 0.75f;
    public float skillQDuration = 0.95f;
    public float skillRDuration = 1.0f;
    public float dashDuration = 0.35f;
    public float hitDuration = 0.45f;
    public float deathToLoopDelay = 0.95f;

    [Header("Motion Sampling")]
    public float moveSpeedSmooth = 12f;
    [Tooltip("速度阈值太低会导致进场轻微抖动就被判定为移动，从而播放跑步。")]
    public float minMoveSpeedToFlag = 0.12f;
    public float crossFadeSeconds = 0.08f;

    [Header("Locomotion Source")]
    [Tooltip("优先使用 DoubleL 的站立/跑步（OneHand_Up_Idle + OneHand_Up_Run_F_InPlace）。仅当 Idle/Move 槽为空时自动填，不会覆盖你在 Inspector 里拖好的片段。")]
    public bool preferDoubleLLocomotion = true;
    [Tooltip("关闭后，站立/跑步完全以 Inspector 为准，Awake 不再自动改 idleClip/moveClip。")]
    public bool autoFillLocomotionClips = true;

    [Header("Auto Weapon Mount")]
    public bool autoAttachSwordOnStart = true;
    public string[] preferredWeaponMountNames = new string[]
    {
        "Bip02 Rhand_Weapon",
        "Bip02 right_Wmount",
        "Bip02 Rhand_WeaponOpp",
        "Bip02 R Hand",
    };
    public Vector3 swordLocalPosition = Vector3.zero;
    public Vector3 swordLocalEuler = Vector3.zero;
    public Vector3 swordLocalScale = Vector3.one;

    Vector3 _lastPos;
    float _smoothedSpeed;
    int _lastHp = -1;
    string _currentState = "";
    [SerializeField] string currentClipName = "";
    [SerializeField] float currentClipLength = 0f;
    float _actionLockUntil;
    bool _deathLoopPlayed;
    PlayableGraph _graph;
    AnimationPlayableOutput _output;
    AnimationClipPlayable _currentPlayable;

    static Animator FindBodyAnimator(Transform root)
    {
        if (root == null)
            return null;
        return root.GetComponentInChildren<Animator>(true);
    }

    void Reset()
    {
        if (targetAnimator == null)
            targetAnimator = FindBodyAnimator(transform);
        if (move == null)
            move = GetComponent<PlayerMoveSimple>();
        if (health == null)
            health = GetComponent<PlayerHealthSimple>();
#if UNITY_EDITOR
        if (swordPrefab == null)
            swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mori_motionAsset/Model/simple_sword.prefab");
        LoadDefaultClipsInEditor();
#endif
    }

    void ResolveRefs()
    {
        if (targetAnimator == null)
            targetAnimator = FindBodyAnimator(transform);
        if (move == null)
            move = GetComponent<PlayerMoveSimple>();
        if (health == null)
            health = GetComponent<PlayerHealthSimple>();
#if UNITY_EDITOR
        if (swordPrefab == null)
            swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Mori_motionAsset/Model/simple_sword.prefab");
        LoadDefaultClipsInEditor();
#endif
    }

    void Awake()
    {
        if (!ResolveDuplicateInstanceOnSameObject())
            return;

        ResolveRefs();

        if (targetAnimator != null)
        {
            targetAnimator.applyRootMotion = false;
            if (disableAnimatorControllerAtRuntime)
                targetAnimator.runtimeAnimatorController = null;
        }

        _lastPos = transform.position;
        if (health != null)
            _lastHp = health.CurrentHp;
    }

    bool ResolveDuplicateInstanceOnSameObject()
    {
        PlayerVisualAnimatorSimple[] all = GetComponents<PlayerVisualAnimatorSimple>();
        if (all == null || all.Length <= 1)
            return true;

        PlayerVisualAnimatorSimple winner = all[0];
        int winnerScore = all[0].GetConfiguredClipScore();
        for (int i = 1; i < all.Length; i++)
        {
            PlayerVisualAnimatorSimple candidate = all[i];
            int score = candidate.GetConfiguredClipScore();
            if (score > winnerScore)
            {
                winner = candidate;
                winnerScore = score;
                continue;
            }

            if (score == winnerScore && winner.targetAnimator == null && candidate.targetAnimator != null)
            {
                winner = candidate;
                winnerScore = score;
            }
        }

        if (winner != this)
        {
            enabled = false;
            return false;
        }

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i] != this)
                all[i].enabled = false;
        }
        return true;
    }

    int GetConfiguredClipScore()
    {
        int score = 0;
        if (idleClip != null) score++;
        if (moveClip != null) score++;
        if (attackClip != null) score++;
        if (skillQClip != null) score++;
        if (skillRClip != null) score++;
        if (dashClip != null) score++;
        if (hitClip != null) score++;
        if (deathClip != null) score++;
        if (deathLoopClip != null) score++;
        return score;
    }

    void Start()
    {
        ResolveRefs();
        if (targetAnimator != null && disableAnimatorControllerAtRuntime)
            targetAnimator.runtimeAnimatorController = null;

        TryAttachSword();
        PlayStateImmediate(idleStateName);
#if UNITY_EDITOR
        if (idleClip != null || moveClip != null)
        {
            string idlePath = idleClip != null ? AssetDatabase.GetAssetPath(idleClip) : "null";
            string movePath = moveClip != null ? AssetDatabase.GetAssetPath(moveClip) : "null";
            Debug.Log($"[PlayerVisualAnimatorSimple] Locomotion clips: idle={idlePath} move={movePath}");
        }
#endif
    }

    void OnDisable()
    {
        DestroyGraph();
    }

    void OnDestroy()
    {
        DestroyGraph();
    }

    void Update()
    {
        if (targetAnimator == null)
            return;

        UpdateMotionSample();
        UpdateHealthDrivenStates();
        if (health != null && health.IsDead)
            return;

        UpdateActionDrivenStates();
        UpdateLocomotionState();
    }

    void UpdateMotionSample()
    {
        Vector3 now = transform.position;
        float rawSpeed = (now - _lastPos).magnitude / Mathf.Max(0.0001f, Time.deltaTime);
        _lastPos = now;
        float lerpT = 1f - Mathf.Exp(-Mathf.Max(0.01f, moveSpeedSmooth) * Time.deltaTime);
        _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, rawSpeed, lerpT);
    }

    void UpdateHealthDrivenStates()
    {
        if (health == null)
            return;

        if (health.IsDead)
        {
            if (_currentState != deathStateName && _currentState != deathLoopStateName)
            {
                PlayLockedState(deathStateName, GetActionLockDuration(deathStateName, deathToLoopDelay));
                _deathLoopPlayed = false;
            }
            else if (!_deathLoopPlayed && Time.time >= _actionLockUntil)
            {
                CrossFadeTo(deathLoopStateName);
                _deathLoopPlayed = true;
            }
        }

        int hp = health.CurrentHp;
        if (_lastHp >= 0 && hp < _lastHp && Time.time >= _actionLockUntil)
            PlayLockedState(hitStateName, GetActionLockDuration(hitStateName, hitDuration));
        _lastHp = hp;
    }

    void UpdateActionDrivenStates()
    {
        if (Time.time < _actionLockUntil)
            return;

        bool isMoving = _smoothedSpeed >= minMoveSpeedToFlag;
        if (isMoving && Input.GetKeyDown(dashKey))
        {
            PlayLockedState(dashStateName, GetActionLockDuration(dashStateName, dashDuration));
            return;
        }

        if (Input.GetKeyDown(attackKey))
        {
            PlayLockedState(attackStateName, GetActionLockDuration(attackStateName, attackDuration));
            return;
        }

        if (Input.GetKeyDown(skillQKey))
        {
            PlayLockedState(skillQStateName, GetActionLockDuration(skillQStateName, skillQDuration));
            return;
        }

        if (Input.GetKeyDown(skillRKey))
            PlayLockedState(skillRStateName, GetActionLockDuration(skillRStateName, skillRDuration));
    }

    void UpdateLocomotionState()
    {
        if (Time.time < _actionLockUntil)
            return;

        bool isMoving = _smoothedSpeed >= minMoveSpeedToFlag;
        string wanted = isMoving ? moveStateName : idleStateName;
        if (_currentState != wanted)
            CrossFadeTo(wanted);
    }

    void PlayLockedState(string stateName, float duration)
    {
        if (string.IsNullOrEmpty(stateName))
            return;
        CrossFadeTo(stateName);
        _actionLockUntil = Time.time + Mathf.Max(0.02f, duration);
    }

    void PlayStateImmediate(string stateName)
    {
        AnimationClip clip = GetClipForState(stateName);
        if (clip == null || targetAnimator == null)
            return;
        PlayClip(clip);
        _currentState = stateName;
    }

    void CrossFadeTo(string stateName)
    {
        AnimationClip clip = GetClipForState(stateName);
        if (clip == null || targetAnimator == null)
            return;
        // 同状态名时若已在播同一 AnimationClip 则跳过；否则必须刷新（例如 Play 下换了 idleClip/moveClip，状态仍是 Idle/Run）
        if (_currentState == stateName && _currentPlayable.IsValid())
        {
            AnimationClip playing = _currentPlayable.GetAnimationClip();
            if (playing == clip)
                return;
        }

        PlayClip(clip);
        if (logStateChanges)
            Debug.Log($"[PlayerVisualAnimatorSimple] State -> {stateName} clip={clip.name}");
        _currentState = stateName;
    }

    void TryAttachSword()
    {
        if (!autoAttachSwordOnStart || swordPrefab == null || targetAnimator == null)
            return;

        Transform root = targetAnimator.transform;
        if (FindDeepChild(root, "simple_sword") != null)
            return;

        Transform mount = null;
        for (int i = 0; i < preferredWeaponMountNames.Length; i++)
        {
            mount = FindDeepChild(root, preferredWeaponMountNames[i]);
            if (mount != null)
                break;
        }
        if (mount == null)
            return;

        GameObject sword = Instantiate(swordPrefab, mount, false);
        sword.name = "simple_sword";
        sword.transform.localPosition = swordLocalPosition;
        sword.transform.localEulerAngles = swordLocalEuler;
        sword.transform.localScale = swordLocalScale;
    }

    AnimationClip GetClipForState(string stateName)
    {
        if (stateName == idleStateName)
            return idleClip;
        if (stateName == moveStateName)
            return moveClip != null ? moveClip : idleClip;
        if (stateName == attackStateName)
            return attackClip;
        if (stateName == skillQStateName)
            return skillQClip != null ? skillQClip : attackClip;
        if (stateName == skillRStateName)
            return skillRClip != null ? skillRClip : skillQClip;
        if (stateName == dashStateName)
            return dashClip != null ? dashClip : moveClip;
        if (stateName == hitStateName)
            return hitClip != null ? hitClip : idleClip;
        if (stateName == deathStateName)
            return deathClip;
        if (stateName == deathLoopStateName)
            return deathLoopClip != null ? deathLoopClip : deathClip;
        return null;
    }

    void PlayClip(AnimationClip clip)
    {
        if (clip == null || targetAnimator == null)
            return;

        EnsureGraph();
        if (_currentPlayable.IsValid())
            _graph.DestroyPlayable(_currentPlayable);

        _currentPlayable = AnimationClipPlayable.Create(_graph, clip);
        _currentPlayable.SetApplyFootIK(false);
        _output.SetSourcePlayable(_currentPlayable);
        currentClipName = clip.name;
        currentClipLength = clip.length;
        if (!_graph.IsPlaying())
            _graph.Play();
    }

    float GetActionLockDuration(string stateName, float fallbackDuration)
    {
        if (!useClipLengthForActionLocks)
            return Mathf.Max(0.02f, fallbackDuration);

        AnimationClip clip = GetClipForState(stateName);
        if (clip == null)
            return Mathf.Max(0.02f, fallbackDuration);

        return Mathf.Max(0.02f, clip.length);
    }

    void EnsureGraph()
    {
        if (_graph.IsValid())
            return;

        _graph = PlayableGraph.Create("PlayerVisualAnimatorSimple");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _output = AnimationPlayableOutput.Create(_graph, "Animation", targetAnimator);
    }

    void DestroyGraph()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }
        _currentPlayable = default;
        currentClipName = "";
        currentClipLength = 0f;
    }

    void OnGUI()
    {
        if (!showRuntimeOverlay)
            return;
        if (targetAnimator == null)
            return;

        float lockRemain = Mathf.Max(0f, _actionLockUntil - Time.time);
        string line = $"Anim:{_currentState}  Clip:{currentClipName}({currentClipLength:F2}s)  Spd:{_smoothedSpeed:F2}  Lock:{lockRemain:F2}";
        GUI.Label(new Rect(10, 10, 1200, 24), line);
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Default Clips")]
    void LoadDefaultClipsInEditor()
    {
        // 站立/跑步：仅在允许自动填充时执行，且只在槽位为空时填，不覆盖 Inspector 里已拖好的片段。
        if (autoFillLocomotionClips)
        {
            if (preferDoubleLLocomotion)
            {
                if (idleClip == null)
                {
                    AnimationClip dlIdle = PickFirstExisting(
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Idle.anim"
                    );
                    if (dlIdle != null)
                        idleClip = dlIdle;
                }
                if (moveClip == null)
                {
                    AnimationClip dlRun = PickFirstExisting(
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F.anim",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F.anim"
                    );
                    if (dlRun != null)
                        moveClip = dlRun;
                }
            }

            const string CustomAnimFolder = "Assets/CustomPackage/Animations";

            if (idleClip == null)
                idleClip = PickFirstExisting(
                    "Assets/CustomPackage/Animations/Male_Idle.anim",
                    "Assets/CustomPackage/Animations/Woman_Idle.anim"
                ) ?? FindClipByHints(CustomAnimFolder, "Idle", "idle", "stand", "swordIdleLoop");

            if (moveClip == null)
                moveClip = PickFirstExisting(
                    "Assets/CustomPackage/Animations/Male_Run.anim",
                    "Assets/CustomPackage/Animations/Woman_run.anim",
                    "Assets/CustomPackage/Animations/woman_walking.anim",
                    "Assets/CustomPackage/Animations/Male_Walk.anim"
                ) ?? FindClipByHints(CustomAnimFolder, "Run", "run", "Walk", "walk", "Jog", "jog", "swordIdleLoop");
        }

        if (attackClip == null)
            attackClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordAttack01", "attack");
        if (skillQClip == null)
            skillQClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordSkillAttack01", "skill");
        if (skillRClip == null)
            skillRClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordSkillAttack01Fly", "fly");
        if (dashClip == null)
            dashClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordDodge", "dodge", "dash");
        if (hitClip == null)
            hitClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordDamageLow", "hit", "damage");
        if (deathClip == null)
            deathClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordDeath", "death");
        if (deathLoopClip == null)
            deathLoopClip = FindClipByHints("Assets/Mori_motionAsset/AnimationFBX", "swordDeathLoop", "deathloop");
    }

    static AnimationClip PickFirstExisting(params string[] assetPaths)
    {
        for (int i = 0; i < assetPaths.Length; i++)
        {
            string p = assetPaths[i];
            if (string.IsNullOrWhiteSpace(p))
                continue;
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p);
            if (clip != null)
                return clip;
        }
        return null;
    }

    static bool IsClipInFolder(AnimationClip clip, string folder)
    {
        if (clip == null || string.IsNullOrEmpty(folder))
            return false;
        string path = AssetDatabase.GetAssetPath(clip);
        if (string.IsNullOrEmpty(path))
            return false;
        return path.Replace('\\', '/').StartsWith(folder, System.StringComparison.OrdinalIgnoreCase);
    }

    static AnimationClip FindClipByHints(string preferredFolder, params string[] hints)
    {
        for (int i = 0; i < hints.Length; i++)
        {
            string hint = hints[i];
            if (string.IsNullOrWhiteSpace(hint))
                continue;

            string[] guids = AssetDatabase.FindAssets(hint + " t:AnimationClip");
            for (int j = 0; j < guids.Length; j++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[j]);
                if (!string.IsNullOrEmpty(preferredFolder) && !path.Replace('\\', '/').StartsWith(preferredFolder, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null && !clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                    return clip;
            }
        }

        return null;
    }
#endif

    static Transform FindDeepChild(Transform root, string exactName)
    {
        if (root == null || string.IsNullOrEmpty(exactName))
            return null;
        if (root.name == exactName)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform hit = FindDeepChild(root.GetChild(i), exactName);
            if (hit != null)
                return hit;
        }
        return null;
    }
}
