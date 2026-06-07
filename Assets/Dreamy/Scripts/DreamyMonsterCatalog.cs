using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    [CreateAssetMenu(menuName = "Dreamy/Prototype/Monster Catalog", fileName = "DreamyMonsterCatalog")]
    public sealed class DreamyMonsterCatalog : ScriptableObject
    {
        [SerializeField] private List<DreamyMonsterDefinition> monsters = new List<DreamyMonsterDefinition>();

        public IReadOnlyList<DreamyMonsterDefinition> Monsters => monsters;
        public int Count => monsters != null ? monsters.Count : 0;

        public DreamyMonsterDefinition GetMonster(int index)
        {
            if (monsters == null || monsters.Count == 0)
            {
                return null;
            }

            return monsters[Mathf.Abs(index) % monsters.Count];
        }

        public DreamyMonsterDefinition GetCombatMonster(int index)
        {
            if (monsters == null || monsters.Count == 0)
            {
                return null;
            }

            int combatIndex = 0;
            for (int i = 0; i < monsters.Count; i++)
            {
                DreamyMonsterDefinition monster = monsters[i];
                if (monster == null || !monster.IsCombatReady)
                {
                    continue;
                }

                if (combatIndex == index)
                {
                    return monster;
                }

                combatIndex++;
            }

            return null;
        }

        public int CombatCount
        {
            get
            {
                if (monsters == null)
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i] != null && monsters[i].IsCombatReady)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
