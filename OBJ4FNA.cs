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

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class OBJ4FNA
{
	public readonly VertexBuffer VertexBuffer;
	public readonly Texture2D Texture;
	public readonly float Height;

	public OBJ4FNA(GraphicsDevice device, string objPath, string pngPath)
	{
		string[] obj = File.ReadAllLines(objPath);

		// Do a first pass to avoid requiring stretchy buffers
		int vCount = 0;
		int vtCount = 0;
		int vnCount = 0;
		int fCount = 0;
		foreach (string line in obj)
		{
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			if (line.StartsWith("v "))
			{
				vCount += 1;
			}
			else if (line.StartsWith("vt "))
			{
				vtCount += 1;
			}
			else if (line.StartsWith("vn "))
			{
				vnCount += 1;
			}
			else if (line.StartsWith("f "))
			{
				fCount += 9;
			}
		}

		// Parsing results
		Vector3[] list_v = new Vector3[vCount];
		Vector2[] list_vt = new Vector2[vtCount];
		Vector3[] list_vn = new Vector3[vnCount];
		int[] list_i = new int[fCount];
		VertexPositionNormalTexture[] buffer = new VertexPositionNormalTexture[fCount / 3];

		// Lots of `fixed` blocks to avoid unnecessary bounds checks
		unsafe {
		fixed (Vector3* pV = &list_v[0]) {
		fixed (Vector2* pVT = &list_vt[0]) {
		fixed (Vector3* pVN = &list_vn[0]) {
		fixed (int* pI = &list_i[0]) {
		fixed (VertexPositionNormalTexture* pBuf = &buffer[0]) {

		// Parse!
		vCount = 0;
		vtCount = 0;
		vnCount = 0;
		fCount = 0;
		foreach (string line in obj)
		{
			if (string.IsNullOrEmpty(line))
			{
				continue;
			}

			string[] args = line.Split(' ');
			if (args[0] == "v")
			{
				pV[vCount++] = new Vector3(
					float.Parse(args[1]),
					float.Parse(args[2]),
					float.Parse(args[3])
				);
			}
			else if (args[0] == "vt")
			{
				// FIXME: Is there an export option that avoids flipping Y?
				pVT[vtCount++] = new Vector2(
					float.Parse(args[1]),
					1 - float.Parse(args[2])
				);
			}
			else if (args[0] == "vn")
			{
				pVN[vnCount++] = new Vector3(
					float.Parse(args[1]),
					float.Parse(args[2]),
					float.Parse(args[3])
				);
			}
			else if (args[0] == "f")
			{
				string[] i1 = args[1].Split('/');
				string[] i2 = args[2].Split('/');
				string[] i3 = args[3].Split('/');

				pI[fCount++] = int.Parse(i1[0]) - 1;
				pI[fCount++] = int.Parse(i1[1]) - 1;
				pI[fCount++] = int.Parse(i1[2]) - 1;

				pI[fCount++] = int.Parse(i2[0]) - 1;
				pI[fCount++] = int.Parse(i2[1]) - 1;
				pI[fCount++] = int.Parse(i2[2]) - 1;

				pI[fCount++] = int.Parse(i3[0]) - 1;
				pI[fCount++] = int.Parse(i3[1]) - 1;
				pI[fCount++] = int.Parse(i3[2]) - 1;
			}
		}

		// Expand the parse data into a static vertex buffer.
		for (int i = 0, f = 0; i < buffer.Length; i += 1)
		{
			pBuf[i].Position = pV[pI[f++]];
			pBuf[i].TextureCoordinate = pVT[pI[f++]];
			pBuf[i].Normal = pVN[pI[f++]];

			if (pBuf[i].Position.Y > Height)
			{
				Height = pBuf[i].Position.Y;
			}
		}

		// Upload the buffer, finally.
		VertexBuffer = new VertexBuffer(
			device,
			VertexPositionNormalTexture.VertexDeclaration,
			buffer.Length,
			BufferUsage.WriteOnly
		);
		VertexBuffer.SetDataPointerEXT(
			0,
			(IntPtr) pBuf,
			buffer.Length * VertexPositionNormalTexture.VertexDeclaration.VertexStride,
			SetDataOptions.NoOverwrite
		);

		// Okay, we're done with the scary unsafe stuff
		}}}}}}

		// Oh yeah, there's a material too
		using (Stream fileIn = TitleContainer.OpenStream(pngPath))
		{
			Texture = Texture2D.FromStream(device, fileIn);
		}
	}

	public void Dispose()
	{
		if (VertexBuffer != null)
		{
			VertexBuffer.Dispose();
		}
		if (Texture != null)
		{
			Texture.Dispose();
		}
	}
}
