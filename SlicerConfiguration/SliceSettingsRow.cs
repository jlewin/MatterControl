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

using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl.SlicerConfiguration
{
	public class SliceSettingsRow : SettingsRow
	{
		private static readonly Color materialSettingBackgroundColor = Color.Orange;
		private static readonly Color qualitySettingBackgroundColor = Color.YellowGreen;
		public static readonly Color userSettingBackgroundColor = new Color(68, 95, 220, 150);

		private SettingsContext settingsContext;
		private PrinterConfig printer;
		private SliceSettingData settingData;

		private GuiWidget dataArea;
		private GuiWidget unitsArea;
		private GuiWidget restoreArea;
		private Button restoreButton = null;

		public SliceSettingsRow(PrinterConfig printer, SettingsContext settingsContext, SliceSettingData settingData, ThemeConfig theme, bool fullRowSelect = false)
			: base (settingData.PresentationName.Localize(), settingData.HelpText.Localize(), theme, fullRowSelect: fullRowSelect)
		{
			this.printer = printer;
			this.settingData = settingData;
			this.settingsContext = settingsContext;

			this.AddChild(dataArea = new FlowLayoutWidget
			{
				VAnchor = VAnchor.Fit | VAnchor.Center,
				DebugShowBounds = debugLayout
			});

			this.AddChild(unitsArea = new GuiWidget()
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Fit | VAnchor.Center,
				Width = 50 * GuiWidget.DeviceScale,
				DebugShowBounds = debugLayout
			});

			// Populate unitsArea as appropriate
			// List elements contain list values in the field which normally contains label details, skip generation of invalid labels
			if (settingData.DataEditType != SliceSettingData.DataEditTypes.LIST
				&& settingData.DataEditType != SliceSettingData.DataEditTypes.HARDWARE_PRESENT)
			{
				unitsArea.AddChild(
				new WrappedTextWidget(settingData.Units.Localize(), pointSize: 8, textColor: theme.Colors.PrimaryTextColor)
				{
					Margin = new BorderDouble(5, 0),
				});
			}

			restoreArea = new GuiWidget()
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Fit | VAnchor.Center,
				Width = 20 * GuiWidget.DeviceScale,
				DebugShowBounds = debugLayout
			};
			this.AddChild(restoreArea);

			this.Name = settingData.SlicerConfigName + " Row";

			if (settingData.ShowAsOverride)
			{
				restoreButton = theme.CreateSmallResetButton();
				restoreButton.HAnchor = HAnchor.Right;
				restoreButton.Margin = 0;
				restoreButton.Name = "Restore " + settingData.SlicerConfigName;
				restoreButton.ToolTipText = "Restore Default".Localize();
				restoreButton.Click += (sender, e) =>
				{
					// Revert the user override
					settingsContext.ClearValue(settingData.SlicerConfigName);
				};

				restoreArea.AddChild(restoreButton);

				restoreArea.Selectable = true;
			}
		}

		public Color HighlightColor
		{
			get => overrideIndicator.BackgroundColor;
			set
			{
				if (overrideIndicator.BackgroundColor != value)
				{
					overrideIndicator.BackgroundColor = value;
				}
			}
		}

		public void UpdateStyle()
		{
			if (settingsContext.ContainsKey(settingData.SlicerConfigName))
			{
				switch (settingsContext.ViewFilter)
				{
					case NamedSettingsLayers.All:
						if (settingData.ShowAsOverride)
						{
							var defaultCascade = printer.Settings.defaultLayerCascade;
							var firstParentValue = printer.Settings.GetValueAndLayerName(settingData.SlicerConfigName, defaultCascade.Skip(1));
							var (currentValue, layerName) = printer.Settings.GetValueAndLayerName(settingData.SlicerConfigName, defaultCascade);

							if (firstParentValue.Item1 == currentValue)
							{
								if (layerName.StartsWith("Material"))
								{
									this.HighlightColor = materialSettingBackgroundColor;
								}
								else if (layerName.StartsWith("Quality"))
								{
									this.HighlightColor = qualitySettingBackgroundColor;
								}
								else
								{
									this.HighlightColor = Color.Transparent;
								}

								if (restoreButton != null)
								{
									restoreButton.Visible = false;
								}
							}
							else
							{
								this.HighlightColor = userSettingBackgroundColor;
								if (restoreButton != null) restoreButton.Visible = true;
							}
						}
						break;
					case NamedSettingsLayers.Material:
						this.HighlightColor = materialSettingBackgroundColor;
						if (restoreButton != null) restoreButton.Visible = true;
						break;
					case NamedSettingsLayers.Quality:
						this.HighlightColor = qualitySettingBackgroundColor;
						if (restoreButton != null) restoreButton.Visible = true;
						break;
				}
			}
			else if (settingsContext.IsPrimarySettingsView)
			{
				if (printer.Settings.SettingExistsInLayer(settingData.SlicerConfigName, NamedSettingsLayers.Material))
				{
					this.HighlightColor = materialSettingBackgroundColor;
				}
				else if (printer.Settings.SettingExistsInLayer(settingData.SlicerConfigName, NamedSettingsLayers.Quality))
				{
					this.HighlightColor = qualitySettingBackgroundColor;
				}
				else
				{
					this.HighlightColor = Color.Transparent;
				}

				if (restoreButton != null) restoreButton.Visible = false;
			}
			else
			{
				if (restoreButton != null) restoreButton.Visible = false;
				this.HighlightColor = Color.Transparent;
			}

		}

		public void AddContent(GuiWidget content)
		{
			dataArea.AddChild(content);
		}
	}
}
