using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MatchThemAll.Scripts
{
    public class ItemPoolManager : MonoBehaviour
    {
        public static ItemPoolManager Instance { get; private set; }

        private readonly Dictionary<EItemName, IObjectPool<Item>> _pools = new();
        private Item _currentRequestedPrefab;

        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        public Item GetItem(Item prefab)
        {
            EItemName key = prefab.ItemNameKey;
            
            if (!_pools.ContainsKey(key))
            {
                _currentRequestedPrefab = prefab;
                _pools[key] = new ObjectPool<Item>(
                    createFunc: CreatePooledItem,
                    actionOnGet: OnTakeFromPool,
                    actionOnRelease: OnReturnedToPool,
                    actionOnDestroy: OnDestroyPoolObject,
                    collectionCheck: false,
                    defaultCapacity: 20,
                    maxSize: 100
                );
            }

            _currentRequestedPrefab = prefab;
            return _pools[key].Get();
        }

        public void ReleaseItem(Item item)
        {
            if (item == null) return;
            
            EItemName key = item.ItemNameKey;
            if (_pools.TryGetValue(key, out var pool))
            {
                pool.Release(item);
            }
            else
            {
                Destroy(item.gameObject);
            }
        }

        private Item CreatePooledItem()
        {
            Item item = Instantiate(_currentRequestedPrefab, transform);
            item.gameObject.SetActive(false);
            return item;
        }

        private void OnTakeFromPool(Item item)
        {
            item.ResetState();
            item.gameObject.SetActive(true);
        }

        private static void OnReturnedToPool(Item item)
        {
            item.ResetState();
            item.gameObject.SetActive(false);
        }

        private static void OnDestroyPoolObject(Item item) => Destroy(item.gameObject);
    }
}
