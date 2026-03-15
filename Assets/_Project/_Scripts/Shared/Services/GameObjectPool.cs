using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace CaseStudy.Shared.Pooling
{
    /// <summary>
    /// Shared prefab pool for runtime GameObject reuse.
    /// </summary>
    public sealed class GameObjectPool : IDisposable
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolRoot;
        private readonly ObjectPool<GameObject> _pool;

        public bool IsValid => _poolRoot != null;

        public GameObjectPool(GameObject prefab, string poolName, Transform parentRoot, int maxSize = -1)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            _prefab = prefab;

            GameObject poolRootObject = new GameObject(poolName);
            _poolRoot = poolRootObject.transform;
            _poolRoot.SetParent(parentRoot, false);

            int effectiveMaxSize = maxSize < 0 ? int.MaxValue : Mathf.Max(1, maxSize);
            _pool = new ObjectPool<GameObject>(
                CreateInstance,
                OnGet,
                OnRelease,
                OnDestroyPooled,
                collectionCheck: Application.isEditor,
                defaultCapacity: 0,
                maxSize: effectiveMaxSize);
        }

        public GameObject GetOrCreate()
        {
            return _pool.Get();
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            _pool.Release(instance);
        }

        public void Dispose()
        {
            _pool.Clear();

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot.gameObject);
            }
        }

        private GameObject CreateInstance()
        {
            return Object.Instantiate(_prefab, _poolRoot);
        }

        private static void OnGet(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.SetActive(true);
        }

        private void OnRelease(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_poolRoot == null)
            {
                Object.Destroy(instance);
                return;
            }

            instance.transform.SetParent(_poolRoot, false);
            instance.SetActive(false);
        }

        private static void OnDestroyPooled(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            Object.Destroy(instance);
        }
    }
}
