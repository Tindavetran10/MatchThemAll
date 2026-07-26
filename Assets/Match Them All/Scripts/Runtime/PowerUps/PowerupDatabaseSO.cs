using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MatchThemAll.Scripts.Power_Ups
{
    /// <summary>
    /// The registry of all power-ups — the single source of truth consumed by PowerupManager.
    /// Order in the list is irrelevant; display order comes from each entry's <see cref="PowerupDataSO.order"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Match Them All/Powerup Database")]
    public class PowerupDatabaseSO : ScriptableObject
    {
        public List<PowerupDataSO> powerups = new();

        /// <summary>First entry matching the id; logs a warning on duplicate ids.</summary>
        public PowerupDataSO FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            PowerupDataSO match = null;
            int hits = 0;
            foreach (var p in powerups)
            {
                if (p && p.id == id)
                {
                    if (match == null) match = p;
                    hits++;
                }
            }
            if (hits > 1)
                Debug.LogWarning($"[PowerupDatabaseSO] Duplicate powerup id '{id}' found {hits} times — using first match.");
            return match;
        }

        /// <summary>All entries sorted by display order (stable enough for designer-controlled layout).</summary>
        public IEnumerable<PowerupDataSO> Ordered => powerups
            .Where(p => p)
            .OrderBy(p => p.order);
    }
}
