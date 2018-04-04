/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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

using System;
using System.Collections.Generic;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
	public class PipsPanel : GuiWidget, IIgnoredPopupChild
	{
		internal class PresetItem
		{
			public string Name { get; set; }
			public double Value { get; set; }
			public IVertexSource VertexSource { get; set; }
			public RectangleDouble Bounds { get; set; }
		}

		private ThemeConfig theme;

		//private double minimum = 0;
		//private double maximum = 1;
		private double range;

		private int smallBarHeight = 8;
		private int tallBarHeight = 11;
		private double barCenter;

		private double targetX;
		private double targetY;
		private double width;
		private Color lineColor;
		private SolidSlider layerSlider;
		private List<(string MenuName, double value)> sortedItems;

		private List<PresetItem> presetItems;

		public PipsPanel(SolidSlider slider, SliceSettingData settingsData, ThemeConfig theme)
		{
			this.DoubleBuffer = true;
			this.theme = theme;
			this.HAnchor = HAnchor.Stretch;
			this.Margin = new BorderDouble(slider.ThumbWidth / 2, 0);

			barCenter = smallBarHeight / 2;
			lineColor = theme.Colors.PrimaryTextColor;

			layerSlider = slider;

			sortedItems = settingsData.QuickMenuSettings.Select(nameValue => (nameValue.MenuName, value: double.Parse(nameValue.Value))).OrderBy(x => x.value).ToList();

			if (sortedItems.Count >= 2)
			{
				slider.Minimum = sortedItems.Min(m => m.value) - settingsData.PlusOrMinus;
				slider.Maximum = sortedItems.Max(m => m.value) + settingsData.PlusOrMinus;
			}

			range = slider.Maximum - slider.Minimum;

			this.MinimumSize = new Vector2(0, 45);
		}

		public override void OnBoundsChanged(EventArgs e)
		{
			targetX = 0;  // layerSlider.Position.X;
			targetY = this.Height; // layerSlider.Position.Y - layerSlider.LocalBounds.Height + 6;

			width = this.Width;

			presetItems = new List<PresetItem>();

			foreach (var tuple in sortedItems)
			{
				var rangedValue = tuple.value -  layerSlider.Minimum;
				var percent = (rangedValue / range);

				double xPos = targetX + width * percent;

				var printer = new TypeFacePrinter(tuple.MenuName, theme.DefaultFontSize * GuiWidget.DeviceScale);
				var rotatedLabel = new VertexSourceApplyTransform(
					printer,
					Affine.NewTranslation(-printer.LocalBounds.Width - 8, -printer.LocalBounds.YCenter) * Affine.NewRotation(MathHelper.DegreesToRadians(40)));

				rotatedLabel.Transform = (Affine)rotatedLabel.Transform * Affine.NewTranslation(xPos, targetY - barCenter);

				presetItems.Add(new PresetItem()
				{
					Value = tuple.value,
					Name = tuple.MenuName,
					VertexSource = rotatedLabel,
					Bounds = rotatedLabel.GetBounds()
				});
			}

			base.OnBoundsChanged(e);
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			base.OnDraw(graphics2D);

			var step = width / 10;

			// Draw steps
			for (int i = 1; i < 10; i++)
			{
				double xPos = targetX + step * i;

				graphics2D.Line(
					xPos,
					targetY,
					xPos,
					targetY - smallBarHeight,
					lineColor);
			}

			// Draw min line
			graphics2D.Line(
				targetX,
				targetY,
				targetX,
				targetY - tallBarHeight,
				Color.Red);

			// Draw max line
			graphics2D.Line(
				width,
				targetY,
				width,
				targetY - tallBarHeight,
				lineColor);

			// Draw presets
			foreach (var item in presetItems)
			{
				var rangedValue = item.Value - layerSlider.Minimum;
				var percent = (rangedValue / range);

				double xPos = targetX + width * percent;

				graphics2D.Circle(xPos, targetY - barCenter, 4, Color.White);
				graphics2D.Circle(xPos, targetY - barCenter, 3, theme.Colors.PrimaryAccentColor);

				graphics2D.Render(item.VertexSource, theme.Colors.PrimaryTextColor);
			}
		}
	}
}
