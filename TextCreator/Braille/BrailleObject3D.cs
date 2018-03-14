﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System.ComponentModel;
using System.IO;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.DataConverters3D;
using MatterHackers.MatterControl.DesignTools.Operations;
using MatterHackers.MatterControl.Plugins.BrailleBuilder;
using MatterHackers.VectorMath;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MatterHackers.MatterControl.DesignTools
{
	[WebPageLink("About Braille", "https://en.wikipedia.org/wiki/Braille")]
	public class BrailleObject3D : Object3D, IRebuildable
	{
		public BrailleObject3D()
		{
		}

		public BrailleObject3D(string textToEncode)
		{
			TextToEncode = textToEncode;
			Rebuild(null);
		}

		public static BrailleObject3D Create()
		{
			var item = new BrailleObject3D();

			item.Rebuild(null);
			return item;
		}

		public override string ActiveEditor => "PublicPropertyEditor";

		[DisplayName("Name")]
		public string TextToEncode { get; set; } = "Braille";

		public double BaseHeight { get; set; } = 3;

		[Description("Use Braille grade 2 (contractions)")]
		public bool UseGrade2 { get; set; }

		public bool RenderAsBraille { get; set; } = true;

		public bool AddHook { get; set; }

		static TypeFace typeFace = TypeFace.LoadFrom(AggContext.StaticData.ReadAllText(Path.Combine("Fonts", "Braille.svg")));


		public void Rebuild(UndoBuffer undoBuffer)
		{
			var aabb = this.GetAxisAlignedBoundingBox();

			this.Children.Modify(list =>
			{
				list.Clear();
			});

			var brailleText = TextToEncode;
			if (UseGrade2)
			{
				brailleText = BrailleGrade2.ConvertString(brailleText);
			}

			double pointSize = 18.5;
			double pointsToMm = 0.352778;
			IObject3D textObject = new Object3D();
			var offest = 0.0;

			TypeFacePrinter textPrinter;
			if (RenderAsBraille)
			{
				textPrinter = new TypeFacePrinter(brailleText, new StyledTypeFace(typeFace, pointSize));
			}
			else
			{
				textPrinter = new TypeFacePrinter(brailleText, new StyledTypeFace(ApplicationController.MonoSpacedTypeFace, pointSize));
			}

			foreach (var letter in brailleText.ToCharArray())
			{
				IObject3D letterObject;
				TypeFacePrinter letterPrinter;
				if (RenderAsBraille)
				{
					letterPrinter = new TypeFacePrinter(letter.ToString(), new StyledTypeFace(typeFace, pointSize));
					var scalledLetterPrinter = new VertexSourceApplyTransform(letterPrinter, Affine.NewScaling(pointsToMm));

					// add all the spheres to letterObject
					letterObject = new Object3D();

					var vertexCount = 0;
					var positionSum = Vector2.Zero;
					var lastPosition = Vector2.Zero;
					// find each dot outline and get it's center and place a sphere there
					foreach (var vertex in scalledLetterPrinter.Vertices())
					{
						switch (vertex.command)
						{
							case Agg.ShapePath.FlagsAndCommand.CommandStop:
							case Agg.ShapePath.FlagsAndCommand.CommandEndPoly:
							case Agg.ShapePath.FlagsAndCommand.FlagClose:
							case Agg.ShapePath.FlagsAndCommand.CommandMoveTo:
								if (vertexCount > 0)
								{
									var center = positionSum / vertexCount;
									double radius = 1.44 / 2;// (center - lastPosition).Length;
									var sphere = new HalfSphereObject3D(radius * 2, 15)
									{
										Color = Color.LightBlue
									};
									sphere.Translate(center.X, center.Y);
									letterObject.Children.Add(sphere);
								}
								vertexCount = 0;
								positionSum = Vector2.Zero;
								break;
							case Agg.ShapePath.FlagsAndCommand.CommandCurve3:
							case Agg.ShapePath.FlagsAndCommand.CommandCurve4:
							case Agg.ShapePath.FlagsAndCommand.CommandLineTo:
								vertexCount++;
								lastPosition = vertex.position;
								positionSum += lastPosition;
								break;
						}
					}
				}
				else
				{
					letterPrinter = new TypeFacePrinter(letter.ToString(), new StyledTypeFace(ApplicationController.MonoSpacedTypeFace, pointSize));
					var scalledLetterPrinter = new VertexSourceApplyTransform(letterPrinter, Affine.NewScaling(pointsToMm));
					letterObject = new Object3D()
					{
						Mesh = VertexSourceToMesh.Extrude(scalledLetterPrinter, 1),
						Color = Color.LightBlue
					};
				}

				letterObject.Matrix = Matrix4X4.CreateTranslation(offest, 0, 0);
				textObject.Children.Add(letterObject);

				offest += letterPrinter.GetSize(letter.ToString()).X * pointsToMm;
			}

			// add a plate under the dots
			var padding = .9 * pointSize * pointsToMm / 2;
			var size = textPrinter.LocalBounds * pointsToMm;

			// make the base
			var basePath = new VertexStorage();
			basePath.MoveTo(0, 0);
			basePath.LineTo(size.Width + padding, 0);
			basePath.LineTo(size.Width + padding, size.Height + padding);
			basePath.LineTo(padding, size.Height + padding);
			basePath.LineTo(0, size.Height);

			IObject3D basePlate = new Object3D()
			{
				Mesh = VertexSourceToMesh.Extrude(basePath, BaseHeight)
			};

			basePlate = new Align(basePlate, Face.Top, textObject, Face.Bottom, 0, 0, .01);
			basePlate = new Align(basePlate, Face.Left | Face.Front,
				size.Left - padding/2,
				size.Bottom - padding/2);
			this.Children.Add(basePlate);

			basePlate.Matrix *= Matrix4X4.CreateRotationX(MathHelper.Tau / 4);

			// add an optional chain hook
			if (AddHook)
			{
				// x 10 to make it smoother
				double edgeWidth = 2;
				double height = basePlate.ZSize();
				IVertexSource leftSideObject = new RoundedRect(0, 0, height / 2, height, 0)
				{
					ResolutionScale = 10
				};

				IVertexSource cicleObject = new Ellipse(0, 0, height / 2, height / 2)
				{
					ResolutionScale = 10
				};

				cicleObject = new Align2D(cicleObject, Side2D.Left | Side2D.Bottom, leftSideObject, Side2D.Left | Side2D.Bottom, -.01);
				IVertexSource holeObject = new Ellipse(0, 0, height / 2 - edgeWidth, height / 2 - edgeWidth)
				{
					ResolutionScale = 10
				};
				holeObject = new SetCenter2D(holeObject, cicleObject.Bounds().Center);

				IVertexSource hookPath = leftSideObject.Plus(cicleObject);
				hookPath = hookPath.Minus(holeObject);

				IObject3D chainHook = new Object3D()
				{
					Mesh = VertexSourceToMesh.Extrude(hookPath, BaseHeight),
					Matrix = Matrix4X4.CreateRotationX(MathHelper.Tau / 4)
				};

				chainHook = new Align(chainHook, Face.Left | Face.Bottom | Face.Back, basePlate, Face.Right | Face.Bottom | Face.Back, -.01);

				this.Children.Add(chainHook);
			}

			// add the object that is the dots
			this.Children.Add(textObject);
			textObject.Matrix *= Matrix4X4.CreateRotationX(MathHelper.Tau / 4);

			if (aabb.ZSize > 0)
			{
				// If the part was already created and at a height, maintain the height.
				PlatingHelper.PlaceMeshAtHeight(this, aabb.minXYZ.Z);
			}
		}
	}
}