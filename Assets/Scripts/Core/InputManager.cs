using UnityEngine;
using System;
using System.Collections.Generic;

namespace KowloonBreak.Core
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        
        [Header("Settings")]
        [SerializeField] private InputSettings inputSettings;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private InputDevice currentDevice;
        private float lastInputTime;
        private Dictionary<string, bool> buttonStates = new Dictionary<string, bool>();
        private Dictionary<string, bool> previousButtonStates = new Dictionary<string, bool>();
        
        // イベント
        public event Action<InputDevice> OnDeviceChanged;
        
        // 入力判定用のプロパティ
        public InputDevice CurrentDevice => currentDevice;
        public InputSettings Settings => inputSettings;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                DontDestroyOnLoad(gameObject);
                
                InitializeInputManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeInputManager()
        {
            if (inputSettings == null)
            {
                Debug.LogError("[InputManager] InputSettings is not assigned!");
                return;
            }
            
            currentDevice = inputSettings.preferredDevice;
            
            // ボタン状態の初期化
            InitializeButtonStates();
            
            Debug.Log($"[InputManager] Initialized with device: {currentDevice}");
        }
        
        private void InitializeButtonStates()
        {
            string[] actions = { "interaction", "useTool", "run", "crouch", "dodge", "menu", "inventory", "toolPrevious", "toolNext", "companionCommand" };
            
            foreach (string action in actions)
            {
                buttonStates[action] = false;
                previousButtonStates[action] = false;
            }
            
            for (int i = 0; i < 8; i++)
            {
                string toolAction = $"tool{i}";
                buttonStates[toolAction] = false;
                previousButtonStates[toolAction] = false;
            }
        }
        
        private void Update()
        {
            if (inputSettings == null) return;
            
            // デバイス自動検出
            if (inputSettings.autoDetectDevice)
            {
                DetectActiveDevice();
            }
            
            // ボタン状態の更新
            UpdateButtonStates();
            
            // デバッグ情報の表示
            if (showDebugInfo)
            {
                ShowDebugInfo();
            }
        }
        
        private void DetectActiveDevice()
        {
            // キーボード・マウス入力をチェック
            if (Input.inputString != "" || Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
            {
                SwitchDevice(InputDevice.KeyboardMouse);
            }
            // コントローラー入力をチェック
            else if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || 
                     Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f ||
                     Input.GetKey(KeyCode.Joystick1Button0) ||
                     Input.GetKey(KeyCode.Joystick1Button1))
            {
                SwitchDevice(InputDevice.Controller);
            }
        }
        
        private void SwitchDevice(InputDevice newDevice)
        {
            if (currentDevice != newDevice && Time.time - lastInputTime > inputSettings.deviceSwitchDelay)
            {
                currentDevice = newDevice;
                lastInputTime = Time.time;
                OnDeviceChanged?.Invoke(currentDevice);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[InputManager] Switched to device: {currentDevice}");
                }
            }
        }
        
        private void UpdateButtonStates()
        {
            // 前フレームの状態を保存
            foreach (var key in buttonStates.Keys)
            {
                previousButtonStates[key] = buttonStates[key];
            }
            
            // 現在の状態を更新
            buttonStates["interaction"] = inputSettings.interactionInput.IsPressed(currentDevice);
            buttonStates["useTool"] = inputSettings.useToolInput.IsPressed(currentDevice);
            buttonStates["run"] = inputSettings.runInput.IsPressed(currentDevice);
            buttonStates["crouch"] = inputSettings.crouchInput.IsPressed(currentDevice);
            buttonStates["dodge"] = inputSettings.dodgeInput.IsPressed(currentDevice);
            buttonStates["menu"] = inputSettings.menuInput.IsPressed(currentDevice);
            buttonStates["inventory"] = inputSettings.inventoryInput.IsPressed(currentDevice);
            buttonStates["toolPrevious"] = inputSettings.toolPreviousInput.IsPressed(currentDevice);
            buttonStates["toolNext"] = inputSettings.toolNextInput.IsPressed(currentDevice);
            buttonStates["companionCommand"] = inputSettings.companionCommandInput.IsPressed(currentDevice);
            
            // ツール選択
            for (int i = 0; i < 8; i++)
            {
                if (i < inputSettings.toolSelectionInputs.Length)
                {
                    buttonStates[$"tool{i}"] = inputSettings.toolSelectionInputs[i].IsPressed(currentDevice);
                }
            }
        }
        
        // 公開メソッド - ボタン入力
        public bool GetButton(string action)
        {
            return buttonStates.ContainsKey(action) && buttonStates[action];
        }
        
        public bool GetButtonDown(string action)
        {
            return buttonStates.ContainsKey(action) && 
                   buttonStates[action] && 
                   !previousButtonStates[action];
        }
        
        public bool GetButtonUp(string action)
        {
            return buttonStates.ContainsKey(action) && 
                   !buttonStates[action] && 
                   previousButtonStates[action];
        }
        
        // 公開メソッド - 軸入力
        public float GetAxis(string axisName)
        {
            switch (axisName.ToLower())
            {
                case "horizontal":
                    return ApplyDeadZone(inputSettings.horizontalAxis.GetAxis(currentDevice), inputSettings.leftStickDeadZone);
                case "vertical":
                    return ApplyDeadZone(inputSettings.verticalAxis.GetAxis(currentDevice), inputSettings.leftStickDeadZone);
                default:
                    return Input.GetAxis(axisName);
            }
        }
        
        public float GetAxisRaw(string axisName)
        {
            switch (axisName.ToLower())
            {
                case "horizontal":
                    return ApplyDeadZone(inputSettings.horizontalAxis.GetAxisRaw(currentDevice), inputSettings.leftStickDeadZone);
                case "vertical":
                    return ApplyDeadZone(inputSettings.verticalAxis.GetAxisRaw(currentDevice), inputSettings.leftStickDeadZone);
                default:
                    return Input.GetAxisRaw(axisName);
            }
        }
        
        private float ApplyDeadZone(float value, float deadZone)
        {
            if (Mathf.Abs(value) < deadZone)
                return 0f;
            
            // デッドゾーンを考慮して正規化
            float sign = Mathf.Sign(value);
            float normalizedValue = (Mathf.Abs(value) - deadZone) / (1f - deadZone);
            return sign * normalizedValue;
        }
        
        // 便利メソッド
        public bool IsInteractionPressed() => GetButtonDown("interaction");
        public bool IsUseToolPressed() => GetButtonDown("useTool");
        public bool IsRunPressed() => GetButton("run");
        public bool IsRunDown() => GetButtonDown("run");
        public bool IsRunUp() => GetButtonUp("run");
        public bool IsCrouchPressed() => GetButton("crouch");
        public bool IsCrouchDown() => GetButtonDown("crouch");
        public bool IsCrouchUp() => GetButtonUp("crouch");
        public bool IsDodgePressed() => GetButtonDown("dodge");
        public bool IsMenuPressed() => GetButtonDown("menu");
        public bool IsInventoryPressed() => GetButtonDown("inventory");
        public bool IsCompanionCommandPressed() 
        {
            // InputBinding.IsDown()を直接呼び出す（より確実）
            return inputSettings != null && inputSettings.companionCommandInput.IsDown(currentDevice);
        }
        
        // Tool switching methods
        public bool IsToolPreviousPressed() => GetButtonDown("toolPrevious");
        public bool IsToolNextPressed() => GetButtonDown("toolNext");
        
        public int GetToolSelectionInput()
        {
            for (int i = 0; i < 8; i++)
            {
                if (GetButtonDown($"tool{i}"))
                {
                    return i;
                }
            }
            return -1;
        }
        
        public Vector2 GetMovementInput()
        {
            return new Vector2(GetAxis("Horizontal"), GetAxis("Vertical"));
        }
        
        public Vector2 GetMovementInputRaw()
        {
            return new Vector2(GetAxisRaw("Horizontal"), GetAxisRaw("Vertical"));
        }
        
        // 設定変更
        public void SetInputSettings(InputSettings newSettings)
        {
            inputSettings = newSettings;
            InitializeButtonStates();
        }
        
        public void SetPreferredDevice(InputDevice device)
        {
            currentDevice = device;
            if (inputSettings != null)
            {
                inputSettings.preferredDevice = device;
            }
        }
        
        private void ShowDebugInfo()
        {
            // デバッグ情報をコンソールに表示（必要に応じて）
        }
        
        private void OnGUI()
        {
            if (!showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label($"Current Device: {currentDevice}");
            GUILayout.Label($"Movement: {GetMovementInput()}");
            
            GUILayout.Label("Button States:");
            foreach (var state in buttonStates)
            {
                if (state.Value)
                {
                    GUILayout.Label($"  {state.Key}: {state.Value}");
                }
            }
            
            GUILayout.EndArea();
        }
    }
}