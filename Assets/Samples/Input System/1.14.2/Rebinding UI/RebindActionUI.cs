using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using static UnityEngine.InputSystem.InputActionRebindingExtensions;

////TODO: localization support
////TODO: deal with composites that have parts bound in different control schemes

namespace UnityEngine.InputSystem.Samples.RebindUI
{
    /// <summary>
    /// Componente reutilizável com UI auto-contida para rebinding de uma única action.
    /// Versão adaptada para usar TextMeshPro em vez de Text legacy.
    /// </summary>
    public class RebindActionUI : MonoBehaviour
    {
        public InputActionReference actionReference
        {
            get => m_Action;
            set
            {
                m_Action = value;
                UpdateActionLabel();
                UpdateBindingDisplay();
            }
        }

        public string bindingId
        {
            get => m_BindingId;
            set
            {
                m_BindingId = value;
                UpdateBindingDisplay();
            }
        }

        public InputBinding.DisplayStringOptions displayStringOptions
        {
            get => m_DisplayStringOptions;
            set
            {
                m_DisplayStringOptions = value;
                UpdateBindingDisplay();
            }
        }

        public TextMeshProUGUI actionLabel
        {
            get => m_ActionLabel;
            set
            {
                m_ActionLabel = value;
                UpdateActionLabel();
            }
        }

        public TextMeshProUGUI bindingText
        {
            get => m_BindingText;
            set
            {
                m_BindingText = value;
                UpdateBindingDisplay();
            }
        }

        public TextMeshProUGUI rebindPrompt
        {
            get => m_RebindText;
            set => m_RebindText = value;
        }

        public GameObject rebindOverlay
        {
            get => m_RebindOverlay;
            set => m_RebindOverlay = value;
        }

        public UpdateBindingUIEvent updateBindingUIEvent
        {
            get
            {
                if (m_UpdateBindingUIEvent == null)
                    m_UpdateBindingUIEvent = new UpdateBindingUIEvent();
                return m_UpdateBindingUIEvent;
            }
        }

        public InteractiveRebindEvent startRebindEvent
        {
            get
            {
                if (m_RebindStartEvent == null)
                    m_RebindStartEvent = new InteractiveRebindEvent();
                return m_RebindStartEvent;
            }
        }

        public InteractiveRebindEvent stopRebindEvent
        {
            get
            {
                if (m_RebindStopEvent == null)
                    m_RebindStopEvent = new InteractiveRebindEvent();
                return m_RebindStopEvent;
            }
        }

        public RebindingOperation ongoingRebind => m_RebindOperation;

        public bool ResolveActionAndBinding(out InputAction action, out int bindingIndex)
        {
            bindingIndex = -1;

            action = m_Action?.action;
            if (action == null)
                return false;

            if (string.IsNullOrEmpty(m_BindingId))
                return false;

            var guid = new Guid(m_BindingId);
            bindingIndex = action.bindings.IndexOf(x => x.id == guid);
            if (bindingIndex == -1)
            {
                Debug.LogError($"Cannot find binding with ID '{guid}' on '{action}'", this);
                return false;
            }

            return true;
        }

        public void UpdateBindingDisplay()
        {
            var displayString = string.Empty;
            var deviceLayoutName = default(string);
            var controlPath = default(string);

            var action = m_Action?.action;
            if (action != null)
            {
                var index = action.bindings.IndexOf(x => x.id.ToString() == m_BindingId);
                if (index != -1)
                    displayString = action.GetBindingDisplayString(
                        index,
                        out deviceLayoutName,
                        out controlPath,
                        displayStringOptions);
            }

            if (m_BindingText != null)
                m_BindingText.text = displayString;

            m_UpdateBindingUIEvent?.Invoke(this, displayString, deviceLayoutName, controlPath);
        }

        public void ResetToDefault()
        {
            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                for (var i = bindingIndex + 1;
                     i < action.bindings.Count && action.bindings[i].isPartOfComposite;
                     ++i)
                {
                    action.RemoveBindingOverride(i);
                }
            }
            else
            {
                action.RemoveBindingOverride(bindingIndex);
            }

            UpdateBindingDisplay();

            // Feedback simples quando volta ao default
            if (m_RebindText != null)
            {
                var actionName = m_Action?.action != null ? m_Action.action.name : "Action";
                m_RebindText.text = $"{actionName}: tecla reposta para o valor padrão.";
            }
        }

        private void Start()
        {
            LoadActionBinding();
        }

        private void SaveActionBinding()
        {
            var map = actionReference.action.actionMap;
            var currentBindings = map.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString($"Bindings_{map.name}", currentBindings);
            PlayerPrefs.Save();
        }

        private void LoadActionBinding()
        {
            var map = actionReference.action.actionMap;
            var savedBindings = PlayerPrefs.GetString($"Bindings_{map.name}", string.Empty);
            if (!string.IsNullOrEmpty(savedBindings))
                map.LoadBindingOverridesFromJson(savedBindings);
        }

        public void StartInteractiveRebind()
        {
            m_Action.action.Disable();

            if (!ResolveActionAndBinding(out var action, out var bindingIndex))
                return;

            if (action.bindings[bindingIndex].isComposite)
            {
                var firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count &&
                    action.bindings[firstPartIndex].isPartOfComposite)
                {
                    PerformInteractiveRebind(action, firstPartIndex, allCompositeParts: true);
                }
            }
            else
            {
                PerformInteractiveRebind(action, bindingIndex);
            }
        }

        private void PerformInteractiveRebind(InputAction action, int bindingIndex, bool allCompositeParts = false)
        {
            action.Disable();
            m_RebindOperation?.Cancel();

            void CleanUp(bool canceled)
            {
                m_RebindOperation?.Dispose();
                m_RebindOperation = null;
                m_Action.action.Enable();
                action.actionMap.Enable();
                m_UIInputActionMap?.Enable();

                if (!canceled)
                    SaveActionBinding();

                // Feedback final
                if (m_RebindText != null)
                {
                    var actionName = m_Action?.action != null ? m_Action.action.name : "Action";
                    if (canceled)
                    {
                        m_RebindText.text = $"{actionName}: change canceled, previous key retained.";
                    }
                    else
                    {
                        var displayString = m_BindingText != null ? m_BindingText.text : string.Empty;
                        m_RebindText.text = string.IsNullOrEmpty(displayString)
                            ? $"{actionName}: new key defined."
                            : $"{actionName}: now use [{displayString}].";
                    }
                }
            }

            action.actionMap.Disable();
            m_UIInputActionMap?.Disable();

            m_RebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                .OnCancel(operation =>
                {
                    m_RebindStopEvent?.Invoke(this, operation);
                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);
                    UpdateBindingDisplay();
                    CleanUp(canceled: true);
                })
                .OnComplete(operation =>
                {
                    if (m_RebindOverlay != null)
                        m_RebindOverlay.SetActive(false);
                    m_RebindStopEvent?.Invoke(this, operation);
                    UpdateBindingDisplay();
                    CleanUp(canceled: false);

                    if (allCompositeParts)
                    {
                        var nextBindingIndex = bindingIndex + 1;
                        if (nextBindingIndex < action.bindings.Count &&
                            action.bindings[nextBindingIndex].isPartOfComposite)
                        {
                            PerformInteractiveRebind(action, nextBindingIndex, true);
                        }
                    }
                });

            var partName = default(string);
            if (action.bindings[bindingIndex].isPartOfComposite)
                partName = $"Parte '{action.bindings[bindingIndex].name}'. ";

            m_RebindOverlay?.SetActive(true);
            if (m_RebindText != null)
            {
                var actionName = m_Action?.action != null ? m_Action.action.name : "esta ação";
                var expectedType = m_RebindOperation.expectedControlType;

                if (!string.IsNullOrEmpty(expectedType))
                {
                    m_RebindText.text =
                        $"{partName}Changing control of [{actionName}].\n" +
                        $"Press a key or button of type {expectedType}...\n" +
                        "Press ESC to cancel.";
                }
                else
                {
                    m_RebindText.text =
                        $"{partName}Changing control of [{actionName}].\n" +
                        "Press any key or button...\n" +
                        "Press ESC to cancel.";
                }
            }

            if (m_RebindOverlay == null &&
                m_RebindText == null &&
                m_RebindStartEvent == null &&
                m_BindingText != null)
            {
                m_BindingText.text = "<A aguardar novo input...>";
            }

            m_RebindStartEvent?.Invoke(this, m_RebindOperation);
            m_RebindOperation.Start();
        }

        protected void OnEnable()
        {
            if (s_RebindActionUIs == null)
                s_RebindActionUIs = new List<RebindActionUI>();
            s_RebindActionUIs.Add(this);
            if (s_RebindActionUIs.Count == 1)
                InputSystem.onActionChange += OnActionChange;
            if (m_DefaultInputActions != null && m_UIInputActionMap == null)
                m_UIInputActionMap = m_DefaultInputActions.FindActionMap("UI");
        }

        protected void OnDisable()
        {
            m_RebindOperation?.Dispose();
            m_RebindOperation = null;

            if (s_RebindActionUIs != null)
            {
                s_RebindActionUIs.Remove(this);
                if (s_RebindActionUIs.Count == 0)
                {
                    s_RebindActionUIs = null;
                    InputSystem.onActionChange -= OnActionChange;
                }
            }
        }

        private static void OnActionChange(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged)
                return;

            var action = obj as InputAction;
            var actionMap = action?.actionMap ?? obj as InputActionMap;
            var actionAsset = actionMap?.asset ?? obj as InputActionAsset;

            if (s_RebindActionUIs == null)
                return;

            for (var i = 0; i < s_RebindActionUIs.Count; ++i)
            {
                var component = s_RebindActionUIs[i];
                var referencedAction = component.actionReference?.action;
                if (referencedAction == null)
                    continue;

                if (referencedAction == action ||
                    referencedAction.actionMap == actionMap ||
                    referencedAction.actionMap?.asset == actionAsset)
                    component.UpdateBindingDisplay();
            }
        }

        [Tooltip("Reference to action that is to be rebound from the UI.")]
        [SerializeField]
        private InputActionReference m_Action;

        [SerializeField]
        private string m_BindingId;

        [SerializeField]
        private InputBinding.DisplayStringOptions m_DisplayStringOptions;

        [SerializeField]
        private TextMeshProUGUI m_ActionLabel;

        [SerializeField]
        private TextMeshProUGUI m_BindingText;

        [SerializeField]
        private GameObject m_RebindOverlay;

        [SerializeField]
        private TextMeshProUGUI m_RebindText;

        [SerializeField]
        private InputActionAsset m_DefaultInputActions;
        private InputActionMap m_UIInputActionMap;

        [SerializeField]
        private UpdateBindingUIEvent m_UpdateBindingUIEvent;

        [SerializeField]
        private InteractiveRebindEvent m_RebindStartEvent;

        [SerializeField]
        private InteractiveRebindEvent m_RebindStopEvent;

        private RebindingOperation m_RebindOperation;

        private static List<RebindActionUI> s_RebindActionUIs;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateActionLabel();
            UpdateBindingDisplay();
        }
#endif

        private void UpdateActionLabel()
        {
            if (m_ActionLabel != null)
            {
                var action = m_Action?.action;
                m_ActionLabel.text = action != null ? action.name : string.Empty;
            }
        }

        [Serializable]
        public class UpdateBindingUIEvent : UnityEvent<RebindActionUI, string, string, string>
        {
        }

        [Serializable]
        public class InteractiveRebindEvent : UnityEvent<RebindActionUI, RebindingOperation>
        {
        }
    }
}