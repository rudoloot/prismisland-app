using UnityEngine;
using PrismIsland.Data;

namespace PrismIsland.Application
{
    public enum InstallState { None, Positioning, Confirming }

    public class InstallationManager : MonoBehaviour
    {
        public static InstallationManager Instance { get; private set; }

        private InstallState currentState = InstallState.None;
        private StructureDataSO currentStructure;
        private GameObject previewObject;
        private Camera mainCamera;
        private Vector3 fixedInstallPosition;
        
        [Header("Settings")]
        public LayerMask groundLayer;
        public float gridSize = 1f;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            mainCamera = Camera.main;
            
            // Try to find ground layer if not set
            if (groundLayer.value == 0)
            {
                int layer = LayerMask.NameToLayer("Ground");
                if (layer != -1) groundLayer = 1 << layer;
                else groundLayer = 1 << LayerMask.NameToLayer("Default"); // fallback
            }
        }

        public void StartInstallation(StructureDataSO structure)
        {
            if (currentState != InstallState.None) EndInstallation();

            currentStructure = structure;
            currentState = InstallState.Positioning;

            // Create preview plate
            previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(previewObject.GetComponent<Collider>()); // No collision for preview
            previewObject.transform.localScale = new Vector3(1f, 0.1f, 1f);
            
            // Set material to slightly transparent or fallback color
            Renderer r = previewObject.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color c = structure.fallbackColor;
            c.a = 0.5f; // transparent preview
            
            // Make material transparent in URP
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Surface", 1.0f); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            
            r.material = mat;
        }

        public void EndInstallation()
        {
            currentState = InstallState.None;
            currentStructure = null;
            if (previewObject != null)
            {
                Destroy(previewObject);
            }
            
            // Ensure Time is restored
            Time.timeScale = 1f;
        }

        void Update()
        {
            if (currentState == InstallState.None || currentStructure == null) return;

            if (currentState == InstallState.Positioning)
            {
                // Update preview position
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
                {
                    Vector3 point = hit.point;
                    // Grid Snapping
                    point.x = Mathf.Round(point.x / gridSize) * gridSize;
                    point.z = Mathf.Round(point.z / gridSize) * gridSize;
                    point.y += 0.05f; // slightly above ground to prevent Z-fighting
                    
                    previewObject.transform.position = point;
                    previewObject.SetActive(true);

                    // Check if mouse is over the cancel button
                    float w = Screen.width * 0.15f;
                    float h = Screen.height * 0.08f;
                    float paddingX = 20f;
                    float paddingY = 80f; // 상단 UI와 안 겹치게 내림
                    Rect cancelRect = new Rect(Screen.width - w - paddingX, paddingY, w, h);
                    Vector2 mousePosGUI = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                    // Install on click (only if not over UI button)
                    if (Input.GetMouseButtonDown(0) && !cancelRect.Contains(mousePosGUI))
                    {
                        currentState = InstallState.Confirming;
                        fixedInstallPosition = point;
                    }
                }
                else
                {
                    previewObject.SetActive(false);
                }
                
                // Cancel entirely on Escape or Right Click
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    EndInstallation();
                }
            }
            else if (currentState == InstallState.Confirming)
            {
                // Blinking effect
                Renderer r = previewObject.GetComponent<Renderer>();
                if (r != null)
                {
                    Color c = currentStructure.fallbackColor;
                    c.a = 0.3f + 0.5f * Mathf.PingPong(Time.unscaledTime * 3f, 1f);
                    r.material.SetColor("_BaseColor", c);
                }

                // Cancel Confirming on Escape or Right Click
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
                {
                    currentState = InstallState.Positioning;
                    if (r != null)
                    {
                        Color bc = currentStructure.fallbackColor;
                        bc.a = 0.5f;
                        r.material.SetColor("_BaseColor", bc);
                    }
                }
            }
        }

        private void InstallStructure(Vector3 position, Quaternion rotation)
        {
            // 1. Consume item
            InventoryManager.Instance.RemoveItem(currentStructure, 1);

            // 2. Spawn structure
            GameObject newObj;
            if (currentStructure.prefabToInstall != null)
            {
                newObj = Instantiate(currentStructure.prefabToInstall, position, rotation);
            }
            else
            {
                // Spawn default flat plate
                newObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                newObj.name = currentStructure.itemName + "_Installed";
                newObj.transform.position = position;
                newObj.transform.rotation = rotation;
                newObj.transform.localScale = new Vector3(1f, 0.1f, 1f);
                
                Renderer r = newObj.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", currentStructure.fallbackColor);
                r.material = mat;

                Collider col = newObj.GetComponent<Collider>();
                if (currentStructure.structureType == StructureType.Walkable)
                {
                    col.isTrigger = true;
                }
                else if (currentStructure.structureType == StructureType.Obstacle)
                {
                    col.isTrigger = false;
                    // Make collider taller to block players
                    BoxCollider box = col as BoxCollider;
                    if (box != null)
                    {
                        box.center = new Vector3(0, 10f, 0); // local space (since scale y is 0.1, 10 means 1 unit up)
                        box.size = new Vector3(1f, 20f, 1f); // 2 units tall
                    }
                }
            }

            // 3. End Mode and Return to Inventory
            EndInstallation();
            UIManager.Instance.OpenInventoryTab();
        }

        void OnGUI()
        {
            if (currentState == InstallState.None) return;

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            int baseFontSize = Mathf.RoundToInt(Screen.width * 0.015f);
            btnStyle.fontSize = baseFontSize;

            float w = Screen.width * 0.15f;
            float h = Screen.height * 0.08f;
            float paddingX = 20f;
            float paddingY = 80f; // 상단 UI와 안 겹치게 내림

            Rect cancelRect = new Rect(Screen.width - w - paddingX, paddingY, w, h);
            
            GUI.color = new Color(1f, 0.5f, 0.5f, 1f);
            if (GUI.Button(cancelRect, "설치 종료\n(Exit)", btnStyle))
            {
                EndInstallation();
            }
            GUI.color = Color.white;

            if (currentState == InstallState.Confirming)
            {
                float menuW = Screen.width * 0.12f;
                float menuH = Screen.height * 0.08f;
                float spacing = 10f;
                float startX = Screen.width - menuW - paddingX;
                float startY = Screen.height * 0.4f;

                if (GUI.Button(new Rect(startX, startY, menuW, menuH), "회전\n(Rotate)", btnStyle))
                {
                    previewObject.transform.Rotate(0, 90f, 0);
                }

                if (GUI.Button(new Rect(startX, startY + menuH + spacing, menuW, menuH), "생성\n(Create)", btnStyle))
                {
                    InstallStructure(fixedInstallPosition, previewObject.transform.rotation);
                }

                if (GUI.Button(new Rect(startX, startY + (menuH + spacing) * 2, menuW, menuH), "취소\n(Cancel)", btnStyle))
                {
                    currentState = InstallState.Positioning;
                    Renderer r = previewObject.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Color bc = currentStructure.fallbackColor;
                        bc.a = 0.5f;
                        r.material.SetColor("_BaseColor", bc);
                    }
                }
            }
        }
    }
}
