using System;

namespace HyyderWorks.Footstepper
{
    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(fileName = "FootstepsDatabase", menuName = "Hyyder Works/Footstepper/Footstep Texture Data")]
    public class FootstepsDatabase : ScriptableObject
    {
        [Serializable]
        public class TextureFootstepPair
        {
            public string name; 
            public Texture2D texture;
            public AudioClip[] footstepSounds;
            public float volumeMultiplier = 1f;
            public float pitchVariation = 0.1f;


            public int terrainLayerIndex = -1; // For terrain textures

            public AudioClip GetRandomFootstep()
            {
                if (footstepSounds == null || footstepSounds.Length == 0)
                    return null;

                return footstepSounds[Random.Range(0, footstepSounds.Length)];
            }
        }


        public float stepDistance = 1.5f;
        public float defaultVolume = 0.7f;
        public LayerMask groundLayerMask = -1;


        public List<TextureFootstepPair> textureFootsteps = new();


        public AudioClip[] defaultFootstepSounds;

        public TextureFootstepPair GetFootstepForTexture(Texture2D texture)
        {
            foreach (var pair in textureFootsteps)
            {
                if (pair.texture == texture)
                    return pair;
            }

            return null;
        }

        public TextureFootstepPair GetFootstepForTerrainLayer(int layerIndex)
        {
            foreach (var pair in textureFootsteps)
            {
                if (pair.terrainLayerIndex == layerIndex)
                    return pair;
            }

            return null;
        }

        public AudioClip GetDefaultFootstep()
        {
            if (defaultFootstepSounds == null || defaultFootstepSounds.Length == 0)
                return null;

            return defaultFootstepSounds[Random.Range(0, defaultFootstepSounds.Length)];
        }
    }
}