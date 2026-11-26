using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System;
using KSP.UI.Screens;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace KerbalLogisticsProgram
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public sealed class KlpWindow : MonoBehaviour
    {
        //Vanilla Integration
        private ApplicationLauncherButton toolbarButton;
        private Texture2D toolbarIcon;

        private readonly KlpPalette palette = new KlpPalette();
        private KlpUIFactory uiFactory;
        private GameObject canvasObject;
        private RectTransform windowRect;
        private Font uiFont;
        private bool escapeClosePending;
        private const string InputLockId = "KLP_MailboxWindow";
        private Text mailboxEmptyText;
        private Text shippingEmptyText;
        private KlpTab activeTab = KlpTab.Mailbox;
        private const bool EnablePerformanceDiagnostics = false;

        // Persistent tab panels
        private RectTransform mailboxPanel;
        private RectTransform shippingPanel;

        // Tab buttons
        private Button mailboxTabButton;
        private Button shippingTabButton;
        // Tab selection indicators
        private Image mailboxTabIndicator;
        private Image shippingTabIndicator;

        // Location header
        private Text locationText;
        private Text accessStatusText;

        // Persistent search controls
        private InputField mailboxSearchInput;
        private InputField shippingSearchInput;

        private bool initialized;
        // Tab identifiers
        // Persistent resource row collections

        private readonly List<KlpResourceRow> mailboxRows =
            new List<KlpResourceRow>();

        private readonly List<KlpResourceRow> shippingRows =
            new List<KlpResourceRow>();

        // Independent resource selections

        private string selectedMailboxResource = "";
        private string selectedShippingResource = "";

        // Scrollable inventory containers

        private RectTransform mailboxInventoryContent;
        private RectTransform shippingInventoryContent;

        private ScrollRect mailboxScroll;
        private ScrollRect shippingScroll;

        // Selected resource labels

        private Text mailboxSelectedText;
        private Text shippingSelectedText;

        private const float ResourceRowHeight = 30f;

        // Cached inventory data

        private KlpInventorySnapshot inventorySnapshot;

        private Dictionary<string, double> destinationInventory =
            new Dictionary<string, double>();

        // Shipping destination state

        private Dropdown destinationDropdown;

        private string selectedDestination = "";

        private bool refreshingDestinations;

        // Refresh diagnostics

        private int snapshotRefreshCount;

        // Footer and access state

        private Text footerStatusText;
        // Local transaction controls

        private InputField mailboxAmountInput;

        private Button depositButton;
        private Button withdrawButton;

        private Button allVesselButton;
        private Button maxWithdrawalButton;

        // Vessel storage information

        private Text vesselQuantityText;
        private Text vesselCapacityText;
        private Text vesselFreeCapacityText;

        private RectTransform vesselCapacityFill;

        // Mailbox storage information

        private Text mailboxQuantityText;

        // Transaction previews

        private Text depositPreviewText;
        private Text withdrawPreviewText;
        private KlpTransferPreview depositPreview;
        private KlpTransferPreview withdrawPreview;


        // Resource currently displayed in transaction panel

        private string displayedMailboxResource = "";


        private const float TransactionPanelWidth = 400f;
        private const float TabPanelPadding = 12f;
        private const float TabPanelGap = 12f;

        //Custom Window Sizing
        private static readonly Vector2 DefaultWindowSize = new Vector2(1120f, 640f);
        private static readonly Vector2 MinimumWindowSize = new Vector2(1000f, 640f);
        private static readonly Vector2 MaximumWindowSize = new Vector2(1600f, 950f);
        private const float ResizeHandleSize = 24f;

        //Shipment related fields 
        private Text shippingOriginQuantityText;
        private Text shippingDestinationQuantityText;
        private InputField shippingAmountInput;
        private Button allOriginButton;
        private Button shipButton;
        private Text shipmentPreviewText;
        private string displayedShippingResource = "";
        private string displayedShippingDestination = "";
        private double requestedShipmentAmount;
        private double approvedShipmentAmount;



        private enum KlpTab
        {
            Mailbox,
            Shipping
        }

        private void Update()
        {
            if (escapeClosePending)
                return;

            if (canvasObject != null &&
                canvasObject.activeSelf &&
                Input.GetKeyDown(KeyCode.Escape))
            {
                StartCoroutine(CloseOnEscape());
            }
        }
        private void OnEnable()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(
                CreateToolbarButton);

            GameEvents.onGUIApplicationLauncherDestroyed.Add(
                OnToolbarDestroyed);

            if (ApplicationLauncher.Ready)
            {
                CreateToolbarButton();
            }
        }
        private Texture2D CreateToolbarIcon()
        {
            const int size = 38;

            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color color = palette.WindowBackground;

                    // Upper arrow pointing right.

                    bool upperShaft =
                        x >= 5 && x <= 30 &&
                        y >= 23 && y <= 27;

                    bool upperHead =
                        x >= 23 && x <= 33 &&
                        Mathf.Abs(y - 25) <= 33 - x;

                    // Lower arrow pointing left.

                    bool lowerShaft =
                        x >= 7 && x <= 32 &&
                        y >= 9 && y <= 13;

                    bool lowerHead =
                        x >= 4 && x <= 14 &&
                        Mathf.Abs(y - 11) <= x - 4;

                    if (upperShaft || upperHead)
                        color = palette.ShippingAccent;

                    if (lowerShaft || lowerHead)
                        color = palette.MailboxAccent;

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
        private void CreateToolbarButton()
        {
            if (toolbarButton != null ||
                !ApplicationLauncher.Ready ||
                ApplicationLauncher.Instance == null)
            {
                return;
            }

            if (toolbarIcon == null)
            {
                toolbarIcon = CreateToolbarIcon();
            }

            toolbarButton =
                ApplicationLauncher.Instance.AddModApplication(
                    OpenFromToolbar,
                    CloseFromToolbar,
                    null,
                    null,
                    null,
                    null,
                    ApplicationLauncher.AppScenes.FLIGHT |
                    ApplicationLauncher.AppScenes.MAPVIEW,
                    toolbarIcon);
        }

        private void OpenFromToolbar()
        {
            if (!initialized)
            {
                BuildWindow();
            }

            SetVisible(true);
        }

        private void CloseFromToolbar()
        {
            SetVisible(false);
        }
        private void OnToolbarDestroyed()
        {
            toolbarButton = null;
        }
        private void OnMailboxAmountChanged(string value)
        {
            UpdateMailboxTransferPreviews();
        }
        private IEnumerator CloseOnEscape()
        {
            escapeClosePending = true;

            // Hide the window immediately.
            // Do not release the input lock yet.

            canvasObject.SetActive(false);

            // Wait until Escape is no longer held.

            while (Input.GetKey(KeyCode.Escape))
            {
                yield return null;
            }

            // Allow the current frame to finish
            // before restoring game controls.

            yield return new WaitForEndOfFrame();

            SetVisible(false);

            escapeClosePending = false;
        }
        private void SetVisible(bool visible)
        {
            if (canvasObject == null)
                return;

            if (visible)
            {
                RefreshInventory();

                InputLockManager.SetControlLock(
                    ControlTypes.All,
                    InputLockId);

                canvasObject.SetActive(true);
            }
            else
            {
                canvasObject.SetActive(false);

                InputLockManager.RemoveControlLock(
                    InputLockId);
            }
            if (toolbarButton != null)
            {
                if (visible)
                    toolbarButton.SetTrue(false);
                else
                    toolbarButton.SetFalse(false);
            }

            //Debug.Log("[KLP] Window " +(visible ? "opened." : "closed.") +" Input lock " +(visible ? "engaged." : "released."));
        }
        private void SetActiveTab(KlpTab tab)
        {
            activeTab = tab;

            bool mailboxActive =
                activeTab == KlpTab.Mailbox;

            mailboxPanel.gameObject.SetActive(
                mailboxActive);

            shippingPanel.gameObject.SetActive(
                !mailboxActive);

            mailboxTabIndicator.enabled =
                mailboxActive;

            shippingTabIndicator.enabled =
                !mailboxActive;

            UpdateTabAppearance(
                mailboxTabButton,
                mailboxActive,
                palette.MailboxSelection);

            UpdateTabAppearance(
                shippingTabButton,
                !mailboxActive,
                palette.ShippingSelection);

            //Debug.Log("[KLP] Active tab: " + activeTab);
        }
        private void SelectMailboxResource(KlpResourceRow selectedRow)
        {
            selectedMailboxResource =
                selectedRow.ResourceName;

            foreach (KlpResourceRow row in mailboxRows)
            {
                row.SetSelected(row == selectedRow);
            }

            mailboxSelectedText.text =
                selectedMailboxResource;

            UpdateMailboxTransferPanel();

            //Debug.Log("[KLP] Mailbox resource selected: " +selectedMailboxResource);
        }
        private void SelectShippingResource(KlpResourceRow selectedRow)
        {
            selectedShippingResource =
                selectedRow.ResourceName;

            foreach (KlpResourceRow row in shippingRows)
            {
                row.SetSelected(row == selectedRow);
            }

            shippingSelectedText.text =
                selectedShippingResource;
            UpdateShippingTransferPanel();
            //Debug.Log("[KLP] Shipping resource selected: " +selectedShippingResource);
        }

        private void UpdateTabAppearance(Button button,bool selected,Color selectionColor)
        {
            Color background = selected
                ? selectionColor
                : palette.PanelBackground;

            ColorBlock colors = button.colors;

            colors.normalColor = background;

            colors.highlightedColor = Color.Lerp(
                background,
                palette.PrimaryText,
                0.08f);

            colors.selectedColor =
                colors.highlightedColor;

            colors.pressedColor = Color.Lerp(
                background,
                palette.WindowBackground,
                0.2f);

            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>();

            if (label != null)
            {
                label.fontStyle = selected
                    ? FontStyle.Bold
                    : FontStyle.Normal;
            }
        }

        private void BuildWindow()
        {
            if (initialized)
                return;

            uiFont = Resources.GetBuiltinResource<Font>(
                "Arial.ttf");

            uiFactory = new KlpUIFactory(palette, uiFont);

            canvasObject = new GameObject(
                "KLP_Canvas",
                typeof(RectTransform));

            canvasObject.SetActive(false);

            // Canvas

            Canvas canvas =
                canvasObject.AddComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 100;

            // Resolution scaling

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(1920, 1080);

            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            scaler.matchWidthOrHeight = 0.5f;

            // Mouse interaction

            canvasObject.AddComponent<GraphicRaycaster>();
            // Modal background.
            // Intercepts mouse clicks outside the KLP window.

            RectTransform modalBlocker = uiFactory.CreatePanel(
                "ModalBlocker",
                canvasObject.transform,
                Color.clear,
                true);

            uiFactory.Stretch(modalBlocker);

            // Main window

            windowRect = uiFactory.CreatePanel(
                "KLP_Window",
                canvasObject.transform,
                palette.WindowBackground);

            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);

            windowRect.sizeDelta = DefaultWindowSize;
            windowRect.anchoredPosition = Vector2.zero;

            BuildTitleBar();
            BuildContent();
            BuildResizeHandle();

            initialized = true;

            //Debug.Log("[KLP] uGUI initialized.");

            //Debug.Log("[KLP] EventSystem available: " +(EventSystem.current != null));
        }

        private void BuildTitleBar()
        {
            RectTransform titleBar = uiFactory.CreatePanel(
                "TitleBar",
                windowRect,
                palette.HeaderBackground);

            titleBar.anchorMin = new Vector2(0, 1);
            titleBar.anchorMax = new Vector2(1, 1);
            titleBar.pivot = new Vector2(0.5f, 1);

            titleBar.sizeDelta = new Vector2(0, 44);
            titleBar.anchoredPosition = Vector2.zero;

            // Draggable title bar

            KlpWindowDrag drag =
                titleBar.gameObject.AddComponent<KlpWindowDrag>();

            drag.Target = windowRect;

            // Window title

            Text title = uiFactory.CreateText(
                "WindowTitle",
                titleBar,
                "KLP",
                24,
                palette.PrimaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.Stretch(title.rectTransform);

            title.rectTransform.offsetMin =
                new Vector2(16, 0);

            title.rectTransform.offsetMax =
                new Vector2(-52, 0);

            // Close button

            Button closeButton = uiFactory.CreateButton(
                "CloseButton",
                titleBar,
                "X",
                palette.PanelBackground,
                palette.PrimaryText,
                () => SetVisible(false),
                18);

            RectTransform closeRect =
                closeButton.GetComponent<RectTransform>();

            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);

            closeRect.sizeDelta = new Vector2(34, 34);
            closeRect.anchoredPosition = new Vector2(-5, 0);
        }

        private void BuildContent()
        {
            RectTransform content = uiFactory.CreatePanel(
                "Content",
                windowRect,
                palette.WindowBackground);

            uiFactory.Stretch(content);

            content.offsetMin = new Vector2(16, 16);
            content.offsetMax = new Vector2(-16, -60);

            BuildLocationHeader(content);
            BuildTabBar(content);

            // Container for both persistent tab panels.

            RectTransform tabContainer = uiFactory.CreatePanel(
                "TabContainer",
                content,
                palette.PanelBackground);

            uiFactory.Stretch(tabContainer);

            tabContainer.offsetMin = new Vector2(0, 30);
            tabContainer.offsetMax = new Vector2(0, -112);

            // Mailbox tab

            mailboxPanel = uiFactory.CreatePanel(
                "MailboxPanel",
                tabContainer,
                palette.PanelBackground);

            uiFactory.Stretch(mailboxPanel);

            // Shipping tab

            shippingPanel = uiFactory.CreatePanel(
                "ShippingPanel",
                tabContainer,
                palette.PanelBackground);

            uiFactory.Stretch(shippingPanel);

            BuildMailboxTab();
            BuildShippingTab();

            BuildFooter(content);

            SetActiveTab(KlpTab.Mailbox);
        }
        private void BuildResizeHandle()
        {
            RectTransform handle =
                uiFactory.CreateResizeHandle(
                    "WindowResizeHandle",
                    windowRect,
                    palette.SecondaryText,
                    ResizeHandleSize);

            // Bottom-right corner of the window.

            handle.anchorMin = new Vector2(1f, 0f);
            handle.anchorMax = new Vector2(1f, 0f);

            handle.pivot = new Vector2(1f, 0f);

            handle.anchoredPosition =
                new Vector2(-4f, 4f);

            KlpWindowResize resize =
                handle.gameObject.AddComponent<KlpWindowResize>();

            resize.Target = windowRect;

            resize.MinimumSize = MinimumWindowSize;
            resize.MaximumSize = MaximumWindowSize;
        }




        private void OnDisable()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(
                CreateToolbarButton);

            GameEvents.onGUIApplicationLauncherDestroyed.Remove(
                OnToolbarDestroyed);

            if (toolbarButton != null)
            {
                if (ApplicationLauncher.Instance != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(
                        toolbarButton);
                }

                toolbarButton = null;
            }

            StopAllCoroutines();

            escapeClosePending = false;

            if (canvasObject != null)
            {
                canvasObject.SetActive(false);
            }

            InputLockManager.RemoveControlLock(InputLockId);
        }
        private void OnDestroy()
        {
            InputLockManager.RemoveControlLock(
                InputLockId);

            if (canvasObject != null)
            {
                Destroy(canvasObject);
            }
            if (toolbarIcon != null)
            {
                Destroy(toolbarIcon);
                toolbarIcon = null;
            }
            //Debug.Log("[KLP] Window component destroyed. " +"Input lock released.");
        }


        private void BuildLocationHeader(RectTransform parent)
        {
            RectTransform header = uiFactory.CreatePanel(
                "LocationHeader",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(header, 0, 58);

            // Mailbox heading

            Text heading = uiFactory.CreateText(
                "LocationHeading",
                header,
                "LOCAL MAILBOX",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 18, 12, 12);

            // Mailbox location

            locationText = uiFactory.CreateText(
                "LocationValue",
                header,
                "Unavailable",
                22,
                palette.PrimaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            RectTransform locationRect =
                locationText.rectTransform;

            locationRect.anchorMin = new Vector2(0, 0);
            locationRect.anchorMax = new Vector2(0.73f, 0);

            locationRect.offsetMin = new Vector2(12, 4);
            locationRect.offsetMax = new Vector2(-8, 34);

            // Access status

            accessStatusText = uiFactory.CreateText(
                "AccessStatus",
                header,
                "",
                15,
                palette.Success,
                TextAnchor.MiddleRight,
                FontStyle.Bold);

            RectTransform accessRect =
                accessStatusText.rectTransform;

            accessRect.anchorMin = new Vector2(0.73f, 0);
            accessRect.anchorMax = new Vector2(1, 0);

            accessRect.offsetMin = new Vector2(8, 4);
            accessRect.offsetMax = new Vector2(-12, 34);
        }
        private void BuildSearchClearButton(RectTransform parent,InputField input,float top,Color accent)
        {
            Button button = uiFactory.CreateButton(
                "ClearSearchButton",
                parent,
                "\u00D7",
                palette.RowBackground,
                accent,
                () => input.text = "",
                22);

            RectTransform rect =
                button.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);

            rect.sizeDelta = new Vector2(32, 32);

            rect.anchoredPosition =
                new Vector2(-12, -top);
        }
        private void BuildTabBar(RectTransform parent)
        {
            RectTransform tabBar = uiFactory.CreatePanel(
                "TabBar",
                parent,
                palette.WindowBackground);

            uiFactory.PlaceTop(tabBar, 66, 38);

            // Mailbox button

            mailboxTabButton = uiFactory.CreateButton(
                "MailboxTabButton",
                tabBar,
                "Mailbox",
                palette.PanelBackground,
                palette.MailboxAccent,
                () => SetActiveTab(KlpTab.Mailbox),
                18);

            RectTransform mailboxRect =
                mailboxTabButton.GetComponent<RectTransform>();

            mailboxRect.anchorMin = new Vector2(0, 0);
            mailboxRect.anchorMax = new Vector2(0.5f, 1);

            mailboxRect.offsetMin = Vector2.zero;
            mailboxRect.offsetMax = new Vector2(-2, 0);

            // Shipping button

            shippingTabButton = uiFactory.CreateButton(
                "ShippingTabButton",
                tabBar,
                "Shipping",
                palette.PanelBackground,
                palette.ShippingAccent,
                () => SetActiveTab(KlpTab.Shipping),
                18);

            RectTransform shippingRect =
                shippingTabButton.GetComponent<RectTransform>();

            shippingRect.anchorMin = new Vector2(0.5f, 0);
            shippingRect.anchorMax = new Vector2(1, 1);

            shippingRect.offsetMin = new Vector2(2, 0);
            shippingRect.offsetMax = Vector2.zero;

            // Selection indicators

            mailboxTabIndicator = CreateTabIndicator(
                mailboxRect,
                palette.MailboxAccent);

            shippingTabIndicator = CreateTabIndicator(
                shippingRect,
                palette.ShippingAccent);
        }
        private ScrollRect BuildResourceTable(RectTransform parent, string objectName, float top, out RectTransform content)
        {
            ScrollRect scroll = uiFactory.CreateScrollView(
                objectName,
                parent,
                out content);

            RectTransform rect =
                scroll.GetComponent<RectTransform>();

            uiFactory.Stretch(rect);

            rect.offsetMin = new Vector2(12, 62);
            rect.offsetMax = new Vector2(-12, -top);

            return scroll;
        }
        private void BuildVesselStoragePanel(RectTransform parent)
        {
            RectTransform panel = uiFactory.CreatePanel(
                "VesselStorage",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(
                panel, 50, 82, 10, 10);

            Text heading = uiFactory.CreateText(
                "VesselStorageHeading",
                panel,
                "VESSEL STORAGE",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 10, 10);

            // Current physical quantity.

            vesselQuantityText = uiFactory.CreateText(
                "VesselQuantity",
                panel,
                "--",
                22,
                palette.PrimaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceSplitTop(
                vesselQuantityText.rectTransform,
                false, 20, 26, 10, 4);

            // Maximum storage capacity.

            vesselCapacityText = uiFactory.CreateText(
                "VesselCapacity",
                panel,
                "--",
                16,
                palette.SecondaryText,
                TextAnchor.MiddleRight);

            uiFactory.PlaceSplitTop(
                vesselCapacityText.rectTransform,
                true, 20, 26, 10, 4);

            // Capacity utilization.

            RectTransform progress =
                uiFactory.CreateProgressBar(
                    "VesselCapacityBar",
                    panel,
                    palette.MailboxAccent,
                    out vesselCapacityFill);

            uiFactory.PlaceTop(
                progress,
                48, 8, 10, 10);

            // Available capacity.

            vesselFreeCapacityText = uiFactory.CreateText(
                "VesselFreeCapacity",
                panel,
                "Free capacity: --",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleLeft);

            uiFactory.PlaceTop(
                vesselFreeCapacityText.rectTransform,
                58, 20, 10, 10);
        }
        private void BuildMailboxStoragePanel(RectTransform parent)
        {
            RectTransform panel = uiFactory.CreatePanel(
                "MailboxStorage",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(
                panel, 140, 54, 10, 10);

            Text heading = uiFactory.CreateText(
                "MailboxStorageHeading",
                panel,
                "LOCAL MAILBOX",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 10, 10);

            mailboxQuantityText = uiFactory.CreateText(
                "MailboxQuantity",
                panel,
                "--",
                22,
                palette.MailboxAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceSplitTop(
                mailboxQuantityText.rectTransform,
                false, 20, 28, 10, 4);

            Text capacityLabel = uiFactory.CreateText(
                "MailboxCapacity",
                panel,
                "Unlimited storage",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleRight);

            uiFactory.PlaceSplitTop(
                capacityLabel.rectTransform,
                true, 20, 28, 10, 4);
        }
        private void BuildMailboxQuantityControls(RectTransform parent)
        {
            RectTransform quantityPanel =
                uiFactory.CreatePanel(
                    "TransferQuantityPanel",
                    parent,
                    palette.PanelBackground);

            uiFactory.PlaceTop(
                quantityPanel,
                202, 92, 10, 10);

            Text heading = uiFactory.CreateText(
                "TransferQuantityHeading",
                quantityPanel,
                "TRANSFER QUANTITY",
                12,
                palette.MailboxAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 8, 8);

            // Clearly identifiable editable field.

            RectTransform inputFrame;

            mailboxAmountInput =
                uiFactory.CreateBorderedInputField(
                    "MailboxAmount",
                    quantityPanel,
                    "",
                    "Enter quantity...",
                    palette.MailboxAccent,
                    out inputFrame,
                    18);

            uiFactory.PlaceTop(
                inputFrame,
                24, 32, 8, 8);

            // Quick-transfer buttons.

            allVesselButton = uiFactory.CreateButton(
                "AllVesselButton",
                quantityPanel,
                "All vessel",
                palette.HeaderBackground,
                palette.PrimaryText,
                SetAllVesselAmount,
                14);

            uiFactory.PlaceSplitTop(
                allVesselButton.GetComponent<RectTransform>(),
                false, 62, 26, 8, 8);

            maxWithdrawalButton = uiFactory.CreateButton(
                "MaxWithdrawalButton",
                quantityPanel,
                "Max withdrawal",
                palette.HeaderBackground,
                palette.PrimaryText,
                SetMaxWithdrawalAmount,
                14);

            uiFactory.PlaceSplitTop(
                maxWithdrawalButton.GetComponent<RectTransform>(),
                true, 62, 26, 8, 8);

            allVesselButton.interactable = false;
            maxWithdrawalButton.interactable = false;
        }
        private void BuildMailboxOperationControls(RectTransform parent)
        {
            // Deposit panel

            RectTransform depositPanel = uiFactory.CreatePanel(
                "DepositOperation",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceSplitTop(
                depositPanel, false, 302, 90, 10, 8);

            depositPreviewText = uiFactory.CreateText(
                "DepositPreview",
                depositPanel,
                "Vessel \u2192 Mailbox",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleCenter);

            uiFactory.PlaceTop(
                depositPreviewText.rectTransform,
                2, 54, 6, 6);

            depositButton = uiFactory.CreateButton(
                "DepositButton",
                depositPanel,
                "Deposit",
                palette.MailboxAccent,
                palette.AccentText,
                OnDepositClicked,
                15);

            uiFactory.PlaceTop(
                depositButton.GetComponent<RectTransform>(),
                58, 28, 6, 6);

            // Withdrawal panel

            RectTransform withdrawPanel = uiFactory.CreatePanel(
                "WithdrawOperation",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceSplitTop(
                withdrawPanel, true, 302, 90, 10, 8);

            withdrawPreviewText = uiFactory.CreateText(
                "WithdrawPreview",
                withdrawPanel,
                "Mailbox \u2192 Vessel",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleCenter);

            uiFactory.PlaceTop(
                withdrawPreviewText.rectTransform,
                2, 54, 6, 6);

            withdrawButton = uiFactory.CreateButton(
                "WithdrawButton",
                withdrawPanel,
                "Withdraw",
                palette.MailboxAccent,
                palette.AccentText,
                OnWithdrawClicked,
                15);

            uiFactory.PlaceTop(
                withdrawButton.GetComponent<RectTransform>(),
                58, 28, 6, 6);

            // Initial state

            depositButton.interactable = false;
            withdrawButton.interactable = false;

            depositButton.GetComponentInChildren<Text>().color =
                palette.SecondaryText;

            withdrawButton.GetComponentInChildren<Text>().color =
                palette.SecondaryText;
        }
        private void BuildMailboxTransferPanel(RectTransform parent)
        {
            RectTransform panel = uiFactory.CreatePanel(
                "MailboxTransferPanel",
                parent,
                palette.RowBackground);

            uiFactory.Stretch(panel);

            // Selected resource heading

            Text heading = uiFactory.CreateText(
                "SelectedResourceHeading",
                panel,
                "SELECTED RESOURCE",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 10, 10);

            mailboxSelectedText = uiFactory.CreateText(
                "SelectedResourceValue",
                panel,
                "None",
                20,
                palette.MailboxAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                mailboxSelectedText.rectTransform,
                20, 26, 10, 10);

            // Storage information

            BuildVesselStoragePanel(panel);
            BuildMailboxStoragePanel(panel);

            // Quantity entry

            BuildMailboxQuantityControls(panel);

            // Transaction controls

            BuildMailboxOperationControls(panel);
            mailboxAmountInput.onValueChanged.AddListener(OnMailboxAmountChanged);
        }
        private void BuildShippingQuantityControls(RectTransform parent)
        {
            RectTransform panel = uiFactory.CreatePanel(
                "ShipmentQuantityPanel",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(
                panel,
                230, 82, 10, 10);

            Text heading = uiFactory.CreateText(
                "ShipmentQuantityHeading",
                panel,
                "SHIPMENT QUANTITY",
                12,
                palette.ShippingAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 8, 8);

            RectTransform inputFrame;

            shippingAmountInput =
                uiFactory.CreateBorderedInputField(
                    "ShipmentAmount",
                    panel,
                    "",
                    "Enter quantity...",
                    palette.ShippingAccent,
                    out inputFrame,
                    18);

            uiFactory.PlaceTop(
                inputFrame,
                22, 32, 8, 8);

            allOriginButton = uiFactory.CreateButton(
                "AllOriginButton",
                panel,
                "All origin inventory",
                palette.HeaderBackground,
                palette.PrimaryText,
                SetAllOriginAmount,
                14);

            uiFactory.PlaceTop(
                allOriginButton.GetComponent<RectTransform>(),
                58, 22, 8, 8);

            allOriginButton.interactable = false;
            shippingAmountInput.interactable = false;
        }
        private void BuildShippingOperationControls(RectTransform parent)
        {
            RectTransform panel = uiFactory.CreatePanel(
                "ShipmentOperation",
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(
                panel,
                320, 72, 10, 10);

            shipmentPreviewText = uiFactory.CreateText(
                "ShipmentPreview",
                panel,
                "Select a resource and destination",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleCenter);

            uiFactory.PlaceTop(
                shipmentPreviewText.rectTransform,
                2, 36, 8, 8);

            shipButton = uiFactory.CreateButton(
                "ShipButton",
                panel,
                "Ship",
                palette.ShippingAccent,
                palette.AccentText,
                OnShipClicked,
                16);

            uiFactory.PlaceTop(
                shipButton.GetComponent<RectTransform>(),
                40, 28, 8, 8);

            shipButton.interactable = false;

            shipButton.GetComponentInChildren<Text>().color =
                palette.SecondaryText;
        }
        private void BuildShippingTransferPanel(RectTransform parent)
        {
            shippingOriginQuantityText =
                BuildShippingBalancePanel(
                    parent,
                    "ShippingOriginStorage",
                    "ORIGIN MAILBOX",
                    120,
                    palette.PrimaryText);

            shippingDestinationQuantityText =
                BuildShippingBalancePanel(
                    parent,
                    "ShippingDestinationStorage",
                    "DESTINATION MAILBOX",
                    174,
                    palette.ShippingAccent);

            BuildShippingQuantityControls(parent);

            BuildShippingOperationControls(parent);
            shippingAmountInput.onValueChanged.AddListener(OnShippingAmountChanged);
        }
        private void BuildTableHeader(RectTransform parent, float top, string column1, string column2, string column3)
        {
            RectTransform header = uiFactory.CreatePanel(
                "TableHeader",
                parent,
                palette.HeaderBackground);

            uiFactory.PlaceTop(
                header, top, 28, 12, 30);

            CreateHeaderColumn(
                header, column1,
                0f, 0.46f,
                TextAnchor.MiddleLeft);

            CreateHeaderColumn(
                header, column2,
                0.46f, 0.73f,
                TextAnchor.MiddleRight);

            CreateHeaderColumn(
                header, column3,
                0.73f, 1f,
                TextAnchor.MiddleRight);
        }
        private void BuildFooter(RectTransform parent)
        {
            RectTransform footer = uiFactory.CreatePanel(
                "Footer",
                parent,
                palette.WindowBackground,
                false);

            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);

            footer.pivot = new Vector2(0.5f, 0f);

            footer.sizeDelta = new Vector2(0f, 24f);
            footer.anchoredPosition = Vector2.zero;

            // Version label

            Text version = uiFactory.CreateText(
                "Version",
                footer,
                "KLP v1.0",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleLeft);

            RectTransform versionRect =
                version.rectTransform;

            versionRect.anchorMin = new Vector2(0f, 0f);
            versionRect.anchorMax = new Vector2(0f, 1f);

            versionRect.offsetMin = Vector2.zero;
            versionRect.offsetMax = new Vector2(100f, 0f);

            // Refresh button

            Button refreshButton = uiFactory.CreateButton(
                "RefreshButton",
                footer,
                "Refresh",
                palette.HeaderBackground,
                palette.PrimaryText,
                RefreshInventory,
                13);

            RectTransform refreshRect =
                refreshButton.GetComponent<RectTransform>();

            refreshRect.anchorMin = new Vector2(1f, 0f);
            refreshRect.anchorMax = new Vector2(1f, 1f);

            refreshRect.offsetMin = new Vector2(-118f, 0f);
            refreshRect.offsetMax = new Vector2(-28f, 0f);

            // Status text

            footerStatusText = uiFactory.CreateText(
                "FooterStatus",
                footer,
                "Inventory not loaded",
                13,
                palette.SecondaryText,
                TextAnchor.MiddleRight);

            uiFactory.Stretch(
                footerStatusText.rectTransform);

            footerStatusText.rectTransform.offsetMin =
                new Vector2(110f, 0f);

            footerStatusText.rectTransform.offsetMax =
                new Vector2(-128f, 0f);
        }
        private void BuildMailboxTab()
        {
            // ===================================
            // HORIZONTAL LAYOUT
            // ===================================

            RectTransform inventoryPanel = uiFactory.CreatePanel(
                "MailboxInventoryPanel",
                mailboxPanel,
                palette.PanelBackground);

            RectTransform transferPanel = uiFactory.CreatePanel(
                "MailboxTransferContainer",
                mailboxPanel,
                palette.RowBackground);

            uiFactory.PlaceHorizontalPanels(
                inventoryPanel,
                transferPanel,
                TransactionPanelWidth,
                TabPanelPadding,
                TabPanelGap);

            // ===================================
            // INVENTORY HEADING
            // ===================================

            Text heading = uiFactory.CreateText(
                "MailboxHeading",
                inventoryPanel,
                "LOCAL RESOURCE INVENTORY",
                16,
                palette.MailboxAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                0, 26, 0, 0);

            // ===================================
            // SEARCH CONTROLS
            // ===================================

            mailboxSearchInput = uiFactory.CreateInputField(
                "MailboxSearch",
                inventoryPanel,
                "",
                "Filter resources...",
                palette.MailboxAccent);

            uiFactory.PlaceTop(
                mailboxSearchInput.GetComponent<RectTransform>(),
                32, 32, 12, 48);

            BuildSearchClearButton(
                inventoryPanel,
                mailboxSearchInput,
                32,
                palette.MailboxAccent);

            // ===================================
            // RESOURCE TABLE
            // ===================================

            BuildTableHeader(
                inventoryPanel,
                72,
                "RESOURCE",
                "VESSEL",
                "MAILBOX");

            mailboxScroll = BuildResourceTable(
                inventoryPanel,
                "MailboxInventory",
                100,
                out mailboxInventoryContent);

            // Expand inventory to bottom of container.

            mailboxScroll.GetComponent<RectTransform>().offsetMin =
                new Vector2(12, 0);

            // ===================================
            // TRANSACTION CONTROLS
            // ===================================

            BuildMailboxTransferPanel(transferPanel);

            // ===================================
            // EMPTY INVENTORY MESSAGE
            // ===================================

            mailboxEmptyText = BuildEmptyInventoryMessage(
                inventoryPanel,
                mailboxScroll);

            mailboxSearchInput.onValueChanged.AddListener(
                OnMailboxFilterChanged);
        }
        private void BuildShippingTab()
        {
            // ===================================
            // HORIZONTAL LAYOUT
            // ===================================

            RectTransform inventoryPanel = uiFactory.CreatePanel(
                "ShippingInventoryPanel",
                shippingPanel,
                palette.PanelBackground);

            RectTransform transferPanel = uiFactory.CreatePanel(
                "ShippingTransferContainer",
                shippingPanel,
                palette.RowBackground);

            uiFactory.PlaceHorizontalPanels(
                inventoryPanel,
                transferPanel,
                TransactionPanelWidth,
                TabPanelPadding,
                TabPanelGap);

            // ===================================
            // INVENTORY HEADING
            // ===================================

            Text heading = uiFactory.CreateText(
                "ShippingHeading",
                inventoryPanel,
                "INTERPLANETARY SHIPPING",
                16,
                palette.ShippingAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                0, 26, 0, 0);

            // ===================================
            // SEARCH CONTROLS
            // ===================================

            shippingSearchInput = uiFactory.CreateInputField(
                "ShippingSearch",
                inventoryPanel,
                "",
                "Filter mailbox resources...",
                palette.ShippingAccent);

            uiFactory.PlaceTop(
                shippingSearchInput.GetComponent<RectTransform>(),
                32, 32, 12, 48);

            BuildSearchClearButton(
                inventoryPanel,
                shippingSearchInput,
                32,
                palette.ShippingAccent);

            // ===================================
            // RESOURCE TABLE
            // ===================================

            BuildTableHeader(
                inventoryPanel,
                72,
                "RESOURCE",
                "ORIGIN",
                "DESTINATION");

            shippingScroll = BuildResourceTable(
                inventoryPanel,
                "ShippingInventory",
                100,
                out shippingInventoryContent);

            shippingScroll.GetComponent<RectTransform>().offsetMin =
                new Vector2(12, 0);

            // ===================================
            // DESTINATION CONTROLS
            // ===================================

            Text destinationLabel = uiFactory.CreateText(
                "DestinationLabel",
                transferPanel,
                "DESTINATION MAILBOX",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                destinationLabel.rectTransform,
                4, 16, 10, 10);

            destinationDropdown = uiFactory.CreateDropdown(
                "DestinationDropdown",
                transferPanel,
                palette.ShippingAccent);
            destinationDropdown.captionText.color = palette.ShippingAccent;
            destinationDropdown.captionText.fontStyle = FontStyle.Bold;

            uiFactory.PlaceTop(
                destinationDropdown.GetComponent<RectTransform>(),
                24, 32, 10, 10);

            destinationDropdown.onValueChanged.AddListener(
                OnDestinationChanged);

            // ===================================
            // SELECTED RESOURCE
            // ===================================

            Text selectedHeading = uiFactory.CreateText(
                "SelectedResourceHeading",
                transferPanel,
                "SELECTED RESOURCE",
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                selectedHeading.rectTransform,
                68, 16, 10, 10);

            shippingSelectedText = uiFactory.CreateText(
                "SelectedResourceValue",
                transferPanel,
                "None",
                20,
                palette.ShippingAccent,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                shippingSelectedText.rectTransform,
                86, 26, 10, 10);

            // Reserved for future shipping controls.

            BuildShippingTransferPanel(transferPanel);

            // ===================================
            // EMPTY INVENTORY MESSAGE
            // ===================================

            shippingEmptyText = BuildEmptyInventoryMessage(
                inventoryPanel,
                shippingScroll);

            shippingSearchInput.onValueChanged.AddListener(
                OnShippingFilterChanged);
        }


        private Image CreateTabIndicator(RectTransform parent,Color color)
        {
            RectTransform indicator = uiFactory.CreatePanel(
                "TabIndicator",
                parent,
                color,
                false);

            indicator.anchorMin = new Vector2(0, 0);
            indicator.anchorMax = new Vector2(1, 0);
            indicator.pivot = new Vector2(0.5f, 0);

            indicator.sizeDelta = new Vector2(0, 3);
            indicator.anchoredPosition = Vector2.zero;

            return indicator.GetComponent<Image>();
        }
        private void CreateHeaderColumn(RectTransform parent,string value,float start,float end,TextAnchor alignment)
        {
            Text label = uiFactory.CreateText(
                value + "_Header",
                parent,
                value,
                13,
                palette.PrimaryText,
                alignment,
                FontStyle.Bold);

            RectTransform rect = label.rectTransform;

            rect.anchorMin = new Vector2(start, 0);
            rect.anchorMax = new Vector2(end, 1);

            rect.offsetMin = new Vector2(10, 0);
            rect.offsetMax = new Vector2(-10, 0);
        }
        private static string FormatQuantity(double amount)
        {
            return amount.ToString(
                "#,##0.####",
                CultureInfo.InvariantCulture);
        }
        private KlpResourceRow GetOrCreateRow(List<KlpResourceRow> rows,RectTransform content,int index,bool shipping,System.Action<KlpResourceRow> onSelected)
        {
            if (index < rows.Count)
            {
                return rows[index];
            }

            KlpResourceRow row =
                uiFactory.CreateResourceRow(
                    (shipping ? "ShippingRow_" : "MailboxRow_")
                        + index,
                    content,
                    shipping,
                    onSelected);

            rows.Add(row);

            return row;
        }
        private void FinishResourceTable(List<KlpResourceRow> rows,RectTransform content,int activeCount)
        {
            for (int i = activeCount; i < rows.Count; i++)
            {
                rows[i].gameObject.SetActive(false);
            }

            content.sizeDelta = new Vector2(
                0,
                activeCount * ResourceRowHeight);
        }
        private void PopulateMailboxTable()
        {
            if (inventorySnapshot == null ||
                !inventorySnapshot.AccessGranted)
            {
                FinishResourceTable(
                    mailboxRows,
                    mailboxInventoryContent,
                    0);

                selectedMailboxResource = "";
                mailboxSelectedText.text = "None";

                mailboxEmptyText.gameObject.SetActive(false);

                return;
            }

            // Build the complete inventory resource list.

            HashSet<string> names = new HashSet<string>(
                System.StringComparer.Ordinal);

            foreach (string name in
                inventorySnapshot.VesselResources.Keys)
            {
                names.Add(name);
            }

            foreach (string name in
                inventorySnapshot.MailboxResources.Keys)
            {
                names.Add(name);
            }

            // Validate selection against the COMPLETE
            // inventory, not the filtered inventory.

            if (!names.Contains(selectedMailboxResource))
            {
                selectedMailboxResource = "";
            }

            // Apply the current search filter.

            string filter = mailboxSearchInput.text;

            List<string> sortedNames =
                new List<string>();

            foreach (string name in names)
            {
                if (MatchesResourceFilter(name, filter))
                {
                    sortedNames.Add(name);
                }
            }

            sortedNames.Sort(
                System.StringComparer.Ordinal);

            // Populate reusable resource rows.

            for (int i = 0; i < sortedNames.Count; i++)
            {
                string name = sortedNames[i];

                double vesselAmount =
                    inventorySnapshot.GetVesselAmount(name);

                double capacity =
                    inventorySnapshot.GetVesselCapacity(name);

                double mailboxAmount =
                    inventorySnapshot.GetMailboxAmount(name);

                string vesselDisplay =
                    capacity <= 0
                        ? "No storage"
                        : FormatQuantity(vesselAmount);

                KlpResourceRow row = GetOrCreateRow(
                    mailboxRows,
                    mailboxInventoryContent,
                    i,
                    false,
                    SelectMailboxResource);

                uiFactory.PlaceTop(
                    row.GetComponent<RectTransform>(),
                    i * ResourceRowHeight,
                    ResourceRowHeight);

                row.Bind(
                    name,
                    vesselDisplay,
                    FormatQuantity(mailboxAmount),
                    i);

                row.SetSelected(
                    name == selectedMailboxResource);

                row.gameObject.SetActive(true);
            }

            // Hide unused rows and resize scroll content.

            FinishResourceTable(
                mailboxRows,
                mailboxInventoryContent,
                sortedNames.Count);

            // Update selection and empty inventory feedback.

            UpdateSelectedResourceDisplay(
                mailboxSelectedText,
                selectedMailboxResource,
                sortedNames);

            UpdateEmptyInventoryMessage(
                mailboxEmptyText,
                names.Count,
                sortedNames.Count);
            UpdateMailboxTransferPanel();
        }
        private void PopulateShippingTable()
        {
            if (inventorySnapshot == null ||
                !inventorySnapshot.AccessGranted)
            {
                FinishResourceTable(
                    shippingRows,
                    shippingInventoryContent,
                    0);

                selectedShippingResource = "";
                shippingSelectedText.text = "None";

                shippingEmptyText.gameObject.SetActive(false);
                UpdateShippingTransferPanel();
                return;
            }

            // Build the complete comparative resource list.

            HashSet<string> names = new HashSet<string>(
                System.StringComparer.Ordinal);

            foreach (string name in
                inventorySnapshot.MailboxResources.Keys)
            {
                names.Add(name);
            }

            foreach (string name in
                destinationInventory.Keys)
            {
                names.Add(name);
            }

            // Validate selection against complete inventory.

            if (!names.Contains(selectedShippingResource))
            {
                selectedShippingResource = "";
            }

            // Apply the current search filter.

            string filter = shippingSearchInput.text;

            List<string> sortedNames =
                new List<string>();

            foreach (string name in names)
            {
                if (MatchesResourceFilter(name, filter))
                {
                    sortedNames.Add(name);
                }
            }

            sortedNames.Sort(
                System.StringComparer.Ordinal);

            // Populate reusable shipping rows.

            for (int i = 0; i < sortedNames.Count; i++)
            {
                string name = sortedNames[i];

                double originAmount =
                    inventorySnapshot.GetMailboxAmount(name);

                double destinationAmount;

                destinationInventory.TryGetValue(
                    name,
                    out destinationAmount);

                KlpResourceRow row = GetOrCreateRow(
                    shippingRows,
                    shippingInventoryContent,
                    i,
                    true,
                    SelectShippingResource);

                uiFactory.PlaceTop(
                    row.GetComponent<RectTransform>(),
                    i * ResourceRowHeight,
                    ResourceRowHeight);

                row.Bind(
                    name,
                    FormatQuantity(originAmount),
                    string.IsNullOrEmpty(selectedDestination)
                        ? "N/A"
                        : FormatQuantity(destinationAmount),
                    i);

                row.SetSelected(
                    name == selectedShippingResource);

                row.gameObject.SetActive(true);
            }

            // Hide unused rows and resize scroll content.

            FinishResourceTable(
                shippingRows,
                shippingInventoryContent,
                sortedNames.Count);

            // Update selection and empty inventory feedback.

            UpdateSelectedResourceDisplay(
                shippingSelectedText,
                selectedShippingResource,
                sortedNames);

            UpdateEmptyInventoryMessage(
                shippingEmptyText,
                names.Count,
                sortedNames.Count);
            UpdateShippingTransferPanel();
        }
        private void RefreshDestinationOptions()
        {
            refreshingDestinations = true;

            try
            {
                destinationDropdown.Hide();
                destinationDropdown.ClearOptions();

                List<string> options = new List<string>();

                options.Add("Select destination...");

                if (inventorySnapshot != null &&
                    inventorySnapshot.AccessGranted)
                {
                    foreach (string destination in
                        inventorySnapshot.Destinations)
                    {
                        options.Add(destination);
                    }
                }

                destinationDropdown.AddOptions(options);

                int selectedIndex =
                    options.IndexOf(selectedDestination);

                if (selectedIndex <= 0)
                {
                    selectedDestination = "";
                    selectedIndex = 0;
                }

                destinationDropdown.value = selectedIndex;

                destinationDropdown.interactable =
                    options.Count > 1;

                destinationDropdown.RefreshShownValue();
            }
            finally
            {
                refreshingDestinations = false;
            }
        }
        private void CaptureDestinationInventory()
        {
            destinationInventory.Clear();

            if (inventorySnapshot == null ||
                !inventorySnapshot.AccessGranted ||
                string.IsNullOrWhiteSpace(selectedDestination))
            {
                return;
            }

            // Verify that the destination exists in
            // our current discovery snapshot.

            bool validDestination = false;

            foreach (string destination in
                inventorySnapshot.Destinations)
            {
                if (destination == selectedDestination)
                {
                    validDestination = true;
                    break;
                }
            }

            if (!validDestination)
                return;

            string[] location =
                selectedDestination.Split(':');

            if (location.Length != 2)
                return;

            var resources =
                KLPMailboxScenario.GetMailboxResources(
                    location[0],
                    location[1]);

            foreach (var resource in resources)
            {
                destinationInventory[resource.Key] =
                    resource.Value;
            }
        }
        private void OnDestinationChanged(int index)
        {
            if (refreshingDestinations)
                return;

            if (index <= 0 ||
                index >= destinationDropdown.options.Count)
            {
                selectedDestination = "";
            }
            else
            {
                selectedDestination =
                    destinationDropdown.options[index].text;
            }

            CaptureDestinationInventory();

            PopulateShippingTable();

            //Debug.Log("[KLP] Destination selected: " +selectedDestination);
        }
        private void RefreshInventory()
        {
            Stopwatch refreshTimer = EnablePerformanceDiagnostics ? Stopwatch.StartNew() : null;
            Vessel vessel = FlightGlobals.ActiveVessel;

            string previousLocation =
                inventorySnapshot == null
                    ? ""
                    : inventorySnapshot.Location;

            inventorySnapshot =
                KlpInventorySnapshot.Capture(vessel);
            double snapshotMs = refreshTimer != null ? refreshTimer.Elapsed.TotalMilliseconds
    : 0d;

            snapshotRefreshCount++;

            // Clear stale selections when the
            // originating mailbox changes.

            if (previousLocation != inventorySnapshot.Location)
            {
                selectedMailboxResource = "";
                selectedShippingResource = "";
                selectedDestination = "";
            }

            // Update location header.

            locationText.text =
                inventorySnapshot.Location;

            accessStatusText.text =
                inventorySnapshot.AccessGranted
                    ? "Access granted"
                    : "Station access required";

            accessStatusText.color =
                inventorySnapshot.AccessGranted
                    ? palette.Success
                    : palette.Error;

            // Refresh cached destination data.

            RefreshDestinationOptions();

            CaptureDestinationInventory();

            double destinationMs = refreshTimer != null ? refreshTimer.Elapsed.TotalMilliseconds : 0d;

            // Populate both resource tables.

            PopulateMailboxTable();

            PopulateShippingTable();

            // Enforce station access restrictions.

            bool allowed =
                inventorySnapshot.AccessGranted;

            mailboxTabButton.interactable = allowed;
            shippingTabButton.interactable = allowed;

            mailboxPanel.gameObject.SetActive(
                allowed && activeTab == KlpTab.Mailbox);

            shippingPanel.gameObject.SetActive(
                allowed && activeTab == KlpTab.Shipping);

            footerStatusText.text =
                allowed
                    ? "Inventory refreshed"
                    : "Mailbox access unavailable";
            footerStatusText.color = allowed ? palette.SecondaryText : palette.Error;

            //Debug.Log("[KLP] Snapshot #" + snapshotRefreshCount +" | Location=" + inventorySnapshot.Location +" | Access=" + allowed +" | Vessel resources=" +inventorySnapshot.VesselResources.Count +" | Mailbox resources=" +inventorySnapshot.MailboxResources.Count +" | Destinations=" +inventorySnapshot.Destinations.Count +" | Allocated mailbox rows=" +mailboxRows.Count +" | Allocated shipping rows=" +shippingRows.Count);
            //if (refreshTimer != null)
            //{
            //    refreshTimer.Stop();

            //    double totalMs =
            //        refreshTimer.Elapsed.TotalMilliseconds;

            //    Debug.Log(
            //        $"[KLP-PERF] Refresh #{snapshotRefreshCount}" +
            //        $" | Parts={vessel?.parts.Count ?? 0}" +
            //        $" | Snapshot={snapshotMs:F3}ms" +
            //        $" | DestinationStage={destinationMs - snapshotMs:F3}ms" +
            //        $" | Remaining={totalMs - destinationMs:F3}ms" +
            //        $" | Total={totalMs:F3}ms");
            //}

        }
        private static bool MatchesResourceFilter(string resourceName,string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                return true;

            return resourceName.IndexOf(
                filter.Trim(),
                System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
        private Text BuildEmptyInventoryMessage(RectTransform parent,ScrollRect scroll)
        {
            Text message = uiFactory.CreateText(
                "EmptyInventoryMessage",
                parent,
                "",
                17,
                palette.SecondaryText,
                TextAnchor.MiddleCenter);

            RectTransform source =
                scroll.GetComponent<RectTransform>();

            RectTransform target =
                message.rectTransform;

            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;

            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;

            message.raycastTarget = false;
            message.gameObject.SetActive(false);

            return message;
        }
        private void UpdateEmptyInventoryMessage(Text message,int totalCount,int visibleCount)
        {
            bool showMessage = visibleCount == 0;

            if (showMessage)
            {
                message.text = totalCount == 0
                    ? "No resources available."
                    : "No matching resources.\n" +
                      "Try a different search term.";
            }

            message.gameObject.SetActive(showMessage);
        }
        private void ResetResourceScroll(ScrollRect scroll)
        {
            if (scroll == null)
                return;

            scroll.StopMovement();

            if (scroll.content != null)
            {
                scroll.content.anchoredPosition = Vector2.zero;
            }

            scroll.verticalNormalizedPosition = 1f;
        }
        private void OnMailboxFilterChanged(string filter)
        {
            PopulateMailboxTable();

            ResetResourceScroll(mailboxScroll);
        }
        private void OnShippingFilterChanged(string filter)
        {
            PopulateShippingTable();

            ResetResourceScroll(shippingScroll);
        }
        private void UpdateSelectedResourceDisplay(Text label,string selectedResource,List<string> visibleResources)
        {
            if (string.IsNullOrEmpty(selectedResource))
            {
                label.text = "None";
                return;
            }

            bool visible =
                visibleResources.Contains(selectedResource);

            label.text = visible
                ? selectedResource
                : selectedResource + " (filtered)";
        }
        private void UpdateMailboxTransferPanel()
        {
            bool validSelection =
                inventorySnapshot != null &&
                inventorySnapshot.AccessGranted &&
                !string.IsNullOrEmpty(selectedMailboxResource);

            string resourceName = validSelection
                ? selectedMailboxResource
                : "";

            // Reset quantity when changing resources.

            if (displayedMailboxResource != resourceName)
            {
                displayedMailboxResource = resourceName;

                mailboxAmountInput.SetTextWithoutNotify("");
            }

            if (!validSelection)
            {
                vesselQuantityText.text = "--";
                vesselCapacityText.text = "--";
                vesselFreeCapacityText.text = "Free capacity: --";

                mailboxQuantityText.text = "--";

                uiFactory.SetProgress(
                    vesselCapacityFill,
                    0f);
            }
            else
            {
                double vesselAmount =
                    inventorySnapshot.GetVesselAmount(
                        resourceName);

                double vesselCapacity =
                    inventorySnapshot.GetVesselCapacity(
                        resourceName);

                double mailboxAmount =
                    inventorySnapshot.GetMailboxAmount(
                        resourceName);

                double freeCapacity = Math.Max(
                    0d,
                    vesselCapacity - vesselAmount);

                if (vesselCapacity > 0d)
                {
                    vesselQuantityText.text =
                        FormatQuantity(vesselAmount);

                    vesselCapacityText.text =
                        "/ " + FormatQuantity(vesselCapacity);

                    vesselFreeCapacityText.text =
                        "Free capacity: " +
                        FormatQuantity(freeCapacity);

                    uiFactory.SetProgress(
                        vesselCapacityFill,
                        (float)(vesselAmount / vesselCapacity));
                }
                else
                {
                    vesselQuantityText.text = "No storage";
                    vesselCapacityText.text = "";
                    vesselFreeCapacityText.text =
                        "Free capacity: 0";

                    uiFactory.SetProgress(
                        vesselCapacityFill,
                        0f);
                }

                mailboxQuantityText.text =
                    FormatQuantity(mailboxAmount);
            }

            // ALWAYS recalculate transaction previews.

            UpdateMailboxTransferPreviews();
        }
        private bool TryGetTransferAmount(InputField input,out double amount)
        {
            amount = 0d;

            if (input == null)
                return false;

            if (!double.TryParse(
                    input.text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out amount))
            {
                return false;
            }

            return KLPMailboxScenario.IsValidAmount(amount);
        }

        private bool TryGetMailboxTransferAmount(out double amount)
        {
            return TryGetTransferAmount(
                mailboxAmountInput,
                out amount);
        }
        private bool CanInteractWithMailboxSelection()
        {
            if (inventorySnapshot == null ||
                !inventorySnapshot.AccessGranted ||
                string.IsNullOrEmpty(selectedMailboxResource))
            {
                return false;
            }

            bool resourceExists =
                inventorySnapshot.VesselResources.ContainsKey(
                    selectedMailboxResource) ||
                inventorySnapshot.MailboxResources.ContainsKey(
                    selectedMailboxResource);

            if (!resourceExists)
                return false;

            return MatchesResourceFilter(
                selectedMailboxResource,
                mailboxSearchInput.text);
        }
        private void SetAllVesselAmount()
        {
            if (!CanInteractWithMailboxSelection())
                return;

            double amount =
                inventorySnapshot.GetVesselAmount(
                    selectedMailboxResource);

            if (!KLPMailboxScenario.IsValidAmount(amount))
                return;

            mailboxAmountInput.text = amount.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }
        private void SetMaxWithdrawalAmount()
        {
            if (!CanInteractWithMailboxSelection())
                return;

            double vesselAmount =
                inventorySnapshot.GetVesselAmount(
                    selectedMailboxResource);

            double vesselCapacity =
                inventorySnapshot.GetVesselCapacity(
                    selectedMailboxResource);

            double mailboxAmount =
                inventorySnapshot.GetMailboxAmount(
                    selectedMailboxResource);

            double freeCapacity = Math.Max(
                0d,
                vesselCapacity - vesselAmount);

            double amount = Math.Min(
                freeCapacity,
                mailboxAmount);

            if (!KLPMailboxScenario.IsValidAmount(amount))
                return;

            mailboxAmountInput.text = amount.ToString(
                "R",
                CultureInfo.InvariantCulture);
        }
        private void RenderMailboxOperationPreview(KlpTransferPreview preview,Text previewText,Button button,string operationName,string unavailableMessage)
        {
            Text buttonLabel =
                button.GetComponentInChildren<Text>();

            bool available =
                preview != null &&
                preview.CanTransfer;

            button.interactable = available;

            buttonLabel.color = available
                ? palette.AccentText
                : palette.SecondaryText;

            if (!available)
            {
                buttonLabel.text = operationName;

                previewText.text = preview != null
                    ? preview.Status
                    : unavailableMessage;

                previewText.color =
                    palette.SecondaryText;

                return;
            }

            buttonLabel.text =
                operationName + " " +
                FormatQuantity(preview.ActualAmount);

            previewText.text =
                "Vessel: " +
                FormatQuantity(preview.ProjectedVesselAmount) +
                "\nMailbox: " +
                FormatQuantity(preview.ProjectedMailboxAmount) +
                "\n" +
                preview.Status;

            previewText.color = preview.IsPartial
                ? palette.Warning
                : palette.PrimaryText;
        }
        private void UpdateMailboxTransferPreviews()
        {
            depositPreview = null;
            withdrawPreview = null;

            bool canInteract =
                CanInteractWithMailboxSelection();

            mailboxAmountInput.interactable =
                canInteract;

            allVesselButton.interactable = false;

            maxWithdrawalButton.interactable = false;

            if (!canInteract)
            {
                string message =
                    string.IsNullOrEmpty(selectedMailboxResource)
                        ? "Select a resource"
                        : "Selection unavailable";

                if (inventorySnapshot != null &&
                    inventorySnapshot.AccessGranted &&
                    !string.IsNullOrEmpty(selectedMailboxResource) &&
                    !MatchesResourceFilter(
                        selectedMailboxResource,
                        mailboxSearchInput.text))
                {
                    message = "Selection filtered out";
                }

                RenderMailboxOperationPreview(
                    null,
                    depositPreviewText,
                    depositButton,
                    "Deposit",
                    message);

                RenderMailboxOperationPreview(
                    null,
                    withdrawPreviewText,
                    withdrawButton,
                    "Withdraw",
                    message);

                return;
            }

            double vesselAmount =
                inventorySnapshot.GetVesselAmount(
                    selectedMailboxResource);

            double vesselCapacity =
                inventorySnapshot.GetVesselCapacity(
                    selectedMailboxResource);

            double mailboxAmount =
                inventorySnapshot.GetMailboxAmount(
                    selectedMailboxResource);

            double freeCapacity = Math.Max(
                0d,
                vesselCapacity - vesselAmount);

            // Quick-transfer availability is independent
            // of the currently entered quantity.

            allVesselButton.interactable =
                KLPMailboxScenario.IsValidAmount(
                    vesselAmount);

            maxWithdrawalButton.interactable =
                KLPMailboxScenario.IsValidAmount(
                    Math.Min(freeCapacity, mailboxAmount));

            // Validate the entered quantity.

            double requestedAmount;

            if (!TryGetMailboxTransferAmount(
                    out requestedAmount))
            {
                RenderMailboxOperationPreview(
                    null,
                    depositPreviewText,
                    depositButton,
                    "Deposit",
                    "Enter a valid quantity");

                RenderMailboxOperationPreview(
                    null,
                    withdrawPreviewText,
                    withdrawButton,
                    "Withdraw",
                    "Enter a valid quantity");

                return;
            }

            // Calculate both possible transactions.

            depositPreview =
                KlpTransferPreview.Calculate(
                    requestedAmount,
                    vesselAmount,
                    vesselCapacity,
                    mailboxAmount,
                    true);

            withdrawPreview =
                KlpTransferPreview.Calculate(
                    requestedAmount,
                    vesselAmount,
                    vesselCapacity,
                    mailboxAmount,
                    false);

            // Update the persistent controls.

            RenderMailboxOperationPreview(
                depositPreview,
                depositPreviewText,
                depositButton,
                "Deposit",
                "");

            RenderMailboxOperationPreview(
                withdrawPreview,
                withdrawPreviewText,
                withdrawButton,
                "Withdraw",
                "");
        }
        private void ExecuteLocalMailboxTransaction(bool isDeposit)
        {
            KlpTransferPreview preview = isDeposit
                ? depositPreview
                : withdrawPreview;

            if (inventorySnapshot == null ||
                preview == null ||
                !preview.CanTransfer)
            {
                return;
            }

            string resourceName = selectedMailboxResource;

            double transferredAmount;
            Stopwatch transactionTimer = EnablePerformanceDiagnostics ? Stopwatch.StartNew() : null;

            bool success =
                KlpTransactionEngine.ExecuteMailboxTransaction(
                    inventorySnapshot.SourceVessel,
                    inventorySnapshot.Body,
                    inventorySnapshot.Biome,
                    resourceName,
                    preview.ActualAmount,
                    isDeposit,
                    out transferredAmount);

            if (transactionTimer != null)
            {
                transactionTimer.Stop();

                Debug.Log(
                    $"[KLP-PERF] " +
                    $"{(isDeposit ? "Deposit" : "Withdrawal")}" +
                    $" | Success={success}" +
                    $" | Engine={transactionTimer.Elapsed.TotalMilliseconds:F3}ms");
            }

            if (success)
            {
                mailboxAmountInput.SetTextWithoutNotify("");
            }

            RefreshInventory();

            if (success)
            {
                bool partial =
                    transferredAmount < preview.RequestedAmount;

                footerStatusText.text =
                    (isDeposit ? "Deposited " : "Withdrew ") +
                    FormatQuantity(transferredAmount) +
                    " " +
                    resourceName +
                    (partial ? " (partial)" : "");

                footerStatusText.color = partial
                    ? palette.Warning
                    : palette.Success;
            }
            else
            {
                footerStatusText.text =
                    "Transfer rejected. Review inventory.";

                footerStatusText.color = palette.Error;
            }
        }
        private void OnDepositClicked()
        {
            ExecuteLocalMailboxTransaction(true);
        }
        private void OnWithdrawClicked()
        {
            ExecuteLocalMailboxTransaction(false);
        }
        private void OnShippingAmountChanged(string value)
        {
            UpdateShippingTransferPreview();
        }
        private Text BuildShippingBalancePanel(RectTransform parent,string objectName,string headingText,float top,Color quantityColor)
        {
            RectTransform panel = uiFactory.CreatePanel(
                objectName,
                parent,
                palette.PanelBackground);

            uiFactory.PlaceTop(
                panel,
                top, 48, 10, 10);

            Text heading = uiFactory.CreateText(
                objectName + "_Heading",
                panel,
                headingText,
                12,
                palette.SecondaryText,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                heading.rectTransform,
                4, 16, 10, 10);

            Text quantity = uiFactory.CreateText(
                objectName + "_Quantity",
                panel,
                "--",
                22,
                quantityColor,
                TextAnchor.MiddleLeft,
                FontStyle.Bold);

            uiFactory.PlaceTop(
                quantity.rectTransform,
                20, 26, 10, 10);

            return quantity;
        }
        private void UpdateShippingTransferPanel()
        {
            bool validSelection =
                inventorySnapshot != null &&
                inventorySnapshot.AccessGranted &&
                !string.IsNullOrEmpty(
                    selectedShippingResource);

            bool validDestination =
                inventorySnapshot != null &&
                inventorySnapshot.AccessGranted &&
                !string.IsNullOrEmpty(
                    selectedDestination);

            string resourceName = validSelection
                ? selectedShippingResource
                : "";

            string destination = validDestination
                ? selectedDestination
                : "";

            // Clear quantity when shipment context changes.

            if (displayedShippingResource != resourceName ||
                displayedShippingDestination != destination)
            {
                displayedShippingResource = resourceName;
                displayedShippingDestination = destination;

                shippingAmountInput.SetTextWithoutNotify("");
            }

            // Display origin inventory.

            if (validSelection)
            {
                shippingOriginQuantityText.text =
                    FormatQuantity(
                        inventorySnapshot.GetMailboxAmount(
                            resourceName));
            }
            else
            {
                shippingOriginQuantityText.text = "--";
            }

            // Display destination inventory.

            if (validSelection && validDestination)
            {
                double destinationAmount;

                destinationInventory.TryGetValue(
                    resourceName,
                    out destinationAmount);

                shippingDestinationQuantityText.text =
                    FormatQuantity(destinationAmount);
            }
            else
            {
                shippingDestinationQuantityText.text = "--";
            }

            UpdateShippingTransferPreview();
        }
        private bool CanInteractWithShippingSelection()
        {
            return inventorySnapshot != null &&
                   inventorySnapshot.AccessGranted &&
                   !string.IsNullOrEmpty(selectedDestination) &&
                   !string.IsNullOrEmpty(selectedShippingResource) &&
                   MatchesResourceFilter(
                       selectedShippingResource,
                       shippingSearchInput.text);
        }
        private void SetAllOriginAmount()
        {
            double amount =
                inventorySnapshot.GetMailboxAmount(
                    selectedShippingResource);

            shippingAmountInput.text =
                amount.ToString(
                    "R",
                    CultureInfo.InvariantCulture);
        }
        private void UpdateShippingTransferPreview()
        {
            requestedShipmentAmount = 0d;
            approvedShipmentAmount = 0d;

            bool canInteract =
                CanInteractWithShippingSelection();

            shippingAmountInput.interactable =
                canInteract;

            allOriginButton.interactable = false;
            shipButton.interactable = false;

            Text buttonLabel =
                shipButton.GetComponentInChildren<Text>();

            buttonLabel.text = "Ship";
            buttonLabel.color = palette.SecondaryText;

            shipmentPreviewText.color =
                palette.SecondaryText;

            if (!canInteract)
            {
                if (string.IsNullOrEmpty(
                        selectedShippingResource))
                {
                    shipmentPreviewText.text =
                        "Select a resource";
                }
                else if (string.IsNullOrEmpty(
                             selectedDestination))
                {
                    shipmentPreviewText.text =
                        "Select a destination";
                }
                else if (!MatchesResourceFilter(
                             selectedShippingResource,
                             shippingSearchInput.text))
                {
                    shipmentPreviewText.text =
                        "Selection filtered out";
                }
                else
                {
                    shipmentPreviewText.text =
                        "Shipment unavailable";
                }

                return;
            }

            // Retrieve cached mailbox balances.

            double originAmount =
                inventorySnapshot.GetMailboxAmount(
                    selectedShippingResource);

            double destinationAmount;

            destinationInventory.TryGetValue(
                selectedShippingResource,
                out destinationAmount);

            allOriginButton.interactable =
                originAmount > 0d;

            if (originAmount <= 0d)
            {
                shipmentPreviewText.text =
                    "Origin mailbox empty";

                return;
            }

            // Parse the requested quantity.

            double requestedAmount;

            if (!TryGetTransferAmount(
                    shippingAmountInput,
                    out requestedAmount))
            {
                shipmentPreviewText.text =
                    "Enter a valid quantity";

                return;
            }

            // Calculate available shipment.

            double actualAmount =
                Math.Min(
                    requestedAmount,
                    originAmount);

            double projectedOrigin =
                originAmount - actualAmount;

            double projectedDestination =
                destinationAmount + actualAmount;

            // Reject a destination balance that cannot
            // be represented as a finite double.

            if (double.IsInfinity(projectedDestination))
            {
                shipmentPreviewText.text =
                    "Destination balance overflow";

                return;
            }

            requestedShipmentAmount = requestedAmount;
            approvedShipmentAmount = actualAmount;

            bool partial =
                actualAmount < requestedAmount;

            // Display projected balances.

            shipmentPreviewText.text =
                "Origin: " +
                FormatQuantity(projectedOrigin) +
                "  |  Destination: " +
                FormatQuantity(projectedDestination) +
                "\n" +
                (partial ? "Partial shipment" : "Ready");

            shipmentPreviewText.color = partial
                ? palette.Warning
                : palette.PrimaryText;

            buttonLabel.text =
                "Ship " + FormatQuantity(actualAmount);

            buttonLabel.color =
                palette.AccentText;

            shipButton.interactable = true;
        }
        private void OnShipClicked()
        {
            if (approvedShipmentAmount <= 0d)
                return;

            string resourceName = selectedShippingResource;
            string originBody = inventorySnapshot.Body;
            string originBiome = inventorySnapshot.Biome;
            string destination = selectedDestination;

            double amount = approvedShipmentAmount;

            bool partial =
                amount < requestedShipmentAmount;

            string destinationBody;
            string destinationBiome;

            if (!KLPMailboxScenario.TryParseLocation(
                    destination,
                    out destinationBody,
                    out destinationBiome))
            {
                return;
            }
            Stopwatch transactionTimer = EnablePerformanceDiagnostics ? Stopwatch.StartNew() : null;
            bool success =
                KLPMailboxScenario.ShipResource(
                    originBody,
                    originBiome,
                    destinationBody,
                    destinationBiome,
                    resourceName,
                    amount);

            if (transactionTimer != null)
            {
                transactionTimer.Stop();

                Debug.Log(
                    $"[KLP-PERF] Shipment" +
                    $" | Success={success}" +
                    $" | Engine={transactionTimer.Elapsed.TotalMilliseconds:F3}ms");
            }
            if (success)
            {
                shippingAmountInput.SetTextWithoutNotify("");
            }

            RefreshInventory();

            if (success)
            {
                footerStatusText.text =
                    "Shipped " +
                    FormatQuantity(amount) +
                    " " +
                    resourceName +
                    (partial ? " (partial)" : "");

                footerStatusText.color = partial
                    ? palette.Warning
                    : palette.Success;
            }
            else
            {
                footerStatusText.text =
                    "Shipment rejected. Review inventory.";

                footerStatusText.color = palette.Error;
            }
        }

    }

    public sealed class KlpWindowDrag :MonoBehaviour,IBeginDragHandler,IDragHandler
    {
        public RectTransform Target;

        private Vector2 pointerStart;
        private Vector2 windowStart;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            RectTransform parent =
                Target.parent as RectTransform;

            if (parent == null)
                return;

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pointerStart);

            windowStart = Target.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null)
                return;

            RectTransform parent =
                Target.parent as RectTransform;

            if (parent == null)
                return;

            Vector2 pointerPosition;

            if (RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    parent,
                    eventData.position,
                    eventData.pressEventCamera,
                    out pointerPosition))
            {
                Target.anchoredPosition =
                    windowStart +
                    (pointerPosition - pointerStart);
            }
        }
    }
}