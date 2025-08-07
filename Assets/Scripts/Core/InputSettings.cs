using UnityEngine;

namespace KowloonBreak.Core
{
    public enum InputDevice
    {
        KeyboardMouse,
        Controller
    }
    
    public enum ControllerType
    {
        Xbox,
        PlayStation,
        Generic
    }
    
    [System.Serializable]
    public class InputBinding
    {
        [Header("Keyboard & Mouse")]
        public KeyCode keyboardKey = KeyCode.None;
        public int mouseButton = -1; // -1: None, 0: Left, 1: Right, 2: Middle
        
        [Header("Controller")]
        public KeyCode controllerButton = KeyCode.None;
        public string controllerAxisName = "";
        public float axisThreshold = 0.5f;
        
        [Header("Alternative Keys")]
        public KeyCode alternativeKey = KeyCode.None;
        
        public bool IsPressed(InputDevice device)
        {
            switch (device)
            {
                case InputDevice.KeyboardMouse:
                    if (keyboardKey != KeyCode.None && Input.GetKey(keyboardKey))
                        return true;
                    if (alternativeKey != KeyCode.None && Input.GetKey(alternativeKey))
                        return true;
                    if (mouseButton >= 0 && Input.GetMouseButton(mouseButton))
                        return true;
                    break;
                    
                case InputDevice.Controller:
                    if (controllerButton != KeyCode.None && Input.GetKey(controllerButton))
                        return true;
                    if (!string.IsNullOrEmpty(controllerAxisName))
                    {
                        float axisValue = Input.GetAxis(controllerAxisName);
                        if (Mathf.Abs(axisValue) >= axisThreshold)
                            return true;
                    }
                    break;
            }
            return false;
        }
        
        public bool IsDown(InputDevice device)
        {
            switch (device)
            {
                case InputDevice.KeyboardMouse:
                    if (keyboardKey != KeyCode.None && Input.GetKeyDown(keyboardKey))
                        return true;
                    if (alternativeKey != KeyCode.None && Input.GetKeyDown(alternativeKey))
                        return true;
                    if (mouseButton >= 0 && Input.GetMouseButtonDown(mouseButton))
                        return true;
                    break;
                    
                case InputDevice.Controller:
                    if (controllerButton != KeyCode.None && Input.GetKeyDown(controllerButton))
                        return true;
                    // コントローラーの軸入力はフレーム毎の変化で判定
                    if (!string.IsNullOrEmpty(controllerAxisName))
                    {
                        float axisValue = Input.GetAxis(controllerAxisName);
                        if (Mathf.Abs(axisValue) >= axisThreshold)
                            return true;
                    }
                    break;
            }
            return false;
        }
        
        public bool IsUp(InputDevice device)
        {
            switch (device)
            {
                case InputDevice.KeyboardMouse:
                    if (keyboardKey != KeyCode.None && Input.GetKeyUp(keyboardKey))
                        return true;
                    if (alternativeKey != KeyCode.None && Input.GetKeyUp(alternativeKey))
                        return true;
                    if (mouseButton >= 0 && Input.GetMouseButtonUp(mouseButton))
                        return true;
                    break;
                    
                case InputDevice.Controller:
                    if (controllerButton != KeyCode.None && Input.GetKeyUp(controllerButton))
                        return true;
                    break;
            }
            return false;
        }
    }
    
    [System.Serializable]
    public class AxisBinding
    {
        [Header("Keyboard & Mouse")]
        public string keyboardAxisName = "";
        public KeyCode positiveKey = KeyCode.None;
        public KeyCode negativeKey = KeyCode.None;
        
        [Header("Controller")]
        public string controllerAxisName = "";
        public bool invertController = false;
        
        public float GetAxis(InputDevice device)
        {
            switch (device)
            {
                case InputDevice.KeyboardMouse:
                    if (!string.IsNullOrEmpty(keyboardAxisName))
                    {
                        return Input.GetAxis(keyboardAxisName);
                    }
                    else if (positiveKey != KeyCode.None || negativeKey != KeyCode.None)
                    {
                        float value = 0f;
                        if (positiveKey != KeyCode.None && Input.GetKey(positiveKey))
                            value += 1f;
                        if (negativeKey != KeyCode.None && Input.GetKey(negativeKey))
                            value -= 1f;
                        return value;
                    }
                    break;
                    
                case InputDevice.Controller:
                    if (!string.IsNullOrEmpty(controllerAxisName))
                    {
                        float value = Input.GetAxis(controllerAxisName);
                        return invertController ? -value : value;
                    }
                    break;
            }
            return 0f;
        }
        
        public float GetAxisRaw(InputDevice device)
        {
            switch (device)
            {
                case InputDevice.KeyboardMouse:
                    if (!string.IsNullOrEmpty(keyboardAxisName))
                    {
                        return Input.GetAxisRaw(keyboardAxisName);
                    }
                    else if (positiveKey != KeyCode.None || negativeKey != KeyCode.None)
                    {
                        float value = 0f;
                        if (positiveKey != KeyCode.None && Input.GetKey(positiveKey))
                            value += 1f;
                        if (negativeKey != KeyCode.None && Input.GetKey(negativeKey))
                            value -= 1f;
                        return value;
                    }
                    break;
                    
                case InputDevice.Controller:
                    if (!string.IsNullOrEmpty(controllerAxisName))
                    {
                        float value = Input.GetAxisRaw(controllerAxisName);
                        return invertController ? -value : value;
                    }
                    break;
            }
            return 0f;
        }
    }
    
    [CreateAssetMenu(fileName = "InputSettings", menuName = "KowloonBreak/Input Settings")]
    public class InputSettings : ScriptableObject
    {
        [Header("Device Settings")]
        public InputDevice preferredDevice = InputDevice.KeyboardMouse;
        public ControllerType controllerType = ControllerType.Xbox;
        public bool autoDetectDevice = true;
        public float deviceSwitchDelay = 0.5f;
        
        [Header("Movement")]
        public AxisBinding horizontalAxis = new AxisBinding
        {
            keyboardAxisName = "Horizontal",
            controllerAxisName = "Horizontal"
        };
        
        public AxisBinding verticalAxis = new AxisBinding
        {
            keyboardAxisName = "Vertical", 
            controllerAxisName = "Vertical"
        };
        
        [Header("Player Actions")]
        public InputBinding interactionInput = new InputBinding
        {
            keyboardKey = KeyCode.R,
            controllerButton = KeyCode.Joystick1Button3 // Xbox: Y
        };
        
        public InputBinding useToolInput = new InputBinding
        {
            keyboardKey = KeyCode.F,
            controllerButton = KeyCode.Joystick1Button2 // Xbox: X
        };
        
        public InputBinding runInput = new InputBinding
        {
            keyboardKey = KeyCode.LeftShift,
            alternativeKey = KeyCode.RightShift,
            controllerButton = KeyCode.Joystick1Button0 // Xbox: A (長押し)
        };
        
        public InputBinding crouchInput = new InputBinding
        {
            keyboardKey = KeyCode.LeftControl,
            alternativeKey = KeyCode.C,
            controllerButton = KeyCode.Joystick1Button8 // Xbox: Left Stick Click
        };
        
        public InputBinding dodgeInput = new InputBinding
        {
            keyboardKey = KeyCode.Space,
            controllerButton = KeyCode.Joystick1Button1 // Xbox: B
        };
        
        [Header("Tool Selection")]
        public InputBinding toolPreviousInput = new InputBinding
        {
            keyboardKey = KeyCode.Q,
            controllerButton = KeyCode.JoystickButton4 // Xbox: LB
        };
        
        public InputBinding toolNextInput = new InputBinding
        {
            keyboardKey = KeyCode.E,
            controllerButton = KeyCode.JoystickButton5 // Xbox: RB
        };
        
        [Header("Legacy Tool Selection (1-8 Keys)")]
        public InputBinding[] toolSelectionInputs = new InputBinding[8]
        {
            new InputBinding { keyboardKey = KeyCode.Alpha1 },
            new InputBinding { keyboardKey = KeyCode.Alpha2 },
            new InputBinding { keyboardKey = KeyCode.Alpha3 },
            new InputBinding { keyboardKey = KeyCode.Alpha4 },
            new InputBinding { keyboardKey = KeyCode.Alpha5 },
            new InputBinding { keyboardKey = KeyCode.Alpha6 },
            new InputBinding { keyboardKey = KeyCode.Alpha7 },
            new InputBinding { keyboardKey = KeyCode.Alpha8 }
        };
        
        [Header("UI Navigation")]
        public InputBinding menuInput = new InputBinding
        {
            keyboardKey = KeyCode.Escape,
            controllerButton = KeyCode.Joystick1Button7 // Xbox: Menu
        };
        
        public InputBinding inventoryInput = new InputBinding
        {
            keyboardKey = KeyCode.Tab,
            controllerButton = KeyCode.Joystick1Button6 // Xbox: View
        };
        
        public InputBinding companionCommandInput = new InputBinding
        {
            keyboardKey = KeyCode.T,
            alternativeKey = KeyCode.Return,
            controllerButton = KeyCode.Joystick1Button3 // Xbox: Y (alternative to interaction)
        };
        
        [Header("Sensitivity")]
        [Range(0.1f, 3.0f)]
        public float mouseSensitivity = 1.0f;
        [Range(0.1f, 3.0f)]
        public float controllerSensitivity = 1.5f;
        
        [Header("Dead Zones")]
        [Range(0.01f, 0.5f)]
        public float leftStickDeadZone = 0.1f;
        [Range(0.01f, 0.5f)]
        public float rightStickDeadZone = 0.1f;
        [Range(0.01f, 1.0f)]
        public float triggerDeadZone = 0.1f;
    }
}