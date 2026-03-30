using System.Collections;
using System.Threading;
using FTR.Gameplay.Common.Environment.Structures;
using UnityEngine;

namespace FTR.Gameplay.Server.Testing
{
    public class MeshStatAnalyzer : MonoBehaviour
    {
        private IEnumerator Start()
        {
            Debug.Log("[MeshAnalyzer] Waiting for 10 seconds to let all structures load...");
            yield return new WaitForSeconds(10f);
            Debug.Log("[MeshAnalyzer] Analyzing mesh statistics...");

            var structures = FindObjectsByType<StructureController>(FindObjectsSortMode.None);

            int sceneTotal = 0;

            foreach (var structure in structures)
            {
                int structureTriangles = 0;

                var meshFilters = structure.GetComponentsInChildren<MeshFilter>();

                foreach (var mf in meshFilters)
                {
                    if (mf.sharedMesh != null)
                    {
                        structureTriangles += mf.sharedMesh.triangles.Length / 3;
                    }
                }

                sceneTotal += structureTriangles;

                Debug.Log($"[MeshAnalyzer] {structure.name} -> {structureTriangles} triangles");
            }

            Debug.Log($"[MeshAnalyzer] TOTAL SCENE TRIANGLES (structures only): {sceneTotal}");
        }
    }
}
