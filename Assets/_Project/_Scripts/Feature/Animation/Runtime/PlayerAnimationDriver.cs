using System;
using CaseStudy.Feature.Animation.Data;
using CaseStudy.Shared.AbilitySystem.Events;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.Animation.Runtime
{
    /// <summary>
    /// Single-component animation driver.
    /// Computes movement in FixedUpdate, writes animator params in Update,
    /// and optionally reacts to ability trigger events.
    /// </summary>
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
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
        private IDisposable _abilityTriggeredSubscription;

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

        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasSpeed;
        private bool _hasIsMoving;
        private bool _hasAbilityUsedTrigger;
        private bool _hasAbilityIndex;

        private bool _pendingAbilityUsedTrigger;
        private int _pendingAbilityIndex;
        private bool _hasPendingAbilityIndex;

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
            if (resolver != null)
            {
                resolver.TryResolve<ISubscriber<AbilityTriggeredEvent>>(out _abilityTriggeredSubscriber);
            }
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

            if (_ownerTransform != null)
            {
                _lastFixedWorldPosition = _ownerTransform.position;
                _hasLastFixedWorldPosition = true;
            }

            _lastAboveThresholdTime = -999f;
        }

        private void OnEnable()
        {
            SubscribeAbilityEvents();
        }

        private void OnDisable()
        {
            _abilityTriggeredSubscription?.Dispose();
            _abilityTriggeredSubscription = null;
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

        private void SubscribeAbilityEvents()
        {
            _abilityTriggeredSubscription?.Dispose();
            _abilityTriggeredSubscription = null;

            if (_abilityTriggeredSubscriber == null)
            {
                return;
            }

            _abilityTriggeredSubscription = _abilityTriggeredSubscriber.Subscribe(OnAbilityTriggered);
        }

        private void OnAbilityTriggered(AbilityTriggeredEvent evt)
        {
            if (_hasAbilityUsedTrigger)
            {
                _pendingAbilityUsedTrigger = true;
            }

            if (_hasAbilityIndex && !string.IsNullOrWhiteSpace(evt.AbilityKey))
            {
                _pendingAbilityIndex = Animator.StringToHash(evt.AbilityKey);
                _hasPendingAbilityIndex = true;
            }
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
                _ownerAnimator.SetInteger(_abilityIndexHash, _pendingAbilityIndex);
                _hasPendingAbilityIndex = false;
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

        private void CacheAnimatorParameterHashes()
        {
            _hasMoveX = TryBuildHash(_config != null ? _config.MoveXParam : string.Empty, out _moveXHash);
            _hasMoveY = TryBuildHash(_config != null ? _config.MoveYParam : string.Empty, out _moveYHash);
            _hasSpeed = TryBuildHash(_config != null ? _config.SpeedParam : string.Empty, out _speedHash);
            _hasIsMoving = TryBuildHash(_config != null ? _config.IsMovingParam : string.Empty, out _isMovingHash);
            _hasAbilityUsedTrigger = TryBuildHash(_config != null ? _config.AbilityUsedTriggerParam : string.Empty, out _abilityUsedTriggerHash);
            _hasAbilityIndex = TryBuildHash(_config != null ? _config.AbilityIndexIntParam : string.Empty, out _abilityIndexHash);
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

