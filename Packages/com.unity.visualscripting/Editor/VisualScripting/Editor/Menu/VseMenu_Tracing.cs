using System;
using System.Diagnostics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    partial class VseMenu
    {
        public event Action<ChangeEvent<bool>> OnToggleTracing;

        const int k_UpdateIntervalMs = 500;

        ToolbarToggle m_EnableTracingButton;
        VisualElement m_FirstFrameTracingButton;
        VisualElement m_PreviousStepTracingButton;
        VisualElement m_PreviousFrameTracingButton;
        VisualElement m_NextStepTracingButton;
        VisualElement m_NextFrameTracingButton;
        VisualElement m_LastFrameTracingButton;
        TextField m_CurrentFrameTextField;
        Label m_TotalFrameLabel;
        Stopwatch m_LastUpdate = Stopwatch.StartNew();

        [Conditional("VS_ENABLE_TRACING")]
        void CreateTracingMenu()
        {
            m_EnableTracingButton = this.MandatoryQ<ToolbarToggle>("enableTracingButton");
            m_EnableTracingButton.tooltip = "Toggle Tracing For Current Instance";
            if (OnToggleTracing != null)
                m_EnableTracingButton.RegisterValueChangedCallback(OnToggleTracing.Invoke);

            m_FirstFrameTracingButton = this.Q("firstFrameTracingButton");
            m_FirstFrameTracingButton.tooltip = "Go To First Frame";
            m_FirstFrameTracingButton.AddManipulator(new Clickable(OnFirstFrameTracingButton));

            m_PreviousFrameTracingButton = this.Q("previousFrameTracingButton");
            m_PreviousFrameTracingButton.tooltip = "Go To Previous Frame";
            m_PreviousFrameTracingButton.AddManipulator(new Clickable(OnPreviousFrameTracingButton));

            m_PreviousStepTracingButton = this.Q("previousStepTracingButton");
            m_PreviousStepTracingButton.tooltip = "Go To Previous Step";
            m_PreviousStepTracingButton.AddManipulator(new Clickable(OnPreviousStepTracingButton));

            m_NextStepTracingButton = this.Q("nextStepTracingButton");
            m_NextStepTracingButton.tooltip = "Go To Next Step";
            m_NextStepTracingButton.AddManipulator(new Clickable(OnNextStepTracingButton));

            m_NextFrameTracingButton = this.Q("nextFrameTracingButton");
            m_NextFrameTracingButton.tooltip = "Go To Next Frame";
            m_NextFrameTracingButton.AddManipulator(new Clickable(OnNextFrameTracingButton));

            m_LastFrameTracingButton = this.Q("lastFrameTracingButton");
            m_LastFrameTracingButton.tooltip = "Go To Last Frame";
            m_LastFrameTracingButton.AddManipulator(new Clickable(OnLastFrameTracingButton));

            m_CurrentFrameTextField = this.Q<TextField>("currentFrameTextField");
            m_CurrentFrameTextField.AddToClassList("frameCounterLabel");
            m_CurrentFrameTextField.RegisterCallback<InputEvent>(OnFrameCounterInput);
            m_CurrentFrameTextField.RegisterCallback<KeyDownEvent>(OnFrameCounterKeyDown);
            m_TotalFrameLabel = this.Q<Label>("totalFrameLabel");
            m_TotalFrameLabel.AddToClassList("frameCounterLabel");
        }

        void OnFrameCounterKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                if (!int.TryParse(m_CurrentFrameTextField.value, out var frame))
                    frame = 0;

                frame = Math.Max(0, Math.Min(frame, Time.frameCount));
                m_CurrentFrameTextField.value = frame.ToString();
                m_Store.GetState().currentTracingFrame = frame;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnFrameCounterInput(InputEvent evt)
        {
            m_CurrentFrameTextField.value = uint.TryParse(evt.newData, out _) || string.IsNullOrEmpty(evt.newData)
                ? evt.newData
                : evt.previousData;
        }

        [Conditional("VS_ENABLE_TRACING")]
        void UpdateTracingMenu(bool force = true)
        {
            if (EditorApplication.isPlaying)
            {
                if (EditorApplication.isPaused)
                {
                    m_FirstFrameTracingButton.SetEnabled(true);
                    m_PreviousFrameTracingButton.SetEnabled(true);
                    m_PreviousStepTracingButton.SetEnabled(true);
                    m_NextStepTracingButton.SetEnabled(true);
                    m_NextFrameTracingButton.SetEnabled(true);
                    m_LastFrameTracingButton.SetEnabled(true);
                    m_CurrentFrameTextField.SetEnabled(true);
                    m_TotalFrameLabel.SetEnabled(true);
                }
                else
                {
                    m_Store.GetState().currentTracingFrame = Time.frameCount;
                    m_Store.GetState().currentTracingStep = -1;
                    m_FirstFrameTracingButton.SetEnabled(false);
                    m_PreviousFrameTracingButton.SetEnabled(false);
                    m_PreviousStepTracingButton.SetEnabled(false);
                    m_NextStepTracingButton.SetEnabled(false);
                    m_NextFrameTracingButton.SetEnabled(false);
                    m_LastFrameTracingButton.SetEnabled(false);
                    m_CurrentFrameTextField.SetEnabled(false);
                    m_TotalFrameLabel.SetEnabled(false);
                }

                if(!m_LastUpdate.IsRunning)
                    m_LastUpdate.Start();
                if (force || EditorApplication.isPaused || m_LastUpdate.ElapsedMilliseconds > k_UpdateIntervalMs)
                {
                    m_CurrentFrameTextField.value = m_Store.GetState().currentTracingFrame.ToString();
                    m_TotalFrameLabel.text = $"/{Time.frameCount.ToString()}";
                    if (m_Store.GetState().currentTracingStep != -1)
                    {
                        m_TotalFrameLabel.text += $" [{m_Store.GetState().currentTracingStep}/{m_Store.GetState().maxTracingStep}]";
                    }

                    m_LastUpdate.Restart();
                }
            }
            else
            {
                m_LastUpdate.Stop();
                m_Store.GetState().currentTracingFrame = Time.frameCount;
                m_Store.GetState().currentTracingStep = -1;
                m_CurrentFrameTextField.value = "0";
                m_CurrentFrameTextField.SetEnabled(false);
                m_TotalFrameLabel.text = "/0";
                m_TotalFrameLabel.SetEnabled(false);
                m_FirstFrameTracingButton.SetEnabled(false);
                m_PreviousFrameTracingButton.SetEnabled(false);
                m_PreviousStepTracingButton.SetEnabled(false);
                m_NextStepTracingButton.SetEnabled(false);
                m_NextFrameTracingButton.SetEnabled(false);
                m_LastFrameTracingButton.SetEnabled(false);
            }
        }

        void OnFirstFrameTracingButton()
        {
            m_Store.GetState().currentTracingFrame = 0;
            m_Store.GetState().currentTracingStep = -1;
            UpdateTracingMenu();
        }

        void OnPreviousFrameTracingButton()
        {
            if (m_Store.GetState().currentTracingFrame > 0)
            {
                m_Store.GetState().currentTracingFrame--;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnPreviousStepTracingButton()
        {
            if (m_Store.GetState().currentTracingStep > 0)
            {
                m_Store.GetState().currentTracingStep--;
            }
            else
            {
                if (m_Store.GetState().currentTracingStep == -1)
                {
                    m_Store.GetState().currentTracingStep = m_Store.GetState().maxTracingStep;
                }
                else
                {
                    if (m_Store.GetState().currentTracingFrame > 0)
                    {
                        m_Store.GetState().currentTracingFrame--;
                        m_Store.GetState().currentTracingStep = m_Store.GetState().maxTracingStep;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextStepTracingButton()
        {
            if (m_Store.GetState().currentTracingStep < m_Store.GetState().maxTracingStep && m_Store.GetState().currentTracingStep >= 0)
            {
                m_Store.GetState().currentTracingStep++;
            }
            else
            {
                if (m_Store.GetState().currentTracingStep == -1 && (m_Store.GetState().currentTracingFrame < Time.frameCount))
                {
                    m_Store.GetState().currentTracingStep = 0;
                }
                else
                {
                    if (m_Store.GetState().currentTracingFrame < Time.frameCount)
                    {
                        m_Store.GetState().currentTracingFrame++;
                        m_Store.GetState().currentTracingStep = 0;
                    }
                }
            }

            UpdateTracingMenu();
        }

        void OnNextFrameTracingButton()
        {
            if (m_Store.GetState().currentTracingFrame < Time.frameCount)
            {
                m_Store.GetState().currentTracingFrame++;
                m_Store.GetState().currentTracingStep = -1;
                UpdateTracingMenu();
            }
        }

        void OnLastFrameTracingButton()
        {
            m_Store.GetState().currentTracingFrame = Time.frameCount;
            m_Store.GetState().currentTracingStep = -1;
            UpdateTracingMenu();
        }
    }
}
