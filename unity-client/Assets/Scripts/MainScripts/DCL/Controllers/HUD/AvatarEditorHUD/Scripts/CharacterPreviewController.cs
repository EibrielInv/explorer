using DCL;
using System;
using System.Collections;
using UnityEngine;

public class CharacterPreviewController : MonoBehaviour
{
    private const int SNAPSHOT_BODY_WIDTH_RES = 512;
    private const int SNAPSHOT_BODY_HEIGHT_RES = 1024;

    private const int SNAPSHOT_FACE_WIDTH_RES = 512;
    private const int SNAPSHOT_FACE_HEIGHT_RES = 512;

    private const int SNAPSHOT_FACE_256_WIDTH_RES = 256;
    private const int SNAPSHOT_FACE_256_HEIGHT_RES = 256;

    private const int SNAPSHOT_FACE_128_WIDTH_RES = 128;
    private const int SNAPSHOT_FACE_128_HEIGHT_RES = 128;

    private const int SUPERSAMPLING = 1;
    private const float CAMERA_TRANSITION_TIME = 0.5f;
    private static int CHARACTER_PREVIEW_LAYER => LayerMask.NameToLayer("CharacterPreview");

    public delegate void OnSnapshotsReady(Sprite face, Sprite face128, Sprite face256, Sprite body);

    public enum CameraFocus
    {
        DefaultEditing,
        FaceEditing,
        FaceSnapshot,
        BodySnapshot
    }

    private System.Collections.Generic.Dictionary<CameraFocus, Transform> cameraFocusLookUp;

    public new Camera camera;
    public AvatarRenderer avatarRenderer;

    public Transform defaultEditingTemplate;
    public Transform faceEditingTemplate;

    public Transform faceSnapshotTemplate;
    public Transform bodySnapshotTemplate;

    private void Awake()
    {
        cameraFocusLookUp = new System.Collections.Generic.Dictionary<CameraFocus, Transform>()
        {
            { CameraFocus.DefaultEditing, defaultEditingTemplate },
            { CameraFocus.FaceEditing, faceEditingTemplate },
            { CameraFocus.FaceSnapshot, faceSnapshotTemplate },
            { CameraFocus.BodySnapshot, bodySnapshotTemplate },
        };
    }

    public void UpdateModel(AvatarModel newModel, Action onDone)
    {
        CoroutineStarter.Start(UpdateModelRoutine(newModel, onDone));
    }

    private IEnumerator UpdateModelRoutine(AvatarModel newModel, Action onDone)
    {
        bool avatarDone = false;
        bool avatarFailed = false;
        avatarRenderer.ApplyModel(newModel, () => avatarDone = true, () => avatarFailed = true);
        yield return new DCL.WaitUntil(() => avatarDone || avatarFailed);

        if (avatarDone)
        {
            SetLayerRecursively(avatarRenderer.gameObject, CHARACTER_PREVIEW_LAYER);
        }

        onDone?.Invoke();
    }

    private void SetLayerRecursively(GameObject gameObject, int layer)
    {
        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void TakeSnapshots(OnSnapshotsReady callback)
    {
        StartCoroutine(TakeSnapshots_Routine(callback));
    }

    private IEnumerator TakeSnapshots_Routine(OnSnapshotsReady callback)
    {
        var current = camera.targetTexture;
        camera.targetTexture = null;
        var avatarAnimator = avatarRenderer.gameObject.GetComponent<AvatarAnimatorLegacy>();

        SetFocus(CameraFocus.FaceSnapshot, false);
        avatarAnimator.Reset();
        yield return null;
        Sprite face = Snapshot(SNAPSHOT_FACE_WIDTH_RES, SNAPSHOT_FACE_HEIGHT_RES);
        Sprite face128 = Snapshot(SNAPSHOT_FACE_128_WIDTH_RES, SNAPSHOT_FACE_128_HEIGHT_RES);
        Sprite face256 = Snapshot(SNAPSHOT_FACE_256_WIDTH_RES, SNAPSHOT_FACE_256_HEIGHT_RES);

        SetFocus(CameraFocus.BodySnapshot, false);
        avatarAnimator.Reset();
        yield return null;
        Sprite body = Snapshot(SNAPSHOT_BODY_WIDTH_RES, SNAPSHOT_BODY_HEIGHT_RES);

        SetFocus(CameraFocus.DefaultEditing, false);

        camera.targetTexture = current;
        callback?.Invoke(face, face128, face256, body);
    }

    private Sprite Snapshot(int width, int height)
    {
        RenderTexture rt = new RenderTexture(width * SUPERSAMPLING, height * SUPERSAMPLING, 32);
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        camera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenShot.Apply();

        return Sprite.Create(screenShot, new Rect(0, 0, screenShot.width, screenShot.height), Vector2.zero);
    }

    private Coroutine cameraTransitionCoroutine;

    public void SetFocus(CameraFocus focus, bool useTransition = true)
    {
        SetFocus(cameraFocusLookUp[focus], useTransition);
    }

    private void SetFocus(Transform transform, bool useTransition = true)
    {
        if (cameraTransitionCoroutine != null)
        {
            StopCoroutine(cameraTransitionCoroutine);
        }

        if (useTransition)
        {
            cameraTransitionCoroutine = StartCoroutine(CameraTransition(camera.transform.position, transform.position, camera.transform.rotation, transform.rotation, CAMERA_TRANSITION_TIME));
        }
        else
        {
            var cameraTransform = camera.transform;
            cameraTransform.position = transform.position;
            cameraTransform.rotation = transform.rotation;
        }
    }

    private IEnumerator CameraTransition(Vector3 initPos, Vector3 endPos, Quaternion initRotation, Quaternion endRotation, float time)
    {
        var cameraTransform = camera.transform;
        float currentTime = 0;

        float inverseTime = 1 / time;
        while (currentTime < time)
        {
            currentTime = Mathf.Clamp(currentTime + Time.deltaTime, 0, time);
            cameraTransform.position = Vector3.Lerp(initPos, endPos, currentTime * inverseTime);
            cameraTransform.rotation = Quaternion.Lerp(initRotation, endRotation, currentTime * inverseTime);
            yield return null;
        }

        cameraTransitionCoroutine = null;
    }

    public void Rotate(float rotationVelocity)
    {
        avatarRenderer.transform.Rotate(Time.deltaTime * rotationVelocity * Vector3.up);
    }
}
