using CaseStudy.Feature.AbilitySystem.Input;
using CaseStudy.Feature.AbilitySystem.Runtime;
using CaseStudy.Feature.Animation.Runtime;
using CaseStudy.Feature.Locomotion.Input;
using CaseStudy.Shared.PlayerControl.Contracts;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace CaseStudy.Feature.PlayerControl.Runtime
{
    /// <summary>
    /// Player-side control node that toggles local input gateways and camera,
    /// and requests character switch when the switch action is pressed.
    /// </summary>
    public sealed class PlayerControlState : MonoBehaviour, IPlayerControlNode
    {
        [Header("Switch Input")]
        [SerializeField] private InputActionReference _switchAction;

        [Header("Controlled Components")]
        [SerializeField] private AbilityInputGateway _abilityInputGateway;
        [SerializeField] private LocomotionInputGateway _locomotionInputGateway;
        [SerializeField] private PlayerAnimationDriver _playerAnimationDriver;
        [SerializeField] private CinemachineCamera _playerCamera;
        [SerializeField] private AbilityRuntimeBootstrap _abilityRuntimeBootstrap;

        private IPlayerSwitchService _switchService;
        private bool _isControlled;

        public bool IsAvailable => isActiveAndEnabled;

        [Inject]
        public void Construct(IPlayerSwitchService switchService)
        {
            _switchService = switchService;
        }

        private void Awake()
        {
            if (_abilityInputGateway == null)
            {
                _abilityInputGateway = FindInOwnerHierarchy<AbilityInputGateway>();
            }

            if (_locomotionInputGateway == null)
            {
                _locomotionInputGateway = FindInOwnerHierarchy<LocomotionInputGateway>();
            }

            if (_playerAnimationDriver == null)
            {
                _playerAnimationDriver = FindInOwnerHierarchy<PlayerAnimationDriver>();
            }

            if (_abilityRuntimeBootstrap == null)
            {
                _abilityRuntimeBootstrap = FindInOwnerHierarchy<AbilityRuntimeBootstrap>();
            }

            if (_playerCamera == null)
            {
                _playerCamera = FindInOwnerHierarchy<CinemachineCamera>();
            }
        }

        private void OnEnable()
        {
            if (_switchService == null)
            {
                Debug.LogWarning("PlayerControlState: dependencies were not injected.", this);
                enabled = false;
                return;
            }

            BindSwitchAction();
            _switchService.Register(this);
        }

        private void OnDisable()
        {
            UnbindSwitchAction();
            _switchService?.Unregister(this);
            _isControlled = false;
        }

        public void SetControlState(bool isControlled)
        {
            _isControlled = isControlled;

            if (_abilityInputGateway != null)
            {
                _abilityInputGateway.enabled = isControlled;
            }

            if (_locomotionInputGateway != null)
            {
                if (!isControlled)
                {
                    _locomotionInputGateway.SetMoveInput(Vector2.zero);
                }

                _locomotionInputGateway.enabled = isControlled;
            }

            if (_playerAnimationDriver != null)
            {
                _playerAnimationDriver.enabled = isControlled;
            }

            if (_playerCamera != null)
            {
                _playerCamera.gameObject.SetActive(isControlled);
            }

            if (isControlled)
            {
                _abilityRuntimeBootstrap?.RepublishPresentationState();
            }
        }

        private void OnSwitchPerformed(InputAction.CallbackContext _)
        {
            if (!_isControlled || _switchService == null)
            {
                return;
            }

            _switchService.RequestSwitch(this);
        }

        private void BindSwitchAction()
        {
            if (_switchAction == null || _switchAction.action == null)
            {
                return;
            }

            _switchAction.action.performed += OnSwitchPerformed;

            if (!_switchAction.action.enabled)
            {
                _switchAction.action.Enable();
            }
        }

        private void UnbindSwitchAction()
        {
            if (_switchAction == null || _switchAction.action == null)
            {
                return;
            }

            _switchAction.action.performed -= OnSwitchPerformed;
        }

        private T FindInOwnerHierarchy<T>() where T : Component
        {
            Transform current = transform;
            while (current != null)
            {
                T found = current.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }

                current = current.parent;
            }

            return null;
        }
    }
}
