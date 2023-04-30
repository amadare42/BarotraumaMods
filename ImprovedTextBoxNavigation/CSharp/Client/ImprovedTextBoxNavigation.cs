using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Barotrauma;
using HarmonyLib;
using OpCodes = System.Reflection.Emit.OpCodes;
// ReSharper disable UnusedType.Global

[assembly: IgnoresAccessChecksTo("Barotrauma")]
namespace ImprovedTextBoxNavigation {

    class Access 
    {
        public static class GUITextBox 
        {
            public delegate void SetTextDelegate(Barotrauma.GUITextBox instance, string text, bool store = true);            

            public static SetTextDelegate SetText = ReflectionHelper.CreateAccessPrivateMethod<SetTextDelegate, Barotrauma.GUITextBox>(
                "SetText",
                new Type[] { typeof(string), typeof(bool) }
            );

            public static Action<Barotrauma.GUITextBox> CalculateCaretPos = ReflectionHelper.CreateAccessPrivateMethod<Action<Barotrauma.GUITextBox>, Barotrauma.GUITextBox>(
                "CalculateCaretPos"
            );
            public static Action<Barotrauma.GUITextBox> ClearSelection = ReflectionHelper.CreateAccessPrivateMethod<Action<Barotrauma.GUITextBox>, Barotrauma.GUITextBox>(
                "ClearSelection"
            );
            public static Action<Barotrauma.GUITextBox> RemoveSelectedText = ReflectionHelper.CreateAccessPrivateMethod<Action<Barotrauma.GUITextBox>, Barotrauma.GUITextBox>(
                "RemoveSelectedText"
            );
            public static Action<Barotrauma.GUITextBox> InitSelectionStart = ReflectionHelper.CreateAccessPrivateMethod<Action<Barotrauma.GUITextBox>, Barotrauma.GUITextBox>(
                "InitSelectionStart"
            );
        }
    }
    
    class ImprovedTextBoxNavigationMain : ACsMod 
    {
        // Internal
        private Harmony HarmonyInstance;
        private static readonly List<Action> RevertActions = new();

        public ImprovedTextBoxNavigationMain()
        {
            ClearLogs();
            this.HarmonyInstance = new Harmony("ImprovedTextBoxNavigation");
            this.HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(Barotrauma.GUITextBox))]
        public static class GUITextBox_ReceiveCommandInput_Patch
        {
            /// Ctrl+Backspace
            [HarmonyPatch("ReceiveCommandInput")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ReceiveCommandInput(IEnumerable<CodeInstruction> instructions) 
            {
                var ops = instructions.ToList();

                var idx = ops.FindIndex(0, i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("SetText"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveCommandInput() method [1]");
                    return ops;
                }
                idx = ops.FindLastIndex(idx, i => i.opcode == OpCodes.Ldarg_0);
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveCommandInput() method [2]");
                    return ops;
                }
                var idx_end = ops.FindIndex(idx, idx => idx.opcode == OpCodes.Call && idx.operand.ToString().Contains("set_CaretIndex"));
                if (idx_end == -1)
                {
                    Log("Failed to patch ReceiveCommandInput() method [3]");
                    return ops;
                }

                var labels = ops[idx].labels;

                ops.RemoveRange(idx, idx_end - idx + 1);
                ops.InsertRange(idx, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0) { labels = labels }, // GUITextBox instance
                    new CodeInstruction(OpCodes.Ldarg_1), // char command
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveCommandInputHook"))
                });

                Log("Patched ReceiveCommandInput() method");
                return ops;
            }

            /// Ctrl+Left, Ctrl+Right
            [HarmonyPatch("ReceiveSpecialInput")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ReceiveSpecialInput_Left_Transpiler(IEnumerable<CodeInstruction> instructions) 
            {
                var ops = instructions.ToList();

                // find index of CaretIndex = Math.Max(CaretIndex - 1, 0); [start]
                var idx = ops.FindIndex(0, i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("get_CaretIndex"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() method [1]");
                    return ops;
                }
                idx -= 2;
                var labels = ops[idx].labels;

                // find index of CaretIndex = Math.Max(CaretIndex - 1, 0); [end]
                var idx_end = ops.FindIndex(idx, idx => idx.opcode == OpCodes.Call && idx.operand.ToString().Contains("set_CaretIndex"));
                if (idx_end == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() method [2]");
                    return ops;
                }

                ops.RemoveRange(idx, idx_end - idx + 1);
                ops.InsertRange(idx, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0) { labels = labels }, // GUITextBox instance
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveSpecialInputHook_Left"))
                });

                // Patching Right

                idx = ops.FindIndex(idx, i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("get_CaretIndex"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() method [3]");
                    return ops;
                }
                idx -= 2;
                labels = ops[idx].labels;

                idx_end = ops.FindIndex(idx, idx => idx.opcode == OpCodes.Call && idx.operand.ToString().Contains("set_CaretIndex"));
                if (idx_end == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() method [4]");
                    return ops;
                }

                ops.RemoveRange(idx, idx_end - idx + 1);
                ops.InsertRange(idx, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0) { labels = labels }, // GUITextBox instance
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveSpecialInputHook_Right"))
                });

                Log("Patched ReceiveSpecialInput() method");
                return ops;
            }
            
            /// Ctrl+Delete
            [HarmonyPatch("ReceiveSpecialInput")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ReceiveSpecialInput_Delete_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator, MethodBase methodBase) 
            {
                var ops = instructions.ToList();

                // find index of if (selectedCharacters > 0)
                var idx = ops.FindIndex(i => i.opcode == OpCodes.Ldfld && i.operand.ToString().Contains("selectedCharacters"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() [Delete] method [1]");
                    return ops;
                }
                idx -= 1;

                // label shenanigans
                var origLabel = ilGenerator.DefineLabel();
                ops[idx].labels.Add(origLabel);

                // find "break;"
                var breakInstruction = ops[ops.FindIndex(idx, i => i.opcode == OpCodes.Br_S)];

                // find OnTextChanged?.Invoke(this, Text)
                var idx2 = ops.FindIndex(idx, i => i.opcode == OpCodes.Ldfld && i.operand.ToString().Contains("OnTextChanged"));
                var idx2_2 = ops.FindIndex(idx2, i => i.opcode == OpCodes.Callvirt && i.operand.ToString().Contains("Invoke"));
                var invokeOnTextChanged = ops.GetRange(idx2 - 1, idx2_2 - idx2 + 3);

                var rangeToInsert = new List<CodeInstruction>()
                {
                    // if (!PlayerInput.IsCtrlDown) goto origLabel;
                    new CodeInstruction(OpCodes.Call, typeof(PlayerInput).GetMethod("IsCtrlDown", BindingFlags.Static | BindingFlags.Public)),
                    new CodeInstruction(OpCodes.Brfalse_S, origLabel),

                    /* 
                    if (ReceiveSpecialInputHook_Delete(instance)) 
                    { 
                        OnTextChanged?.Invoke(this, Text); 
                        ReceiveSpecialInputHook_Delete_2(instance); 
                        break; 
                    } 
                    else 
                    {
                        break; 
                    }
                    */
                    new CodeInstruction(OpCodes.Ldarg_0), // GUITextBox instance
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveSpecialInputHook_Delete", BindingFlags.Static | BindingFlags.Public)),
                    new CodeInstruction(OpCodes.Brfalse_S, breakInstruction.operand)
                };
                rangeToInsert.AddRange(invokeOnTextChanged);
                rangeToInsert.AddRange(new List<CodeInstruction>() {
                    new CodeInstruction(OpCodes.Ldarg_0), // GUITextBox instance
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveSpecialInputHook_Delete_2")),
                    breakInstruction
                });

                ops.InsertRange(idx, rangeToInsert);
                return ops;
            }
            
            /// Home & End
            [HarmonyPatch("ReceiveSpecialInput")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ReceiveSpecialInput_Delete_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator) 
            {
                var ops = instructions.ToList();
                var origOps = ops.ToList();

                var idx = ops.FindIndex(i => i.opcode == OpCodes.Call && i.operand.ToString().Contains("HandleSelection"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() [Home & End] method [1]");
                    return ops;
                }
                var handleSelectionRange = ops.GetRange(idx - 1, 2);

                idx = ops.FindIndex(idx, i => i.opcode == OpCodes.Ldfld && i.operand.ToString().Contains("caretPosDirty"));
                if (idx == -1)
                {
                    Log("Failed to patch ReceiveSpecialInput() [Home & End] method [2]");
                    return ops;
                }

                var label = ilGenerator.DefineLabel();

                ops.InsertRange(idx, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_1), // key
                    new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("ReceiveSpecialInput_HomeEnd", BindingFlags.Static | BindingFlags.Public)),
                    new CodeInstruction(OpCodes.Brfalse_S, label),
                    handleSelectionRange[0],
                    handleSelectionRange[1],
                    new CodeInstruction(OpCodes.Ldarg_0) { labels = new List<System.Reflection.Emit.Label>() { label } }, // GUITextBox instance
                });

                Log("Patched ReceiveSpecialInput() method");

                return ops;
            }

            /// Hold Shift to select text
            [HarmonyPatch("Update")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> ReceiveSpecialInput_Update_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator) 
            {
                var ops = instructions.ToList();

                var idx = ops.FindIndex(i => i.opcode == OpCodes.Ldfld && i.operand.ToString().Contains("isSelecting"));
                if (idx == -1)
                {
                    Log("Failed to patch Update() method [1]");
                    return ops;
                }
                var isSelectingLabel = iLGenerator.DefineLabel();
                ops[idx - 1].labels.Add(isSelectingLabel);
                ops.InsertRange(idx - 1, new List<CodeInstruction> {
                    new CodeInstruction(OpCodes.Call, typeof(PlayerInput).GetMethod("IsShiftDown", BindingFlags.Static | BindingFlags.Public)),
                    new CodeInstruction(OpCodes.Brfalse_S, isSelectingLabel),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Callvirt, typeof(GUITextBox).GetMethod("get_State", BindingFlags.Public | BindingFlags.Instance)),
                    new CodeInstruction(OpCodes.Ldc_I4, (int)GUIComponent.ComponentState.Selected),
                    new CodeInstruction(OpCodes.Ceq),
                    ops[idx - 2] // brtrue.s [trueLabel]
                });

                idx = ops.FindIndex(i => i.opcode == OpCodes.Stfld && i.operand.ToString().Contains("isSelecting"));
                if (idx == -1)
                {
                    Log("Failed to patch Update() method [2]");
                    return ops;
                }
                ops[idx - 1] = new CodeInstruction(OpCodes.Call, typeof(GUITextBox_ReceiveCommandInput_Patch).GetMethod("IsSelecting_Hook", BindingFlags.Static | BindingFlags.Public));
                return ops;
            }
            
            public static void ReceiveCommandInputHook(GUITextBox instance, char command)
            {
                var removeTo = FindStopSymbolLeft(instance.Text, instance.CaretIndex);
                var len = instance.CaretIndex - removeTo;
                var newText = instance.Text.Remove(removeTo, instance.CaretIndex - removeTo);
                Access.GUITextBox.SetText(instance, newText, false);
                instance.CaretIndex = Math.Max(0, instance.CaretIndex - len);
                Access.GUITextBox.CalculateCaretPos(instance);
                Access.GUITextBox.ClearSelection(instance);
            }

            public static void ReceiveSpecialInputHook_Left(GUITextBox instance)
            {
                if (PlayerInput.IsCtrlDown())
                {
                    instance.CaretIndex = FindStopSymbolLeft(instance.Text, instance.CaretIndex);
                }
                else
                {
                    instance.CaretIndex = Math.Max(instance.CaretIndex - 1, 0);
                }
            }

            public static void ReceiveSpecialInputHook_Right(GUITextBox instance)
            {
                if (PlayerInput.IsCtrlDown())
                {
                    instance.CaretIndex = FindStopSymbolRight(instance.Text, instance.CaretIndex);
                }
                else
                {
                    instance.CaretIndex = Math.Min(instance.CaretIndex + 1, instance.Text.Length);
                }
            }

            public static bool ReceiveSpecialInputHook_Delete(GUITextBox instance) 
            {
                var removeTo = FindStopSymbolRight(instance.Text, instance.CaretIndex);
                var len = removeTo - instance.CaretIndex;
                if (len <= 0)
                {
                    if (ReflectionHelper.GetPrivateField<GUITextBox, int>(instance, "selectedCharacters") > 0)
                    {
                        Access.GUITextBox.RemoveSelectedText(instance);
                    }

                    return false;
                }

                var newText = instance.Text.Remove(instance.CaretIndex, len);
                Access.GUITextBox.SetText(instance, newText, false);
                return true;
            }

            public static void ReceiveSpecialInputHook_Delete_2(GUITextBox instance) 
            {
                Access.GUITextBox.CalculateCaretPos(instance);
                Access.GUITextBox.ClearSelection(instance);
            }

            public static bool IsSelecting_Hook() {
                return PlayerInput.PrimaryMouseButtonHeld() || PlayerInput.IsShiftDown();
            }
        
            public static bool ReceiveSpecialInput_HomeEnd(GUITextBox instance, Microsoft.Xna.Framework.Input.Keys key) 
            {
                if (key == Microsoft.Xna.Framework.Input.Keys.Home) 
                {
                    if (ReflectionHelper.GetPrivateField<GUITextBox, bool>(instance, "isSelecting"))
                    {
                        Access.GUITextBox.InitSelectionStart(instance);
                    }

                    if (PlayerInput.IsCtrlDown())
                    {
                        instance.CaretIndex = 0;
                    }
                    else if (instance.Text.Length == 0) 
                    {
                        return false;
                    }
                    else
                    {
                        instance.CaretIndex = instance.Text.LastIndexOf('\n', Math.Clamp(instance.CaretIndex - 1, 0, instance.Text.Length - 1)) + 1;
                    }

                    ReflectionHelper.SetPrivateField<GUITextBox, float>(instance, "caretTimer", 0);
                    return true;
                } 
                else if (key ==  Microsoft.Xna.Framework.Input.Keys.End) 
                {
                    if (ReflectionHelper.GetPrivateField<GUITextBox, bool>(instance, "isSelecting"))
                    {
                        Access.GUITextBox.InitSelectionStart(instance);
                    }

                    if (PlayerInput.IsCtrlDown())
                    {
                        instance.CaretIndex = instance.Text.Length;
                    }
                    else
                    {
                        var idx = instance.Text.IndexOf('\n', instance.CaretIndex);
                        if (idx < 0)
                        {
                            instance.CaretIndex = instance.Text.Length;
                        }
                        else
                        {
                            instance.CaretIndex = idx;
                        }
                    }

                    ReflectionHelper.SetPrivateField<GUITextBox, float>(instance, "caretTimer", 0);
                    return true;
                }

                return false;
            }
        }

        private static int FindStopSymbolLeft(string text, int idx)
        {
            // special case: if cursor at the start of the line, move to the end of previous line
            if (idx > 0 && text[idx - 1] == '\n')
            {
                return idx - 1;
            }
            // convert cursor index to character index
            idx = Math.Max(0, idx - 2);
            if (idx == 0) return idx;

            for (; idx >= 0; idx--)
            {
                var c = text[idx];
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || c == '\n')
                {
                    // convert character index back cursor index
                    return idx + 1;
                }
            }
            return Math.Clamp(idx, 0, text.Length);
        }

        private static int FindStopSymbolRight(string text, int idx)
        {
            // special case: if cursor is at the end of line, move to the start of next line
            if (idx < text.Length && text[idx] == '\n')
            {
                return idx + 1;
            }

            for (; idx < text.Length; idx++)
            {
                var c = text[idx];
                if (c == '\n')
                {
                    return idx;
                }
                if (char.IsWhiteSpace(c) || char.IsPunctuation(c))
                {
                    // convert character index to cursor index
                    return Math.Min(idx + 1, text.Length);
                }
            }
            return Math.Clamp(idx, 0, text.Length);
        }
        

        public override void Stop()
        {
            this.HarmonyInstance.UnpatchAll("ItemFinderCount");

            foreach (var reventAction in RevertActions)
            {
                try
                {
                    reventAction();
                }
                catch (Exception ex)
                {
                    Log("Error occured while executing ReventAction: " + ex);
                }
            }
        }

        public static void Log(string arg)
        {
            var line = "[ImprovedTextBoxNavigation] " + arg;
            LuaCsLogger.Log(line);
        }
    }
}