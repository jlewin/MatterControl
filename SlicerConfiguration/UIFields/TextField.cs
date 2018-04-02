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
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.SlicerConfiguration
{

	public class FunkySlider : IgnoredPopupWidget
	{
		private class PresetItem
		{
			public string Name { get; set; }
			public double Value { get; set; }
			public IVertexSource VertexSource { get; set; }
			public RectangleDouble Bounds { get; set; }
		}

		private SolidSlider layerSlider;

		private double minimum = 0;
		private double maximum = 1;
		private double range;
		private int smallBarHeight;
		private double targetX;
		private double targetY;
		private double width;
		private Color highlightA = new Color(Color.White, 200);
		private Color highlightB = new Color(Color.White, 180);

		private ThemeConfig theme;
		private List<(string MenuName, double value)> sortedItems;

		private List<PresetItem> presetItems;

		public FunkySlider(SliceSettingData settingsData, ThemeConfig theme)
		{
			this.theme = theme;

			sortedItems = settingsData.QuickMenuSettings.Select(nameValue => (nameValue.MenuName, value: double.Parse(nameValue.Value))).OrderBy(x => x.value).ToList();

			if (sortedItems.Count >= 2)
			{
				minimum = sortedItems.Min(m => m.value) - settingsData.PlusOrMinus;
				maximum = sortedItems.Max(m => m.value) + settingsData.PlusOrMinus;
			}

			var textEditWidget = new MHTextEditWidget("", pixelWidth: 50/* pixelWidth: ControlWidth, tabIndex: tabIndex*/)
			{
				SelectAllOnFocus = true,
				Name = this.Name,
				VAnchor = VAnchor.Bottom,
				HAnchor = HAnchor.Right
			};
			this.AddChild(textEditWidget);

			layerSlider = new SolidSlider(new Vector2(), 8, 0, 1, Orientation.Horizontal)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Top,
				//Margin = new BorderDouble(5),
				Minimum = minimum,
				Maximum = maximum,
				Value = 0.3
			};
			layerSlider.ValueChanged += (s, e) =>
			{
				textEditWidget.Text = layerSlider.Value.ToString();
			};
			this.AddChild(layerSlider);

			range = maximum - minimum;
			smallBarHeight = 4;
		}

		public override void OnBoundsChanged(EventArgs e)
		{
			targetX = layerSlider.Position.X;
			targetY = layerSlider.Position.Y - layerSlider.LocalBounds.Height + 6;

			width = layerSlider.Width;

			presetItems = new List<PresetItem>();

			foreach (var tuple in sortedItems)
			{
				var rangedValue = tuple.value - minimum;
				var percent = (rangedValue / range);

				double xPos = targetX + width * percent;

				var printer = new TypeFacePrinter(tuple.MenuName, theme.DefaultFontSize * GuiWidget.DeviceScale);
				var rotatedLabel = new VertexSourceApplyTransform(
					printer,
					Affine.NewTranslation(-printer.LocalBounds.Width - 5, -2) * Affine.NewRotation(MathHelper.DegreesToRadians(40)));

				rotatedLabel.Transform = (Affine)rotatedLabel.Transform * Affine.NewTranslation(xPos, targetY - smallBarHeight);

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

			for (int i = 1; i < 10; i++)
			{
				double xPos = targetX + step * i;

				graphics2D.Line(
					xPos,
					targetY - smallBarHeight,
					xPos,
					targetY + smallBarHeight,
					highlightB);
			}

			graphics2D.Line(
				targetX + 1,
				targetY - smallBarHeight - 3,
				targetX + 1,
				targetY + smallBarHeight,
				highlightB);

			graphics2D.Line(
				targetX + width - 2,
				targetY - smallBarHeight - 3,
				targetX + width - 2,
				targetY + smallBarHeight,
				highlightB);

			foreach (var item in presetItems)
			{
				var rangedValue = item.Value - minimum;
				var percent = (rangedValue / range);

				double xPos = targetX + width * percent;

				graphics2D.Circle(xPos, targetY, 3, Color.White);
				graphics2D.Circle(xPos, targetY, 2, theme.Colors.PrimaryAccentColor);

				graphics2D.Render(item.VertexSource, theme.Colors.PrimaryTextColor);
			}
		}
	}

	public class RangedField : UIField
	{
		public SliceSettingData SettingsData { get; set; }

		public override void Initialize(int tabIndex)
		{
			GuiWidget container;

			var theme = ApplicationController.Instance.Theme;

			PopupMenuButton popup;

			this.Content = popup = new PopupMenuButton("RangedDrop", theme)
			{
				Margin = new BorderDouble(right: 5, bottom: 2), // TODO: Bottom margin required as VAnchor is having no effect
				Padding = new BorderDouble(2, 0),
				AlignToRightEdge = true,
				DrawArrow = true,
				PopupContent = container = new FunkySlider(this.SettingsData, theme)
				{
					MinimumSize = new Vector2(400, 100),
					//Padding = 10,
					BackgroundColor = theme.ResolveColor(theme.TabBodyBackground, theme.SlightShade)
				}
			};

			container.BackgroundColor = theme.ResolveColor(theme.Colors.PrimaryBackgroundColor, popup.HoverColor);

			base.Initialize(tabIndex);
		}

		protected override void OnValueChanged(FieldChangedEventArgs fieldChangedEventArgs)
		{
			//if (this.Value != layerSlider.Value.ToString())
			//{
			//	if (double.TryParse(this.Value, out double result))
			//	{
			//		layerSlider.Value = result;
			//	}
			//	else
			//	{
			//		// TODO: Show a conversion/invalid value error
			//	}
			//}

			base.OnValueChanged(fieldChangedEventArgs);
		}
	}

	public class TextField : UIField
	{
		protected MHTextEditWidget textEditWidget;

		public override void Initialize(int tabIndex)
		{
			textEditWidget = new MHTextEditWidget("", pixelWidth: ControlWidth, tabIndex: tabIndex)
			{
				ToolTipText = this.HelpText,
				SelectAllOnFocus = true,
				Name = this.Name,
			};
			textEditWidget.ActualTextEditWidget.EditComplete += (s, e) =>
			{
				if (this.Value != textEditWidget.Text)
				{
					this.SetValue(
						textEditWidget.Text, 
						userInitiated: true);
				}
			};

			this.Content = textEditWidget;
		}


		protected override void OnValueChanged(FieldChangedEventArgs fieldChangedEventArgs)
		{
			if (this.Value != textEditWidget.Text)
			{
				textEditWidget.Text = this.Value;
			}

			base.OnValueChanged(fieldChangedEventArgs);
		}
	}
}
