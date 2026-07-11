using UnityEngine;

namespace PrismIsland.Application
{
    public class MobileInputManager : MonoBehaviour
    {
        public static MobileInputManager Instance { get; private set; }

        public Vector2 MovementInput { get; private set; }
        
        private Vector2 touchStartPos;
        private Vector2 currentTouchPos;
        private bool isTouching = false;
        private int touchId = -1;
        
        private float maxRadius;
        
        void Awake()
        {
            if (Instance == null) {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            } else {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            maxRadius = Screen.height * 0.1f; // 10% of screen height
        }

        void Update()
        {
            MovementInput = Vector2.zero;

            if (Input.touchCount > 0)
            {
                foreach (Touch touch in Input.touches)
                {
                    // Using left half of the screen for movement
                    // Exclude touches on the upper portion to avoid UI conflicts if possible, 
                    // but for a simple joystick, just left half is fine.
                    if (!isTouching && touch.phase == TouchPhase.Began && touch.position.x < Screen.width / 2f)
                    {
                        isTouching = true;
                        touchId = touch.fingerId;
                        touchStartPos = touch.position;
                        currentTouchPos = touch.position;
                    }

                    if (isTouching && touch.fingerId == touchId)
                    {
                        if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                        {
                            currentTouchPos = touch.position;
                            Vector2 offset = currentTouchPos - touchStartPos;
                            
                            // Clamp visually
                            if (offset.magnitude > maxRadius) {
                                currentTouchPos = touchStartPos + offset.normalized * maxRadius;
                            }

                            // Normalize output (-1 to 1)
                            MovementInput = offset / maxRadius;
                            if (MovementInput.magnitude > 1f) {
                                MovementInput.Normalize();
                            }
                        }
                        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                        {
                            isTouching = false;
                            touchId = -1;
                            MovementInput = Vector2.zero;
                        }
                    }
                }
            }
#if UNITY_EDITOR || UNITY_STANDALONE
            // Mouse simulation for testing in Editor
            else 
            {
                if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < Screen.width / 2f)
                {
                    // Make sure mouse is not over a UI popup maybe?
                    // Simple logic for now.
                    isTouching = true;
                    touchStartPos = Input.mousePosition;
                    currentTouchPos = Input.mousePosition;
                }

                if (isTouching)
                {
                    if (Input.GetMouseButton(0))
                    {
                        currentTouchPos = Input.mousePosition;
                        Vector2 offset = currentTouchPos - touchStartPos;
                        
                        if (offset.magnitude > maxRadius) {
                            currentTouchPos = touchStartPos + offset.normalized * maxRadius;
                        }

                        MovementInput = offset / maxRadius;
                        if (MovementInput.magnitude > 1f) {
                            MovementInput.Normalize();
                        }
                    }
                    else if (Input.GetMouseButtonUp(0))
                    {
                        isTouching = false;
                        MovementInput = Vector2.zero;
                    }
                }
            }
#endif
        }

        void OnGUI()
        {
            if (isTouching)
            {
                // In OnGUI, Y is inverted (0 is top). Screen position Y is 0 at bottom.
                Vector2 guiStartPos = new Vector2(touchStartPos.x, Screen.height - touchStartPos.y);
                Vector2 guiCurrentPos = new Vector2(currentTouchPos.x, Screen.height - currentTouchPos.y);

                float bgSize = maxRadius * 2f;
                float stickSize = maxRadius * 0.8f;

                Rect bgRect = new Rect(guiStartPos.x - bgSize / 2f, guiStartPos.y - bgSize / 2f, bgSize, bgSize);
                Rect stickRect = new Rect(guiCurrentPos.x - stickSize / 2f, guiCurrentPos.y - stickSize / 2f, stickSize, stickSize);

                GUIStyle bgStyle = new GUIStyle(GUI.skin.box);
                GUIStyle stickStyle = new GUIStyle(GUI.skin.box);

                GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                GUI.Box(bgRect, "", bgStyle); // Draw Base

                GUI.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                GUI.Box(stickRect, "", stickStyle); // Draw Stick
                
                GUI.color = Color.white;
            }
        }
    }
}
