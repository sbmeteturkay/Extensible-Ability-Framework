using System.Threading;
using CaseStudy.Feature.AbilitySystem.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Feature.AbilitySystem.Data
{
    [CreateAssetMenu(fileName = "SO_Module_CastFeedback", menuName = "Ability/Modules/Cast Feedback")]
    public sealed class AbilityCastFeedbackModuleSO : AbilityOptionalModuleSO
    {
        [SerializeField] private GameObject _castVfxPrefab;
        [SerializeField] private AudioClip _castSfx;

        public GameObject CastVfxPrefab => _castVfxPrefab;

        public AudioClip CastSfx => _castSfx;

        public override AbilityModuleExecutionTime ExecutionTime => AbilityModuleExecutionTime.BeforeExecute;

        public override UniTask OnBeforeExecuteAsync(
            AbilityContext context,
            AbilityDataSO data,
            AbilityExecutionOptions options,
            CancellationToken cancellationToken)
        {
            if (context == null || context.OwnerTransform == null)
            {
                return UniTask.CompletedTask;
            }

            Vector3 origin = context.OwnerTransform.position;

            if (_castVfxPrefab != null)
            {
                if (context.PooledVfxService != null)
                {
                    context.PooledVfxService.Spawn(_castVfxPrefab, origin, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("AbilityCastFeedbackModuleSO: IPooledVfxService is missing. Skipping cast VFX spawn.");
                }
            }

            if (_castSfx == null)
            {
                return UniTask.CompletedTask;
            }

            if (context.OwnerAudioSource != null)
            {
                context.OwnerAudioSource.PlayOneShot(_castSfx);
            }
            else
            {
                AudioSource.PlayClipAtPoint(_castSfx, origin);
            }

            return UniTask.CompletedTask;
        }
    }
}

