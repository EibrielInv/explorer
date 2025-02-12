using DCL.Components;
using DCL.Models;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace DCL.Controllers
{
    public class SceneBoundariesChecker
    {
        [System.NonSerialized]
        public float timeBetweenChecks = 1f;

        HashSet<DecentralandEntity> entitiesToCheck = new HashSet<DecentralandEntity>();
        HashSet<DecentralandEntity> checkedEntities = new HashSet<DecentralandEntity>();
        Coroutine entitiesCheckRoutine = null;
        float lastCheckTime;

        public SceneBoundariesChecker()
        {
            entitiesCheckRoutine = CoroutineStarter.Start(CheckEntities());
            lastCheckTime = Time.realtimeSinceStartup;
        }

        // TODO: Improve MessagingControllersManager.i.timeBudgetCounter usage once we have the centralized budget controller for our immortal coroutines
        IEnumerator CheckEntities()
        {
            while (true)
            {
                float elapsedTime = Time.realtimeSinceStartup - lastCheckTime;
                if (entitiesToCheck.Count > 0 && (timeBetweenChecks <= 0f || elapsedTime >= timeBetweenChecks))
                {
                    using (var iterator = entitiesToCheck.GetEnumerator())
                    {
                        while (iterator.MoveNext())
                        {
                            if (MessagingControllersManager.i.timeBudgetCounter <= 0f) break;

                            float startTime = Time.realtimeSinceStartup;

                            EvaluateEntityPosition(iterator.Current);
                            checkedEntities.Add(iterator.Current);

                            float finishTime = Time.realtimeSinceStartup;
                            MessagingControllersManager.i.timeBudgetCounter -= (finishTime - startTime);
                        }
                    }

                    // As we can't modify the hashset while traversing it, we keep track of the entities that should be removed afterwards
                    using (var iterator = checkedEntities.GetEnumerator())
                    {
                        while (iterator.MoveNext())
                        {
                            entitiesToCheck.Remove(iterator.Current);
                        }
                    }

                    checkedEntities.Clear();

                    lastCheckTime = Time.realtimeSinceStartup;
                }

                yield return null;
            }
        }

        public void Stop()
        {
            if (entitiesCheckRoutine != null)
                CoroutineStarter.Stop(entitiesCheckRoutine);
        }

        public void AddEntityToBeChecked(DecentralandEntity entity)
        {
            if (!SceneController.i.useBoundariesChecker) return;

            entitiesToCheck.Add(entity);
        }

        public void RemoveEntityToBeChecked(DecentralandEntity entity)
        {
            if (!SceneController.i.useBoundariesChecker) return;

            entitiesToCheck.Remove(entity);
        }

        public void EvaluateEntityPosition(DecentralandEntity entity)
        {
            if (entity == null || entity.scene == null) return;

            // Recursively evaluate entity children as well, we need to check this up front because this entity may not have meshes of its own, but the children may.
            if (entity.children.Count > 0)
            {
                using (var iterator = entity.children.GetEnumerator())
                {
                    while (iterator.MoveNext())
                    {
                        EvaluateEntityPosition(iterator.Current.Value);
                    }
                }
            }

            if (entity.meshRootGameObject == null || entity.meshesInfo.renderers == null || entity.meshesInfo.renderers.Length == 0) return;

            // If the mesh is being loaded we should skip the evaluation (it will be triggered again later when the loading finishes)
            if (entity.meshRootGameObject.GetComponent<MaterialTransitionController>()) // the object's MaterialTransitionController is destroyed when it finishes loading
            {
                return;
            }
            else
            {
                var loadWrapper = LoadableShape.GetLoaderForEntity(entity);

                if (loadWrapper != null && !loadWrapper.alreadyLoaded)
                    return;
            }

            EvaluateMeshBounds(entity);
        }

        void EvaluateMeshBounds(DecentralandEntity entity)
        {
            Bounds meshBounds = entity.meshesInfo.mergedBounds;

            // 1st check (full mesh AABB)
            bool isInsideBoundaries = entity.scene.IsInsideSceneBoundaries(meshBounds);

            // 2nd check (submeshes AABB)
            if (!isInsideBoundaries)
            {
                isInsideBoundaries = AreSubmeshesInsideBoundaries(entity);
            }

            UpdateEntityMeshesValidState(entity, isInsideBoundaries, meshBounds);

            UpdateEntityCollidersValidState(entity, isInsideBoundaries);
        }

        protected virtual bool AreSubmeshesInsideBoundaries(DecentralandEntity entity)
        {
            for (int i = 0; i < entity.meshesInfo.renderers.Length; i++)
            {
                if (!entity.scene.IsInsideSceneBoundaries(entity.meshesInfo.renderers[i].bounds))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual void UpdateEntityMeshesValidState(DecentralandEntity entity, bool isInsideBoundaries, Bounds meshBounds)
        {
            if (entity.meshesInfo.renderers[0] == null) return;

            if (isInsideBoundaries != entity.meshesInfo.renderers[0].enabled && entity.meshesInfo.currentShape.IsVisible())
            {
                for (int i = 0; i < entity.meshesInfo.renderers.Length; i++)
                {
                    entity.meshesInfo.renderers[i].enabled = isInsideBoundaries;
                }
            }
        }

        protected virtual void UpdateEntityCollidersValidState(DecentralandEntity entity, bool isInsideBoundaries)
        {
            int collidersCount = entity.meshesInfo.colliders.Count;
            if (collidersCount > 0 && isInsideBoundaries != entity.meshesInfo.colliders[0].enabled && entity.meshesInfo.currentShape.HasCollisions())
            {
                for (int i = 0; i < collidersCount; i++)
                {
                    entity.meshesInfo.colliders[i].enabled = isInsideBoundaries;
                }
            }
        }
    }
}
