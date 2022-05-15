using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System;

namespace IngameScript
{
    partial class Program
    {

        class ManagedDisplay
        {
            private IMyTextSurface surface;
            private RectangleF viewport;
            private MySpriteDrawFrame frame;
            private float StartHeight = 0f;
            private float HeadingHeight = 35f;
            private float LineHeight = 30f;
            private float HeadingFontSize = 1.3f;
            private float RegularFontSize = 1.0f;
            private Vector2 Position;
            private int WindowSize;         // Number of lines shown on screen at once after heading
            private Color HighlightColor;
            private int linesToSkip;
            private bool monospace;
            private readonly String SpritePrefix = "MyObjectBuilder_Component/";
            private bool MakeSpriteCacheDirty = false;
            private string previousType = "";

            public ManagedDisplay(IMyTextSurface surface, float scale = 1.0f, Color highlightColor = new Color(), int linesToSkip = 0, bool monospace = false)
            {
                this.surface = surface;
                this.HighlightColor = highlightColor;
                this.linesToSkip = linesToSkip;
                this.monospace = monospace;

                // Scale everything!
                StartHeight *= scale;
                HeadingHeight *= scale;
                LineHeight *= scale;
                HeadingFontSize *= scale;
                RegularFontSize *= scale;

                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "TSS_FactionIcon";
                Vector2 padding = surface.TextureSize * (surface.TextPadding / 100);
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f + padding, surface.SurfaceSize - (2*padding));
                WindowSize = ((int)((viewport.Height - 10 * scale) / LineHeight));
            }

            private void AddHeading()
            {
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;
                Position = new Vector2(viewport.Width / 10f, StartHeight) + viewport.Position;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds",
                    Position = Position + new Vector2(0f,LineHeight/2f),
                    Size = new Vector2(LineHeight,LineHeight),
                    RotationOrScale = HeadingFontSize,
                    Color = HighlightColor,
                    Alignment = TextAlignment.CENTER
                });
                Position.X += viewport.Width / 8f;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Item",
                    Position = Position,
                    RotationOrScale = HeadingFontSize,
                    Color = HighlightColor,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
                Position.X += viewport.Width * 6f / 8f;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Stock",
                    Position = Position,
                    RotationOrScale = HeadingFontSize,
                    Color = HighlightColor,
                    Alignment = TextAlignment.RIGHT,
                    FontId = "White"
                });
                Position.Y += HeadingHeight;
            }
            private void RenderRow(Program.Item item)
            {
                Color TextColor;
                if (item.Amount == 0)
                {
                    TextColor = Color.Brown;
                }
                else
                {
                    TextColor = Color.Gray;
                }
                if (previousType != item.ItemType)
                {
                    previousType = item.ItemType;
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = new Vector2(viewport.X,Position.Y),
                        Size = new Vector2(viewport.Width, 1),
                        RotationOrScale = 0,
                        Color = HighlightColor,
                    });
                }
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = item.Sprite,
                    Position = Position + new Vector2(0f,LineHeight/2f),
                    Size = new Vector2(LineHeight,LineHeight),
                    RotationOrScale = 0,
                    Color = TextColor,
                    Alignment = TextAlignment.CENTER,
                });
                Position.X += viewport.Width / 8f;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = item.Name,
                    Position = Position,
                    RotationOrScale = RegularFontSize,
                    Color = TextColor,
                    Alignment = TextAlignment.LEFT,
                    FontId = monospace?"Monospace":"White"
                });
                Position.X += viewport.Width * 6f / 8f;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = item.Amount.ToString(),
                    Position = Position,
                    RotationOrScale = RegularFontSize,
                    Color = TextColor,
                    Alignment = TextAlignment.RIGHT,
                    FontId = monospace?"Monospace":"White"
                });
            }

            internal void Render(SortedDictionary<String, Program.Item> Stock)
            {
                MakeSpriteCacheDirty = !MakeSpriteCacheDirty;
                frame = surface.DrawFrame();
                if (MakeSpriteCacheDirty)
                {
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Color = surface.BackgroundColor,
                    });
                }
                AddHeading();
                int renderLineCount = 0;
                foreach (var item in Stock.Keys)
                {
                    if (++renderLineCount > linesToSkip)
                    {
                        Position.X = viewport.Width / 10f + viewport.Position.X;
                        if (renderLineCount >= linesToSkip && renderLineCount < linesToSkip + WindowSize)
                            RenderRow(Stock[item]);
                        Position.Y += LineHeight;
                    }
                }
                frame.Dispose();
            }
        }
    }
}