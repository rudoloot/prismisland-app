using UnityEngine;
using PrismIsland.Data;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private bool showStats = false;
    private int currentTab = 0; // 0 = Stats, 1 = Inventory
    private int currentInvTab = 0; // 0=All, 1=Equip, 2=Consumable, 3=Tools, 4=Quest, 5=Other
    private PlayerStats ps;
    private WeaponController wc;

    private RenderTexture previewRT;
    private Vector2 inventoryScrollPos = Vector2.zero;

    // --- Crafting State ---
    private Vector2 craftingScrollPos = Vector2.zero;
    private RecipeDataSO selectedRecipe = null;
    private int craftQuantity = 1;

    // --- Popup State ---
    private bool showPopup = false;
    private Rect popupRect;
    private ItemDataSO popupItemType;
    private bool isPopupForEquipment = false;
    private EquipSlot popupEquipSlot;
    private Vector2 cachedMousePos;
    private bool isMouseOverPopup = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ps = PlayerStats.Instance;
        GameObject player = GameObject.Find("Player");
        if (player != null) wc = player.GetComponent<WeaponController>();

        // Ensure CraftingManager exists
        if (CraftingManager.Instance == null)
        {
            gameObject.AddComponent<CraftingManager>();
        }

        // Ensure InstallationManager exists
        if (PrismIsland.Application.InstallationManager.Instance == null)
        {
            gameObject.AddComponent<PrismIsland.Application.InstallationManager>();
        }

        // Ensure MobileInputManager exists
        if (PrismIsland.Application.MobileInputManager.Instance == null)
        {
            gameObject.AddComponent<PrismIsland.Application.MobileInputManager>();
        }

        // Ensure AutoUpdater exists
        if (gameObject.GetComponent<AutoUpdater>() == null)
        {
            gameObject.AddComponent<AutoUpdater>();
        }
    }

    private int lastScreenHeight = 0;
    private Font customFont;

    void ApplyTheme()
    {
        if (Screen.height != lastScreenHeight || customFont == null)
        {
            lastScreenHeight = Screen.height;
            int baseFontSize = Mathf.RoundToInt(Screen.height * 0.015f);

            if (customFont == null)
            {
                customFont = Resources.Load<Font>("Fonts/Pretendard-Regular");
            }
            
            if (customFont != null)
            {
                GUI.skin.font = customFont;
            }
            
            // Set default styles size without bold
            GUI.skin.label.fontSize = baseFontSize;
            GUI.skin.label.fontStyle = FontStyle.Normal;

            GUI.skin.button.fontSize = baseFontSize;
            GUI.skin.button.fontStyle = FontStyle.Normal;

            GUI.skin.box.fontSize = baseFontSize;
            GUI.skin.box.fontStyle = FontStyle.Normal;
            
            GUI.skin.textField.fontSize = baseFontSize;
            GUI.skin.textField.fontStyle = FontStyle.Normal;
            
            GUI.skin.textArea.fontSize = baseFontSize;
            GUI.skin.textArea.fontStyle = FontStyle.Normal;
            
            GUI.skin.window.fontSize = baseFontSize;
            GUI.skin.window.fontStyle = FontStyle.Normal;
        }
    }

    void OnGUI()
    {
        ApplyTheme();

        if (ps == null) ps = PlayerStats.Instance;
        if (ps == null) return;

        // Cache absolute mouse position for popups
        cachedMousePos = Event.current.mousePosition;

        int baseFontSize = Mathf.RoundToInt(Screen.height * 0.015f);
        
        Color darkBrown = new Color(0.25f, 0.15f, 0.05f, 1f);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = baseFontSize;

        labelStyle.normal.textColor = darkBrown;

        GUIStyle whiteLabelStyle = new GUIStyle(GUI.skin.label);
        whiteLabelStyle.fontSize = baseFontSize;

        whiteLabelStyle.normal.textColor = Color.white;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontSize = baseFontSize;


        float padding = Screen.width * 0.02f;
        float topY = Screen.height * 0.02f;
        float barHeight = Screen.height * 0.035f;
        float btnWidth = barHeight; // ?�사각형
        float barWidth = (Screen.width - padding * 5 - btnWidth) / 3f;

        GUIStyle barTextStyle = new GUIStyle(GUI.skin.label);
        barTextStyle.alignment = TextAnchor.MiddleCenter;
        barTextStyle.fontSize = Mathf.RoundToInt(barHeight * 0.45f);
        barTextStyle.normal.textColor = Color.white;

        // 1. HP Bar (Left)
        float hpRatio = ps.maxHealth > 0 ? ps.currentHealth / ps.maxHealth : 0f;
        Rect hpRect = new Rect(padding, topY, barWidth, barHeight);
        DrawProgressBar(hpRect, hpRatio, new Color(0.8f, 0.2f, 0.2f), $"HP: {Mathf.CeilToInt(ps.currentHealth)} / {Mathf.CeilToInt(ps.maxHealth)}", barTextStyle);

        // 2. EXP Bar (Center)
        float expRatio = ps.expToNextLevel > 0 ? ps.currentExp / ps.expToNextLevel : 0f;
        Rect expRect = new Rect(padding * 2 + barWidth, topY, barWidth, barHeight);
        string expText = $"Lv.{ps.currentLevel} EXP: {Mathf.Floor(ps.currentExp)}/{Mathf.Floor(ps.expToNextLevel)}";
        if (ps.statPoints > 0) expText += $" (+{ps.statPoints} Pts)";
        DrawProgressBar(expRect, expRatio, new Color(0.2f, 0.6f, 0.8f), expText, barTextStyle);

        // 3. Weight Bar (Right)
        float weightRatio = 0f;
        string weightText = "Weight: - / -";
        if (InventoryManager.Instance != null) {
            float total = InventoryManager.Instance.TotalWeight;
            float max = InventoryManager.Instance.MaxWeight;
            weightRatio = max > 0 ? total / max : 0f;
            weightText = $"Weight: {total:F1} / {max:F1}kg";
        }
        Rect weightRect = new Rect(padding * 3 + barWidth * 2, topY, barWidth, barHeight);
        Color weightColor = weightRatio > 1f ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.8f, 0.6f, 0.2f);
        DrawProgressBar(weightRect, weightRatio, weightColor, weightText, barTextStyle);

        // 4. Menu Button (Right of Weight Bar)
        Rect btnRect = new Rect(padding * 4 + barWidth * 3, topY, btnWidth, barHeight);
        
        GUIStyle topBtnStyle = new GUIStyle(GUI.skin.button);
        topBtnStyle.fontSize = Mathf.RoundToInt(barHeight * 0.7f); // 기어 ?�이�??�기

        topBtnStyle.alignment = TextAnchor.MiddleCenter;

        if (GUI.Button(btnRect, "⚙", topBtnStyle))
        {
            ToggleStats();
        }

        // Remove popup if clicking elsewhere
        if (Event.current.type == EventType.MouseDown && showPopup)
        {
            if (!popupRect.Contains(cachedMousePos))
            {
                showPopup = false;
                // Do not Use() event here, so slots underneath can be clicked in the same frame!
            }
        }
        
        isMouseOverPopup = showPopup && popupRect.Contains(cachedMousePos);

        if (Event.current.type == EventType.KeyDown)
        {
            if (Event.current.keyCode == KeyCode.Tab) { ToggleStats(); Event.current.Use(); }
            else if (Event.current.keyCode == KeyCode.C) { if (!showStats) ToggleStats(); currentTab = 0; Event.current.Use(); }
            else if (Event.current.keyCode == KeyCode.I) { if (!showStats) ToggleStats(); currentTab = 1; Event.current.Use(); }
        }

        if (showStats)
        {
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            Rect windowRect = new Rect(Screen.width * 0.05f, Screen.height * 0.1f, Screen.width * 0.9f, Screen.height * 0.8f);
            
            GUI.color = new Color(0.92f, 0.86f, 0.72f, 1f); 
            GUI.DrawTexture(windowRect, Texture2D.whiteTexture);
            
            GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
            GUI.Box(windowRect, "");
            GUI.color = Color.white;
            
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = Mathf.RoundToInt(baseFontSize * 1.2f);

            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = darkBrown;

            float tabHeight = baseFontSize * 2.5f;
            float tabWidth = windowRect.width / 3f;

            if (GUI.Button(new Rect(windowRect.x, windowRect.y, tabWidth, tabHeight), "Stats", currentTab == 0 ? headerStyle : btnStyle)) {
                currentTab = 0;
                showPopup = false;
            }
            if (GUI.Button(new Rect(windowRect.x + tabWidth, windowRect.y, tabWidth, tabHeight), "Bag/Equip", currentTab == 1 ? headerStyle : btnStyle)) {
                currentTab = 1;
                showPopup = false;
            }
            if (GUI.Button(new Rect(windowRect.x + tabWidth * 2f, windowRect.y, tabWidth, tabHeight), "Crafting", currentTab == 2 ? headerStyle : btnStyle)) {
                currentTab = 2;
                showPopup = false;
            }

            float contentY = windowRect.y + tabHeight + padding;

            if (currentTab == 0)
            {
                GUI.Label(new Rect(windowRect.x + padding, contentY, windowRect.width, baseFontSize * 2), $"Available Points: {ps.statPoints}", labelStyle);
                float rowStart = contentY + baseFontSize * 2.5f;
                float rowHeight = Screen.height * 0.08f;

                DrawStatRow("STR (근력)", ps.strength, PrismIsland.Domain.StatType.Strength, 0, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
                DrawStatRow("INT (지능)", ps.intelligence, PrismIsland.Domain.StatType.Intelligence, 1, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
                DrawStatRow("AGI (민첩)", ps.agility, PrismIsland.Domain.StatType.Agility, 2, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
                DrawStatRow("VIT (체력)", ps.vitality, PrismIsland.Domain.StatType.Vitality, 3, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
                DrawStatRow("CHA (매력)", ps.charisma, PrismIsland.Domain.StatType.Charisma, 4, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
                DrawStatRow("LUK (행운)", ps.luck, PrismIsland.Domain.StatType.Luck, 5, windowRect.x + padding, rowStart, windowRect.width - padding*2, rowHeight, labelStyle, btnStyle);
            }
            else if (currentTab == 1)
            {
                int columns = 5;
                float invSlotSpacing = padding * 0.5f;
                float totalSpacing = invSlotSpacing * (columns - 1);
                float scrollBarWidth = padding;
                float invSlotSize = (windowRect.width - (padding * 2) - scrollBarWidth - totalSpacing) / columns;

                float slotW = invSlotSize; 
                float slotH = invSlotSize;
                
                float eqSlotSpacingY = invSlotSpacing;
                float topHalfHeight = (slotH * 4) + (eqSlotSpacingY * 5);
                
                Rect eqRect = new Rect(windowRect.x, windowRect.y + tabHeight, windowRect.width, topHalfHeight);
                
                GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.3f);
                GUI.Box(eqRect, "");
                GUI.color = Color.white;
                
                float previewWidth = eqRect.width - (slotW * 2) - (padding * 4);
                float previewHeight = topHalfHeight - padding * 2;
                Rect previewRect = new Rect(eqRect.x + padding * 2 + slotW, eqRect.y + padding, previewWidth, previewHeight);

                GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
                GUI.Box(previewRect, "");
                GUI.color = Color.white;

                if (previewRT == null) previewRT = Resources.Load<RenderTexture>("PreviewRT");
                if (previewRT != null) {
                    GUI.DrawTexture(previewRect, previewRT, ScaleMode.ScaleToFit, false);
                } else {
                    GUI.color = new Color(0.85f, 0.78f, 0.65f, 1f);
                    GUI.DrawTexture(previewRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(previewRect, "No Preview", headerStyle);
                }

                EquipSlot[] leftSlots = { EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.R_Weapon };
                EquipSlot[] rightSlots = { EquipSlot.Accessory, EquipSlot.Gloves, EquipSlot.Shoes, EquipSlot.L_Weapon };

                for (int j = 0; j < 4; j++) {
                    float yPos = eqRect.y + eqSlotSpacingY + j * (slotH + eqSlotSpacingY);
                    
                    Rect lSlotRect = new Rect(previewRect.x - padding - slotW, yPos, slotW, slotH);
                    DrawEquipSlot(lSlotRect, leftSlots[j]);

                    Rect rSlotRect = new Rect(previewRect.x + previewWidth + padding, yPos, slotW, slotH);
                    DrawEquipSlot(rSlotRect, rightSlots[j]);
                }

                float invY = eqRect.y + eqRect.height;
                float invHeight = windowRect.height - tabHeight - topHalfHeight;
                Rect invRect = new Rect(windowRect.x, invY, windowRect.width, invHeight);
                
                GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.1f);
                GUI.Box(invRect, ""); 
                GUI.color = Color.white;

                if (InventoryManager.Instance != null) {
                    float invTabH = baseFontSize * 2f;
                    string[] invTabs = { "전체", "장비", "소모품", "도구", "퀘스트", "기타" };
                    float totalTabWidth = invRect.width * 0.7f;
                    float singleTabWidth = totalTabWidth / invTabs.Length;

                    for (int i = 0; i < invTabs.Length; i++) {
                        if (GUI.Button(new Rect(invRect.x + padding + i * singleTabWidth, invRect.y + padding, singleTabWidth, invTabH), invTabs[i], currentInvTab == i ? headerStyle : btnStyle)) {
                            currentInvTab = i;
                            inventoryScrollPos = Vector2.zero; // Reset scroll on tab change
                        }
                    }

                    GUIStyle rightAlignStyle = new GUIStyle(labelStyle);
                    rightAlignStyle.alignment = TextAnchor.MiddleRight;
                    
                    float weightWidth = invRect.width - totalTabWidth - padding * 3;
                    Rect invWeightRect = new Rect(invRect.x + padding * 2 + totalTabWidth, invRect.y + padding, weightWidth, invTabH);
                    
                    if (InventoryManager.Instance.TotalWeight > InventoryManager.Instance.MaxWeight) {
                        GUI.contentColor = Color.red;
                        GUI.Label(invWeightRect, $"⚠️ {InventoryManager.Instance.TotalWeight:F1} / {InventoryManager.Instance.MaxWeight:F1}kg", rightAlignStyle);
                        GUI.contentColor = Color.white;
                    } else {
                        GUI.Label(invWeightRect, $"{InventoryManager.Instance.TotalWeight:F1} / {InventoryManager.Instance.MaxWeight:F1}kg", rightAlignStyle);
                    }

                    var allItemsList = InventoryManager.Instance.Model.Items;
                    var itemsList = new System.Collections.Generic.List<PrismIsland.Domain.InventoryItem>();
                    for (int i = 0; i < allItemsList.Count; i++) {
                        ItemDataSO data = ItemDatabase.GetItem(allItemsList[i].Id);
                        if (data != null && IsItemInCurrentTab(data)) {
                            itemsList.Add(allItemsList[i]);
                        }
                    }
                    
                    int itemCount = itemsList.Count;
                    int rows = Mathf.CeilToInt((float)itemCount / columns);
                    if (rows < 3) rows = 3; 

                    Rect scrollViewRect = new Rect(invRect.x + padding, invRect.y + invTabH + padding * 1.5f, invRect.width - padding*2, invRect.height - invTabH - padding * 2.5f);
                    Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width - scrollBarWidth - padding, rows * (invSlotSize + invSlotSpacing));

                    inventoryScrollPos = GUI.BeginScrollView(scrollViewRect, inventoryScrollPos, scrollContentRect);

                    for (int i = 0; i < itemsList.Count; i++) {
                        var invItem = itemsList[i];
                        ItemDataSO itemData = ItemDatabase.GetItem(invItem.Id);
                        if (itemData == null) continue;

                        int r = i / columns;
                        int c = i % columns;
                        
                        Rect slotRect = new Rect(c * (invSlotSize + invSlotSpacing), r * (invSlotSize + invSlotSpacing), invSlotSize, invSlotSize);
                        
                        GUI.color = new Color(0.85f, 0.78f, 0.65f, 1f);
                        GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
                        GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
                        GUI.Box(slotRect, "");
                        GUI.color = Color.white;

                        if (Event.current.type == EventType.MouseDown && slotRect.Contains(Event.current.mousePosition))
                        {
                            if (!isMouseOverPopup) 
                            {
                                showPopup = true;
                                isPopupForEquipment = false;
                                popupItemType = itemData;
                                
                                float pWidth = Screen.width * 0.3f;
                                popupRect = new Rect(cachedMousePos.x - pWidth/2, cachedMousePos.y - 10, pWidth, 100); 
                                Event.current.Use();
                            }
                        }

                        float iconSize = invSlotSize * 0.5f;
                        Rect iconRect = new Rect(slotRect.x + (invSlotSize - iconSize)/2, slotRect.y + invSlotSize * 0.1f, iconSize, iconSize);
                        GUI.color = itemData.fallbackColor;

                        GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                        GUI.color = Color.white;

                        GUIStyle itemTextStyle = new GUIStyle(GUI.skin.label);
                        itemTextStyle.alignment = TextAnchor.UpperCenter;
                        itemTextStyle.fontSize = baseFontSize;

                        itemTextStyle.normal.textColor = darkBrown;

                        GUI.Label(new Rect(slotRect.x, slotRect.y + invSlotSize * 0.65f, invSlotSize, baseFontSize * 2f), $"x {invItem.Count}", itemTextStyle);
                    }
                    GUI.EndScrollView();
                }
            }
            else if (currentTab == 2)
            {
                if (CraftingManager.Instance == null) return;
                
                float topHalfHeight = windowRect.height * 0.4f;
                Rect topRect = new Rect(windowRect.x, windowRect.y + tabHeight, windowRect.width, topHalfHeight);
                
                GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.3f);
                GUI.Box(topRect, "");
                GUI.color = Color.white;

                if (selectedRecipe != null)
                {
                    float boxWidth = (topRect.width - padding * 4) / 3f;

                    Rect iconRect = new Rect(topRect.x + padding, topRect.y + padding, boxWidth, topHalfHeight - padding * 2);
                    GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.1f);
                    GUI.Box(iconRect, "");
                    GUI.color = Color.white;
                    
                    float ovalSize = Mathf.Min(boxWidth * 0.8f, iconRect.height * 0.8f);
                    Rect ovalRect = new Rect(iconRect.x + (boxWidth - ovalSize)/2f, iconRect.y + (iconRect.height - ovalSize)/2f, ovalSize, ovalSize);
                    GUI.color = selectedRecipe.outputItem.fallbackColor;
                    GUI.DrawTexture(ovalRect, Texture2D.whiteTexture, ScaleMode.ScaleToFit, true, 1f); 
                    GUI.color = Color.white;
                    GUIStyle modelStyle = new GUIStyle(GUI.skin.label);
                    modelStyle.alignment = TextAnchor.MiddleCenter;
                    modelStyle.fontSize = Mathf.RoundToInt(baseFontSize * 0.8f);
                    modelStyle.normal.textColor = Color.black;
                    GUI.Label(ovalRect, "미리보기", modelStyle);

                    Rect infoRect = new Rect(iconRect.x + boxWidth + padding, topRect.y + padding, boxWidth, topHalfHeight - padding * 2);
                    GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.1f);
                    GUI.Box(infoRect, "");
                    GUI.color = Color.white;
                    
                    GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
                    titleStyle.fontSize = baseFontSize;

                    titleStyle.normal.textColor = darkBrown;
                    GUI.Label(new Rect(infoRect.x + padding, infoRect.y + padding, infoRect.width, baseFontSize * 1.5f), selectedRecipe.recipeName, titleStyle);
                    
                    GUIStyle descStyle = new GUIStyle(GUI.skin.label);
                    descStyle.fontSize = Mathf.RoundToInt(baseFontSize * 0.8f);
                    descStyle.wordWrap = true;
                    descStyle.normal.textColor = darkBrown;
                    GUI.Label(new Rect(infoRect.x + padding, infoRect.y + padding + baseFontSize * 1.5f, infoRect.width - padding*2, infoRect.height - baseFontSize*2f), selectedRecipe.description, descStyle);

                    Rect ingrRect = new Rect(infoRect.x + boxWidth + padding, topRect.y + padding, boxWidth, topHalfHeight - padding * 3 - baseFontSize * 2f);
                    GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.1f);
                    GUI.Box(ingrRect, "");
                    GUI.color = Color.white;
                    
                    float currY = ingrRect.y + padding;
                    foreach (var ing in selectedRecipe.ingredients)
                    {
                        int currentCount = InventoryManager.Instance != null ? InventoryManager.Instance.GetItemCount(ing.item) : 0;
                        int reqCount = ing.quantity * craftQuantity;
                        GUIStyle ingStyle = new GUIStyle(descStyle);
                        ingStyle.wordWrap = false;
                        ingStyle.normal.textColor = currentCount >= reqCount ? darkBrown : Color.red;
                        GUI.Label(new Rect(ingrRect.x + padding, currY, ingrRect.width - padding * 2, baseFontSize * 1.5f), $"{ing.item.itemName} : {currentCount} / {reqCount}", ingStyle);
                        currY += baseFontSize * 1.2f;
                    }

                    Rect qtyRect = new Rect(ingrRect.x, ingrRect.y + ingrRect.height + padding, boxWidth * 0.6f, baseFontSize * 2f);
                    GUI.color = new Color(0.95f, 0.9f, 0.8f, 1f);
                    GUI.DrawTexture(qtyRect, Texture2D.whiteTexture);
                    GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
                    GUI.Box(qtyRect, "");
                    GUI.color = Color.white;
                    
                    float btnW = qtyRect.width * 0.3f;
                    if (GUI.Button(new Rect(qtyRect.x, qtyRect.y, btnW, qtyRect.height), "<", btnStyle)) {
                        craftQuantity = Mathf.Max(1, craftQuantity - 1);
                    }
                    GUIStyle qtyStyle = new GUIStyle(GUI.skin.label);
                    qtyStyle.alignment = TextAnchor.MiddleCenter;
                    qtyStyle.fontSize = baseFontSize;

                    qtyStyle.normal.textColor = darkBrown;
                    GUI.Label(new Rect(qtyRect.x + btnW, qtyRect.y, qtyRect.width - btnW*2f, qtyRect.height), craftQuantity.ToString(), qtyStyle);
                    
                    if (GUI.Button(new Rect(qtyRect.x + qtyRect.width - btnW, qtyRect.y, btnW, qtyRect.height), ">", btnStyle)) {
                        craftQuantity++;
                    }

                    Rect craftBtnRect = new Rect(qtyRect.x + qtyRect.width + padding, qtyRect.y, boxWidth * 0.4f - padding, baseFontSize * 2f);
                    bool canCraft = CraftingManager.Instance.CanCraft(selectedRecipe, craftQuantity);
                    GUI.enabled = canCraft;
                    if (GUI.Button(craftBtnRect, "만들기", btnStyle))
                    {
                        CraftingManager.Instance.CraftItem(selectedRecipe, craftQuantity);
                        craftQuantity = 1;
                    }
                    GUI.enabled = true;
                }
                else
                {
                    GUI.Label(new Rect(topRect.x, topRect.y + topHalfHeight/2f - baseFontSize, topRect.width, baseFontSize*2), "레시피를 선택해주세요.", headerStyle);
                }

                float botY = topRect.y + topRect.height;
                float botHeight = windowRect.height - tabHeight - topHalfHeight;
                Rect botRect = new Rect(windowRect.x, botY, windowRect.width, botHeight);
                
                GUI.color = new Color(0.5f, 0.35f, 0.2f, 0.1f);
                GUI.Box(botRect, ""); 
                GUI.color = Color.white;

                var recipes = CraftingManager.Instance.availableRecipes;
                int columns = 5;
                float slotSpacing = padding * 0.5f;
                float scrollBarWidth = padding;
                float slotSize = (botRect.width - (padding * 2) - scrollBarWidth - slotSpacing * (columns - 1)) / columns;

                int rows = Mathf.CeilToInt((float)recipes.Count / columns);
                if (rows < 3) rows = 3;

                Rect scrollViewRect = new Rect(botRect.x + padding, botRect.y + padding, botRect.width - padding*2, botRect.height - padding*2);
                Rect scrollContentRect = new Rect(0, 0, scrollViewRect.width - scrollBarWidth - padding, rows * (slotSize + slotSpacing));

                craftingScrollPos = GUI.BeginScrollView(scrollViewRect, craftingScrollPos, scrollContentRect);

                for (int i = 0; i < recipes.Count; i++)
                {
                    var recipe = recipes[i];
                    int r = i / columns;
                    int c = i % columns;
                    
                    Rect slotRect = new Rect(c * (slotSize + slotSpacing), r * (slotSize + slotSpacing), slotSize, slotSize);
                    
                    GUI.color = selectedRecipe == recipe ? new Color(1f, 0.9f, 0.6f, 1f) : new Color(0.85f, 0.78f, 0.65f, 1f);
                    GUI.DrawTexture(slotRect, Texture2D.whiteTexture);
                    GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
                    GUI.Box(slotRect, "");
                    GUI.color = Color.white;

                    if (GUI.Button(slotRect, "", GUIStyle.none))
                    {
                        selectedRecipe = recipe;
                        craftQuantity = 1;
                    }

                    float iconSize = slotSize * 0.5f;
                    Rect iconRect = new Rect(slotRect.x + (slotSize - iconSize)/2, slotRect.y + slotSize * 0.1f, iconSize, iconSize);
                    GUI.color = recipe.outputItem.fallbackColor;
                    GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                GUI.EndScrollView();
            }
            
            if (showPopup)
            {
                DrawPopup();
            }
        }

        if (!showStats && EquipmentManager.Instance != null)
        {
            WeaponDataSO rWeapon = EquipmentManager.Instance.GetItemInSlot(EquipSlot.R_Weapon) as WeaponDataSO;
            WeaponDataSO lWeapon = EquipmentManager.Instance.GetItemInSlot(EquipSlot.L_Weapon) as WeaponDataSO;
            
            if (rWeapon != null && rWeapon.combatType != PrismIsland.Data.CombatType.None) {
                DrawWeaponHUD(rWeapon, true, wc);
            }
            if (lWeapon != null && lWeapon.combatType != PrismIsland.Data.CombatType.None) {
                DrawWeaponHUD(lWeapon, false, wc);
            }
        }
    }

    void DrawWeaponHUD(WeaponDataSO weapon, bool isRightSide, WeaponController wc)
    {
        float hudWidth = Screen.width * 0.25f;
        float hudHeight = Screen.height * 0.12f;
        float margin = Screen.width * 0.03f;
        
        float xPos = isRightSide ? (Screen.width - hudWidth - margin) : margin;
        float yPos = Screen.height - hudHeight - margin;
        
        Rect hudRect = new Rect(xPos, yPos, hudWidth, hudHeight);
        
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.Box(hudRect, "");
        GUI.color = Color.white;

        float iconWidth = hudWidth * 0.4f;
        float iconHeight = hudHeight * 0.25f;
        Rect iconRect = new Rect(hudRect.x + (hudWidth - iconWidth)/2, hudRect.y + hudHeight * 0.15f, iconWidth, iconHeight);
        GUI.color = weapon.fallbackColor;
        GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle ammoStyle = new GUIStyle(GUI.skin.label);
        ammoStyle.fontSize = Mathf.RoundToInt(Screen.width * 0.04f);

        ammoStyle.alignment = TextAnchor.MiddleCenter;
        ammoStyle.normal.textColor = Color.white; 

        Rect ammoRect = new Rect(hudRect.x, hudRect.y + hudHeight * 0.45f, hudWidth, hudHeight * 0.5f);
        
        if (weapon.combatType == PrismIsland.Data.CombatType.Ranged) {
            bool isReloading = false;
            int ammoToShow = 0;
            if (wc != null && ((isRightSide && wc.rightWeapon.model != null && wc.rightWeapon.model.IsRanged) || (!isRightSide && wc.leftWeapon.model != null && wc.leftWeapon.model.IsRanged))) {
                if (isRightSide) {
                    ammoToShow = wc.rightWeapon.model.CurrentAmmo;
                    isReloading = wc.rightWeapon.model.IsReloading;
                } else {
                    ammoToShow = wc.leftWeapon.model.CurrentAmmo;
                    isReloading = wc.leftWeapon.model.IsReloading;
                }
            } else if (weapon != null) {
                ammoToShow = weapon.maxAmmo;
            }
            
            int totalBullets = 0;
            if (InventoryManager.Instance != null) {
                ItemDataSO bulletItem = ItemDatabase.GetItem("Bullet");
                if (bulletItem != null) totalBullets = InventoryManager.Instance.GetItemCount(bulletItem);
            }
            
            int spareBullets = totalBullets - ammoToShow;
            if (spareBullets < 0) spareBullets = 0;

            if (isReloading) {
                GUI.color = Color.yellow;
                GUI.Label(ammoRect, "RELOADING", ammoStyle);
                GUI.color = Color.white;
            } else {
                GUI.Label(ammoRect, $"{ammoToShow} / {spareBullets}", ammoStyle);
            }
        } else {
            GUI.Label(ammoRect, "- / -", ammoStyle);
        }
    }

    void ToggleStats()
    {
        showStats = !showStats;
        showPopup = false;
        Time.timeScale = showStats ? 0f : 1f;
    }

    public void OpenInventoryTab()
    {
        showStats = true;
        currentTab = 1;
        showPopup = false;
        Time.timeScale = 0f;
    }

    void DrawStatRow(string name, int statValue, PrismIsland.Domain.StatType statType, int index, float x, float startY, float width, float height, GUIStyle labelStyle, GUIStyle btnStyle)
    {
        float yPos = startY + (index * height);
        
        GUI.color = new Color(0.85f, 0.78f, 0.65f, 0.5f);
        GUI.DrawTexture(new Rect(x, yPos, width, height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(x + Screen.width*0.02f, yPos + height*0.2f, width * 0.6f, height), $"{name}: {statValue}", labelStyle);
        
        if (ps.statPoints > 0)
        {
            float btnW = width * 0.2f;
            if (GUI.Button(new Rect(x + width - btnW, yPos + height*0.1f, btnW, height * 0.8f), "+", btnStyle))
            {
                ps.Model.IncreaseStat(statType);
            }
        }
    }

    void DrawEquipSlot(Rect rect, EquipSlot slot)
    {
        GUI.color = new Color(0.85f, 0.78f, 0.65f, 1f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
        GUI.Box(rect, "");
        GUI.color = Color.white;
        
        if (Event.current.type == EventType.MouseDown && rect.Contains(cachedMousePos))
        {
            if (!isMouseOverPopup && EquipmentManager.Instance != null && EquipmentManager.Instance.GetItemInSlot(slot) != null)
            {
                showPopup = true;
                isPopupForEquipment = true;
                popupEquipSlot = slot;
                popupItemType = EquipmentManager.Instance.GetItemInSlot(slot);
                
                float pWidth = Screen.width * 0.3f;
                popupRect = new Rect(cachedMousePos.x - pWidth/2, cachedMousePos.y - 10, pWidth, 80); 
                Event.current.Use();
            }
        }
        
        GUIStyle slotNameStyle = new GUIStyle(GUI.skin.label);
        slotNameStyle.fontSize = Mathf.RoundToInt(rect.height * 0.15f);
        slotNameStyle.alignment = TextAnchor.UpperCenter;
        slotNameStyle.normal.textColor = new Color(0.4f, 0.25f, 0.1f, 1f); 
        
        string slotDisplayName = slot.ToString();
        if (slot == EquipSlot.R_Weapon) slotDisplayName = "주무기";
        else if (slot == EquipSlot.L_Weapon) slotDisplayName = "보조무기";
        
        GUI.Label(new Rect(rect.x, rect.y + rect.height*0.05f, rect.width, rect.height*0.3f), slotDisplayName, slotNameStyle);

        if (EquipmentManager.Instance != null) {
            ItemDataSO item = EquipmentManager.Instance.GetItemInSlot(slot);
            if (item != null) {
                float iconSize = rect.width * 0.5f;
                Rect iconRect = new Rect(rect.x + (rect.width - iconSize)/2, rect.y + rect.height * 0.4f, iconSize, iconSize);
                GUI.color = item.fallbackColor;
                GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }

    void DrawPopup()
    {
        int baseFontSize = Mathf.RoundToInt(Screen.height * 0.015f);
        float btnH = Screen.height * 0.06f;
        float padding = Screen.height * 0.01f;
        float titleH = baseFontSize * 2f;

        // Calculate height first
        int btnCount = 1; 
        if (!isPopupForEquipment)
        {
            bool isEquipable = popupItemType is EquipmentDataSO;
            bool isConsumable = popupItemType.id == "HealthPotion";
            bool isInstallable = popupItemType is StructureDataSO;
            if (isEquipable) btnCount++;
            if (isConsumable) btnCount++;
            if (isInstallable) btnCount++;
        }
        
        popupRect.height = btnCount * btnH + (btnCount + 1) * padding + titleH + padding;

        GUI.color = new Color(0.95f, 0.9f, 0.8f, 1f); 
        GUI.DrawTexture(popupRect, Texture2D.whiteTexture);
        GUI.color = new Color(0.5f, 0.35f, 0.2f, 1f);
        GUI.Box(popupRect, "");
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = baseFontSize;
        titleStyle.normal.textColor = new Color(0.2f, 0.1f, 0.05f, 1f);
        GUI.Label(new Rect(popupRect.x + padding, popupRect.y + padding, popupRect.width - padding*2, titleH), popupItemType.itemName, titleStyle);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.normal.textColor = new Color(0.2f, 0.1f, 0.05f, 1f);
        btnStyle.fontSize = baseFontSize;

        float y = popupRect.y + titleH + padding * 2;

        if (isPopupForEquipment)
        {
            if (GUI.Button(new Rect(popupRect.x + padding, y, popupRect.width - padding*2, btnH), "Unequip (해제)", btnStyle))
            {
                EquipmentManager.Instance.UnequipItem(popupEquipSlot);
                showPopup = false;
            }
        }
        else
        {
            bool isEquipable = popupItemType is EquipmentDataSO;
            bool isConsumable = popupItemType.id == "HealthPotion";
            bool isInstallable = popupItemType is StructureDataSO;
            
            if (isInstallable)
            {
                if (GUI.Button(new Rect(popupRect.x + padding, y, popupRect.width - padding*2, btnH), "Install (설치)", btnStyle))
                {
                    PrismIsland.Application.InstallationManager.Instance.StartInstallation(popupItemType as StructureDataSO);
                    showStats = false; 
                    showPopup = false;
                }
                y += btnH + padding;
            }

            if (isConsumable)
            {
                if (GUI.Button(new Rect(popupRect.x + padding, y, popupRect.width - padding*2, btnH), "Use (사용하기)", btnStyle))
                {
                    if (popupItemType.id == "HealthPotion") {
                        if (PlayerStats.Instance != null && PlayerStats.Instance.Model != null) {
                            PlayerStats.Instance.Model.Heal(30f);
                        }
                        InventoryManager.Instance.RemoveItem(popupItemType, 1);
                    }
                    showPopup = false;
                }
                y += btnH + padding;
            }
            
            if (isEquipable)
            {
                if (GUI.Button(new Rect(popupRect.x + padding, y, popupRect.width - padding*2, btnH), "Equip (장착)", btnStyle))
                {
                    EquipmentManager.Instance.EquipItem(popupItemType);
                    showPopup = false;
                }
                y += btnH + padding;
            }

            if (GUI.Button(new Rect(popupRect.x + padding, y, popupRect.width - padding*2, btnH), "Drop (버리기)", btnStyle))
            {
                InventoryManager.Instance.DropItem(popupItemType);
                showPopup = false;
            }
        }
    }

    private bool IsItemInCurrentTab(ItemDataSO item)
    {
        if (currentInvTab == 0) return true; // 전체
        
        bool isEquip = item is EquipmentDataSO;
        bool isQuest = item is QuestItemDataSO;
        bool isConsumable = item is EdibleDataSO || item is ConsumableDataSO || item.id == "HealthPotion";
        
        bool isTool = false;
        if (item is WeaponDataSO weapon) {
            if (weapon.id.Contains("Axe") || weapon.id.Contains("Pickaxe") || weapon.id.Contains("Hoe") || weapon.id.Contains("Shovel") || weapon.id.Contains("Hammer")) {
                isTool = true;
            }
        }

        if (currentInvTab == 1) { // 장비
            return isEquip && !isTool;
        }
        else if (currentInvTab == 2) { // 소모품
            return isConsumable;
        }
        else if (currentInvTab == 3) { // 도구
            return isTool;
        }
        else if (currentInvTab == 4) { // 퀘스트
            return isQuest;
        }
        else if (currentInvTab == 5) { // 기타
            return !isEquip && !isConsumable && !isTool && !isQuest;
        }
        return true;
    }

    private void DrawProgressBar(Rect rect, float fillRatio, Color color, string text, GUIStyle textStyle)
    {
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        
        GUI.color = color;
        Rect fillRect = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fillRatio), rect.height);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        
        GUI.color = Color.white;
        
        // Shadow
        GUIStyle shadowStyle = new GUIStyle(textStyle);
        shadowStyle.normal.textColor = Color.black;
        Rect shadowRect = new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height);
        GUI.Label(shadowRect, text, shadowStyle);
        
        // Main Text
        GUI.Label(rect, text, textStyle);
    }
}

