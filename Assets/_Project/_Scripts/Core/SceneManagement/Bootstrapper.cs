using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace SabanCoreTemplate.SceneManagement
{
    public class Bootstrapper : IStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public Bootstrapper(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        public void Start()
        {
            LoadMainMenuAsync().Forget();
        }

        private async UniTaskVoid LoadMainMenuAsync()
        {
            await _sceneLoader.LoadAsync("MainMenu");
        }
    }
}