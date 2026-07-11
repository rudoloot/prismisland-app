using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class EnvironmentGeneratorMenu : EditorWindow
{
    private static float mapSize = 50f;
    private static int attemptCount = 3000;
    private static float minSpacing = 2.5f;
    private static float pathWidth = 4f;
    private static float campSafeMargin = 2f; // Added to camp.spawnRadius

    [MenuItem("PrismIsland/Generate Environment")]
    public static void GenerateEnvironment()
    {
        // 1. 기존 나무와 바위 삭제
        CleanupEnvironment();

        // 2. 부모 객체 생성
        GameObject envParent = new GameObject("Environment");
        GameObject treesParent = new GameObject("Environment_Trees");
        GameObject rocksParent = new GameObject("Environment_Rocks");
        treesParent.transform.SetParent(envParent.transform);
        rocksParent.transform.SetParent(envParent.transform);

        // 3. 캠프 정보 수집
        EnemyCamp[] camps = FindObjectsByType<EnemyCamp>(FindObjectsSortMode.None);
        Vector3 origin = Vector3.zero;

        List<Vector3> placedPositions = new List<Vector3>();
        int treeCount = 0;
        int rockCount = 0;

        Material treeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        treeMat.SetColor("_BaseColor", new Color(0.1f, 0.4f, 0.1f)); // Dark Green

        Material rockMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        rockMat.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f)); // Gray

        // 4. 무작위 배치 시도
        for (int i = 0; i < attemptCount; i++)
        {
            float rx = Random.Range(-mapSize, mapSize);
            float rz = Random.Range(-mapSize, mapSize);
            Vector3 pos = new Vector3(rx, 0f, rz);

            // 캠프 반경 체크
            bool isValid = true;
            foreach (var camp in camps)
            {
                float distToCamp = Vector3.Distance(pos, camp.transform.position);
                if (distToCamp < camp.spawnRadius + campSafeMargin)
                {
                    isValid = false;
                    break;
                }

                // 길(Origin -> Camp) 거리 체크
                float distToPath = DistancePointToLineSegment(pos, origin, camp.transform.position);
                if (distToPath < pathWidth)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid) continue;

            // 겹침 체크
            foreach (var placed in placedPositions)
            {
                if (Vector3.Distance(pos, placed) < minSpacing)
                {
                    isValid = false;
                    break;
                }
            }

            if (!isValid) continue;

            // 배치 결정
            placedPositions.Add(pos);
            bool isRock = Random.value < 0.2f; // 20% 확률로 바위

            if (isRock)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock_" + rockCount;
                rock.transform.SetParent(rocksParent.transform);

                float scaleX = Random.Range(1f, 2.5f);
                float scaleY = Random.Range(0.8f, 2f);
                float scaleZ = Random.Range(1f, 2.5f);
                rock.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);
                
                rock.transform.position = new Vector3(pos.x, scaleY / 2f, pos.z);
                rock.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                Renderer r = rock.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = rockMat;

                rockCount++;
            }
            else
            {
                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                tree.name = "Tree_" + treeCount;
                tree.transform.SetParent(treesParent.transform);

                float scaleY = Random.Range(1.5f, 3.5f);
                float scaleXZ = Random.Range(0.8f, 1.5f);
                tree.transform.localScale = new Vector3(scaleXZ, scaleY, scaleXZ);

                tree.transform.position = new Vector3(pos.x, scaleY, pos.z); // 캡슐 중심 = 높이 / 2, 스케일 반영 시 2 * scaleY 이므로 높이는 2*scaleY. 바닥은 0이 되려면 scaleY
                
                Renderer r = tree.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = treeMat;

                treeCount++;
            }
        }

        Debug.Log($"Environment Generated: {treeCount} Trees, {rockCount} Rocks.");
        Undo.RegisterCreatedObjectUndo(envParent, "Generate Environment");
    }

    private static void CleanupEnvironment()
    {
        string[] targets = { "Environment", "Environment_Trees", "Environment_Rocks", "Trees" };
        foreach (var t in targets)
        {
            GameObject obj = GameObject.Find(t);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private static float DistancePointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDir = lineEnd - lineStart;
        float lengthSq = lineDir.sqrMagnitude;

        if (lengthSq == 0f)
            return Vector3.Distance(point, lineStart);

        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, lineDir) / lengthSq);
        Vector3 projection = lineStart + t * lineDir;
        return Vector3.Distance(point, projection);
    }
}
