using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

internal static class ScreenEffectSetup
{
    internal static Camera FindRenderCamera()
    {
        var brain = Object.FindFirstObjectByType<CinemachineBrain>();
        return brain != null ? brain.GetComponent<Camera>() : Camera.main;
    }

    internal static (GameObject go, ParticleSystem ps) Instantiate(
        IObjectResolver resolver,
        GameObject prefab,
        Camera renderCamera
    )
    {
        var go = resolver.Instantiate(prefab);
        var comp = go.GetComponent("HS_ScreenEffect") as MonoBehaviour;
        if (comp != null)
        {
            var type = comp.GetType();
            type.GetField("sourceCamera")?.SetValue(comp, renderCamera);
            if (renderCamera != null)
                type.GetField("fallbackDistance")
                    ?.SetValue(comp, renderCamera.nearClipPlane + 0.05f);
            var ps = type.GetField("screenEffect")?.GetValue(comp) as ParticleSystem;
            go.SetActive(false);
            return (go, ps);
        }
        go.SetActive(false);
        return (go, null);
    }

    internal static void Play(GameObject go, ParticleSystem ps)
    {
        if (go == null)
            return;
        go.SetActive(true);
        if (ps != null)
            ps.Play();
    }

    internal static void Stop(GameObject go, ParticleSystem ps)
    {
        if (go == null)
            return;
        if (ps != null)
            ps.Stop();
        go.SetActive(false);
    }
}
