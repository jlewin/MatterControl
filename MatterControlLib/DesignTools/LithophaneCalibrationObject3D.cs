/*
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
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.VertexSource;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.DesignTools
{
	public class LithophaneCalibrationObject3D : Object3D
	{
		public double NozzleWidth = .4;

		public LithophaneCalibrationObject3D()
		{
			Name = "Lithophane Calibration".Localize();
		}

		public double BaseHeight { get; set; } = .4;

		[DisplayName("Material")]
		public int CalibrationMaterialIndex { get; set; } = 1;

		public override bool CanFlatten => true;
		public double ChangingHeight { get; set; } = .4;
		public int Layers { get; set; } = 10;
		public double Offset { get; set; } = .5;
		public double WipeTowerSize { get; set; } = 10;

		private double TabDepth => NozzleWidth * TabScale * 5;
		private double TabScale => 3;
		private double TabWidth => NozzleWidth * TabScale * 3;

		public static async Task<LithophaneCalibrationObject3D> Create(int calibrationMaterialIndex,
							double baseHeight, double changingHeight, double offset, double nozzleWidth, double wipeTowerSize, int layers)
		{
			var item = new LithophaneCalibrationObject3D()
			{
				WipeTowerSize = wipeTowerSize,
				Layers = layers,
				CalibrationMaterialIndex = calibrationMaterialIndex,
				BaseHeight = baseHeight,
				ChangingHeight = changingHeight,
				Offset = offset,
				NozzleWidth = nozzleWidth
			};

			await item.Rebuild();
			return item;
		}

		public override async void OnInvalidate(InvalidateArgs invalidateType)
		{
			if (invalidateType.InvalidateType.HasFlag(InvalidateType.Properties)
				&& invalidateType.Source == this)
			{
				await Rebuild();
			}
			else
			{
				base.OnInvalidate(invalidateType);
			}
		}

		public override Task Rebuild()
		{
			this.DebugDepth("Rebuild");

			using (RebuildLock())
			{
				using (new CenterAndHeightMantainer(this))
				{
					this.Children.Modify((list) =>
					{
						list.Clear();
					});

					this.Children.Add(
						this.GetTab(false));
				}
			}

			Parent?.Invalidate(new InvalidateArgs(this, InvalidateType.Mesh));
			return Task.CompletedTask;
		}

		private Object3D GetTab(bool calibrateX)
		{
			var content = new Object3D();

			var spaceBetween = NozzleWidth * TabScale;

			// left + spaces + blocks + right
			var sampleCount = 9;
			var baseWidth = (2 * spaceBetween) + ((sampleCount - 1) * spaceBetween) + (sampleCount * TabWidth) + (2 * spaceBetween);

			var shape = new VertexStorage();
			shape.MoveTo(0, 0);
			shape.LineTo(baseWidth, 0);
			shape.LineTo(baseWidth + TabDepth, TabDepth / 2); // a point on the left
			shape.LineTo(baseWidth, TabDepth);
			shape.LineTo(0, TabDepth);

			content.Children.Add(new Object3D()
			{
				Mesh = shape.Extrude(BaseHeight),
				Color = Color.LightBlue
			});

			var position = new Vector2(TabWidth / 2 + 2 * spaceBetween, TabDepth / 2);
			var step = new Vector2(spaceBetween + TabWidth, 0);

			var layerHeight = 0.2;

			int zLayersPerStep = 1;

			var zStep = layerHeight * zLayersPerStep;

			for (int i = 1; i <= sampleCount; i++)
			{
				var mesh = PlatonicSolids.CreateCube(TabWidth, TabDepth, zStep * i);
				mesh.Translate(new Vector3(0, 0, (zStep * i) / 2 + BaseHeight));

				var item = new Object3D()
				{
					Mesh = mesh,
					Matrix = Matrix4X4.CreateTranslation(position.X, position.Y, 0)
				};
				content.Children.Add(item);

				position += step;
			}

			return content;
		}
	}
}