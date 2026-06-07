using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionMarketStation : MonoBehaviour
    {
        [SerializeField] private List<ExtractionRecipeData> listings = new List<ExtractionRecipeData>();

        public IReadOnlyList<ExtractionRecipeData> Listings => listings;

        public bool TryBuy(ExtractionRecipeData listing, ExtractionBaseStorage storage)
        {
            if (listing == null || storage == null || !listings.Contains(listing))
            {
                return false;
            }

            if (!storage.Container.ConsumeItems(listing.Inputs))
            {
                return false;
            }

            if (storage.Container.AddItems(listing.Outputs))
            {
                return true;
            }

            storage.Container.AddItems(listing.Inputs);
            return false;
        }
    }
}
