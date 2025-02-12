using System;
using DCL.Helpers;
using UnityEngine;

public class BodyShapeController : WearableController
{
    public string bodyShapeType => wearable.id;
    private Transform animationTarget;

    public BodyShapeController(WearableItem wearableItem) : base(wearableItem, wearableItem?.id) { }
    protected BodyShapeController(WearableController original) : base(original) { }

    public SkinnedMeshRenderer skinnedMeshRenderer { get; private set; }

    public override void Load(Transform parent, Action<WearableController> onSuccess, Action<WearableController> onFail)
    {
        animationTarget = parent;
        skinnedMeshRenderer = null;
        base.Load(parent, onSuccess, onFail);
    }

    public void RemoveUnusedParts()
    {
        AvatarUtils.RemoveUnusedBodyParts_Hack(assetContainer.gameObject);
    }

    public void SetupEyes(Material material, Texture texture, Texture mask, Color color)
    {
        var eyesMaterial = new Material(material);

        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                eyesMaterial.SetTexture(AvatarUtils._EyesTexture, texture);
                eyesMaterial.SetTexture(AvatarUtils._IrisMask, mask);
                eyesMaterial.SetColor(AvatarUtils._EyeTint, color);

                return eyesMaterial;
            },
            "eyes");
    }

    public void SetupEyebrows(Material material, Texture texture, Color color)
    {
        var eyebrowsMaterial = new Material(material);
        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                eyebrowsMaterial.SetTexture(AvatarUtils._BaseMap, texture);

                //NOTE(Brian): This isn't an error, we must also apply hair color to this mat
                eyebrowsMaterial.SetColor(AvatarUtils._BaseColor, color);

                return eyebrowsMaterial;
            },
            "eyebrows");
    }

    public void SetupMouth(Material material, Texture texture, Color color)
    {
        var mouthMaterial = new Material(material);
        AvatarUtils.MapSharedMaterialsRecursively(assetContainer.transform,
            (mat) =>
            {
                mouthMaterial.SetTexture(AvatarUtils._BaseMap, texture);

                //NOTE(Brian): This isn't an error, we must also apply skin color to this mat
                mouthMaterial.SetColor(AvatarUtils._BaseColor, color);

                return mouthMaterial;
            },
            "mouth");
    }

    private Animation PrepareAnimation()
    {
        Animation createdAnimation = null;

        //NOTE(Brian): Fix to support hierarchy difference between AssetBundle and GLTF wearables.
        Utils.ForwardTransformChildTraversal<Transform>((x) =>
            {
                if (x.name.Contains("Armature"))
                {
                    createdAnimation = x.parent.gameObject.GetOrCreateComponent<Animation>();
                    return false; //NOTE(Brian): If we return false the traversal is stopped.
                }

                return true;
            },
            assetContainer.transform);

        createdAnimation.cullingType = AnimationCullingType.BasedOnRenderers;
        return createdAnimation;
    }

    protected override void PrepareWearable(GameObject assetContainer)
    {
        base.PrepareWearable(assetContainer);
        skinnedMeshRenderer = assetContainer.GetComponentInChildren<SkinnedMeshRenderer>();
        var animation = PrepareAnimation();
        var animator = animationTarget.GetComponent<AvatarAnimatorLegacy>();
        animator.BindBodyShape(animation, bodyShapeType, animationTarget);
    }
}
