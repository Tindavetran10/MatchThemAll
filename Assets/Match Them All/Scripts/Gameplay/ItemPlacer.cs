using System.Collections.Generic;
using System.Linq;
using MatchThemAll.Scripts;
using NaughtyAttributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class ItemPlacer : MonoBehaviour
{
    [Header("Elements")]
    [SerializeField] private List<ItemLevelData> itemData;
    
    [Header("Settings")]
    [SerializeField] private BoxCollider spawnZone;

    [SerializeField] private int seed;
    
    public ItemLevelData[] GetGoals() => 
        itemData.Where(data => data.isGoal).ToArray();

#if UNITY_EDITOR
    [Button]
    private void SpawnItem()
    {
        while (transform.childCount > 0)
        {
            Transform child = transform.GetChild(0);
            child.parent = null;
            DestroyImmediate(child.gameObject);
        }

        Random.InitState(seed);

        foreach (var data in itemData)
        {
            for (int j = 0; j < data.amount; j++)
            {
                Vector3 spawnPos = GetSpawnPosition();
                
                Item itemInstance = PrefabUtility.InstantiatePrefab(data.itemPrefab, transform) as Item;
                itemInstance!.transform.position = spawnPos;
                itemInstance.transform.rotation = Quaternion.Euler(Random.onUnitSphere * 360);
            }
        }
    }
    
    private void OnValidate()
    {
        Debug.Log("OnValidate called!"); // Add this line
        for (int i = 0; i < itemData.Count; i++) {
            var data = itemData[i]; // Get a copy of the struct

            // Snap to nearest multiple of 3
            int corrected = Mathf.RoundToInt(data.amount / 3f) * 3;

            if (data.amount != corrected) {
                data.amount = corrected; // Update the copy
                itemData[i] = data;      // Reassign to the list
            }
        }
    }
    
    private Vector3 GetSpawnPosition()
    {
        float x = Random.Range(-spawnZone.size.x / 2f, spawnZone.size.x / 2f);
        float y = Random.Range(-spawnZone.size.y / 2f, spawnZone.size.y / 2f);
        float z = Random.Range(-spawnZone.size.z / 2f, spawnZone.size.z / 2f);
        
        Vector3 localPosition = spawnZone.center + new Vector3(x, y, z);
        Vector3 spawnPosition = transform.TransformPoint(localPosition);
        
        return spawnPosition;
    }
#endif
}
