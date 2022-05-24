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
        static readonly string Version = "Version 1.1.0";
        MyIni ini = new MyIni();
        static readonly string ConfigSection = "Inventory";
        static readonly string DisplaySectionPrefix = ConfigSection + "_Display";
        StringBuilder SectionCandidateName = new StringBuilder();
        List<String> SectionNames = new List<string>();
        SortedDictionary<string, Item> Stock = new SortedDictionary<string, Item>();
        List<MyInventoryItem> Items = new List<MyInventoryItem>();
        List<MyIniKey> iniKeys = new List<MyIniKey>();
        List<MyProductionItem> Queue = new List<MyProductionItem>();
        List<ManagedDisplay> Screens = new List<ManagedDisplay>();
        IEnumerator<bool> _stateMachine;
        int delayCounter = 0;
        int delay;
        bool TranslateEnabled; // Enable translate feature globally
        bool FilterEnabled;    // Enable filter feature globally
        bool rebuild = false;

        public class Item
        {
            public Item(MyInventoryItem item, int Amount = 0)
            {
                this.Sprite = item.Type.ToString();
                this.Name = item.Type.SubtypeId;
                this.ItemType = item.Type.TypeId;
                this.Amount = Amount;
            }
            public int Amount;
            public string Sprite;
            public string Name;
            public string ItemType;
        }

        public void GetBlocks()
        {
            Containers.Clear();
            Screens.Clear();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(Containers, block =>
            {
                if (!block.IsSameConstructAs(Me))
                    return false;
                TryAddDiscreteScreens(block);
                TryAddScreen(block);
                if (!block.HasInventory)
                    return false;
                return true;
            });
        }

        void AddScreen(IMyTextSurfaceProvider provider, int displayNumber, string section)
        {
            var display = ((IMyTextSurfaceProvider)provider).GetSurface(displayNumber);
            var linesToSkip = ini.Get(section, "skip").ToInt16();
            bool monospace = ini.Get(section, "mono").ToBoolean();
            float scale = ini.Get(section, "scale").ToSingle(1.0f);
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
            var managedDisplay = new ManagedDisplay(display, scale, color, linesToSkip, monospace);
            if (FilterEnabled)
            {
                managedDisplay.SetFilter(ini.Get(section, "filter").ToString(null));
            }
            Screens.Add(managedDisplay);
        }

        private void TryAddDiscreteScreens(IMyTerminalBlock block)
        {
            IMyTextSurfaceProvider Provider = block as IMyTextSurfaceProvider;
            if (null == Provider || Provider.SurfaceCount == 0)
                return;
            StringComparison ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            ini.TryParse(block.CustomData);
            ini.GetSections(SectionNames);
            foreach (var section in SectionNames)
            {
                if (section.StartsWith(DisplaySectionPrefix, ignoreCase))
                {
                    for (int displayNumber = 0; displayNumber < Provider.SurfaceCount; ++displayNumber)
                    {
                        if (displayNumber < Provider.SurfaceCount || Provider.SurfaceCount == 0)
                        {
                            SectionCandidateName.Clear();
                            SectionCandidateName.Append(DisplaySectionPrefix).Append(displayNumber.ToString());
                            if (section.Equals(SectionCandidateName.ToString(), ignoreCase))
                            {
                                AddScreen(Provider, displayNumber, section);
                            }
                        }
                        else
                        {
                            Echo("Warning: " + block.CustomName + " doesn't have a display number " + ini.Get(ConfigSection, "display").ToString());
                        }
                    }
                }
            }
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
                                Stock.Add(key, new Item(item));
                            Stock[key].Amount += item.Amount.ToIntSafe();
                        }
                        yield return true;
                    }
                }
            }
            EchoStuff();
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            ReadConfig();
            GetBlocks();
        }

        private void ReadConfig()
        {
            if (ini.TryParse(Me.CustomData))
            {
                delay = ini.Get(ConfigSection, "delay").ToInt32(3);
                TranslateEnabled = ini.Get(ConfigSection, "enabletranslate").ToBoolean(false);
                FilterEnabled = ini.Get(ConfigSection, "enablefilter").ToBoolean(false);
            }
        }

        private void EchoStuff()
        {
            Echo(Version);
            Echo(Screens.Count + " screens");
            Echo(Containers.Count + " blocks with inventories");
            Echo(Stock.Count + " items being tracked");
            Echo("Filtering " + (FilterEnabled ? "enabled" : "disabled"));
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
                    delayCounter = 0;
                    _stateMachine = CountItems();
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
            if (argument == "rebuild")
            {
                rebuild = true;
            }
        }
    }
}
