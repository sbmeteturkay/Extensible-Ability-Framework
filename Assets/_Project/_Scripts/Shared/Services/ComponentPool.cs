using System;
using UnityEngine;
using UnityEngine.Pool;

namespace CaseStudy.Shared.Pooling
{
    /// <summary>
    /// Shared prefab pool for runtime component reuse.
    /// </summary>
    public sealed class ComponentPool<TComponent> : IDisposable where TComponent : Component
    {
        private readonly TComponent _prefab;
        private readonly Transform _poolRoot;
        private readonly ObjectPool<TComponent> _pool;

        public bool IsValid => _poolRoot != null;

        public ComponentPool(TComponent prefab, string poolName, Transform parentRoot, int maxSize = -1)
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
            _pool = new ObjectPool<TComponent>(
                CreateInstance,
                OnGet,
                OnRelease,
                OnDestroyPooled,
                collectionCheck: Application.isEditor,
                defaultCapacity: 0,
                maxSize: effectiveMaxSize);
        }

        public TComponent GetOrCreate()
        {
            return _pool.Get();
        }

        public void Release(TComponent instance)
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
                UnityEngine.Object.Destroy(_poolRoot.gameObject);
            }
        }

        private TComponent CreateInstance()
        {
            return UnityEngine.Object.Instantiate(_prefab, _poolRoot);
        }

        private static void OnGet(TComponent instance)
        {
            if (instance == null)
            {
                return;
            }

            instance.gameObject.SetActive(true);
        }

        private void OnRelease(TComponent instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_poolRoot == null)
            {
                UnityEngine.Object.Destroy(instance.gameObject);
                return;
            }

            instance.transform.SetParent(_poolRoot, false);
            instance.gameObject.SetActive(false);
        }

        private static void OnDestroyPooled(TComponent instance)
        {
            if (instance == null)
            {
                return;
            }

            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
