using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionRepairStation : MonoBehaviour
    {
        public bool TryRepair(ExtractionWeaponController weapon, ExtractionRecipeData repairRecipe, ExtractionBaseStorage storage)
        {
            if (weapon == null || repairRecipe == null || storage == null)
            {
                return false;
            }

            if (!storage.Container.ConsumeItems(repairRecipe.Inputs))
            {
                return false;
            }

            weapon.RepairActiveWeapon(repairRecipe.WeaponRepairAmount);
            return true;
        }
    }
}
