using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityGLTF.Cache;

namespace DCL.Helpers
{
    public static class MaterialCachingHelper
    {
        [System.Flags]
        public enum Mode
        {
            NONE = 0,
            CACHE_MATERIALS = 1,
            CACHE_SHADERS = 2,
            CACHE_EVERYTHING = CACHE_MATERIALS | CACHE_SHADERS,
        }

        public static bool timeBudgetEnabled => CommonScriptableObjects.rendererState.Get();
        public static float timeBudgetMax = 0.003f;
        public static float timeBudget = 0;

        public static Dictionary<string, Shader> shaderByHash = new Dictionary<string, Shader>();

        private static Shader mainShader;

        public static string ComputeHash(Material mat)
        {
            return mat.ComputeCRC().ToString();
        }

        static Shader EnsureMainShader()
        {
            if (mainShader == null)
            {
                mainShader = Shader.Find("DCL/LWRP/Lit");
            }

            return mainShader;
        }

        public static IEnumerator Process(List<Renderer> renderers, bool enableRenderers = true, Mode cachingFlags = Mode.CACHE_EVERYTHING)
        {
            if (renderers == null)
                yield break;

            int renderersCount = renderers.Count;

            if (renderersCount == 0)
                yield break;

            var matList = new List<Material>(1);

            for (int i = 0; i < renderersCount; i++)
            {
                Renderer r = renderers[i];

                if (!enableRenderers)
                    r.enabled = false;

                matList.Clear();

                for (int i1 = 0; i1 < r.sharedMaterials.Length; i1++)
                {
                    Material mat = r.sharedMaterials[i1];

                    float elapsedTime = Time.realtimeSinceStartup;

                    if ((cachingFlags & Mode.CACHE_SHADERS) != 0)
                    {
                        string shaderHash = mat.shader.name;

                        if (!shaderByHash.ContainsKey(shaderHash))
                        {
                            if (!mat.shader.name.Contains("Error"))
                            {
                                shaderByHash.Add(shaderHash, Shader.Find(mat.shader.name));
                            }
                            else
                            {
                                shaderByHash.Add(shaderHash, EnsureMainShader());
                            }
                        }

                        mat.shader = shaderByHash[shaderHash];
                    }

                    if ((cachingFlags & Mode.CACHE_MATERIALS) != 0)
                    {
                        int crc = mat.ComputeCRC();
                        string hash = crc.ToString();

                        RefCountedMaterialData refCountedMat;

                        if (!PersistentAssetCache.MaterialCacheByCRC.ContainsKey(hash))
                        {
                            //NOTE(Brian): Have to copy material because will be unloaded later.
                            var materialCopy = new Material(mat);

#if UNITY_EDITOR
                            //NOTE(Brian): _Surface is only used by the material inspector. If we don't modify this
                            //             the inspector overrides the _ALPHABLEND_ON and the material turns opaque.
                            //             This is confusing when debugging.
                            if (materialCopy.IsKeywordEnabled("_ALPHABLEND_ON"))
                                materialCopy.SetFloat("_Surface", 1);
                            else
                                materialCopy.SetFloat("_Surface", 0);
#endif
                            SRPBatchingHelper.OptimizeMaterial(r, materialCopy);

                            PersistentAssetCache.MaterialCacheByCRC.Add(hash, new RefCountedMaterialData(hash, materialCopy));
                        }

                        refCountedMat = PersistentAssetCache.MaterialCacheByCRC[hash];
                        refCountedMat.IncreaseRefCount();

                        matList.Add(refCountedMat.material);
                    }
                    else
                    {
                        matList.Add(new Material(mat));
                    }

                    if (timeBudgetEnabled)
                    {
                        elapsedTime = Time.realtimeSinceStartup - elapsedTime;
                        timeBudget -= elapsedTime;

                        if (timeBudget < 0)
                        {
                            yield return null;
                            timeBudget += timeBudgetMax;
                        }
                    }
                }

                r.sharedMaterials = matList.ToArray();
            }
        }
    }
}