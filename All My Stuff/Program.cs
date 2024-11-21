using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyTerminalBlock> Containers = new List<IMyTerminalBlock>();
        static readonly string Version = "Version 1.6.0";
        MyIni ini = new MyIni();
        static readonly string ConfigSection = "Inventory";
        static readonly string DisplaySectionPrefix = ConfigSection + "_Display";
        private const string KnowItemsString = "KnownItems";
        StringBuilder SectionCandidateName = new StringBuilder();
        StringBuilder knownItemsSb = new StringBuilder();
        List<string> SectionNames = new List<string>();
        SortedDictionary<string, Item> Stock = new SortedDictionary<string, Item>();
        SortedDictionary<string, string> Translation = new SortedDictionary<string, string>();
        List<MyInventoryItem> Items = new List<MyInventoryItem>();
        List<ManagedDisplay> Screens = new List<ManagedDisplay>();
        IEnumerator<bool> _stateMachine;
        int delayCounter = 0;
        int delay;
        private static int characters_to_skip = "MyObjectBuilder_".Length;
        bool StoreKnownTypes;  // Enable save known types globally
        bool TranslateEnabled; // Enable translate feature globally
        bool FilterEnabled; // Enable filter feature globally
        bool FormatNames; // Enable name prettify feature globally
        bool FormatAmount; // Enable Number Abbreviation feature globally
        bool separators;    // Control whether type separator lines are displayed on screen
        bool rebuild;
        bool clear;
        private List<MyIniKey> TranslationKeys = new List<MyIniKey>();

        public class Item
        {
            public Item(string itemType, Program program, int amount = 0)
            {
                var temp = itemType.Split('/');
                Sprite = itemType;
                Name = NaturalName = temp[1];
                ItemType = temp[0];
                Amount = amount;
                KeyString = program.TranslateEnabled ? itemType.Substring(characters_to_skip).ToLower() : "";
            }

            public string KeyString;
            public int Amount;
            public string Sprite;
            public string Name;
            public string ItemType;
            public string NaturalName;
            public string FormatedAmount;
        }

        public void GetBlocks()
        {
            Containers.Clear();
            Screens.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(Containers, block =>
            {
                if (!block.IsSameConstructAs(Me))
                    return false;
                if (!TryAddDiscreteScreens(block))
                    TryAddScreen(block);
                return block.HasInventory & block.ShowInInventory;
            });
        }

        void AddScreen(IMyTextSurfaceProvider provider, int displayNumber, string section)
        {
            var display = ((IMyTextSurfaceProvider)provider).GetSurface(displayNumber);
            var linesToSkip = ini.Get(section, "skip").ToInt16();
            bool monospace = ini.Get(section, "mono").ToBoolean();
            bool suppressZeros = ini.Get(section, "suppress_zeros").ToBoolean();
            bool separators = ini.Get(section, "separators").ToBoolean(this.separators);
            float scale = ini.Get(section, "scale").ToSingle(1.0f);
            bool formatAmount = ini.Get(section, "format_amount").ToBoolean(FormatAmount);
            string DefaultColor = "FF4500";
            string ColorStr = ini.Get(section, "color").ToString(DefaultColor);
            if (ColorStr.Length < 6)
                ColorStr = DefaultColor;
            Color color = new Color()
            {
                R = byte.Parse(ColorStr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                G = byte.Parse(ColorStr.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                B = byte.Parse(ColorStr.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                A = 255
            };
            var managedDisplay = new ManagedDisplay(display, scale, color, linesToSkip, monospace, suppressZeros, separators, formatAmount);
            if (FilterEnabled)
            {
                managedDisplay.SetFilter(ini.Get(section, "filter").ToString(null));
            }
            Screens.Add(managedDisplay);
        }

        private bool TryAddDiscreteScreens(IMyTerminalBlock block)
        {
            bool retval = false;
            IMyTextSurfaceProvider Provider = block as IMyTextSurfaceProvider;
            if (null == Provider || Provider.SurfaceCount == 0)
                return true;
            StringComparison ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            ini.TryParse(block.CustomData);
            ini.GetSections(SectionNames);
            foreach (var section in SectionNames)
            {
                if (section.StartsWith(DisplaySectionPrefix, ignoreCase))
                {
                    for (int displayNumber = 0; displayNumber < Provider.SurfaceCount; ++displayNumber)
                    {
                        if (displayNumber < Provider.SurfaceCount)
                        {
                            SectionCandidateName.Clear();
                            SectionCandidateName.Append(DisplaySectionPrefix).Append(displayNumber.ToString());
                            if (section.Equals(SectionCandidateName.ToString(), ignoreCase))
                            {
                                AddScreen(Provider, displayNumber, section);
                                retval = true;
                            }
                        }
                    }
                }
            }
            return retval;
        }

        private void TryAddScreen(IMyTerminalBlock block)
        {
            IMyTextSurfaceProvider Provider = block as IMyTextSurfaceProvider;
            if (null == Provider || Provider.SurfaceCount == 0 || !MyIni.HasSection(block.CustomData, ConfigSection))
                return;
            ini.TryParse(block.CustomData);
            var displayNumber = ini.Get(ConfigSection, "display").ToUInt16();
            if (displayNumber < Provider.SurfaceCount || Provider.SurfaceCount == 0)
            {
                AddScreen(Provider, displayNumber, ConfigSection);
            }
            else
            {
                Echo("Warning: " + block.CustomName + " doesn't have a display number " + ini.Get(ConfigSection, "display").ToString());
            }
        }

        public void RunItemCounter()
        {
            if (_stateMachine != null)
            {
                bool hasMoreSteps = _stateMachine.MoveNext();

                if (hasMoreSteps)
                {
                    Runtime.UpdateFrequency |= UpdateFrequency.Once;
                }
                else
                {
                    _stateMachine.Dispose();
                    _stateMachine = null;
                }
            }
        }

        public IEnumerator<bool> LoadStorage()
        {
            ini.TryParse(Storage);

            if (ini.ContainsKey(ConfigSection, KnowItemsString))
            {
                var items = ini.Get(ConfigSection, KnowItemsString).ToString();

                if(string.IsNullOrWhiteSpace(items))
                    yield break;

                var itemTypes = items.Split('\n');

                foreach (var item in itemTypes)
                {
                    if (!Stock.ContainsKey(item))
                    {
                        Item newItem = new Item(item, this);
                        string translation;
                        newItem.NaturalName = Translation.TryGetValue(newItem.KeyString, out translation) ? translation : newItem.Name;
                        Stock.Add(item, newItem);
                    }
                    yield return true;
                }
            }
        }

        public IEnumerator<bool> CountItems()
        {
            foreach (var Item in Stock.Keys)
                Stock[Item].Amount = 0;
            yield return true;
            ReadConfig();
            yield return true;
            yield return true;
            foreach (var container in Containers)
            {
                for (int i = 0; i < container.InventoryCount; ++i)
                {
                    var inventory = container.GetInventory(i);
                    if (inventory.ItemCount > 0)
                    {
                        Items.Clear();
                        inventory.GetItems(Items);
                        foreach (var item in Items)
                        {
                            string key = item.Type.ToString();
                            if (!Stock.ContainsKey(key))
                            {
                                Item newItem = new Item(item.Type.ToString(), this);
                                newItem.NaturalName = Translation.ContainsKey(newItem.KeyString) ? Translation[newItem.KeyString] : newItem.Name;
                                Stock.Add(key, newItem);
                            }
                            Stock[key].Amount += item.Amount.ToIntSafe();
                        }
                        yield return true;
                    }
                }
            }
            
            if(FormatAmount || Screens.Any(s => s.UseFormatedAmount))
            {
                foreach (var item in Stock.Values) 
                    item.FormatedAmount = MetricFormat(item.Amount);
            }
            
            RenderScreens();
        }

        public void UpdateNaturalName(Item item)
        {
            string value;
            item.NaturalName = Translation.TryGetValue(item.KeyString, out value) ? value :
                FormatNames ? PrettifyString(item.Name) : item.Name;
        }

        public IEnumerator<bool> RemoveEmptyItems()
        {
            var keys = Stock.Keys.ToList();

            for (int i = Stock.Keys.Count - 1; i >= 0; i--)
            {
                if (Stock[keys[i]].Amount == 0)
                {
                    Stock.Remove(keys[i]);
                    yield return true;
                }
            }
            RenderScreens();
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100 | UpdateFrequency.Once;
            ReadConfig();
            GetBlocks();

            if (StoreKnownTypes)
                _stateMachine = LoadStorage();
        }

        private void ReadConfig()
        {
            if (ini.TryParse(Me.CustomData))
            {
                delay = ini.Get(ConfigSection, "delay").ToInt32(3);
                TranslateEnabled = ini.ContainsSection("translation");
                FilterEnabled = ini.Get(ConfigSection, "enablefilter").ToBoolean(true);
                separators = ini.Get(ConfigSection, "separators").ToBoolean(true);
                StoreKnownTypes = ini.Get(ConfigSection, "savetypes").ToBoolean(true);
                FormatAmount = ini.Get(ConfigSection, "format_amount").ToBoolean(true);
                var formatNames = ini.Get(ConfigSection, "format_names").ToBoolean(true);

                var updateAllNames = formatNames != FormatNames;
                FormatNames = formatNames;
                
                if (TranslateEnabled)
                {
                    TranslationKeys.Clear();
                    ini.GetKeys("translation", TranslationKeys);
                    foreach (var key in TranslationKeys)
                    {
                        var lowerKey = key.Name.ToLower();
                        if (Translation.ContainsKey(lowerKey))
                        {
                            var translated = ini.Get(key).ToString();
                            if (Translation[lowerKey] == translated) 
                                continue;

                            Translation[lowerKey] = translated;
                        }
                        else
                        {
                            Translation.Add(lowerKey, ini.Get(key).ToString());
                        }
                        
                        if (!updateAllNames && Stock.ContainsKey(lowerKey))
                        {
                            UpdateNaturalName(Stock[lowerKey]);
                        }
                    }
                }
                else
                {
                    Translation.Clear();
                }

                if (updateAllNames)
                    foreach (var item in Stock.Values) 
                        UpdateNaturalName(item);
            }
        }

        private void RenderScreens()
        {
            Echo(Version);
            Echo(Screens.Count + " screens");
            Echo(Containers.Count + " blocks with inventories");
            Echo(Stock.Count + " items being tracked");
            Echo("Saving " + (StoreKnownTypes ? "enabled" : "disabled"));
            Echo("Filtering " + (FilterEnabled ? "enabled" : "disabled"));
            Echo("Separators (global default) " + (separators ? "enabled" : "disabled"));
            Echo("Translation " + (TranslateEnabled ? "enabled" : "disabled"));
            foreach (var display in Screens)
            {
                display.Render(Stock);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.Once) == UpdateType.Once)
            {
                RunItemCounter();
            }
            if ((updateSource & UpdateType.Update100) == UpdateType.Update100)
            {
                if (delayCounter > delay && _stateMachine == null)
                {
                    if (rebuild)
                    {
                        rebuild = false;
                        ReadConfig();
                        GetBlocks();
                    }

                    if (clear)
                    {
                        clear = false;
                        _stateMachine = RemoveEmptyItems();
                    }
                    else
                    {
                        _stateMachine = CountItems();
                    }

                    delayCounter = 0;
                    RunItemCounter();
                }
                else
                {
                    ++delayCounter;
                }
            }
            if (argument == "count" && _stateMachine == null)
            {
                _stateMachine = CountItems();
                RunItemCounter();
            }
            switch (argument)
            {
                case "rebuild":
                    rebuild = true;
                    break;
                case "clear":
                    clear = true;
                    break;
            }
        }

        public void Save()
        {
            if(!StoreKnownTypes)
                return;

            ini.TryParse(Storage);
            knownItemsSb.Clear();
            Stock.Keys.ToList().ForEach(key => knownItemsSb.AppendLine(key));
            ini.Set(ConfigSection, KnowItemsString, knownItemsSb.ToString());
            Storage = ini.ToString();
        }
        
        public static string PrettifyString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var result = new StringBuilder();
            result.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]) && !char.IsWhiteSpace(input[i - 1]))
                {
                    result.Append(' ');
                }
                result.Append(input[i]);
            }

            return result.ToString();
        }
        
        public static string MetricFormat(int input)
        {
            if (input >= 1000000000)
                // Congratulations, you've successfully created a singularity
                return (input / 1000000000d).ToString("0.00") + "G"; 
            if (input >= 10000000)
                return (input / 1000000d).ToString("0.00") + "M";
            if (input >= 10000)
                return (input / 1000d).ToString("0.00") + "k";
            
            return input.ToString();
        }
    }
}
