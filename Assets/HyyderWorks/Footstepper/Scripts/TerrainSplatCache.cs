namespace  HyyderWorks.Footstepper
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Provides a lower-resolution dominant-texture cache for TerrainData.
    /// Builds a downsampled map of dominant layer indices to speed up terrain texture lookups.
    /// </summary>
    public static class TerrainSplatCache
    {
        // Represents a cached entry for a terrain's dominant texture map
        private class CacheEntry
        {
            public int[,] DominantMap;
            public int CacheWidth;
            public int CacheHeight;
        }

        // Maps each TerrainData to its cached downsampled dominant map
        private static Dictionary<TerrainData, CacheEntry> _cache =
            new Dictionary<TerrainData, CacheEntry>();

        /// <summary>
        /// Retrieves the dominant layer index at normalized coordinates [0..1] using a downsampled cache.
        /// If the cache doesn't exist, it is built with the specified downsample factor.
        /// </summary>
        /// <param name="data">The TerrainData to sample.</param>
        /// <param name="normalizedX">Normalized X coordinate (0..1).</param>
        /// <param name="normalizedZ">Normalized Z coordinate (0..1).</param>
        /// <param name="downsampleFactor">Factor by which to downsample the alphamap.
        /// Higher values yield lower resolution caches.</param>
        /// <returns>Index of the dominant terrain layer.</returns>
        public static int GetDominantLayer(
            TerrainData data,
            float normalizedX,
            float normalizedZ
        )
        {
            if (!_cache.TryGetValue(data, out var entry))
            {
                entry = BuildCache(data, 8);
                _cache[data] = entry;
            }

            // Clamp normalized coords
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedZ = Mathf.Clamp01(normalizedZ);

            // Map to cache indices
            int cx = Mathf.FloorToInt(normalizedX * entry.CacheWidth);
            int cz = Mathf.FloorToInt(normalizedZ * entry.CacheHeight);
            cx = Mathf.Clamp(cx, 0, entry.CacheWidth - 1);
            cz = Mathf.Clamp(cz, 0, entry.CacheHeight - 1);

            return entry.DominantMap[cz, cx];
        }

        /// <summary>
        /// Builds and returns a downsampled dominant-layer cache for the given terrain.
        /// </summary>
        private static CacheEntry BuildCache(TerrainData data, int downsampleFactor)
        {
            int fullW = data.alphamapWidth;
            int fullH = data.alphamapHeight;
            int layers = data.alphamapLayers;

            int cacheW = Mathf.Max(1, fullW / downsampleFactor);
            int cacheH = Mathf.Max(1, fullH / downsampleFactor);
            var dominantMap = new int[cacheH, cacheW];

            // Fetch entire alphamap once
            float[,,] fullMap = data.GetAlphamaps(0, 0, fullW, fullH);

            for (int y = 0; y < cacheH; y++)
            {
                int startY = y * downsampleFactor;
                int endY = Mathf.Min(startY + downsampleFactor, fullH);

                for (int x = 0; x < cacheW; x++)
                {
                    int startX = x * downsampleFactor;
                    int endX = Mathf.Min(startX + downsampleFactor, fullW);

                    // Sum weights per layer
                    float[] sums = new float[layers];
                    for (int yy = startY; yy < endY; yy++)
                    for (int xx = startX; xx < endX; xx++)
                    for (int layer = 0; layer < layers; layer++)
                        sums[layer] += fullMap[yy, xx, layer];

                    // Find dominant layer in block
                    int bestLayer = 0;
                    float bestSum = sums[0];
                    for (int layer = 1; layer < layers; layer++)
                    {
                        if (sums[layer] > bestSum)
                        {
                            bestSum = sums[layer];
                            bestLayer = layer;
                        }
                    }

                    dominantMap[y, x] = bestLayer;
                }
            }

            return new CacheEntry
            {
                DominantMap = dominantMap,
                CacheWidth = cacheW,
                CacheHeight = cacheH
            };
        }

        /// <summary>
        /// Clears the entire cache. Call this if you repaint or dynamically modify terrain textures.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }
    }

}