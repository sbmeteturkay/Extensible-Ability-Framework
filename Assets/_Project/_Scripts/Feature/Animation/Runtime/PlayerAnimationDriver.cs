using System;
using CaseStudy.Shared.AbilitySystem.Contracts;
using CaseStudy.Feature.Animation.Data;
using CaseStudy.Shared.AbilitySystem.Events.Domain;
using CaseStudy.Shared.AbilitySystem.Events.Presentation;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.Animation.Runtime
{
    /// <summary>
    /// Single-component animation driver.
    /// Computes movement in FixedUpdate, writes animator params in Update,
    /// and reacts to ability/loadout events.
    /// </summary>
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        private const int DEFAULT_ABILITY_SLOT_COUNT = 8;
        private const float DEFAULT_ABILITY_SPEED = 1f;
        private const float MIN_REFERENCE_SPEED = 0.0001f;
        private const float MIN_FIXED_DELTA = 0.0001f;
        private const float START_MOVING_MULTIPLIER = 1.15f;
        private const float FLOAT_WRITE_EPSILON = 0.001f;

        [Header("References")]
        [SerializeField] private Transform _ownerTransform;
        [SerializeField] private Rigidbody _ownerRigidbody;
        [SerializeField] private Animator _ownerAnimator;
        [SerializeField] private PlayerAnimationConfigSO _config;
        
        private ISubscriber<AbilityTriggeredEvent> _abilityTriggeredSubscriber;
        private IAbilityInputGate _abilityInputGate;
        private ISubscriber<AbilityLoadoutSlotAssignedEvent> _loadoutSlotAssignedSubscriber;
        private IDisposable _abilityTriggeredSubscription;
        private IDisposable _loadoutSlotAssignedSubscription;

        private AnimatorOverrideController _runtimeOverrideController;
        private bool _didLogMissingOverrideController;

        private readonly float[] _abilitySpeedBySlot = new float[DEFAULT_ABILITY_SLOT_COUNT];

        private Vector3 _lastFixedWorldPosition;
        private bool _hasLastFixedWorldPosition;

        private float _cachedMoveX;
        private float _cachedMoveY;
        private float _cachedSpeed;
        private bool _cachedIsMoving;
        private float _lastAboveThresholdTime;

        private int _moveXHash;
        private int _moveYHash;
        private int _speedHash;
        private int _isMovingHash;
        private int _abilityUsedTriggerHash;
        private int _abilityIndexHash;
        private int _abilityAnimationSpeedHash;

        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasSpeed;
        private bool _hasIsMoving;
        private bool _hasAbilityUsedTrigger;
        private bool _hasAbilityIndex;
        private bool _hasAbilityAnimationSpeed;

        private bool _pendingAbilityUsedTrigger;
        private int _pendingAbilityIndex;
        private bool _hasPendingAbilityIndex;
        private float _pendingAbilityAnimationSpeed = DEFAULT_ABILITY_SPEED;

        private float _lastSentMoveX;
        private float _lastSentMoveY;
        private float _lastSentSpeed;
        private bool _lastSentIsMoving;
        private bool _hasLastSentMoveX;
        private bool _hasLastSentMoveY;
        private bool _hasLastSentSpeed;
        private bool _hasLastSentIsMoving;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            if (resolver == null)
            {
                return;
            }

            resolver.TryResolve<ISubscriber<AbilityTriggeredEvent>>(out _abilityTriggeredSubscriber);
            resolver.TryResolve<ISubscriber<AbilityLoadoutSlotAssignedEvent>>(out _loadoutSlotAssignedSubscriber);
            resolver.TryResolve<IAbilityInputGate>(out _abilityInputGate);
        }

        private void Awake()
        {
            if (_ownerTransform == null)
            {
                _ownerTransform = transform;
            }

            if (_ownerRigidbody == null)
            {
                _ownerRigidbody = GetComponent<Rigidbody>();
            }

            if (_ownerAnimator == null)
            {
                _ownerAnimator = GetComponentInChildren<Animator>();
            }

            CacheAnimatorParameterHashes();
            InitializeAbilitySpeeds();

            if (_ownerTransform != null)
            {
                _lastFixedWorldPosition = _ownerTransform.position;
                _hasLastFixedWorldPosition = true;
            }

            _lastAboveThresholdTime = -999f;
            EnsureRuntimeOverrideController();
            EnsureInputGateReference();
        }

        private void OnEnable()
        {
            EnsureInputGateReference();
            SubscribeAbilityEvents();
        }

        private void OnDisable()
        {
            _abilityTriggeredSubscription?.Dispose();
            _abilityTriggeredSubscription = null;

            _loadoutSlotAssignedSubscription?.Dispose();
            _loadoutSlotAssignedSubscription = null;

            ResetMotionStateOnDisable();
        }

        private void ResetMotionStateOnDisable()
        {
            _cachedMoveX = 0f;
            _cachedMoveY = 0f;
            _cachedSpeed = 0f;
            _cachedIsMoving = false;

            _pendingAbilityUsedTrigger = false;
            _hasPendingAbilityIndex = false;
            _pendingAbilityAnimationSpeed = DEFAULT_ABILITY_SPEED;

            _hasLastFixedWorldPosition = false;
            _lastAboveThresholdTime = -999f;

            _hasLastSentMoveX = false;
            _hasLastSentMoveY = false;
            _hasLastSentSpeed = false;
            _hasLastSentIsMoving = false;

            if (_ownerAnimator == null)
            {
                return;
            }

            if (_hasMoveX)
            {
                _ownerAnimator.SetFloat(_moveXHash, 0f);
            }

            if (_hasMoveY)
            {
                _ownerAnimator.SetFloat(_moveYHash, 0f);
            }

            if (_hasSpeed)
            {
                _ownerAnimator.SetFloat(_speedHash, 0f);
            }

            if (_hasIsMoving)
            {
                _ownerAnimator.SetBool(_isMovingHash, false);
            }
        }

        private void FixedUpdate()
        {
            if (!HasValidSetup())
            {
                return;
            }

            Vector3 worldVelocity = CalculateWorldVelocityFromFixedDelta();
            worldVelocity.y = 0f;

            float threshold = Mathf.Max(0f, _config.MovingThreshold);
            float speedMagnitude = worldVelocity.magnitude;

            if (speedMagnitude > threshold)
            {
                _lastAboveThresholdTime = Time.time;
            }

            float holdSeconds = Mathf.Max(0f, _config.MovingHoldSeconds);
            float startThreshold = threshold * START_MOVING_MULTIPLIER;
            bool aboveStartThreshold = speedMagnitude > startThreshold;
            bool withinHoldWindow = (Time.time - _lastAboveThresholdTime) <= holdSeconds;
            _cachedIsMoving = aboveStartThreshold || withinHoldWindow;

            if (speedMagnitude <= threshold)
            {
                worldVelocity = Vector3.zero;
            }

            Vector3 localVelocity = _ownerTransform.InverseTransformDirection(worldVelocity);
            float referenceSpeed = Mathf.Max(MIN_REFERENCE_SPEED, _config.ReferenceMoveSpeed);

            _cachedMoveX = SnapIfNearZero(Mathf.Clamp(localVelocity.x / referenceSpeed, -1f, 1f));
            _cachedMoveY = SnapIfNearZero(Mathf.Clamp(localVelocity.z / referenceSpeed, -1f, 1f));
            _cachedSpeed = SnapIfNearZero(Mathf.Clamp01(worldVelocity.magnitude / referenceSpeed));
        }

        private void Update()
        {
            if (!HasValidSetup())
            {
                return;
            }

            WriteMotionParameters();
            ConsumeAbilitySignals();
        }

        private void EnsureInputGateReference()
        {
            if (_abilityInputGate != null)
            {
                return;
            }

            Transform current = transform;
            while (current != null)
            {
                MonoBehaviour[] components = current.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < components.Length; i++)
                {
                    if (components[i] is IAbilityInputGate inputGate)
                    {
                        _abilityInputGate = inputGate;
                        return;
                    }
                }

                current = current.parent;
            }
        }

        private void SubscribeAbilityEvents()
        {
            _abilityTriggeredSubscription?.Dispose();
            _abilityTriggeredSubscription = null;

            _loadoutSlotAssignedSubscription?.Dispose();
            _loadoutSlotAssignedSubscription = null;

            if (_abilityTriggeredSubscriber != null)
            {
                _abilityTriggeredSubscription = _abilityTriggeredSubscriber.Subscribe(OnAbilityTriggered);
            }

            if (_loadoutSlotAssignedSubscriber != null)
            {
                _loadoutSlotAssignedSubscription = _loadoutSlotAssignedSubscriber.Subscribe(OnLoadoutSlotAssigned);
            }
        }

        private void OnAbilityTriggered(AbilityTriggeredEvent evt)
        {
            if (_abilityInputGate != null && !_abilityInputGate.IsInputGateOpen)
            {
                return;
            }
            if (_hasAbilityUsedTrigger)
            {
                _pendingAbilityUsedTrigger = true;
            }

            if (_hasAbilityIndex)
            {
                _pendingAbilityIndex = evt.AbilityIndex;
                _hasPendingAbilityIndex = true;
            }

            if (_hasAbilityAnimationSpeed)
            {
                _pendingAbilityAnimationSpeed = ResolveAbilitySpeed(evt.AbilityIndex);
            }
        }

        private void OnLoadoutSlotAssigned(AbilityLoadoutSlotAssignedEvent evt)
        {
            if (_abilityInputGate != null && !_abilityInputGate.IsInputGateOpen)
            {
                return;
            }
            if (evt.SlotIndex < 0)
            {
                return;
            }

            if (evt.SlotIndex < _abilitySpeedBySlot.Length)
            {
                _abilitySpeedBySlot[evt.SlotIndex] = evt.AbilityAnimationSpeed;
            }

            if (_config == null || _config.AbilitySlotSourceClips == null || evt.SlotIndex >= _config.AbilitySlotSourceClips.Length)
            {
                return;
            }

            AnimationClip sourceClip = _config.AbilitySlotSourceClips[evt.SlotIndex];
            if (sourceClip == null)
            {
                return;
            }

            if (!EnsureRuntimeOverrideController())
            {
                return;
            }

            AnimationClip targetClip = evt.AbilityAnimationClip != null ? evt.AbilityAnimationClip : sourceClip;
            _runtimeOverrideController[sourceClip] = targetClip;
        }

        private void ConsumeAbilitySignals()
        {
            if (_hasAbilityUsedTrigger && _pendingAbilityUsedTrigger)
            {
                _ownerAnimator.SetTrigger(_abilityUsedTriggerHash);
                _pendingAbilityUsedTrigger = false;
            }

            if (_hasAbilityIndex && _hasPendingAbilityIndex)
            {
                _ownerAnimator.SetFloat(_abilityIndexHash, _pendingAbilityIndex);
                _hasPendingAbilityIndex = false;
            }

            if (_hasAbilityAnimationSpeed)
            {
                _ownerAnimator.SetFloat(_abilityAnimationSpeedHash, _pendingAbilityAnimationSpeed);
            }
        }

        private void WriteMotionParameters()
        {
            float deltaTime = Time.deltaTime;

            if (_hasMoveX)
            {
                TrySetFloat(ref _hasLastSentMoveX, ref _lastSentMoveX, _moveXHash, _cachedMoveX, _config.MoveXDampTime, deltaTime);
            }

            if (_hasMoveY)
            {
                TrySetFloat(ref _hasLastSentMoveY, ref _lastSentMoveY, _moveYHash, _cachedMoveY, _config.MoveYDampTime, deltaTime);
            }

            if (_hasSpeed)
            {
                TrySetFloat(ref _hasLastSentSpeed, ref _lastSentSpeed, _speedHash, _cachedSpeed, _config.SpeedDampTime, deltaTime);
            }

            if (_hasIsMoving)
            {
                if (!_hasLastSentIsMoving || _lastSentIsMoving != _cachedIsMoving)
                {
                    _ownerAnimator.SetBool(_isMovingHash, _cachedIsMoving);
                    _lastSentIsMoving = _cachedIsMoving;
                    _hasLastSentIsMoving = true;
                }
            }
        }

        private void TrySetFloat(
            ref bool hasLastValue,
            ref float lastValue,
            int parameterHash,
            float nextValue,
            float dampTime,
            float deltaTime)
        {
            if (hasLastValue && Mathf.Abs(nextValue - lastValue) <= FLOAT_WRITE_EPSILON)
            {
                return;
            }

            if (_config.UseDamping)
            {
                _ownerAnimator.SetFloat(parameterHash, nextValue, dampTime, deltaTime);
            }
            else
            {
                _ownerAnimator.SetFloat(parameterHash, nextValue);
            }

            lastValue = nextValue;
            hasLastValue = true;
        }

        private Vector3 CalculateWorldVelocityFromFixedDelta()
        {
            Vector3 currentWorldPosition = _ownerTransform.position;
            if (!_hasLastFixedWorldPosition)
            {
                _lastFixedWorldPosition = currentWorldPosition;
                _hasLastFixedWorldPosition = true;
                return Vector3.zero;
            }

            float deltaTime = Mathf.Max(MIN_FIXED_DELTA, Time.fixedDeltaTime);
            Vector3 worldVelocity = (currentWorldPosition - _lastFixedWorldPosition) / deltaTime;
            _lastFixedWorldPosition = currentWorldPosition;
            return worldVelocity;
        }

        private bool HasValidSetup()
        {
            return _ownerTransform != null
                   && _ownerRigidbody != null
                   && _ownerAnimator != null
                   && _config != null;
        }

        private bool EnsureRuntimeOverrideController()
        {
            if (_ownerAnimator == null)
            {
                return false;
            }

            if (_runtimeOverrideController != null)
            {
                return true;
            }

            if (_ownerAnimator.runtimeAnimatorController is AnimatorOverrideController sourceOverrideController)
            {
                _runtimeOverrideController = new AnimatorOverrideController(sourceOverrideController);
                _ownerAnimator.runtimeAnimatorController = _runtimeOverrideController;
                return true;
            }

            if (!_didLogMissingOverrideController)
            {
                Debug.LogWarning("PlayerAnimationDriver: runtime controller is not AnimatorOverrideController. Ability clip binding is skipped.", this);
                _didLogMissingOverrideController = true;
            }

            return false;
        }

        private void CacheAnimatorParameterHashes()
        {
            _hasMoveX = TryBuildHash(_config != null ? _config.MoveXParam : string.Empty, out _moveXHash);
            _hasMoveY = TryBuildHash(_config != null ? _config.MoveYParam : string.Empty, out _moveYHash);
            _hasSpeed = TryBuildHash(_config != null ? _config.SpeedParam : string.Empty, out _speedHash);
            _hasIsMoving = TryBuildHash(_config != null ? _config.IsMovingParam : string.Empty, out _isMovingHash);
            _hasAbilityUsedTrigger = TryBuildHash(_config != null ? _config.AbilityUsedTriggerParam : string.Empty, out _abilityUsedTriggerHash);
            _hasAbilityIndex = TryBuildHash(_config != null ? _config.AbilityIndexIntParam : string.Empty, out _abilityIndexHash);
            _hasAbilityAnimationSpeed = TryBuildHash(_config != null ? _config.AbilityAnimationSpeedParam : string.Empty, out _abilityAnimationSpeedHash);
        }

        private void InitializeAbilitySpeeds()
        {
            int count = _abilitySpeedBySlot.Length;
            for (int i = 0; i < count; i++)
            {
                _abilitySpeedBySlot[i] = DEFAULT_ABILITY_SPEED;
            }
        }

        private float ResolveAbilitySpeed(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _abilitySpeedBySlot.Length)
            {
                return DEFAULT_ABILITY_SPEED;
            }

            return _abilitySpeedBySlot[slotIndex];
        }

        private static float SnapIfNearZero(float value)
        {
            return Mathf.Abs(value) <= FLOAT_WRITE_EPSILON ? 0f : value;
        }

        private static bool TryBuildHash(string parameterName, out int hash)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                hash = 0;
                return false;
            }

            hash = Animator.StringToHash(parameterName);
            return true;
        }
    }
}











