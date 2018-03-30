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
	public class RangedField : UIField
	{
		private SolidSlider layerSlider;

		public SliceSettingData SettingsData { get; set; }

		public override void Initialize(int tabIndex)
		{
			GuiWidget container;

			var theme = ApplicationController.Instance.Theme;

			var xxx = new List<(string name, double value)>();

			foreach (QuickMenuNameValue nameValue in this.SettingsData.QuickMenuSettings)
			{
				xxx.Add((nameValue.MenuName, double.Parse(nameValue.Value)));
			}

			var sorted = xxx.OrderBy(x => x.value);

			double minimum = 0;
			double maximum = 1;

			if (xxx.Count >= 2)
			{
				minimum = xxx.Min(m => m.value) - this.SettingsData.PlusOrMinus;
				maximum = xxx.Max(m => m.value) + this.SettingsData.PlusOrMinus;
			}

			// Wrap content with popup
			var popup = new PopupMenuButton("Something", theme)
			{
				//VAnchor = VAnchor.Center,
				Margin = new BorderDouble(right: 5, bottom: 2), // TODO: Bottom margin required as VAnchor is having no effect
				Padding = new BorderDouble(5, 0),
				AlignToRightEdge = true,
				DrawArrow = true,
				PopupContent = container = new IgnoredPopupWidget()
				{
					MinimumSize = new Vector2(400, 100),
					BackgroundColor = theme.TabBodyBackground
				}
			};

			var textEditWidget = new MHTextEditWidget("", pixelWidth: ControlWidth, tabIndex: tabIndex)
			{
				ToolTipText = this.HelpText,
				SelectAllOnFocus = true,
				Name = this.Name,
			};
			container.AddChild(textEditWidget);

			layerSlider = new SolidSlider(new Vector2(), 8, 0, 1, Orientation.Horizontal)
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Top,
				Margin = new BorderDouble(5),
				Minimum = minimum,
				Maximum = maximum,
				Value = 0.3
			};
			layerSlider.ValueChanged += (s, e) =>
			{
				textEditWidget.Text = layerSlider.Value.ToString();
			};
			container.AddChild(layerSlider);

			var highlightA = new Color(Color.White, 200);
			var highlightB = new Color(Color.White, 180);

			var targetX = layerSlider.Position.X;
			var targetY = layerSlider.Position.Y - layerSlider.LocalBounds.Height - 4;

			container.AfterDraw += (s, e) =>
			{
				var width = layerSlider.Width;

				var range = maximum - minimum;

				foreach(var tuple in sorted)
				{
					var rangedValue = tuple.value - minimum;

					var percent = (rangedValue / range);
					double xPos = targetX + width * percent;

					e.graphics2D.Line(xPos, targetY, xPos, targetY + 10, highlightB);

					var printer = new TypeFacePrinter(tuple.name, 12 * GuiWidget.DeviceScale);
					var rotatedLabel = new VertexSourceApplyTransform(
						printer,
						Affine.NewRotation(MathHelper.DegreesToRadians(45)));

					var textBounds = rotatedLabel.GetBounds();
					var bounds = new RectangleDouble(printer.TypeFaceStyle.DescentInPixels, textBounds.Bottom, printer.TypeFaceStyle.AscentInPixels, textBounds.Top);

					rotatedLabel.Transform = ((Affine)rotatedLabel.Transform) * Affine.NewTranslation(new Vector2(xPos-bounds.Width, targetY - 4 - bounds.Height));

					e.graphics2D.Render(rotatedLabel, theme.Colors.PrimaryTextColor);

					//e.graphics2D.DrawString($"{tuple.name} ({tuple.value})" , xPos, targetY - 14, justification: Agg.Font.Justification.Center, pointSize: 8, color: theme.Colors.PrimaryTextColor);
				}

				e.graphics2D.Line(targetX, targetY, targetX, targetY + 10, Color.Red);

				//var Height = container.Height;
				//var Width = container.Width;

				//for (int i = 0; i < Width / pixelSkip; i++)
				//{
				//	double xPos = Width - ((i * pixelSkip + 0) % Width);
				//	int inset = (int)((i % 2) == 0 ? Height / 6 : Height / 3);
				//	e.graphics2D.Line(xPos, inset, xPos, Height - inset, highlightB);
				//}
			};

			this.Content = popup;

			base.Initialize(tabIndex);
		}

		protected override void OnValueChanged(FieldChangedEventArgs fieldChangedEventArgs)
		{
			if (this.Value != layerSlider.Value.ToString())
			{
				if (double.TryParse(this.Value, out double result))
				{
					layerSlider.Value = result;
				}
				else
				{
					// TODO: Show a conversion/invalid value error
				}
			}

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
