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

class Example : Game
{
	OBJ4FNA sampleModel;
	BasicEffect fixedFunction;

	Vector2 spinnies;
	float distance;
	int currentWheel;

	Example() : base()
	{
		new GraphicsDeviceManager(this);
		IsMouseVisible = true;
	}

	protected override void LoadContent()
	{
		sampleModel = new OBJ4FNA(GraphicsDevice, "model.obj", "model.png");

		fixedFunction = new BasicEffect(GraphicsDevice);
		fixedFunction.TextureEnabled = true;

		distance = 2.0f;
		currentWheel = Mouse.GetState().ScrollWheelValue;

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

		// TODO: Lighting example
		fixedFunction.LightingEnabled = false;
		fixedFunction.DiffuseColor = Vector3.One;

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

		base.Draw(gameTime);
	}

	protected override void Update(GameTime gameTime)
	{
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

	static void Main(string[] args)
	{
		using (Example e = new Example())
		{
			e.Run();
		}
	}
}
