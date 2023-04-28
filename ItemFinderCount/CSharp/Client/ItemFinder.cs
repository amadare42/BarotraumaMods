using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Barotrauma;
using HarmonyLib;
using OpCodes = System.Reflection.Emit.OpCodes;
using MiniMap = Barotrauma.Items.Components.MiniMap;

[assembly: IgnoresAccessChecksTo("Barotrauma")]
namespace ItemFinderCount {
    
    partial class ItemFinderCountMain : ACsMod 
    {
        private Harmony harmony;

        private static GUITextBlock ItemsTextBlock;
        
        private static ItemFinderCountMain Instance;

        public ItemFinderCountMain()
        {
            Log("InitClient");
            this.harmony = new Harmony("ItemFinderCount");
            this.harmony.PatchAll();
            Instance = this;
        }

        public override void Stop()
        {
            this.harmony.UnpatchAll("ItemFinderCount");
            Log("Desctruct");
            Instance = null;

            if (ItemsTextBlock != null)
            {
                ItemsTextBlock.RectTransform.Parent = null;
                ItemsTextBlock = null;
            }
        }

        [HarmonyPatch(typeof(Barotrauma.Items.Components.MiniMap))]
        [HarmonyPatch("SearchItems")]
        public static class MiniMap_SearchItems_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                var ops = new List<CodeInstruction>(instructions);
                for (var index = 0; index < ops.Count; index++)
                {
                    var op = ops[index];
                    if (op.opcode == OpCodes.Newobj)
                    {
                        var ctorInfo = (ConstructorInfo)op.operand;
                        Log(index + " " + ctorInfo.DeclaringType);
                        if (ctorInfo.DeclaringType == typeof(HashSet<Microsoft.Xna.Framework.Vector2>))
                        {
                            ops.InsertRange(index - 1, new [] {
                                new CodeInstruction(OpCodes.Ldarg_0),
                                new CodeInstruction(OpCodes.Call, typeof(ItemFinderCountMain).GetMethod(nameof(OnSearchItems), BindingFlags.Static | BindingFlags.Public)!)
                            });
                            Log("SearchItems patched!");
                            return ops;
                        }
                    }
                }
                
                Log("Cannot patch SearchItems method!");
                return ops;
            }
        }

        [HarmonyPatch(typeof(Barotrauma.Items.Components.MiniMap))]
        [HarmonyPatch("CreateGUI")]
        public static class MiniMap_CreateGUI_Patch
        {
            static void Postfix(MiniMap __instance)
            {
                CreateItemCountLabel(__instance);
            }
        }
        
        [HarmonyPatch(typeof(Barotrauma.Items.Components.MiniMap))]
        [HarmonyPatch("UpdateHUD")]
        public static class MiniMap_UpdateHUD_Patch
        {
            static void Postfix(MiniMap __instance)
            {
                if (ItemsTextBlock == null) return;

                ItemsTextBlock.Visible = ReflectionHelper.GetCurrentMode(__instance) ==
                                         Barotrauma.Items.Components.MiniMapMode.ItemFinder;
            }
        }

        public static void CreateItemCountLabel(MiniMap miniMap)
        {
            var GuiFrame = miniMap.GuiFrame;
            var point = new Microsoft.Xna.Framework.Point(993, 140);
            ItemsTextBlock = new GUITextBlock(
                new RectTransform(point, GuiFrame.RectTransform, Anchor.TopCenter),
                "",
                Microsoft.Xna.Framework.Color.White,
                GUIStyle.Font,
                Alignment.Right
            )
            {
                CanBeFocused = false
            };
            Log("Added block");
        }
        
        public static void OnSearchItems(MiniMap miniMap)
        {
            var total = 0;
            var onSub = 0;

            var searchedPrefab = ReflectionHelper.GetSearchedPrefab(miniMap);
            
            foreach (var it in Item.ItemList)
            {
                if (!ReflectionHelper.VisibleOnItemFinder(miniMap, it)) { continue; }
                
                if (it.Prefab == searchedPrefab)
                {
                    total++;
                    // ignore items on players and hidden inventories
                    if (it.FindParentInventory(inv => inv is CharacterInventory || inv is ItemInventory { Owner: Item { HiddenInGame: true }}) is { }) { continue; }
                    onSub++;
                }
            }

            if (ItemsTextBlock == null)
            {
                CreateItemCountLabel(miniMap);
            }
            ItemsTextBlock.Text = $"Total Count {onSub} ({total})";
            Log($"Total Count {onSub} ({total})");
        }



        public static void Log(string arg)
        {
            LuaCsLogger.Log(arg);
        }
    }

    static class ReflectionHelper
    {
        public static Func<MiniMap, Item, bool> VisibleOnItemFinder => _visibleOnItemFinder.Value;
        public static Func<MiniMap, ItemPrefab> GetSearchedPrefab => _getSearchedPrefab.Value;
        public static Func<MiniMap, Barotrauma.Items.Components.MiniMapMode> GetCurrentMode => _getCurrentMode.Value;
        
        public static Lazy<Func<MiniMap, Item, bool>> _visibleOnItemFinder = new(CreateVisibleOnItemFinder);
        public static Lazy<Func<MiniMap, ItemPrefab>> _getSearchedPrefab = new(() => CreateAccessPrivateField<MiniMap, ItemPrefab>("searchedPrefab"));
        public static Lazy<Func<MiniMap, Barotrauma.Items.Components.MiniMapMode>> _getCurrentMode = new(() => CreateAccessPrivateField<MiniMap, Barotrauma.Items.Components.MiniMapMode>("currentMode"));

        public static Func<MiniMap, Item, bool> CreateVisibleOnItemFinder()
        {
            var instanceParam = Expression.Parameter(typeof(MiniMap));
            var itemParam = Expression.Parameter(typeof(Item));
            var callExpr = Expression.Call(instanceParam, "VisibleOnItemFinder", Type.EmptyTypes, itemParam);
            var lambdaExpr = Expression.Lambda<Func<MiniMap, Item, bool>>(callExpr, instanceParam, itemParam);
            return lambdaExpr.Compile();
        }

        public static Func<TInstance, TField> CreateAccessPrivateField<TInstance, TField>(string name)
        {
            var instanceParam = Expression.Parameter(typeof(TInstance));
            var fieldExpr = Expression.Field(instanceParam, name);
            var lambdaExpr = Expression.Lambda<Func<TInstance, TField>>(fieldExpr, instanceParam);
            return lambdaExpr.Compile();
        }
    }
}