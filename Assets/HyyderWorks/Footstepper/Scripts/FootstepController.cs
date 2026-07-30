namespace HyyderWorks.Footstepper
{
    using UnityEngine;

    public class FootstepController : MonoBehaviour
    {

        public FootstepsDatabase footstepData;
        public AudioSource audioSource;

        public Transform raycastOrigin;
        public float raycastDistance = 2f;
        public float raycastOffset = 0.1f; // Small offset from ground

        public FootstepTriggerMode triggerMode = FootstepTriggerMode.Distance;

        public bool enableDebug = false;

        public enum FootstepTriggerMode
        {
            Distance, 
            AnimationEvent,
        }

        private Vector3 lastFootstepPosition;
        private float distanceTraveled = 0f;
        private bool isGrounded = false;

        void Start()
        {
            // Initialize audio source if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }

            // Set initial position
            if (raycastOrigin == null)
                raycastOrigin = transform;

            lastFootstepPosition = raycastOrigin.position;
        }

        void Update()
        {
            if (footstepData == null) return;

            // Only check distance if using distance-based or both modes
            if (triggerMode == FootstepTriggerMode.Distance)
            {
                CheckDistance();
            }

        }

        void CheckDistance()
        {
            float currentDistance = Vector3.Distance(lastFootstepPosition, raycastOrigin.position);
            distanceTraveled += currentDistance;

            if (distanceTraveled >= footstepData.stepDistance)
            {
                PlayFootstep();
                distanceTraveled = 0f;
            }

            lastFootstepPosition = raycastOrigin.position;
        }

        void PlayFootstep()
        {
            PlayFootstepAtPosition(raycastOrigin);
        }

        void PlayFootstepAtPosition(Transform origin)
        {
            RaycastHit hit;
            Vector3 rayOrigin = origin.position + Vector3.up * raycastOffset;

            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance, footstepData.groundLayerMask))
            {
                isGrounded = true;

                // Check if it's terrain
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                if (terrain != null)
                {
                    PlayTerrainFootstep(terrain, hit.point);
                }
                else
                {
                    // Check for mesh renderer
                    MeshRenderer meshRenderer = hit.collider.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        PlayMeshFootstep(meshRenderer, hit);
                    }
                    else
                    {
                        PlayDefaultFootstep();
                    }
                }
            }
            else
            {
                isGrounded = false;
                if (enableDebug)
                    Debug.Log("No ground detected for footstep");
            }
        }

        //Called externally from an event
        public void OnFootstep()
        {
            if (triggerMode == FootstepTriggerMode.Distance) return;

            PlayFootstepAtPosition(raycastOrigin);
        }



        void PlayTerrainFootstep(Terrain terrain, Vector3 worldPosition)
        {
            // Get terrain data
            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPosition = terrain.transform.position;

            // Convert world position to terrain local position
            Vector3 localPosition = worldPosition - terrainPosition;

            // Convert to texture coordinates
            float x = localPosition.x / terrainData.size.x;
            float z = localPosition.z / terrainData.size.z;

            // Get the dominant texture at this position
            int dominantLayerIndex = GetDominantTerrainTexture(terrainData, x, z);

            // Find matching footstep sound
            FootstepsDatabase.TextureFootstepPair footstepPair =
                footstepData.GetFootstepForTerrainLayer(dominantLayerIndex);

            if (footstepPair != null)
            {
                PlayFootstepSound(footstepPair);
                if (enableDebug)
                    Debug.Log($"Playing terrain footstep for layer {dominantLayerIndex}");
            }
            else
            {
                PlayDefaultFootstep();
                if (enableDebug)
                    Debug.Log($"No footstep found for terrain layer {dominantLayerIndex}, using default");
            }
        }

        int GetDominantTerrainTexture(TerrainData terrainData, float x, float z)
        {
            // Clamp normalized coords
            float normalizedX = Mathf.Clamp01(x);
            float normalizedZ = Mathf.Clamp01(z);
            int dominantLayer = TerrainSplatCache.GetDominantLayer(
                terrainData,
                normalizedX,
                normalizedZ
            );

            return dominantLayer;
        }

        void PlayMeshFootstep(MeshRenderer meshRenderer, RaycastHit hit)
        {
            // Get the main texture from the material
            Material material = meshRenderer.material;
            Texture2D mainTexture = material.mainTexture as Texture2D;

            if (mainTexture != null)
            {
                FootstepsDatabase.TextureFootstepPair footstepPair = footstepData.GetFootstepForTexture(mainTexture);

                if (footstepPair != null)
                {
                    PlayFootstepSound(footstepPair);
                    if (enableDebug)
                        Debug.Log($"Playing mesh footstep for texture {mainTexture.name}");
                }
                else
                {
                    PlayDefaultFootstep();
                    if (enableDebug)
                        Debug.Log($"No footstep found for texture {mainTexture.name}, using default");
                }
            }
            else
            {
                PlayDefaultFootstep();
                if (enableDebug)
                    Debug.Log("No main texture found, using default footstep");
            }
        }

        void PlayFootstepSound(FootstepsDatabase.TextureFootstepPair footstepPair)
        {
            AudioClip clip = footstepPair.GetRandomFootstep();
            if (clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.volume = footstepData.defaultVolume * footstepPair.volumeMultiplier;
                audioSource.pitch = 1f + Random.Range(-footstepPair.pitchVariation, footstepPair.pitchVariation);
                audioSource.Play();
            }
        }

        void PlayDefaultFootstep()
        {
            AudioClip clip = footstepData.GetDefaultFootstep();
            if (clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.volume = footstepData.defaultVolume;
                audioSource.pitch = 1f + Random.Range(-0.1f, 0.1f);
                audioSource.Play();
            }
        }



        private void OnDrawGizmos()
        {
            if(enableDebug == false)return;
            if (raycastOrigin == null) return;

            // Set up gizmo colors
            Color rayColor = isGrounded ? Color.green : Color.red;
            Color originColor = Color.yellow;
            Color distanceColor = Color.blue;

            // Draw raycast line for main origin
            Gizmos.color = rayColor;
            Vector3 rayStart = raycastOrigin.position + Vector3.up * raycastOffset;
            Vector3 rayEnd = rayStart + Vector3.down * raycastDistance;
            Gizmos.DrawLine(rayStart, rayEnd);

            // Draw raycast origin sphere
            Gizmos.color = originColor;
            Gizmos.DrawWireSphere(rayStart, 0.1f);




            // Draw step distance visualization (only for distance-based modes)
            if (footstepData != null && (triggerMode == FootstepTriggerMode.Distance))
            {
                Gizmos.color = distanceColor;
                Gizmos.DrawWireSphere(raycastOrigin.position, footstepData.stepDistance);

                // Draw progress indicator
                float progress = distanceTraveled / footstepData.stepDistance;
                Gizmos.color = Color.Lerp(Color.blue, Color.cyan, progress);
                Gizmos.DrawWireSphere(raycastOrigin.position, footstepData.stepDistance * progress);
            }

            // Draw last footstep position (only for distance-based modes)
            if (triggerMode == FootstepTriggerMode.Distance)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(lastFootstepPosition, Vector3.one * 0.2f);
            }

            // Draw ground hit point if grounded
            if (isGrounded)
            {
                RaycastHit hit;
                Vector3 rayOrigin = raycastOrigin.position + Vector3.up * raycastOffset;
                if (Physics.Raycast(rayOrigin, Vector3.down, out hit, raycastDistance,
                        footstepData?.groundLayerMask ?? -1))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(hit.point, 0.15f);
                    Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.5f);
                }
            }
        }
    }
}