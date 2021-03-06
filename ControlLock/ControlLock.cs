﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using KSP.UI.Screens;
using System.Collections;

namespace ControlLock
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ControlLock : MonoBehaviour
    {

        private static List<KeyBindString> allKeys; //make list of all keybinds manually, 3 hours spent on trying to use .GetFields didn't work
        private static List<string> lockedMods; //which mods have an active lock?
        private static List<KeyBind> unboundKeys; //which keys have we unbound?
        private static bool lockIsSet; //we have set the lock
        private static IButton CLBtn; //blizzy's toolbar button
        static ApplicationLauncherButton CLButton = null; //stock toolbar icon
        private static bool buttonCreated; //have we created our toolbar button, only for stock
        static Texture buttonGray = new Texture2D(64, 64);
        static Texture buttonRed = new Texture2D(64, 64);
        static Texture buttonYellow = new Texture2D(64, 64);
        private ConfigNode settings;
        private static bool showKeysWindow;
        private KeyCode clKey;
        private KeyCode clModKey;
        private bool setClKey;
        private bool setClModKey;
        
        
        public void Start()
        {
            buttonGray = (Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButton", false);
            buttonRed = (Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButtonRed", false);
            buttonYellow = (Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButtonYellow", false);
            buttonCreated = false;
            lockedMods = new List<string>();
            unboundKeys = new List<KeyBind>();
            allKeys = new List<KeyBindString>();
            allKeys = GetKeyList();
            Debug.Log("ControlLock V1.5 Started");
            DoNotDestroy.DontDestroyOnLoad(this); //never unload this class so we persist across scenes.
            ConfigNode controlLockNode = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/001ControlLock/LockedKeys.cfg");
            if(controlLockNode.nodes.Count > 0)
            {
                for(int i = 1;i <= controlLockNode.nodes.Count;i++) //under normal contions, this should find no nodes, but if we crashed rebind our keys
                {
                    ConfigNode key = controlLockNode.nodes[i - 1]; //nodes is a 0 index list, so first node is zero position
                    string kbStr = key.GetValue("KeyBind");
                    KeyBindString kbMaster = allKeys.Find(kb3 => kb3.keyBindString == kbStr);
                    kbMaster.keyBind2.primary = (KeyCode)Enum.Parse(typeof(KeyCode), key.GetValue("Key1"));
                    kbMaster.keyBind2.secondary = (KeyCode)Enum.Parse(typeof(KeyCode), key.GetValue("Key2"));
                }
                GameSettings.SaveSettings(); //this will only be hit if there are nodes in the file, means we crashed so need to save our settings back
            }
            GameEvents.onGameSceneLoadRequested.Add(SceneSwitch);
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {
                //Debug.Log("Make button3");
                CLBtn = ToolbarManager.Instance.add("CtrlLock", "CLButton");
                CLBtn.TexturePath = "001ControlLock/ToolbarButton";
                CLBtn.ToolTip = "Control Lock";
                CLBtn.OnClick += (e) =>
                {
                    if (e.MouseButton == 0)
                    {
                        ToolbarClick();
                    }
                    if (e.MouseButton == 1)
                    {
                        showKeysWindow = !showKeysWindow;
                    } 
                };
            }
            else
            {
                StartCoroutine(AddButtons());//GameEvents.onGUIApplicationLauncherReady.Add(AddButtons);
                //GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButtons);
            }
            settings = LoadSettings();
        }

        public void SaveKeys()
        {
            ConfigNode toSave = new ConfigNode("ControlLockConfig");
            toSave.AddValue("CtrlLockKey", clKey.ToString());
            toSave.AddValue("CtrlLockModKey", clModKey.ToString());
            ConfigNode cfgNode = new ConfigNode("ControlLock");
            cfgNode.AddNode(toSave);
            cfgNode.Save(KSPUtil.ApplicationRootPath + "GameData/001ControlLock/ControlLock.cfg");
        }

        private ConfigNode LoadSettings()
        {
            
            {
                ConfigNode nodeLoad = new ConfigNode("ControlLockConfig");
                nodeLoad = GameDatabase.Instance.GetConfigNode("001ControlLock/ControlLock/ControlLockConfig");
                if (nodeLoad == null)
                {
                    Debug.Log("CtrlLock: node null");
                    nodeLoad = new ConfigNode("ControlLockConfig");
                }
                if (!nodeLoad.HasValue("CtrlLockKey"))
                {
                    nodeLoad.AddValue("CtrlLockKey", "None");
                }
                if (!nodeLoad.HasValue("CtrlLockModKey"))
                {
                    nodeLoad.AddValue("CtrlLockModKey", "None");
                }
                clKey = (KeyCode)Enum.Parse(typeof(KeyCode), nodeLoad.GetValue("CtrlLockKey"));
                clModKey = (KeyCode)Enum.Parse(typeof(KeyCode), nodeLoad.GetValue("CtrlLockModKey"));
                return nodeLoad;
            }
        }
        public void OnDisable()
        {
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {
                //GameEvents.onGUIApplicationLauncherReady.Remove(AddButtons);
                //GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveButtons);
                CLBtn.Destroy();
            }
            else
            {
                ApplicationLauncher.Instance.RemoveModApplication(CLButton);
            }
        }

        public static void UpdateButton()
        {
            if(!lockIsSet)
            {
                if(ToolbarManager.ToolbarAvailable)
                {
                    CLBtn.TexturePath = "001ControlLock/ToolbarButton";
                }
                else
                {
                    CLButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButton", false));
                }
            }
            else if(lockedMods.Contains("ButtonLock"))
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    CLBtn.TexturePath = "001ControlLock/ToolbarButtonYellow";
                }
                else
                {
                    CLButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButtonYellow", false));
                }
            }
            else
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    CLBtn.TexturePath = "001ControlLock/ToolbarButtonRed";
                }
                else
                {
                    CLButton.SetTexture((Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButtonRed", false));
                }
            }
        }

        //public Callback TextTest = Delegate
        //{
            
        //}

        //delegate testDel()
        //{
        public Callback rightClick = delegate //this is the right click button now, note this breaks references forcing the static bool 
        {
            //Debug.Log("CL deltest");
            showKeysWindow = !showKeysWindow;
        };
        

    

        IEnumerator AddButtons()
        {
            while(!ApplicationLauncher.Ready)
            {
                yield return null;
            }
            if (!buttonCreated)
                {
                    CLButton = ApplicationLauncher.Instance.AddModApplication(StockToolbarClick, StockToolbarClick, DummyVoid, DummyVoid, DummyVoid, DummyVoid, ApplicationLauncher.AppScenes.ALWAYS, (Texture)GameDatabase.Instance.GetTexture("001ControlLock/ToolbarButton", false));
                    //GameEvents.onGUIApplicationLauncherReady.Remove(AddButtons);
                    //CLButton.onLeftClick(StockToolbarClick);
                    CLButton.onRightClick = (Callback)Delegate.Combine(CLButton.onRightClick, rightClick); //combine delegates together
                    buttonCreated = true;
                }
        }
        //public void RemoveButtons(GameScenes scene)
        //{
        //    Debug.Log("destroy button");
        //    ApplicationLauncher.Instance.RemoveModApplication(CLButton);
        //}

        public void StockToolbarClick()
        {
            //right-clicks now done via delegate combination

            //Debug.Log("C: " + Event.current.button);
            //if (Event.current.button==1)
            //{
                
            //    showKeysWindow = !showKeysWindow;
            //}
            //else
            //{
               ToolbarClick();
            //}
        }

        public void DummyVoid()
        {

        }

        public static bool IsLockSet() //are we locked by anything?
        {
            return lockIsSet;
        }

        public static bool IsLockSet(string modName) //is a specific mod's lock engaged? note this can return false and we are still locked by another mod
        {
            if(!lockIsSet)
            {
                return false;
            }
            else if(lockedMods.Contains(modName))
            {
                return true;
            }
            else
            { 
                return false;
            }
        }

        public void ToolbarClick()
        {
            //Debug.Log("clicked");
            if(!lockedMods.Contains("ButtonLock")) //are we currently locked by toolbar button? 
            {
                SetFullLock("ButtonLock"); //no we are not, lock
            }
            else
            {
                UnsetFullLock("ButtonLock"); //yes we are, try to release
            }
        }

        public void SceneSwitch(GameScenes scene) //scene change, undo lock if enabled
        {
            lockedMods.Clear();
            if (lockIsSet)
            {
                InputLockManager.RemoveControlLock("ControlLock");
                RebindKeys(); 
                lockIsSet = false;
            }
        }

        public void Update() //error trap, neither of these should ever run, but just in case we are checking 
        {
            if(lockedMods.Count == 0 && lockIsSet)
            {
                RebindKeys();
                InputLockManager.RemoveControlLock("ControlLock");
                lockIsSet = false;
            }
            if (lockedMods.Count >= 1 && !lockIsSet)
            {
                UnbindKeys();
                InputLockManager.SetControlLock((ControlTypes)18442513152965869503, "ControlLock");
                lockIsSet = true;
            }

            if(clModKey == KeyCode.None && Input.GetKeyDown(clKey) || clModKey != KeyCode.None && Input.GetKey(clModKey) && Input.GetKeyDown(clKey)) //keypress?
            {
                ToolbarClick();
            }
        }

        public void OnGUI()
        {
            if (showKeysWindow)
            {
                GUI.Window(45779, new Rect((Screen.width / 2) - 100, 75, 200, 70), ControlLockWindow, "Control Lock", HighLogic.Skin.window);
            }
        }

        public void ControlLockWindow(int windowId)
        {
            HighLogic.Skin.button.alignment = TextAnchor.MiddleCenter;
            HighLogic.Skin.label.alignment = TextAnchor.MiddleCenter;
            if (GUI.Button(new Rect(145, 5, 50, 20), "RESET", HighLogic.Skin.button))
            {
                SceneSwitch(GameScenes.FLIGHT); //use the clear options that sceneswitch does
                UpdateButton();
            }
            if (setClKey || setClModKey)
            {
                if (GUI.Button(new Rect(25, 27, 150, 20), "Clear Key Assignment", HighLogic.Skin.button))
                {
                    if(setClKey)
                    {
                        clKey = KeyCode.None;
                        setClKey = false;
                    }
                    else
                    {
                        clModKey = KeyCode.None;
                        setClModKey = false;
                    }
                    SaveKeys();
                }
                GUI.Label(new Rect(25, 47, 150, 20), "Press New Key", HighLogic.Skin.label);
                if (Event.current.keyCode != KeyCode.None) //wait for keypress
                {
                    if (setClKey)
                    {
                        clKey = Event.current.keyCode;
                        setClKey = false;
                    }
                    else
                    {
                        clModKey = Event.current.keyCode;
                        setClModKey = false;
                    }
                    SaveKeys();
                }
            }
            else
            {
                if (GUI.Button(new Rect(100, 27, 80, 20), clKey.ToString(), HighLogic.Skin.button))
                {
                    setClKey = true;
                }
                if (GUI.Button(new Rect(10, 27, 80, 20), clModKey.ToString(), HighLogic.Skin.button))
                {
                    setClModKey = true;
                }
                GUI.Label(new Rect(100, 47, 80, 20), "Key", HighLogic.Skin.label);
                GUI.Label(new Rect(10, 47, 80, 20), "Modifier", HighLogic.Skin.label);
            }
            
        }

        public static void SetFullLock(string modName) //set the lock
        {
            if (!lockedMods.Contains(modName)) //only lock mod if not already locked for that mod
            {
                lockedMods.Add(modName); //add the mod to the list of locked mods
            }
            if (!lockIsSet)
            {
                UnbindKeys();
                lockIsSet = true; //set our lock true as we just locked everything
                //ulong lmask = 17293822569102705000;
                //InputLockManager.SetControlLock((ControlTypes)111011111111111111111111111111111111111111111111111111b, "ControlLock");
                //InputLockManager.SetControlLock(~ControlTypes.UI_MAIN, "ControlLock");
                ControlTypes myLock = ~ControlTypes.UI_MAIN;
                myLock ^= ControlTypes.UI_DIALOGS;
                InputLockManager.SetControlLock(myLock, "ControlLock");
             }
            UpdateButton();
        }

        private static void UnbindKeys() //unbind our keys
        {
            //Debug.Log("ControlLock set!");
            foreach (KeyBindString kb in allKeys) //find all keybinds in the game
            {
                if (kb.keyBind2.primary != KeyCode.None || kb.keyBind2.secondary != KeyCode.None) //only unbind if that action is actually bound to a key
                {
                    UnbindKey(kb); //unbind key and add to unboundKeys
                }
            }
            ConfigNode nodeToSave = new ConfigNode("ControlLock");
            nodeToSave.AddValue("placeholder", "fileCanNotBeEmpty");
            foreach(KeyBind kb in unboundKeys)
            {
                ConfigNode keyNode = new ConfigNode("Key");
                keyNode.AddValue("KeyBind", kb.keyBind);
                keyNode.AddValue("Key1", kb.mainBind.ToString());
                keyNode.AddValue("Key2", kb.secondBind.ToString());
                nodeToSave.AddNode(keyNode);
            }
            nodeToSave.Save(KSPUtil.ApplicationRootPath + "GameData/001ControlLock/LockedKeys.cfg"); //save keys to disk for backup
            
        }

        private static void UnbindKey(KeyBindString keyBind)
        {
            unboundKeys.Add(new KeyBind(keyBind)); //keep track of what has been unbound
            if (keyBind.keyBind2.primary != KeyCode.Escape) //do not unbind Esc key so player can still exit on fatal error
            {
                keyBind.keyBind2.primary = KeyCode.None; //unbind key
            }
            if (keyBind.keyBind2.secondary != KeyCode.Escape)
            {
                keyBind.keyBind2.secondary = KeyCode.None; //unbind key
            }
        }

        public static void UnsetFullLock(string modName) //unset the lock
        {
            lockedMods.Remove(modName); //add the mod to the list of locked mods
            if (lockIsSet && lockedMods.Count == 0) //only rebind keys if no other mod is locking us out, if not zero after remove on line above, still another mod locking us out so do not rebind
            {
                RebindKeys();
                InputLockManager.RemoveControlLock("ControlLock");
                lockIsSet = false; //set our lock false as we just unlocked everything
            }
            UpdateButton();
        }

        private static void RebindKeys()
        {
            //Debug.Log("ControlLock release!");
            foreach (KeyBindString kb in allKeys) //find all keybinds in the game
            {
                if(unboundKeys.Where(kb2 => kb2.keyBind == kb.keyBindString).Count() >= 1) //did we unbind this key?
                {
                    kb.keyBind2.primary = unboundKeys.Find(kb2 => kb2.keyBind == kb.keyBindString).mainBind; //rebind the keys
                    kb.keyBind2.secondary = unboundKeys.Find(kb2 => kb2.keyBind == kb.keyBindString).secondBind;
                }
            }
            ConfigNode nodeToSave = new ConfigNode("ControlLock"); //just rebound our keys, save a blank file to disk
            nodeToSave.AddValue("placeholder", "fileCanNotBeEmpty");
            nodeToSave.Save(KSPUtil.ApplicationRootPath + "GameData/001ControlLock/LockedKeys.cfg"); //save keys to disk for backup
            
        }

        private static List<KeyBindString> GetKeyList() //things are never simple, make our own index of all keybinds in game
        {
            List<KeyBindString> kbList = new List<KeyBindString>();
            kbList.Add(new KeyBindString(GameSettings.AbortActionGroup,"AbortActionGroup"));
            kbList.Add(new KeyBindString(GameSettings.BRAKES,"BRAKES"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_MODE,"CAMERA_MODE"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_NEXT,"CAMERA_NEXT"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_ORBIT_DOWN, "CAMERA_ORBIT_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_ORBIT_LEFT, "CAMERA_ORBIT_LEFT"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_ORBIT_RIGHT, "CAMERA_ORBIT_RIGHT"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_ORBIT_UP, "CAMERA_ORBIT_UP"));
            kbList.Add(new KeyBindString(GameSettings.CAMERA_RESET, "CAMERA_RESET"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup1, "CustomActionGroup1"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup10, "CustomActionGroup10"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup2, "CustomActionGroup2"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup3, "CustomActionGroup3"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup4, "CustomActionGroup4"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup5, "CustomActionGroup5"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup6, "CustomActionGroup6"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup7, "CustomActionGroup7"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup8, "CustomActionGroup8"));
            kbList.Add(new KeyBindString(GameSettings.CustomActionGroup9, "CustomActionGroup9"));
            kbList.Add(new KeyBindString(GameSettings.Docking_toggleRotLin, "Docking_toggleRotLin"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linBack, "Docking_linBack"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linDown, "Docking_linDown"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linFwd, "Docking_linFwd"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linLeft, "Docking_linLeft"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linRight, "Docking_linRight"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_linUp, "Docking_linUp"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_pitchDown, "Docking_pitchDown"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_pitchUp, "Docking_pitchUp"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_rollLeft, "Docking_rollLeft"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_rollRight, "Docking_rollRight"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_staging, "Docking_staging"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_throttleDown, "Docking_throttleDown"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_throttleUp, "Docking_throttleUp"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_toggleRotLin, "Docking_toggleRotLin"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_yawLeft, "Docking_yawLeft"));
            //kbList.Add(new KeyBindString(GameSettings.Docking_yawRight, "Docking_yawRight"));
            kbList.Add(new KeyBindString(GameSettings.Editor_coordSystem, "Editor_coordSystem"));
            kbList.Add(new KeyBindString(GameSettings.Editor_fineTweak, "Editor_fineTweak"));
            kbList.Add(new KeyBindString(GameSettings.Editor_modeOffset, "Editor_modeOffset"));
            kbList.Add(new KeyBindString(GameSettings.Editor_modePlace, "Editor_modePlace"));
            kbList.Add(new KeyBindString(GameSettings.Editor_modeRoot, "Editor_modeRoot"));
            kbList.Add(new KeyBindString(GameSettings.Editor_modeRotate, "Editor_modeRotate"));
            kbList.Add(new KeyBindString(GameSettings.Editor_pitchDown, "Editor_pitchDown"));
            kbList.Add(new KeyBindString(GameSettings.Editor_pitchUp, "Editor_pitchUp"));
            kbList.Add(new KeyBindString(GameSettings.Editor_resetRotation, "Editor_resetRotation"));
            kbList.Add(new KeyBindString(GameSettings.Editor_rollLeft, "Editor_rollLeft"));
            kbList.Add(new KeyBindString(GameSettings.Editor_rollRight, "Editor_rollRight"));
            kbList.Add(new KeyBindString(GameSettings.Editor_toggleAngleSnap, "Editor_toggleAngleSnap"));
            kbList.Add(new KeyBindString(GameSettings.Editor_toggleSymMethod, "Editor_toggleSymMethod"));
            kbList.Add(new KeyBindString(GameSettings.Editor_toggleSymMode, "Editor_toggleSymMode"));
            kbList.Add(new KeyBindString(GameSettings.Editor_yawLeft, "Editor_yawLeft"));
            kbList.Add(new KeyBindString(GameSettings.Editor_yawRight,"Editor_yawRight"));
            kbList.Add(new KeyBindString(GameSettings.EVA_back, "EVA_back"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Board, "EVA_Board"));
            kbList.Add(new KeyBindString(GameSettings.EVA_forward, "EVA_forward"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Jump, "EVA_Jump"));
            kbList.Add(new KeyBindString(GameSettings.EVA_left, "EVA_left"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Lights, "EVA_Lights"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Orient, "EVA_Orient"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_back, "EVA_Pack_back"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_down, "EVA_Pack_down"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_forward, "EVA_Pack_forward"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_left, "EVA_Pack_left"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_right, "EVA_Pack_right"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Pack_up, "EVA_Pack_up"));
            kbList.Add(new KeyBindString(GameSettings.EVA_right, "EVA_right"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Run, "EVA_Run"));
            kbList.Add(new KeyBindString(GameSettings.EVA_ToggleMovementMode, "EVA_ToggleMovementMode"));
            kbList.Add(new KeyBindString(GameSettings.EVA_TogglePack, "EVA_TogglePack"));
            kbList.Add(new KeyBindString(GameSettings.EVA_Use, "EVA_Use"));
            kbList.Add(new KeyBindString(GameSettings.EVA_yaw_left, "EVA_yaw_left"));
            kbList.Add(new KeyBindString(GameSettings.EVA_yaw_right, "EVA_yaw_right"));
            kbList.Add(new KeyBindString(GameSettings.FOCUS_NEXT_VESSEL, "FOCUS_NEXT_VESSEL"));
            kbList.Add(new KeyBindString(GameSettings.FOCUS_PREV_VESSEL, "FOCUS_PREV_VESSEL"));
            kbList.Add(new KeyBindString(GameSettings.HEADLIGHT_TOGGLE, "HEADLIGHT_TOGGLE"));
            kbList.Add(new KeyBindString(GameSettings.LANDING_GEAR, "LANDING_GEAR"));
            kbList.Add(new KeyBindString(GameSettings.LAUNCH_STAGES, "LAUNCH_STAGES"));
            kbList.Add(new KeyBindString(GameSettings.MAP_VIEW_TOGGLE, "MAP_VIEW_TOGGLE"));
            kbList.Add(new KeyBindString(GameSettings.MODIFIER_KEY, "MODIFIER_KEY"));
            kbList.Add(new KeyBindString(GameSettings.PAUSE, "PAUSE"));
            kbList.Add(new KeyBindString(GameSettings.PITCH_DOWN, "PITCH_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.PITCH_UP, "PITCH_UP"));
            kbList.Add(new KeyBindString(GameSettings.PRECISION_CTRL, "PRECISION_CTRL"));
            kbList.Add(new KeyBindString(GameSettings.QUICKLOAD, "QUICKLOAD"));
            kbList.Add(new KeyBindString(GameSettings.QUICKSAVE, "QUICKSAVE"));
            kbList.Add(new KeyBindString(GameSettings.RCS_TOGGLE, "RCS_TOGGLE"));
            kbList.Add(new KeyBindString(GameSettings.ROLL_LEFT, "ROLL_LEFT"));
            kbList.Add(new KeyBindString(GameSettings.ROLL_RIGHT, "ROLL_RIGHT"));
            kbList.Add(new KeyBindString(GameSettings.SAS_HOLD, "SAS_HOLD"));
            kbList.Add(new KeyBindString(GameSettings.SAS_TOGGLE, "SAS_TOGGLE"));
            kbList.Add(new KeyBindString(GameSettings.SCROLL_ICONS_DOWN, "SCROLL_ICONS_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.SCROLL_ICONS_UP, "SCROLL_ICONS_UP"));
            kbList.Add(new KeyBindString(GameSettings.SCROLL_VIEW_DOWN, "SCROLL_VIEW_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.SCROLL_VIEW_UP, "SCROLL_VIEW_UP"));
            kbList.Add(new KeyBindString(GameSettings.TAKE_SCREENSHOT, "TAKE_SCREENSHOT"));
            kbList.Add(new KeyBindString(GameSettings.THROTTLE_CUTOFF, "THROTTLE_CUTOFF"));
            kbList.Add(new KeyBindString(GameSettings.THROTTLE_DOWN, "THROTTLE_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.THROTTLE_FULL, "THROTTLE_FULL"));
            kbList.Add(new KeyBindString(GameSettings.THROTTLE_UP, "THROTTLE_UP"));
            kbList.Add(new KeyBindString(GameSettings.TIME_WARP_DECREASE, "TIME_WARP_DECREASE"));
            kbList.Add(new KeyBindString(GameSettings.TIME_WARP_INCREASE, "TIME_WARP_INCREASE"));
            kbList.Add(new KeyBindString(GameSettings.TOGGLE_LABELS, "TOGGLE_LABELS"));
            kbList.Add(new KeyBindString(GameSettings.TOGGLE_SPACENAV_FLIGHT_CONTROL, "TOGGLE_SPACENAV_FLIGHT_CONTROL"));
            kbList.Add(new KeyBindString(GameSettings.TOGGLE_SPACENAV_ROLL_LOCK, "TOGGLE_SPACENAV_ROLL_LOCK"));
            kbList.Add(new KeyBindString(GameSettings.TOGGLE_STATUS_SCREEN, "TOGGLE_STATUS_SCREEN"));
            kbList.Add(new KeyBindString(GameSettings.TOGGLE_UI, "TOGGLE_UI"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_BACK, "TRANSLATE_BACK"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_DOWN, "TRANSLATE_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_FWD, "TRANSLATE_FWD"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_LEFT, "TRANSLATE_LEFT"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_RIGHT, "TRANSLATE_RIGHT"));
            kbList.Add(new KeyBindString(GameSettings.TRANSLATE_UP, "TRANSLATE_UP"));
            kbList.Add(new KeyBindString(GameSettings.UIMODE_DOCKING, "UIMODE_DOCKING"));
            kbList.Add(new KeyBindString(GameSettings.UIMODE_STAGING, "UIMODE_STAGING"));
            kbList.Add(new KeyBindString(GameSettings.WHEEL_STEER_LEFT, "WHEEL_STEER_LEFT"));
            kbList.Add(new KeyBindString(GameSettings.WHEEL_STEER_RIGHT, "WHEEL_STEER_RIGHT"));
            kbList.Add(new KeyBindString(GameSettings.WHEEL_THROTTLE_DOWN, "WHEEL_THROTTLE_DOWN"));
            kbList.Add(new KeyBindString(GameSettings.WHEEL_THROTTLE_UP, "WHEEL_THROTTLE_UP"));
            kbList.Add(new KeyBindString(GameSettings.YAW_LEFT, "YAW_LEFT"));
            kbList.Add(new KeyBindString(GameSettings.YAW_RIGHT, "YAW_RIGHT"));
            kbList.Add(new KeyBindString(GameSettings.ZOOM_IN, "ZOOM_IN"));
            kbList.Add(new KeyBindString(GameSettings.ZOOM_OUT, "ZOOM_OUT"));
            return kbList;
        }
    }

    public class KeyBind
    {
        public string keyBind; //our keybind string as per index we created
        public KeyCode mainBind;
        public KeyCode secondBind;

        public KeyBind(KeyBindString kb)
        {
            keyBind = kb.keyBindString;
            mainBind = kb.keyBind2.primary;
            secondBind = kb.keyBind2.secondary;
        }
    }

    public class KeyBindString
    {
        public KeyBinding keyBind2;
        public string keyBindString;

        public KeyBindString(KeyBinding kb, string str)
        {
            keyBind2 = kb;
            keyBindString = str;
        }
    }

    //public class MyLauncherButton :ApplicationLauncherButton
    //{
    //    public override Callback onRightClick()
    //    {

    //}
    //}
}
