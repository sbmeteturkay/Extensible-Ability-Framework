using CaseStudy.Feature.Locomotion.Contracts;
using CaseStudy.Feature.Locomotion.Data;
using CaseStudy.Feature.Locomotion.Domain;
using UnityEngine;
using VContainer;

namespace CaseStudy.Feature.Locomotion.Runtime
{
    /// <summary>
    /// Builds locomotion runtime context from scene references and config data.
    /// Invalid setup is handled gracefully to avoid scene crashes.
    /// </summary>
    public sealed class LocomotionRuntimeBootstrap : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _ownerTransform;
        [SerializeField] private Rigidbody _ownerRigidbody;
        [SerializeField] private LocomotionDataSO _locomotionData;

        private ILocomotionController _locomotionController;
        private ILocomotionInputReader _inputReader;

        [Inject]
        public void Construct(ILocomotionController locomotionController, ILocomotionInputReader inputReader)
        {
            _locomotionController = locomotionController;
            _inputReader = inputReader;
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
        }

        private void Start()
        {
            if (_locomotionController == null || _inputReader == null)
            {
                Debug.LogWarning("LocomotionRuntimeBootstrap: dependencies were not injected.");
                enabled = false;
                return;
            }

            if (_ownerTransform == null || _ownerRigidbody == null)
            {
                Debug.LogWarning("LocomotionRuntimeBootstrap: owner transform/rigidbody is missing.");
                enabled = false;
                return;
            }

            if (_locomotionData == null)
            {
                Debug.LogWarning("LocomotionRuntimeBootstrap: locomotion data asset is missing.");
                enabled = false;
                return;
            }

            if (_locomotionData.MoveSpeed < 0f || _locomotionData.RotationSpeedDegreesPerSecond < 0f)
            {
                Debug.LogWarning("LocomotionRuntimeBootstrap: locomotion data contains negative speed values.");
                enabled = false;
                return;
            }

            LocomotionContext context = new LocomotionContext(_ownerTransform, _ownerRigidbody, _inputReader, _locomotionData);
            _locomotionController.Configure(context);
        }
    }
}
