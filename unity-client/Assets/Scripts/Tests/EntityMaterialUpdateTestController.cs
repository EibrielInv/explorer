﻿using DCL;
using DCL.Components;
using DCL.Helpers;
using DCL.Models;
using UnityEngine;

public class EntityMaterialUpdateTestController : MonoBehaviour
{
    void Start()
    {
        var sceneController = FindObjectOfType<SceneController>();
        var scenesToLoad = (Resources.Load("TestJSON/SceneLoadingTest") as TextAsset).text;

        sceneController.UnloadAllScenes();
        sceneController.LoadParcelScenes(scenesToLoad);

        var scene = sceneController.loadedScenes["0,0"];

        DCLTexture dclAtlasTexture = TestHelpers.CreateDCLTexture(
            scene,
            Utils.GetTestsAssetsPath() + "/Images/atlas.png",
            DCLTexture.BabylonWrapMode.CLAMP,
            FilterMode.Bilinear);

        DCLTexture dclAvatarTexture = TestHelpers.CreateDCLTexture(
            scene,
            Utils.GetTestsAssetsPath() + "/Images/avatar.png",
            DCLTexture.BabylonWrapMode.CLAMP,
            FilterMode.Bilinear);


        DecentralandEntity entity;

        TestHelpers.CreateEntityWithBasicMaterial(
            scene,
            new BasicMaterial.Model
            {
                texture = dclAtlasTexture.id,
            },
            out entity);

        TestHelpers.CreateEntityWithPBRMaterial(scene,
            new PBRMaterial.Model
            {
                albedoTexture = dclAvatarTexture.id,
                metallic = 0,
                roughness = 1,
            },
            out entity);

        PBRMaterial mat = TestHelpers.CreateEntityWithPBRMaterial(scene,
            new PBRMaterial.Model
            {
                albedoTexture = dclAvatarTexture.id,
                metallic = 1,
                roughness = 1,
                alphaTexture = dclAvatarTexture.id,
            },
            out entity);

        // Re-assign last PBR material to new entity
        BoxShape shape = TestHelpers.CreateEntityWithBoxShape(scene, new Vector3(5, 1, 2));
        BasicMaterial m =
            TestHelpers.SharedComponentCreate<BasicMaterial, BasicMaterial.Model>(scene, CLASS_ID.BASIC_MATERIAL);

        Color color1;
        ColorUtility.TryParseHtmlString("#FF9292", out color1);

        // Update material attached to 2 entities, adding albedoColor
        scene.SharedComponentUpdate(m.id, JsonUtility.ToJson(new DCL.Components.PBRMaterial.Model
        {
            albedoTexture = dclAvatarTexture.id,
            metallic = 1,
            roughness = 1,
            alphaTexture = dclAvatarTexture.id,
            albedoColor = color1
        }));
    }
}
