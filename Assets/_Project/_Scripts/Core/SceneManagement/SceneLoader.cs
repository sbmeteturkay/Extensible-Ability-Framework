using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

namespace SabanCoreTemplate.SceneManagement
{
    public interface ISceneLoader
    {
        UniTask LoadAsync(string sceneName);
    }

    public class SceneLoader : ISceneLoader
    {
        private readonly LoadingScreen _loadingScreen;

        [Inject]
        public SceneLoader(LoadingScreen loadingScreen)
        {
            _loadingScreen = loadingScreen;
        }

        public async UniTask LoadAsync(string sceneName)
        {
            // 1. Loading screen aç
            await _loadingScreen.ShowAsync();

            // 2. Sahneyi arka planda yükle (aktivasyon manuel)
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            // 3. Fake progress — operation %90'a gelene kadar smooth ilerle
            await _loadingScreen.AnimateProgressAsync(operation);

            // 4. Sahneyi aktive et
            operation.allowSceneActivation = true;
            await UniTask.WaitUntil(() => operation.isDone);

            // 5. Loading screen kapat
            await _loadingScreen.HideAsync();
        }
    }
}