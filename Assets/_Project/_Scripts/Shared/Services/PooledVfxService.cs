using System;
using System.Collections.Generic;
using System.Threading;
using CaseStudy.Shared.Pooling;
using CaseStudy.Shared.Vfx.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CaseStudy.Shared.Vfx.Services
{
    /// <summary>
    /// Provides pooled VFX spawning with delayed spawn and timed auto-return.
    /// </summary>
    public sealed class PooledVfxService : IPooledVfxService, IDisposable
    {
        private const float DEFAULT_AUTO_RETURN_SECONDS = 2f;
        private const float MIN_AUTO_RETURN_SECONDS = 0.05f;
        private const float AUTO_RETURN_PADDING_SECONDS = 0.1f;

        private readonly Dictionary<int, VfxPool> _pools = new();
        private readonly Dictionary<int, float> _cachedAutoReturnByPrefabKey = new();
        private readonly Dictionary<int, PooledInstanceState> _instanceStates = new();
        private readonly Transform _root;

        private bool _isDisposed;

        public PooledVfxService()
        {
            GameObject rootObject = new GameObject("VfxPools");
            _root = rootObject.transform;
            _root.SetParent(PoolRootRegistry.GetAbilityPoolsRoot(), false);
        }

        public void Spawn(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float delaySeconds = 0f, float autoReturnSeconds = 0f)
        {
            if (_isDisposed || vfxPrefab == null)
            {
                return;
            }

            if (delaySeconds > 0f)
            {
                SpawnDelayedAsync(vfxPrefab, position, rotation, delaySeconds, autoReturnSeconds).Forget();
                return;
            }

            SpawnNow(vfxPrefab, position, rotation, autoReturnSeconds);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (KeyValuePair<int, VfxPool> pair in _pools)
            {
                pair.Value.Dispose();
            }

            _pools.Clear();
            _cachedAutoReturnByPrefabKey.Clear();
            _instanceStates.Clear();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
        }

        private async UniTaskVoid SpawnDelayedAsync(
            GameObject vfxPrefab,
            Vector3 position,
            Quaternion rotation,
            float delaySeconds,
            float autoReturnSeconds)
        {
            int delayMilliseconds = Mathf.CeilToInt(delaySeconds * 1000f);
            if (delayMilliseconds > 0)
            {
                await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            }

            if (_isDisposed || vfxPrefab == null)
            {
                return;
            }

            SpawnNow(vfxPrefab, position, rotation, autoReturnSeconds);
        }

        private void SpawnNow(GameObject vfxPrefab, Vector3 position, Quaternion rotation, float autoReturnSeconds)
        {
            int prefabKey = vfxPrefab.GetInstanceID();
            VfxPool pool = GetOrCreatePool(vfxPrefab, prefabKey);
            GameObject instance = pool.GetOrCreate();

            if (instance == null)
            {
                return;
            }

            Transform instanceTransform = instance.transform;
            instanceTransform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);

            int instanceId = instance.GetInstanceID();
            int spawnVersion = 1;
            if (_instanceStates.TryGetValue(instanceId, out PooledInstanceState previousState))
            {
                spawnVersion = previousState.SpawnVersion + 1;
            }

            _instanceStates[instanceId] = new PooledInstanceState(instance, prefabKey, spawnVersion);

            float returnDelaySeconds = ResolveAutoReturnSeconds(vfxPrefab, prefabKey, autoReturnSeconds);
            ReturnToPoolDelayedAsync(instance, instanceId, prefabKey, spawnVersion, returnDelaySeconds).Forget();
        }

        private async UniTaskVoid ReturnToPoolDelayedAsync(GameObject instance, int instanceId, int poolKey, int spawnVersion, float delaySeconds)
        {
            int delayMilliseconds = Mathf.CeilToInt(delaySeconds * 1000f);
            if (delayMilliseconds > 0)
            {
                await UniTask.Delay(delayMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, CancellationToken.None);
            }

            if (_isDisposed || instance == null)
            {
                return;
            }

            if (!_instanceStates.TryGetValue(instanceId, out PooledInstanceState state))
            {
                return;
            }

            if (state.Instance != instance || state.PoolKey != poolKey || state.SpawnVersion != spawnVersion)
            {
                return;
            }

            if (!_pools.TryGetValue(poolKey, out VfxPool pool))
            {
                UnityEngine.Object.Destroy(instance);
                _instanceStates.Remove(instanceId);
                return;
            }

            pool.Release(instance);
        }

        private VfxPool GetOrCreatePool(GameObject prefab, int prefabKey)
        {
            if (_pools.TryGetValue(prefabKey, out VfxPool pool))
            {
                return pool;
            }

            pool = new VfxPool(prefab, _root);
            _pools[prefabKey] = pool;
            return pool;
        }

        private float ResolveAutoReturnSeconds(GameObject prefab, int prefabKey, float requestedAutoReturnSeconds)
        {
            if (requestedAutoReturnSeconds > 0f)
            {
                return Mathf.Max(MIN_AUTO_RETURN_SECONDS, requestedAutoReturnSeconds);
            }

            if (_cachedAutoReturnByPrefabKey.TryGetValue(prefabKey, out float cachedSeconds))
            {
                return cachedSeconds;
            }

            float estimatedSeconds = EstimateAutoReturnSeconds(prefab);
            float resolvedSeconds = Mathf.Max(MIN_AUTO_RETURN_SECONDS, estimatedSeconds);
            _cachedAutoReturnByPrefabKey[prefabKey] = resolvedSeconds;
            return resolvedSeconds;
        }

        private static float EstimateAutoReturnSeconds(GameObject prefab)
        {
            ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            if (systems == null || systems.Length == 0)
            {
                return DEFAULT_AUTO_RETURN_SECONDS;
            }

            float maxDurationSeconds = 0f;

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem particleSystem = systems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                if (main.loop)
                {
                    return DEFAULT_AUTO_RETURN_SECONDS;
                }

                float particleLifeSeconds = GetStartLifetimeMax(main.startLifetime);
                float durationSeconds = main.duration + particleLifeSeconds;

                if (durationSeconds > maxDurationSeconds)
                {
                    maxDurationSeconds = durationSeconds;
                }
            }

            if (maxDurationSeconds <= 0f)
            {
                return DEFAULT_AUTO_RETURN_SECONDS;
            }

            return maxDurationSeconds + AUTO_RETURN_PADDING_SECONDS;
        }

        private static float GetStartLifetimeMax(ParticleSystem.MinMaxCurve curve)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return curve.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return curve.constantMax;
                case ParticleSystemCurveMode.Curve:
                    return curve.curveMultiplier;
                case ParticleSystemCurveMode.TwoCurves:
                    return curve.curveMultiplier;
                default:
                    return curve.constantMax;
            }
        }

        private readonly struct PooledInstanceState
        {
            public readonly GameObject Instance;
            public readonly int PoolKey;
            public readonly int SpawnVersion;

            public PooledInstanceState(GameObject instance, int poolKey, int spawnVersion)
            {
                Instance = instance;
                PoolKey = poolKey;
                SpawnVersion = spawnVersion;
            }
        }

        private sealed class VfxPool
        {
            private readonly GameObject _prefab;
            private readonly Queue<GameObject> _inactive = new();
            private readonly Transform _poolRoot;

            public VfxPool(GameObject prefab, Transform parentRoot)
            {
                _prefab = prefab;

                GameObject poolRootObject = new GameObject($"VfxPool_{prefab.name}");
                _poolRoot = poolRootObject.transform;
                _poolRoot.SetParent(parentRoot, false);
            }

            public GameObject GetOrCreate()
            {
                while (_inactive.Count > 0)
                {
                    GameObject pooled = _inactive.Dequeue();
                    if (pooled != null)
                    {
                        return pooled;
                    }
                }

                return UnityEngine.Object.Instantiate(_prefab, _poolRoot);
            }

            public void Release(GameObject instance)
            {
                if (instance == null)
                {
                    return;
                }

                instance.transform.SetParent(_poolRoot, false);
                instance.SetActive(false);
                _inactive.Enqueue(instance);
            }

            public void Dispose()
            {
                while (_inactive.Count > 0)
                {
                    GameObject pooled = _inactive.Dequeue();
                    if (pooled != null)
                    {
                        UnityEngine.Object.Destroy(pooled);
                    }
                }

                if (_poolRoot != null)
                {
                    UnityEngine.Object.Destroy(_poolRoot.gameObject);
                }
            }
        }
    }
}
