/* OBJ4FNA - Wavefront OBJ Container for FNA
 *
 * Copyright (c) 2024 Ethan Lee
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the authors be held liable for any damages arising from
 * the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 * claim that you wrote the original software. If you use this software in a
 * product, an acknowledgment in the product documentation would be
 * appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not be
 * misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source distribution.
 *
 * Ethan "flibitijibibo" Lee <flibitijibibo@flibitijibibo.com>
 *
 */

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using ImGuiNET;
using ImGuiNET.SampleProgram.XNA;

class Example : Game
{
	OBJ4FNA sampleModel;
	BasicEffect fixedFunction;

	Vector2 spinnies;
	float distance;
	int currentWheel;

	bool editorEnabled;
	ImGuiRenderer imGuiRenderer;
	KeyboardState kbdPrev;

	Example() : base()
	{
		GraphicsDeviceManager gdm = new GraphicsDeviceManager(this);
		gdm.PreferredBackBufferWidth = 1280;
		gdm.PreferredBackBufferHeight = 720;
		IsMouseVisible = true;
		Window.AllowUserResizing = true;
	}

	protected override void LoadContent()
	{
		sampleModel = new OBJ4FNA(GraphicsDevice, "model.obj", "model.png");

		fixedFunction = new BasicEffect(GraphicsDevice);
		fixedFunction.TextureEnabled = true;
		fixedFunction.DiffuseColor = Vector3.One;
		fixedFunction.SpecularPower = 1.0f;
		fixedFunction.Alpha = 1.0f;

		distance = 2.0f;
		currentWheel = Mouse.GetState().ScrollWheelValue;

		imGuiRenderer = new ImGuiRenderer(this);
		imGuiRenderer.RebuildFontAtlas();

		base.LoadContent();
	}

	protected override void UnloadContent()
	{
		if (sampleModel != null)
		{
			sampleModel.Dispose();
		}
		if (fixedFunction != null)
		{
			fixedFunction.Dispose();
		}

		base.UnloadContent();
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);

		GraphicsDevice.BlendState = BlendState.NonPremultiplied;
		GraphicsDevice.DepthStencilState = DepthStencilState.Default;
		GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
		GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
		GraphicsDevice.SetVertexBuffer(sampleModel.VertexBuffer);

		fixedFunction.Texture = sampleModel.Texture;

		// FIXME: The Y axis feels funny, but I can't into math
		fixedFunction.World = (
			Matrix.CreateTranslation(0.0f, -(sampleModel.Height / 2.0f), 0.0f) *
			Matrix.CreateRotationY(MathHelper.ToRadians(spinnies.X)) *
			Matrix.CreateRotationX(MathHelper.ToRadians(spinnies.Y))
		);
		fixedFunction.View = Matrix.CreateLookAt(
			Vector3.One * distance,
			Vector3.Zero,
			Vector3.Up
		);
		fixedFunction.Projection = Matrix.CreatePerspectiveFieldOfView(
			MathHelper.PiOver2,
			GraphicsDevice.Viewport.AspectRatio,
			1.0f,
			10000.0f
		);

		foreach (EffectPass p in fixedFunction.CurrentTechnique.Passes)
		{
			p.Apply();
			GraphicsDevice.DrawPrimitives(
				PrimitiveType.TriangleList,
				0,
				sampleModel.VertexBuffer.VertexCount / 3
			);
		}

		if (editorEnabled)
		{
			imGuiRenderer.BeforeLayout(gameTime);
			DrawEditor();
			imGuiRenderer.AfterLayout();
		}

		base.Draw(gameTime);
	}

	protected override void Update(GameTime gameTime)
	{
		KeyboardState kbd = Keyboard.GetState();
		if (kbd.IsKeyDown(Keys.OemTilde) && !kbdPrev.IsKeyDown(Keys.OemTilde))
		{
			editorEnabled = !editorEnabled;
		}
		kbdPrev = kbd;

		if (editorEnabled)
		{
			Mouse.IsRelativeMouseModeEXT = false;
			base.Update(gameTime);
			return;
		}

		MouseState state = Mouse.GetState();
		if (Mouse.IsRelativeMouseModeEXT)
		{
			spinnies.X += state.X;
			spinnies.Y += state.Y;
		}
		if (state.ScrollWheelValue > currentWheel)
		{
			distance -= 0.1f;
			currentWheel = state.ScrollWheelValue;
		}
		else if (state.ScrollWheelValue < currentWheel)
		{
			distance += 0.1f;
			currentWheel = state.ScrollWheelValue;
		}
		Mouse.IsRelativeMouseModeEXT = state.LeftButton == ButtonState.Pressed;

		base.Update(gameTime);
	}

	bool enableTexturing = true;
	bool enableLighting;
	bool perPixelLighting;
	System.Numerics.Vector3 diffuseColor = System.Numerics.Vector3.One;
	System.Numerics.Vector3 emissiveColor;
	System.Numerics.Vector3 specularColor;
	float specularPower = 1.0f;
	float materialAlpha = 1.0f;
	System.Numerics.Vector3 ambientLightColor;
	int currentLight;
	bool enabled0 = true, enabled1, enabled2;
	System.Numerics.Vector3 direction0, direction1, direction2;
	System.Numerics.Vector3 diffuseColor0, diffuseColor1, diffuseColor2;
	System.Numerics.Vector3 specularColor0, specularColor1, specularColor2;

	void DrawEditor()
	{
		ImGui.SetNextWindowPos(
			new System.Numerics.Vector2(815, 80),
			ImGuiCond.FirstUseEver
		);
		ImGui.SetNextWindowSize(
			new System.Numerics.Vector2(440, 460),
			ImGuiCond.FirstUseEver
		);
		if (ImGui.Begin("BasicEffect Editor"))
		{
			ImGui.Text("Debug Options:");
			if (ImGui.Checkbox("Enable texturing", ref enableTexturing))
			{
				fixedFunction.TextureEnabled = enableTexturing;
			}
			if (ImGui.Checkbox("Enable lighting", ref enableLighting))
			{
				fixedFunction.LightingEnabled = enableLighting;
			}
			if (ImGui.Checkbox("Per-pixel lighting", ref perPixelLighting))
			{
				fixedFunction.PreferPerPixelLighting = perPixelLighting;
			}
;
			ImGui.Separator();

			ImGui.Text("Material Options:");
			if (ImGui.ColorEdit3("Diffuse", ref diffuseColor))
			{
				fixedFunction.DiffuseColor = new Vector3(
					diffuseColor.X,
					diffuseColor.Y,
					diffuseColor.Z
				);
			}
			if (ImGui.ColorEdit3("Emissive", ref emissiveColor))
			{
				fixedFunction.EmissiveColor = new Vector3(
					emissiveColor.X,
					emissiveColor.Y,
					emissiveColor.Z
				);
			}
			if (ImGui.ColorEdit3("Specular", ref specularColor))
			{
				fixedFunction.SpecularColor = new Vector3(
					specularColor.X,
					specularColor.Y,
					specularColor.Z
				);
			}
			if (ImGui.SliderFloat("Specular Power", ref specularPower, 0.0f, 1.0f))
			{
				fixedFunction.SpecularPower = specularPower;
			}
			if (ImGui.SliderFloat("Alpha", ref materialAlpha, 0.0f, 1.0f))
			{
				fixedFunction.Alpha = materialAlpha;
			}

			ImGui.Separator();

			ImGui.Text("Light Options:");
			if (ImGui.ColorEdit3("Ambient Light Color", ref ambientLightColor))
			{
				fixedFunction.AmbientLightColor = new Vector3(
					ambientLightColor.X,
					ambientLightColor.Y,
					ambientLightColor.Z
				);
			}
			if (ImGui.RadioButton("Directional Light 1", currentLight == 0))
			{
				currentLight = 0;
			}
			if (ImGui.RadioButton("Directional Light 2", currentLight == 1))
			{
				currentLight = 1;
			}
			if (ImGui.RadioButton("Directional Light 3", currentLight == 2))
			{
				currentLight = 2;
			}
			if (currentLight == 0)
			{
				DirectionalLightEditor(
					fixedFunction.DirectionalLight0,
					ref enabled0,
					ref direction0,
					ref diffuseColor0,
					ref specularColor0
				);
			}
			else if (currentLight == 1)
			{
				DirectionalLightEditor(
					fixedFunction.DirectionalLight1,
					ref enabled1,
					ref direction1,
					ref diffuseColor1,
					ref specularColor1
				);
			}
			else if (currentLight == 2)
			{
				DirectionalLightEditor(
					fixedFunction.DirectionalLight2,
					ref enabled2,
					ref direction2,
					ref diffuseColor2,
					ref specularColor2
				);
			}

			ImGui.End();
		}
	}

	void DirectionalLightEditor(
		DirectionalLight light,
		ref bool enabled,
		ref System.Numerics.Vector3 direction,
		ref System.Numerics.Vector3 diffuse,
		ref System.Numerics.Vector3 specular
	) {
		if (ImGui.Checkbox("Enabled", ref enabled))
		{
			light.Enabled = enabled;
		}
		if (ImGui.SliderFloat3("Direction", ref direction, -1, 1))
		{
			if (direction.X != 0 || direction.Y != 0 || direction.Z != 0)
			{
				Vector3 dir = new Vector3(
					direction.X,
					direction.Y,
					direction.Z
				);
				dir.Normalize();
				light.Direction = dir;
			}
		}
		if (ImGui.ColorEdit3("Diffuse Color", ref diffuse))
		{
			light.DiffuseColor = new Vector3(
				diffuse.X,
				diffuse.Y,
				diffuse.Z
			);
		}
		if (ImGui.ColorEdit3("Specular Color", ref specular))
		{
			light.SpecularColor = new Vector3(
				specular.X,
				specular.Y,
				specular.Z
			);
		}
	}

	static void Main(string[] args)
	{
		using (Example e = new Example())
		{
			e.Run();
		}
	}
}
