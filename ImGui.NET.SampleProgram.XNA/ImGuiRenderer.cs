using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImGuiNET.SampleProgram.XNA
{
    /// <summary>
    /// ImGui renderer for use with XNA-likes (FNA & MonoGame)
    /// </summary>
    public class ImGuiRenderer
    {
        private Game _game;

        // Graphics
        private GraphicsDevice _graphicsDevice;

        private BasicEffect _effect;
        private RasterizerState _rasterizerState;

        private byte[] _vertexData;
        private DynamicVertexBuffer _vertexBuffer;
        private int _vertexBufferSize;

        private byte[] _indexData;
        private DynamicIndexBuffer _indexBuffer;
        private int _indexBufferSize;

        // Textures
        private Dictionary<IntPtr, Texture2D> _loadedTextures;

        private int _textureId;
        private IntPtr? _fontTextureId;

        // Input
        private int _scrollWheelValue;
        //private int _horizontalScrollWheelValue;
        private readonly float WHEEL_DELTA = 120;
        private Keys[] _allKeys = (Keys[]) Enum.GetValues(typeof(Keys));

        public ImGuiRenderer(Game game)
        {
            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            _game = game ?? throw new ArgumentNullException(nameof(game));
            _graphicsDevice = game.GraphicsDevice;

            _loadedTextures = new Dictionary<IntPtr, Texture2D>();

            _rasterizerState = new RasterizerState()
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };

            SetupInput();
        }

        #region ImGuiRenderer

        /// <summary>
        /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
        /// </summary>
        public virtual unsafe void RebuildFontAtlas()
        {
            // Get font texture from ImGui
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            // Copy the data to a managed array
            var pixels = new byte[width * height * bytesPerPixel];
            unsafe { Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length); }

            // Create and register the texture as an XNA texture
            var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            tex2d.SetData(pixels);

            // Should a texture already have been build previously, unbind it first so it can be deallocated
            if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);

            // Bind the new texture to an ImGui-friendly id
            _fontTextureId = BindTexture(tex2d);

            // Let ImGui know where to find the texture
            io.Fonts.SetTexID(_fontTextureId.Value);
            io.Fonts.ClearTexData(); // Clears CPU side texture data
        }

        /// <summary>
        /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
        /// </summary>
        public virtual IntPtr BindTexture(Texture2D texture)
        {
            var id = new IntPtr(_textureId++);

            _loadedTextures.Add(id, texture);

            return id;
        }

        /// <summary>
        /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
        /// </summary>
        public virtual void UnbindTexture(IntPtr textureId)
        {
            _loadedTextures.Remove(textureId);
        }

        /// <summary>
        /// Sets up ImGui for a new frame, should be called at frame start
        /// </summary>
        public virtual void BeforeLayout(GameTime gameTime)
        {
            ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            UpdateInput();

            ImGui.NewFrame();
        }

        /// <summary>
        /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, should be called after the UI is drawn using ImGui.** calls
        /// </summary>
        public virtual void AfterLayout()
        {
            ImGui.Render();

            unsafe { RenderDrawData(ImGui.GetDrawData()); }
        }

        #endregion ImGuiRenderer

        #region Setup & Update

        /// <summary>
        /// Setup key input event handler.
        /// </summary>
        protected virtual void SetupInput()
        {
            var io = ImGui.GetIO();

            // MonoGame-specific //////////////////////
            //_game.Window.TextInput += (s, a) =>
            //{
                //if (a.Character == '\t') return;
                //io.AddInputCharacter(a.Character);
            //};

            ///////////////////////////////////////////

            // FNA-specific ///////////////////////////
            TextInputEXT.TextInput += c =>
            {
                if (c == '\t') return;

                ImGui.GetIO().AddInputCharacter(c);
            };
            ///////////////////////////////////////////
        }

        /// <summary>
        /// Updates the <see cref="Effect" /> to the current matrices and texture
        /// </summary>
        protected virtual Effect UpdateEffect(Texture2D texture)
        {
            _effect = _effect ?? new BasicEffect(_graphicsDevice);

            var io = ImGui.GetIO();

            _effect.World = Matrix.Identity;
            _effect.View = Matrix.Identity;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            _effect.TextureEnabled = true;
            _effect.Texture = texture;
            _effect.VertexColorEnabled = true;

            return _effect;
        }

        /// <summary>
        /// Sends XNA input state to ImGui
        /// </summary>
        protected virtual void UpdateInput()
        {
            if (!_game.IsActive) return;
            
            var io = ImGui.GetIO();

            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            io.AddMousePosEvent(mouse.X, mouse.Y);
            io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
            io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

            io.AddMouseWheelEvent(
                0, // (mouse.HorizontalScrollWheelValue - _horizontalScrollWheelValue) / WHEEL_DELTA,
                (mouse.ScrollWheelValue - _scrollWheelValue) / WHEEL_DELTA);
            _scrollWheelValue = mouse.ScrollWheelValue;
            // _horizontalScrollWheelValue = mouse.HorizontalScrollWheelValue;

            foreach (var key in _allKeys)
            {
                if (TryMapKeys(key, out ImGuiKey imguikey))
                {
                    io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
                }
            }

            io.DisplaySize = new System.Numerics.Vector2(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
        }

        private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
        {
            //Special case not handed in the switch...
            //If the actual key we put in is "None", return none and true. 
            //otherwise, return none and false.
            if (key == Keys.None)
            {
                imguikey = ImGuiKey.None;
                return true;
            }

            imguikey = key switch
            {
                Keys.Back => ImGuiKey.Backspace,
                Keys.Tab => ImGuiKey.Tab,
                Keys.Enter => ImGuiKey.Enter,
                Keys.CapsLock => ImGuiKey.CapsLock,
                Keys.Escape => ImGuiKey.Escape,
                Keys.Space => ImGuiKey.Space,
                Keys.PageUp => ImGuiKey.PageUp,
                Keys.PageDown => ImGuiKey.PageDown,
                Keys.End => ImGuiKey.End,
                Keys.Home => ImGuiKey.Home,
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.PrintScreen => ImGuiKey.PrintScreen,
                Keys.Insert => ImGuiKey.Insert,
                Keys.Delete => ImGuiKey.Delete,
                Keys.D0 => ImGuiKey._0,
                Keys.D1 => ImGuiKey._1,
                Keys.D2 => ImGuiKey._2,
                Keys.D3 => ImGuiKey._3,
                Keys.D4 => ImGuiKey._4,
                Keys.D5 => ImGuiKey._5,
                Keys.D6 => ImGuiKey._6,
                Keys.D7 => ImGuiKey._7,
                Keys.D8 => ImGuiKey._8,
                Keys.D9 => ImGuiKey._9,
                Keys.A  => ImGuiKey.A,
                Keys.B  => ImGuiKey.B,
                Keys.C  => ImGuiKey.C,
                Keys.D  => ImGuiKey.D,
                Keys.E  => ImGuiKey.E,
                Keys.F  => ImGuiKey.F,
                Keys.G  => ImGuiKey.G,
                Keys.H  => ImGuiKey.H,
                Keys.I  => ImGuiKey.I,
                Keys.J  => ImGuiKey.J,
                Keys.K  => ImGuiKey.K,
                Keys.L  => ImGuiKey.L,
                Keys.M  => ImGuiKey.M,
                Keys.N  => ImGuiKey.N,
                Keys.O  => ImGuiKey.O,
                Keys.P  => ImGuiKey.P,
                Keys.Q  => ImGuiKey.Q,
                Keys.R  => ImGuiKey.R,
                Keys.S  => ImGuiKey.S,
                Keys.T  => ImGuiKey.T,
                Keys.U  => ImGuiKey.U,
                Keys.V  => ImGuiKey.V,
                Keys.W  => ImGuiKey.W,
                Keys.X  => ImGuiKey.X,
                Keys.Y  => ImGuiKey.Y,
                Keys.Z  => ImGuiKey.Z,
                Keys.NumPad0 => ImGuiKey.Keypad0,
                Keys.NumPad1 => ImGuiKey.Keypad1,
                Keys.NumPad2 => ImGuiKey.Keypad2,
                Keys.NumPad3 => ImGuiKey.Keypad3,
                Keys.NumPad4 => ImGuiKey.Keypad4,
                Keys.NumPad5 => ImGuiKey.Keypad5,
                Keys.NumPad6 => ImGuiKey.Keypad6,
                Keys.NumPad7 => ImGuiKey.Keypad7,
                Keys.NumPad8 => ImGuiKey.Keypad8,
                Keys.NumPad9 => ImGuiKey.Keypad9,
                Keys.Multiply => ImGuiKey.KeypadMultiply,
                Keys.Add => ImGuiKey.KeypadAdd,
                Keys.Subtract => ImGuiKey.KeypadSubtract,
                Keys.Decimal => ImGuiKey.KeypadDecimal,
                Keys.Divide => ImGuiKey.KeypadDivide,
                Keys.F1 => ImGuiKey.F1,
                Keys.F2 => ImGuiKey.F2,
                Keys.F3 => ImGuiKey.F3,
                Keys.F4 => ImGuiKey.F4,
                Keys.F5 => ImGuiKey.F5,
                Keys.F6 => ImGuiKey.F6,
                Keys.F7 => ImGuiKey.F7,
                Keys.F8 => ImGuiKey.F8,
                Keys.F9 => ImGuiKey.F9,
                Keys.F10 => ImGuiKey.F10,
                Keys.F11 => ImGuiKey.F11,
                Keys.F12 => ImGuiKey.F12,
                Keys.F13 => ImGuiKey.F13,
                Keys.F14 => ImGuiKey.F14,
                Keys.F15 => ImGuiKey.F15,
                Keys.F16 => ImGuiKey.F16,
                Keys.F17 => ImGuiKey.F17,
                Keys.F18 => ImGuiKey.F18,
                Keys.F19 => ImGuiKey.F19,
                Keys.F20 => ImGuiKey.F20,
                Keys.F21 => ImGuiKey.F21,
                Keys.F22 => ImGuiKey.F22,
                Keys.F23 => ImGuiKey.F23,
                Keys.F24 => ImGuiKey.F24,
                Keys.NumLock => ImGuiKey.NumLock,
                Keys.Scroll => ImGuiKey.ScrollLock,
                Keys.LeftShift => ImGuiKey.ModShift,
                Keys.LeftControl => ImGuiKey.ModCtrl,
                Keys.LeftAlt => ImGuiKey.ModAlt,
                Keys.OemSemicolon => ImGuiKey.Semicolon,
                Keys.OemPlus => ImGuiKey.Equal,
                Keys.OemComma => ImGuiKey.Comma,
                Keys.OemMinus => ImGuiKey.Minus,
                Keys.OemPeriod => ImGuiKey.Period,
                Keys.OemQuestion => ImGuiKey.Slash,
                Keys.OemTilde => ImGuiKey.GraveAccent,
                Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
                Keys.OemCloseBrackets => ImGuiKey.RightBracket,
                Keys.OemPipe => ImGuiKey.Backslash,
                Keys.OemQuotes => ImGuiKey.Apostrophe,
                Keys.BrowserBack => ImGuiKey.AppBack,
                Keys.BrowserForward => ImGuiKey.AppForward,
                _ => ImGuiKey.None,
            };

            return imguikey != ImGuiKey.None;
        }

        #endregion Setup & Update

        #region Internals

        /// <summary>
        /// Gets the geometry as set up by ImGui and sends it to the graphics device
        /// </summary>
        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
            var lastViewport = _graphicsDevice.Viewport;
            var lastScissorBox = _graphicsDevice.ScissorRectangle;

            _graphicsDevice.BlendFactor = Color.White;
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.RasterizerState = _rasterizerState;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            // Setup projection
            _graphicsDevice.Viewport = new Viewport(0, 0, _graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);

            UpdateBuffers(drawData);

            RenderCommandLists(drawData);

            // Restore modified state
            _graphicsDevice.Viewport = lastViewport;
            _graphicsDevice.ScissorRectangle = lastScissorBox;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        {
            if (drawData.TotalVtxCount == 0)
            {
                return;
            }

            // Expand buffers if we need more room
            if (drawData.TotalVtxCount > _vertexBufferSize)
            {
                _vertexBuffer?.Dispose();

                _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
                _vertexBuffer = new DynamicVertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.WriteOnly);
                _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
            }

            if (drawData.TotalIdxCount > _indexBufferSize)
            {
                _indexBuffer?.Dispose();

                _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
                _indexBuffer = new DynamicIndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.WriteOnly);
                _indexData = new byte[_indexBufferSize * sizeof(ushort)];
            }

            // Copy ImGui's vertices and indices to a set of managed byte arrays
            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];

                fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
                fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
                {
                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            // Copy the managed byte arrays to the gpu vertex- and index buffers
            _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size, SetDataOptions.Discard);
            _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort), SetDataOptions.Discard);
        }

        private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
        {
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];

                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                {
                    ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                    if (drawCmd.ElemCount == 0) 
                    {
                        continue;
                    }

                    if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
                    {
                        throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                    }

                    _graphicsDevice.ScissorRectangle = new Rectangle(
                        (int)drawCmd.ClipRect.X,
                        (int)drawCmd.ClipRect.Y,
                        (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                        (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                    );

                    var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                        _graphicsDevice.DrawIndexedPrimitives(
                            primitiveType: PrimitiveType.TriangleList,
                            baseVertex: (int)drawCmd.VtxOffset + vtxOffset,
                            minVertexIndex: 0,
                            numVertices: cmdList.VtxBuffer.Size,
                            startIndex: (int)drawCmd.IdxOffset + idxOffset,
                            primitiveCount: (int)drawCmd.ElemCount / 3
                        );
#pragma warning restore CS0618
                    }
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }
        }

        #endregion Internals
    }
}
