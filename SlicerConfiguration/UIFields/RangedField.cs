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

using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
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
				PopupContent = container = new RangedSlider(this.SettingsData, theme)
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
}
