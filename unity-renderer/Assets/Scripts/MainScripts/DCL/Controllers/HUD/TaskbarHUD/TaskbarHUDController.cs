using System;
using DCL;
using DCL.HelpAndSupportHUD;
using DCL.Helpers;
using DCL.Interface;
using DCL.SettingsPanelHUD;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DCL.Controllers;

public class TaskbarHUDController : IHUD
{
    [Serializable]
    public struct Configuration
    {
        public bool enableVoiceChat;
        public bool enableQuestPanel;
    }

    public TaskbarHUDView view;
    public WorldChatWindowHUDController worldChatWindowHud;
    public PrivateChatWindowHUDController privateChatWindowHud;
    public FriendsHUDController friendsHud;
    public SettingsPanelHUDController settingsPanelHud;
    public ExploreHUDController exploreHud;
    public IExploreV2MenuComponentController exploreV2Hud;
    public HelpAndSupportHUDController helpAndSupportHud;

    IMouseCatcher mouseCatcher;
    protected IChatController chatController;
    protected IFriendsController friendsController;

    private InputAction_Trigger toggleFriendsTrigger;
    private InputAction_Trigger closeWindowTrigger;
    private InputAction_Trigger toggleWorldChatTrigger;
    private ISceneController sceneController;
    private IWorldState worldState;

    public event System.Action OnAnyTaskbarButtonClicked;

    public RectTransform tutorialTooltipReference { get => view.moreTooltipReference; }

    public RectTransform exploreTooltipReference { get => view.exploreTooltipReference; }

    public RectTransform socialTooltipReference { get => view.socialTooltipReference; }

    public TaskbarMoreMenu moreMenu { get => view.moreMenu; }

    protected internal virtual TaskbarHUDView CreateView() { return TaskbarHUDView.Create(this, chatController, friendsController); }

    public void Initialize(
        IMouseCatcher mouseCatcher,
        IChatController chatController,
        IFriendsController friendsController,
        ISceneController sceneController,
        IWorldState worldState)
    {
        this.friendsController = friendsController;
        this.mouseCatcher = mouseCatcher;
        this.chatController = chatController;

        view = CreateView();

        this.sceneController = sceneController;
        this.worldState = worldState;

        if (mouseCatcher != null)
        {
            mouseCatcher.OnMouseLock -= MouseCatcher_OnMouseLock;
            mouseCatcher.OnMouseUnlock -= MouseCatcher_OnMouseUnlock;
            mouseCatcher.OnMouseLock += MouseCatcher_OnMouseLock;
            mouseCatcher.OnMouseUnlock += MouseCatcher_OnMouseUnlock;
        }

        view.chatHeadsGroup.OnHeadToggleOn += ChatHeadsGroup_OnHeadOpen;
        view.chatHeadsGroup.OnHeadToggleOff += ChatHeadsGroup_OnHeadClose;

        view.leftWindowContainerLayout.enabled = false;

        view.OnChatToggleOff += View_OnChatToggleOff;
        view.OnChatToggleOn += View_OnChatToggleOn;
        view.OnFriendsToggleOff += View_OnFriendsToggleOff;
        view.OnFriendsToggleOn += View_OnFriendsToggleOn;
        view.OnSettingsToggleOff += View_OnSettingsToggleOff;
        view.OnSettingsToggleOn += View_OnSettingsToggleOn;
        view.OnBuilderInWorldToggleOff += View_OnBuilderInWorldToggleOff;
        view.OnBuilderInWorldToggleOn += View_OnBuilderInWorldToggleOn;
        view.OnExploreToggleOff += View_OnExploreToggleOff;
        view.OnExploreToggleOn += View_OnExploreToggleOn;
        view.OnExploreV2ToggleOff += View_OnExploreV2ToggleOff;
        view.OnExploreV2ToggleOn += View_OnExploreV2ToggleOn;
        view.OnQuestPanelToggled -= View_OnQuestPanelToggled;
        view.OnQuestPanelToggled += View_OnQuestPanelToggled;

        toggleFriendsTrigger = Resources.Load<InputAction_Trigger>("ToggleFriends");
        toggleFriendsTrigger.OnTriggered -= ToggleFriendsTrigger_OnTriggered;
        toggleFriendsTrigger.OnTriggered += ToggleFriendsTrigger_OnTriggered;

        closeWindowTrigger = Resources.Load<InputAction_Trigger>("CloseWindow");
        closeWindowTrigger.OnTriggered -= CloseWindowTrigger_OnTriggered;
        closeWindowTrigger.OnTriggered += CloseWindowTrigger_OnTriggered;

        toggleWorldChatTrigger = Resources.Load<InputAction_Trigger>("ToggleWorldChat");
        toggleWorldChatTrigger.OnTriggered -= ToggleWorldChatTrigger_OnTriggered;
        toggleWorldChatTrigger.OnTriggered += ToggleWorldChatTrigger_OnTriggered;

        DataStore.i.HUDs.questsPanelVisible.OnChange -= OnToggleQuestsPanelTriggered;
        DataStore.i.HUDs.questsPanelVisible.OnChange += OnToggleQuestsPanelTriggered;

        if (chatController != null)
        {
            chatController.OnAddMessage -= OnAddMessage;
            chatController.OnAddMessage += OnAddMessage;
        }

        if (this.sceneController != null && this.worldState != null)
        {
            this.sceneController.OnNewPortableExperienceSceneAdded += SceneController_OnNewPortableExperienceSceneAdded;
            this.sceneController.OnNewPortableExperienceSceneRemoved += SceneController_OnNewPortableExperienceSceneRemoved;

            List<GlobalScene> activePortableExperiences = WorldStateUtils.GetActivePortableExperienceScenes();
            for (int i = 0; i < activePortableExperiences.Count; i++)
            {
                SceneController_OnNewPortableExperienceSceneAdded(activePortableExperiences[i]);
            }
        }

        view.leftWindowContainerAnimator.Show();

        CommonScriptableObjects.isTaskbarHUDInitialized.Set(true);
        DataStore.i.builderInWorld.showTaskBar.OnChange += SetVisibility;

        ConfigureExploreV2Feature();
    }

    private void View_OnQuestPanelToggled(bool value)
    {
        QuestsUIAnalytics.SendQuestLogVisibiltyChanged(value, "taskbar");
        DataStore.i.HUDs.questsPanelVisible.Set(value);
    }

    private void ChatHeadsGroup_OnHeadClose(TaskbarButton obj) { privateChatWindowHud.SetVisibility(false); }

    private void View_OnFriendsToggleOn()
    {
        friendsHud?.SetVisibility(true);
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnFriendsToggleOff() { friendsHud?.SetVisibility(false); }

    private void ToggleFriendsTrigger_OnTriggered(DCLAction_Trigger action)
    {
        if (!view.friendsButton.gameObject.activeSelf)
            return;

        OnFriendsToggleInputPress();
    }

    private void ToggleWorldChatTrigger_OnTriggered(DCLAction_Trigger action) { OnWorldChatToggleInputPress(); }

    private void OnToggleQuestsPanelTriggered(bool current, bool previous)
    {
        bool anyInputFieldIsSelected = EventSystem.current != null &&
                                       EventSystem.current.currentSelectedGameObject != null &&
                                       EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null &&
                                       (!worldChatWindowHud.view.chatHudView.inputField.isFocused || !worldChatWindowHud.view.isInPreview);

        if (anyInputFieldIsSelected)
            return;

        view.questPanelButton.SetToggleState(current, false);
        if (current)
            view.SelectButton(view.questPanelButton);
    }

    private void CloseWindowTrigger_OnTriggered(DCLAction_Trigger action) { OnCloseWindowToggleInputPress(); }

    private void View_OnChatToggleOn()
    {
        worldChatWindowHud.SetVisibility(true);
        worldChatWindowHud.MarkWorldChatMessagesAsRead();
        worldChatWindowHud.view.DeactivatePreview();
        worldChatWindowHud.OnPressReturn();
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnChatToggleOff()
    {
        if (view.AllButtonsToggledOff())
        {
            worldChatWindowHud.SetVisibility(true);
            worldChatWindowHud.view.ActivatePreview();
        }
        else
        {
            worldChatWindowHud.SetVisibility(false);
        }
    }

    private void ChatHeadsGroup_OnHeadOpen(TaskbarButton taskbarBtn)
    {
        ChatHeadButton head = taskbarBtn as ChatHeadButton;

        if (taskbarBtn == null)
            return;

        OpenPrivateChatWindow(head.profile.userId);
    }

    private void View_OnSettingsToggleOn()
    {
        settingsPanelHud.SetVisibility(true);
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnSettingsToggleOff() { settingsPanelHud.SetVisibility(false); }

    private void View_OnBuilderInWorldToggleOn()
    {
        OnBuilderProjectsPanelTriggered(true, false);
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnBuilderInWorldToggleOff() { OnBuilderProjectsPanelTriggered(false, true); }

    private void View_OnExploreToggleOn()
    {
        exploreHud.SetVisibility(true);
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnExploreToggleOff() { exploreHud.SetVisibility(false); }

    private void View_OnExploreV2ToggleOn()
    {
        DataStore.i.taskbar.isExploreV2Enabled.Set(false);
        DataStore.i.taskbar.isExploreV2Enabled.Set(true);
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    private void View_OnExploreV2ToggleOff() { DataStore.i.taskbar.isExploreV2Enabled.Set(false); }

    private void MouseCatcher_OnMouseUnlock() { view.leftWindowContainerAnimator.Show(); }

    private void MouseCatcher_OnMouseLock()
    {
        view.leftWindowContainerAnimator.Hide();

        foreach (var btn in view.GetButtonList())
        {
            btn.SetToggleState(false);
        }

        worldChatWindowHud.SetVisibility(true);
        worldChatWindowHud.view.ActivatePreview();

        MarkWorldChatAsReadIfOtherWindowIsOpen();
    }

    public void SetBuilderInWorldStatus(bool isActive)
    {
        view.SetBuilderInWorldStatus(isActive);
        DataStore.i.HUDs.builderProjectsPanelVisible.OnChange -= OnBuilderProjectsPanelTriggered;

        if (isActive)
            DataStore.i.HUDs.builderProjectsPanelVisible.OnChange += OnBuilderProjectsPanelTriggered;
    }

    public void SetQuestsPanelStatus(bool isActive) { view.SetQuestsPanelStatus(isActive); }

    public void AddWorldChatWindow(WorldChatWindowHUDController controller)
    {
        if (controller == null || controller.view == null)
        {
            Debug.LogWarning("AddChatWindow >>> World Chat Window doesn't exist yet!");
            return;
        }

        if (controller.view.transform.parent == view.leftWindowContainer)
            return;

        controller.view.transform.SetParent(view.leftWindowContainer, false);

        worldChatWindowHud = controller;

        view.OnAddChatWindow();
        worldChatWindowHud.view.OnClose += () => { view.friendsButton.SetToggleState(false, false); };

        view.chatButton.SetToggleState(true);
        view.chatButton.SetToggleState(false);
    }

    public void OpenFriendsWindow() { view.friendsButton.SetToggleState(true); }

    public void OpenPrivateChatTo(string userId)
    {
        var button = view.chatHeadsGroup.AddChatHead(userId, (ulong) System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        button.toggleButton.onClick.Invoke();
    }

    public void AddPrivateChatWindow(PrivateChatWindowHUDController controller)
    {
        if (controller == null || controller.view == null)
        {
            Debug.LogWarning("AddPrivateChatWindow >>> Private Chat Window doesn't exist yet!");
            return;
        }

        if (controller.view.transform.parent == view.leftWindowContainer)
            return;

        controller.view.transform.SetParent(view.leftWindowContainer, false);

        privateChatWindowHud = controller;

        privateChatWindowHud.view.OnMinimize += () =>
        {
            ChatHeadButton btn = view.GetButtonList()
                                     .FirstOrDefault(
                                         (x) => x is ChatHeadButton &&
                                                (x as ChatHeadButton).profile.userId == privateChatWindowHud.conversationUserId) as
                ChatHeadButton;

            if (btn != null)
                btn.SetToggleState(false, false);

            MarkWorldChatAsReadIfOtherWindowIsOpen();
        };

        privateChatWindowHud.view.OnClose += () =>
        {
            ChatHeadButton btn = view.GetButtonList()
                                     .FirstOrDefault(
                                         (x) => x is ChatHeadButton &&
                                                (x as ChatHeadButton).profile.userId == privateChatWindowHud.conversationUserId) as
                ChatHeadButton;

            if (btn != null)
            {
                btn.SetToggleState(false, false);
                view.chatHeadsGroup.RemoveChatHead(btn);
            }

            MarkWorldChatAsReadIfOtherWindowIsOpen();
        };
    }

    public void AddFriendsWindow(FriendsHUDController controller)
    {
        if (controller == null || controller.view == null)
        {
            Debug.LogWarning("AddFriendsWindow >>> Friends window doesn't exist yet!");
            return;
        }

        if (controller.view.transform.parent == view.leftWindowContainer)
            return;

        controller.view.transform.SetParent(view.leftWindowContainer, false);

        friendsHud = controller;
        view.OnAddFriendsWindow();
        friendsHud.view.OnClose += () =>
        {
            view.friendsButton.SetToggleState(false, false);
            MarkWorldChatAsReadIfOtherWindowIsOpen();
        };

        friendsHud.view.friendsList.OnDeleteConfirmation += (userIdToRemove) => { view.chatHeadsGroup.RemoveChatHead(userIdToRemove); };
    }

    public void AddSettingsWindow(SettingsPanelHUDController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("AddSettingsWindow >>> Settings window doesn't exist yet!");
            return;
        }

        settingsPanelHud = controller;
        view.OnAddSettingsWindow();
        settingsPanelHud.OnOpen += () =>
        {
            view.settingsButton.SetToggleState(true, false);
            view.exploreButton.SetToggleState(false);
            view.exploreV2Button.SetToggleState(false);
        };
        settingsPanelHud.OnClose += () =>
        {
            view.settingsButton.SetToggleState(false, false);
            MarkWorldChatAsReadIfOtherWindowIsOpen();
        };
    }

    public void AddExploreWindow(ExploreHUDController controller)
    {
        if (controller == null)
        {
            Debug.LogWarning("AddExploreWindow >>> Explore window doesn't exist yet!");
            return;
        }

        exploreHud = controller;
        view.OnAddExploreWindow();
        exploreHud.OnOpen += () =>
        {
            view.exploreButton.SetToggleState(true, false);
            view.settingsButton.SetToggleState(false);
        };
        exploreHud.OnClose += () =>
        {
            view.exploreButton.SetToggleState(false, false);
            MarkWorldChatAsReadIfOtherWindowIsOpen();
        };
    }

    private void ConfigureExploreV2Feature()
    {
        bool isExploreV2AlreadyInitialized = DataStore.i.exploreV2.isInitialized.Get();
        if (isExploreV2AlreadyInitialized)
            OnExploreV2ControllerInitialized(true, false);
        else
            DataStore.i.exploreV2.isInitialized.OnChange += OnExploreV2ControllerInitialized;
    }

    private void OnExploreV2ControllerInitialized(bool current, bool previous)
    {
        if (!current)
            return;

        DataStore.i.exploreV2.isInitialized.OnChange -= OnExploreV2ControllerInitialized;
        DataStore.i.exploreV2.isOpen.OnChange += OnExploreV2Open;
        view.OnAddExploreV2Window();
    }

    private void OnExploreV2Open(bool current, bool previous)
    {
        if (current)
        {
            view.exploreV2Button.SetToggleState(true, false);
            view.settingsButton.SetToggleState(false);
        }
        else
        {
            view.exploreV2Button.SetToggleState(false, false);
            MarkWorldChatAsReadIfOtherWindowIsOpen();
        }
    }

    public void AddHelpAndSupportWindow(HelpAndSupportHUDController controller)
    {
        if (controller == null || controller.view == null)
        {
            Debug.LogWarning("AddHelpAndSupportWindow >>> Help and Support window doesn't exist yet!");
            return;
        }

        helpAndSupportHud = controller;
        view.OnAddHelpAndSupportWindow();
        helpAndSupportHud.view.OnClose += () => { MarkWorldChatAsReadIfOtherWindowIsOpen(); };
    }

    public void OnAddVoiceChat() { view.OnAddVoiceChat(); }

    public void AddControlsMoreOption() { view.OnAddControlsMoreOption(); }

    public void DisableFriendsWindow()
    {
        view.friendsButton.gameObject.SetActive(false);
        view.chatHeadsGroup.ClearChatHeads();
    }

    private void OpenPrivateChatWindow(string userId)
    {
        privateChatWindowHud.Configure(userId);
        privateChatWindowHud.SetVisibility(true);
        privateChatWindowHud.ForceFocus();
        OnAnyTaskbarButtonClicked?.Invoke();
    }

    public void Dispose()
    {
        if (view != null)
        {
            view.chatHeadsGroup.OnHeadToggleOn -= ChatHeadsGroup_OnHeadOpen;
            view.chatHeadsGroup.OnHeadToggleOff -= ChatHeadsGroup_OnHeadClose;

            view.OnChatToggleOff -= View_OnChatToggleOff;
            view.OnChatToggleOn -= View_OnChatToggleOn;
            view.OnFriendsToggleOff -= View_OnFriendsToggleOff;
            view.OnFriendsToggleOn -= View_OnFriendsToggleOn;
            view.OnSettingsToggleOff -= View_OnSettingsToggleOff;
            view.OnSettingsToggleOn -= View_OnSettingsToggleOn;
            view.OnBuilderInWorldToggleOff -= View_OnBuilderInWorldToggleOff;
            view.OnBuilderInWorldToggleOn -= View_OnBuilderInWorldToggleOn;
            view.OnExploreToggleOff -= View_OnExploreToggleOff;
            view.OnExploreToggleOn -= View_OnExploreToggleOn;
            view.OnExploreV2ToggleOff -= View_OnExploreV2ToggleOff;
            view.OnExploreV2ToggleOn -= View_OnExploreV2ToggleOn;
            view.OnQuestPanelToggled -= View_OnQuestPanelToggled;

            CoroutineStarter.Stop(view.moreMenu.moreMenuAnimationsCoroutine);
            UnityEngine.Object.Destroy(view.gameObject);
        }

        if (mouseCatcher != null)
        {
            mouseCatcher.OnMouseLock -= MouseCatcher_OnMouseLock;
            mouseCatcher.OnMouseUnlock -= MouseCatcher_OnMouseUnlock;
        }

        if (toggleFriendsTrigger != null)
            toggleFriendsTrigger.OnTriggered -= ToggleFriendsTrigger_OnTriggered;

        if (closeWindowTrigger != null)
            closeWindowTrigger.OnTriggered -= CloseWindowTrigger_OnTriggered;

        if (toggleWorldChatTrigger != null)
            toggleWorldChatTrigger.OnTriggered -= ToggleWorldChatTrigger_OnTriggered;

        if (chatController != null)
            chatController.OnAddMessage -= OnAddMessage;

        if (sceneController != null)
        {
            sceneController.OnNewPortableExperienceSceneAdded -= SceneController_OnNewPortableExperienceSceneAdded;
            sceneController.OnNewPortableExperienceSceneRemoved -= SceneController_OnNewPortableExperienceSceneRemoved;
        }

        DataStore.i.HUDs.questsPanelVisible.OnChange -= OnToggleQuestsPanelTriggered;
        DataStore.i.HUDs.builderProjectsPanelVisible.OnChange -= OnBuilderProjectsPanelTriggered;
        DataStore.i.builderInWorld.showTaskBar.OnChange -= SetVisibility;
        DataStore.i.exploreV2.isInitialized.OnChange -= OnExploreV2ControllerInitialized;
        DataStore.i.exploreV2.isOpen.OnChange -= OnExploreV2Open;
    }

    public void SetVisibility(bool visible, bool previus) { SetVisibility(visible); }

    public void SetVisibility(bool visible) { view.SetVisibility(visible); }

    public void OnWorldChatToggleInputPress()
    {
        bool anyInputFieldIsSelected = EventSystem.current != null &&
                                       EventSystem.current.currentSelectedGameObject != null &&
                                       EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null;

        if (anyInputFieldIsSelected)
            return;

        worldChatWindowHud.OnPressReturn();

        if (AnyWindowsDifferentThanChatIsOpen())
        {
            foreach (var btn in view.GetButtonList())
            {
                btn.SetToggleState(btn == view.chatButton);
            }
        }
    }

    public void OnCloseWindowToggleInputPress()
    {
        if (mouseCatcher.isLocked)
            return;

        view.chatButton.SetToggleState(true);
        view.chatButton.SetToggleState(false, false);
        worldChatWindowHud.view.chatHudView.ResetInputField();
        worldChatWindowHud.view.ActivatePreview();
    }

    public void SetVoiceChatRecording(bool recording) { view?.voiceChatButton.SetOnRecording(recording); }

    public void SetVoiceChatEnabledByScene(bool enabled) { view?.voiceChatButton.SetEnabledByScene(enabled); }

    private void OnFriendsToggleInputPress()
    {
        bool anyInputFieldIsSelected = EventSystem.current != null &&
                                       EventSystem.current.currentSelectedGameObject != null &&
                                       EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null &&
                                       (!worldChatWindowHud.view.chatHudView.inputField.isFocused || !worldChatWindowHud.view.isInPreview);

        if (anyInputFieldIsSelected)
            return;

        Utils.UnlockCursor();
        view.leftWindowContainerAnimator.Show();
        view.friendsButton.SetToggleState(!view.friendsButton.toggledOn);
    }

    void OnAddMessage(ChatMessage message)
    {
        if (!AnyWindowsDifferentThanChatIsOpen() && message.messageType == ChatMessage.Type.PUBLIC)
            worldChatWindowHud.MarkWorldChatMessagesAsRead((long) message.timestamp);
    }

    private bool AnyWindowsDifferentThanChatIsOpen()
    {
        return (friendsHud != null && friendsHud.view.gameObject.activeSelf) ||
               (privateChatWindowHud != null && privateChatWindowHud.view.gameObject.activeSelf);
    }

    private void MarkWorldChatAsReadIfOtherWindowIsOpen()
    {
        if (!AnyWindowsDifferentThanChatIsOpen())
            worldChatWindowHud.MarkWorldChatMessagesAsRead();
    }

    public void ShowTutorialOption(bool isActive)
    {
        if (view != null && view.moreMenu != null)
            view.moreMenu.ShowTutorialButton(isActive);
    }

    private void SceneController_OnNewPortableExperienceSceneAdded(IParcelScene scene)
    {
        GlobalScene newPortableExperienceScene = scene as GlobalScene;

        if ( newPortableExperienceScene == null )
        {
            Debug.LogError("Portable experience must be of type GlobalScene!");
            return;
        }

        view.AddPortableExperienceElement(
            scene.sceneData.id,
            newPortableExperienceScene.sceneName,
            newPortableExperienceScene.iconUrl);
    }

    private void SceneController_OnNewPortableExperienceSceneRemoved(string portableExperienceSceneIdToRemove) { view.RemovePortableExperienceElement(portableExperienceSceneIdToRemove); }

    public void KillPortableExperience(string portableExperienceSceneIdToKill) { WebInterface.KillPortableExperience(portableExperienceSceneIdToKill); }

    private void OnBuilderProjectsPanelTriggered(bool isOn, bool prev)
    {
        if (isOn)
        {
            OnAnyTaskbarButtonClicked?.Invoke();
        }

        DataStore.i.HUDs.builderProjectsPanelVisible.Set(isOn);
        view.builderInWorldButton.SetToggleState(isOn, false);
    }
}