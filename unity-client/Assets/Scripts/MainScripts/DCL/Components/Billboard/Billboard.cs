﻿using DCL.Components;
using System.Collections;
using UnityEngine;

namespace DCL
{
    public class Billboard : BaseComponent
    {
        [System.Serializable]
        public class Model
        {
            public bool x = true;
            public bool y = true;
            public bool z = true;
        }

        public Model model = new Model();

        Transform entityTransform;
        Vector3Variable cameraPosition => CommonScriptableObjects.cameraPosition;
        Vector3 lastPosition;

        public override IEnumerator ApplyChanges(string newJson)
        {
            DCLCharacterController.OnCharacterMoved -= OnCharacterMoved;
            DCLCharacterController.OnCharacterMoved += OnCharacterMoved;

            model = SceneController.i.SafeFromJson<Model>(newJson);

            ChangeOrientation();

            if (entityTransform == null)
            {
                //NOTE(Zak): We have to wait one frame because if not the entity will be null. (I'm Brian, but Zak wrote the code so read this in his voice)
                yield return null;
                entityTransform = entity.gameObject.transform;
            }
        }

        public void OnDestroy()
        {
            DCLCharacterController.OnCharacterMoved -= OnCharacterMoved;
        }

        // This runs on LateUpdate() instead of Update() to be applied AFTER the transform was moved by the transform component
        public void LateUpdate()
        {
            //NOTE(Brian): This fixes #757 (https://github.com/decentraland/unity-client/issues/757)
            //             We must find a more performant way to handle this, until that time, this is the approach.

            if (transform.position == lastPosition) return;

            lastPosition = transform.position;

            ChangeOrientation();
        }

        Vector3 GetLookAtVector()
        {
            bool hasTextShape = entity.components.ContainsKey(Models.CLASS_ID_COMPONENT.TEXT_SHAPE);
            Vector3 lookAtDir = hasTextShape ? (entityTransform.position - cameraPosition) : (cameraPosition - entityTransform.position);

            // Note (Zak): This check is here to avoid normalizing twice if not needed
            if (!(model.x && model.y && model.z))
            {
                lookAtDir.Normalize();

                // Note (Zak): Model x,y,z are axis that we want to enable/disable
                // while lookAtDir x,y,z are the components of the look-at vector
                if (!model.x || model.z)
                    lookAtDir.y = entityTransform.forward.y;
                if (!model.y)
                    lookAtDir.x = entityTransform.forward.x;
            }

            return lookAtDir.normalized;
        }

        void OnCharacterMoved(DCLCharacterPosition position)
        {
            ChangeOrientation();
        }

        void ChangeOrientation()
        {
            if (entityTransform != null)
            {
                entityTransform.forward = GetLookAtVector();
            }
        }
    }
}
