using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace SabanCoreTemplate.SceneManagement
{
    public class LoadingScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [SerializeField] private Slider _progressBar;

        [Header("Settings")]
        [SerializeField] private float _fadeDuration = 0.3f;

        [SerializeField] private float _minLoadDuration = 0.8f; // fake progress için minimum süre
        [SerializeField] private float _progressSmooth = 3f; // ne kadar smooth ilerlesin
        private float _currentProgress;

        private float _targetProgress;

        private void Awake()
        {
            // Sahneler arası yaşasın
            DontDestroyOnLoad(gameObject);

            // Başta gizli
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _progressBar.value = 0f;
        }

        // ─── Public API ──────────────────────────────────────

        public async UniTask ShowAsync()
        {
            _currentProgress = 0f;
            _targetProgress = 0f;
            _progressBar.value = 0f;
            _canvasGroup.blocksRaycasts = true;

            await Tween.Custom(
                _canvasGroup.alpha, 1f, _fadeDuration,
                v => _canvasGroup.alpha = v
            );
        }

        public async UniTask HideAsync()
        {
            // Önce %100'e tamamla
            await AnimateToAsync(1f);

            await Tween.Custom(
                _canvasGroup.alpha, 0f, _fadeDuration,
                v => _canvasGroup.alpha = v
            );

            _canvasGroup.blocksRaycasts = false;
            _progressBar.value = 0f;
        }

        public async UniTask AnimateProgressAsync(AsyncOperation operation)
        {
            float startTime = Time.time;

            // operation.progress 0.9'da durur (allowSceneActivation=false iken Unity tasarımı)
            // Biz bunu 0→1 aralığına normalize edip fake progress ile dolduruyoruz
            // Loop koşulu: sahne henüz %90'a ulaşmadı VEYA min süre dolmadı
            while (true)
            {
                float operationProgress = Mathf.Clamp01(operation.progress / 0.9f);
                float elapsed = Time.time - startTime;
                float timeProgress = Mathf.Clamp01(elapsed / _minLoadDuration);

                // İkisinden küçük olanı hedef al — ikisi de dolmadan çıkma
                _targetProgress = Mathf.Min(operationProgress, timeProgress);
                _targetProgress = Mathf.Clamp(_targetProgress, 0f, 0.95f); // %95'te beklet

                _currentProgress = Mathf.Lerp(_currentProgress, _targetProgress, Time.deltaTime * _progressSmooth);
                _progressBar.value = _currentProgress;

                // Her iki koşul da tamamlandıysa çık
                if (operationProgress >= 1f && elapsed >= _minLoadDuration)
                    break;

                await UniTask.Yield();
            }
        }

        // ─── Private ─────────────────────────────────────────

        private async UniTask AnimateToAsync(float target)
        {
            await Tween.Custom(
                _progressBar.value, target, 0.2f,
                v => _progressBar.value = v
            ).ToYieldInstruction();
        }
    }
}