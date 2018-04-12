﻿/*
Copyright (c) 2017, Kevin Pope, John Lewin
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
using System.Diagnostics;
using System.IO;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.ConfigurationPage
{
	public class ApplicationSettingsWidget : FlowLayoutWidget, IIgnoredPopupChild
	{
		public static Action OpenPrintNotification = null;

		private ThemeConfig theme;

		public ApplicationSettingsWidget(ThemeConfig theme)
			: base(FlowDirection.TopToBottom)
		{
			this.HAnchor = HAnchor.Stretch;
			this.VAnchor = VAnchor.Fit;
			this.BackgroundColor = theme.Colors.PrimaryBackgroundColor;
			this.theme = theme;

			var configureIcon = AggContext.StaticData.LoadIcon("fa-cog_16.png");

#if __ANDROID__
			// Camera Monitoring
			bool hasCamera = true || ApplicationSettings.Instance.get(ApplicationSettingsKey.HardwareHasCamera) == "true";

			var previewButton = new IconButton(configureIcon, theme)
			{
				ToolTipText = "Configure Camera View".Localize()
			};
			previewButton.Click += (s, e) =>
			{
				AppContext.Platform.OpenCameraPreview();
			};

			this.AddSettingsRow(
				new SettingsItem(
					"Camera Monitoring".Localize(),
					new SettingsItem.ToggleSwitchConfig()
					{
						Checked = ActiveSliceSettings.Instance.GetValue<bool>(SettingsKey.publish_bed_image),
						ToggleAction = (itemChecked) =>
						{
							ActiveSliceSettings.Instance.SetValue(SettingsKey.publish_bed_image, itemChecked ? "1" : "0");
						}
					},
					previewButton,
					AggContext.StaticData.LoadIcon("camera-24x24.png", 24, 24))
			);
#endif

			// Print Notifications
			var configureNotificationsButton = new IconButton(configureIcon, theme)
			{
				Name = "Configure Notification Settings Button",
				ToolTipText = "Configure Notifications".Localize(),
				Margin = new BorderDouble(left: 6),
				VAnchor = VAnchor.Center
			};
			configureNotificationsButton.Click += (s, e) =>
			{
				if (OpenPrintNotification != null)
				{
					UiThread.RunOnIdle(OpenPrintNotification);
				}
			};

			this.AddSettingsRow(
				new SettingsItem(
					"Notifications".Localize(),
					theme,
					new SettingsItem.ToggleSwitchConfig()
					{
						Checked = UserSettings.Instance.get("PrintNotificationsEnabled") == "true",
						ToggleAction = (itemChecked) =>
						{
							UserSettings.Instance.set("PrintNotificationsEnabled", itemChecked ? "true" : "false");
						}
					},
					configureNotificationsButton,
					AggContext.StaticData.LoadIcon("notify-24x24.png")));

			// Touch Screen Mode
			this.AddSettingsRow(
				new SettingsItem(
					"Touch Screen Mode".Localize(),
					theme,
					new SettingsItem.ToggleSwitchConfig()
					{
						Checked = UserSettings.Instance.get(UserSettingsKey.ApplicationDisplayMode) == "touchscreen",
						ToggleAction = (itemChecked) =>
						{
							string displayMode = itemChecked ? "touchscreen" : "responsive";
							if (displayMode != UserSettings.Instance.get(UserSettingsKey.ApplicationDisplayMode))
							{
								UserSettings.Instance.set(UserSettingsKey.ApplicationDisplayMode, displayMode);
								ApplicationController.Instance.ReloadAll();
							}
						}
					}));

			// LanguageControl
			var languageSelector = new LanguageSelector(theme);
			languageSelector.SelectionChanged += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					string languageCode = languageSelector.SelectedValue;
					if (languageCode != UserSettings.Instance.get("Language"))
					{
						UserSettings.Instance.set("Language", languageCode);

						if (languageCode == "L10N")
						{
							GenerateLocalizationValidationFile();
						}

						ApplicationController.Instance.ResetTranslationMap();
						ApplicationController.Instance.ReloadAll();
					}
				});
			};

			this.AddSettingsRow(new SettingsItem("Language".Localize(), languageSelector, theme));

#if !__ANDROID__
			{
				// ThumbnailRendering
				var thumbnailsModeDropList = new DropDownList("", theme.Colors.PrimaryTextColor, maxHeight: 200, pointSize: theme.DefaultFontSize)
				{
					BorderColor = theme.GetBorderColor(75)
				};
				thumbnailsModeDropList.AddItem("Flat".Localize(), "orthographic");
				thumbnailsModeDropList.AddItem("3D".Localize(), "raytraced");

				thumbnailsModeDropList.SelectedValue = UserSettings.Instance.ThumbnailRenderingMode;
				thumbnailsModeDropList.SelectionChanged += (s, e) =>
				{
					string thumbnailRenderingMode = thumbnailsModeDropList.SelectedValue;
					if (thumbnailRenderingMode != UserSettings.Instance.ThumbnailRenderingMode)
					{
						UserSettings.Instance.ThumbnailRenderingMode = thumbnailRenderingMode;

						UiThread.RunOnIdle(() =>
						{
							// Ask if the user they would like to rebuild their thumbnails
							StyledMessageBox.ShowMessageBox(
								(bool rebuildThumbnails) =>
								{
									if (rebuildThumbnails)
									{
										string directoryToRemove = ApplicationController.CacheablePath("ItemThumbnails", "");
										try
										{
											if (Directory.Exists(directoryToRemove))
											{
												Directory.Delete(directoryToRemove, true);
											}
										}
										catch (Exception)
										{
											GuiWidget.BreakInDebugger();
										}

										Directory.CreateDirectory(directoryToRemove);

										ApplicationController.Instance.Library.NotifyContainerChanged();
									}
								},
								rebuildThumbnailsMessage,
								rebuildThumbnailsTitle,
								StyledMessageBox.MessageType.YES_NO,
								"Rebuild".Localize());
						});
					}
				};

				this.AddSettingsRow(
					new SettingsItem(
						"Thumbnails".Localize(),
						thumbnailsModeDropList,
						theme));

				// TextSize
				if (!double.TryParse(UserSettings.Instance.get(UserSettingsKey.ApplicationTextSize), out double currentTexSize))
				{
					currentTexSize = 1.0;
				}

				double sliderThumbWidth = 10 * GuiWidget.DeviceScale;
				double sliderWidth = 100 * GuiWidget.DeviceScale;
				var textSizeSlider = new SolidSlider(new Vector2(), sliderThumbWidth, .7, 1.4)
				{
					Name = "Text Size Slider",
					Margin = new BorderDouble(5, 0),
					Value = currentTexSize,
					HAnchor = HAnchor.Stretch,
					VAnchor = VAnchor.Center,
					TotalWidthInPixels = sliderWidth,
				};

				var optionalContainer = new FlowLayoutWidget()
				{
					VAnchor = VAnchor.Center | VAnchor.Fit,
					HAnchor = HAnchor.Fit
				};

				TextWidget sectionLabel = null;

				var textSizeApplyButton = new TextButton("Apply".Localize(), theme)
				{
					VAnchor = VAnchor.Center,
					BackgroundColor = theme.SlightShade,
					Visible = false,
					Margin = new BorderDouble(right: 6)
				};
				textSizeApplyButton.Click += (s, e) =>
				{
					GuiWidget.DeviceScale = textSizeSlider.Value;
					ApplicationController.Instance.ReloadAll();
				};
				optionalContainer.AddChild(textSizeApplyButton);

				textSizeSlider.ValueChanged += (s, e) =>
				{
					double textSizeNew = textSizeSlider.Value;
					UserSettings.Instance.set(UserSettingsKey.ApplicationTextSize, textSizeNew.ToString("0.0"));
					sectionLabel.Text = "Text Size".Localize() + $" : {textSizeNew:0.0}";
					textSizeApplyButton.Visible = textSizeNew != currentTexSize;
				};

				var section = new SettingsItem(
						"Text Size".Localize() + $" : {currentTexSize:0.0}",
						textSizeSlider,
						theme,
						optionalContainer);

				sectionLabel = section.Children<TextWidget>().FirstOrDefault();

				this.AddSettingsRow(section);
			}
#endif

			AddMenuItem("Forums".Localize(), () => ApplicationController.Instance.LaunchBrowser("https://forums.matterhackers.com/category/20/mattercontrol"));
			AddMenuItem("Wiki".Localize(), () => ApplicationController.Instance.LaunchBrowser("http://wiki.mattercontrol.com"));
			AddMenuItem("Guides and Articles".Localize(), () => ApplicationController.Instance.LaunchBrowser("http://www.matterhackers.com/topic/mattercontrol"));
			AddMenuItem("Release Notes".Localize(), () => ApplicationController.Instance.LaunchBrowser("http://wiki.mattercontrol.com/Release_Notes"));
			AddMenuItem("Report a Bug".Localize(), () => ApplicationController.Instance.LaunchBrowser("https://github.com/MatterHackers/MatterControl/issues"));

			var updateMatterControl = new SettingsItem("Check For Update".Localize(), theme);
			updateMatterControl.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					UpdateControlData.Instance.CheckForUpdate();
					DialogWindow.Show<CheckForUpdatesPage>();
				});
			};
			this.AddSettingsRow(updateMatterControl);

			this.AddChild(new SettingsItem("Theme".Localize(), new GuiWidget(), theme));
			this.AddChild(this.GetThemeControl(theme));

			var aboutMatterControl = new SettingsItem("About".Localize() + " " + ApplicationController.Instance.ProductName, theme);
			if (IntPtr.Size == 8)
			{
				// Push right
				aboutMatterControl.AddChild(new HorizontalSpacer());

				// Add x64 adornment
				var blueBox = new FlowLayoutWidget()
				{
					Margin = new BorderDouble(10, 0),
					Padding = new BorderDouble(2),
					Border = new BorderDouble(1),
					BorderColor = theme.Colors.PrimaryAccentColor,
					VAnchor = VAnchor.Center | VAnchor.Fit
				};
				blueBox.AddChild(new TextWidget("64", pointSize: 8, textColor: theme.Colors.PrimaryAccentColor));

				aboutMatterControl.AddChild(blueBox);
			}
			aboutMatterControl.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() => DialogWindow.Show<AboutPage>());
			};
			this.AddSettingsRow(aboutMatterControl);
		}

		private void AddMenuItem(string title, Action callback)
		{
			var newItem = new SettingsItem(title, theme);
			newItem.Click += (s, e) =>
			{
				UiThread.RunOnIdle(() =>
				{
					callback?.Invoke();
				});
			};

			this.AddSettingsRow(newItem);
		}

		private void AddSettingsRow(GuiWidget widget)
		{
			this.AddChild(widget);
			widget.Padding = widget.Padding.Clone(right: 10);
		}

		private FlowLayoutWidget GetThemeControl(ThemeConfig theme)
		{
			var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Margin = new BorderDouble(left: 30)
			};

			// Determine if we should set the dark or light version of the theme
			var activeThemeIndex = ActiveTheme.AvailableThemes.IndexOf(ApplicationController.Instance.Theme.Colors);

			var midPoint = ActiveTheme.AvailableThemes.Count / 2;

			int darkThemeIndex;
			int lightThemeIndex;

			bool isLightTheme = activeThemeIndex >= midPoint;
			if (isLightTheme)
			{
				lightThemeIndex = activeThemeIndex;
				darkThemeIndex = activeThemeIndex - midPoint;
			}
			else
			{
				darkThemeIndex = activeThemeIndex;
				lightThemeIndex = activeThemeIndex + midPoint;
			}

			var darkPreview = new ThemePreviewButton(ActiveTheme.AvailableThemes[darkThemeIndex], !isLightTheme)
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Absolute,
				Width = 80,
				Height = 65,
				Margin = new BorderDouble(5, 15, 10, 10)
			};

			var lightPreview = new ThemePreviewButton(ActiveTheme.AvailableThemes[lightThemeIndex], isLightTheme)
			{
				HAnchor = HAnchor.Absolute,
				VAnchor = VAnchor.Absolute,
				Width = 80,
				Height = 65,
				Margin = new BorderDouble(5, 15, 10, 10)
			};

			// Add color selector
			container.AddChild(new ThemeColorSelectorWidget(darkPreview, lightPreview)
			{
				Margin = new BorderDouble(right: 5)
			});

			var themePreviews = new FlowLayoutWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Fit
			};

			themePreviews.AddChild(darkPreview);
			themePreviews.AddChild(lightPreview);

			container.AddChild(themePreviews);

			return container;
		}

		private string rebuildThumbnailsMessage = "You are switching to a different thumbnail rendering mode. If you want, your current thumbnails can be removed and recreated in the new style. You can switch back and forth at any time. There will be some processing overhead while the new thumbnails are created.\n\nDo you want to rebuild your existing thumbnails now?".Localize();
		private string rebuildThumbnailsTitle = "Rebuild Thumbnails Now".Localize();

		[Conditional("DEBUG")]
		private void GenerateLocalizationValidationFile()
		{
#if !__ANDROID__
			if (AggContext.StaticData is FileSystemStaticData fileSystemStaticData)
			{
				char currentChar = 'A';

				// Note: Functionality only expected to work on Desktop/Debug builds and as such, is coupled to FileSystemStaticData
				string outputPath = fileSystemStaticData.MapPath(Path.Combine("Translations", "L10N", "Translation.txt"));
				string sourceFilePath = fileSystemStaticData.MapPath(Path.Combine("Translations", "Master.txt"));

				// Ensure the output directory exists
				Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

				using (var outstream = new StreamWriter(outputPath))
				{
					foreach (var line in File.ReadAllLines(sourceFilePath))
					{
						if (line.StartsWith("Translated:"))
						{
							var pos = line.IndexOf(':');
							var segments = new string[]
							{
							line.Substring(0, pos),
							line.Substring(pos + 1),
							};

							outstream.WriteLine("{0}:{1}", segments[0], new string(segments[1].ToCharArray().Select(c => c == ' ' ? ' ' : currentChar).ToArray()));

							if (currentChar++ == 'Z')
							{
								currentChar = 'A';
							}
						}
						else
						{
							outstream.WriteLine(line);
						}
					}
				}
			}
#endif
		}
	}
}