using System.Collections.Generic;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.SlicerConfiguration;

namespace MatterHackers.MatterControl
{
	public class ConfigurePrinterPage : DialogPage
	{
		private TabControl primaryTabControl;
		private ThemeConfig theme;

		public ConfigurePrinterPage()
		{

			theme = ApplicationController.Instance.Theme;
			this.WindowTitle = this.HeaderText = "Configure Printer".Localize();

			primaryTabControl = new TabControl();
			primaryTabControl.TabBar.BorderColor = ActiveTheme.Instance.PrimaryTextColor;
			//primaryTabControl.Margin = new BorderDouble(top: 8);
			primaryTabControl.AnchorAll();

			var defaultBackgroundColor = this.contentRow.BackgroundColor;

			this.contentRow.Padding = 0;
			this.contentRow.BackgroundColor = Color.Transparent;
			this.contentRow.AddChild(primaryTabControl);

			var sliceSettings = ApplicationController.Instance.DragDropData.View3DWidget.Parent.Parent.Parent.ChildrenRecursive<SliceSettingsWidget>().FirstOrDefault();

			var lastChild = primaryTabControl.Children.Last();
			lastChild.BackgroundColor = defaultBackgroundColor;

			foreach (var section in SliceSettingsOrganizer.Instance.UserLevels["Printer"].CategoriesList)
			{
				var tabPage = new TabPage(section.Name.Localize());

				primaryTabControl.AddTab(new TextTab(
					tabPage,
					section.Name + " Tab",
					theme.DefaultFontSize,
					ActiveTheme.Instance.TabLabelSelected,
					new Color(),
					ActiveTheme.Instance.TabLabelUnselected,
					new Color(),
					useUnderlineStyling: true));

				var scrollable = new ScrollableWidget(true)
				{
					VAnchor = VAnchor.Stretch,
					HAnchor = HAnchor.Stretch,
				};
				scrollable.ScrollArea.HAnchor = HAnchor.Stretch;
				scrollable.ScrollArea.VAnchor = VAnchor.Stretch;
				scrollable.AddChild(
					sliceSettings.CreateGroupContent(section.GroupsList.FirstOrDefault() , sliceSettings.settingsContext, sliceSettings.ShowHelpControls));
				tabPage.AddChild(scrollable);
			}
		}

		//private void RebuildSliceSettingsTabs()
		//{
		//	// Close and remove children
		//	primaryTabControl?.Close();

		//	var sideTabBarsListForLayout = new List<TabBar>();

		//	for (int topCategoryIndex = 0; topCategoryIndex < SliceSettingsOrganizer.Instance.UserLevels[UserLevel].CategoriesList.Count; topCategoryIndex++)
		//	{
		//		var categoryPage = new TabPage(category.Name.Localize());
		//		categoryPage.AnchorAll();

		//		primaryTabControl.AddTab(new TextTab(
		//			categoryPage,
		//			category.Name + " Tab",
		//			theme.DefaultFontSize,
		//			ActiveTheme.Instance.TabLabelSelected,
		//			new Color(),
		//			ActiveTheme.Instance.TabLabelUnselected,
		//			new Color(),
		//			useUnderlineStyling: true));


		//		var column = new FlowLayoutWidget(FlowDirection.TopToBottom);
		//		column.AnchorAll();

		//		var hline = new HorizontalLine()
		//		{
		//			BackgroundColor = ApplicationController.Instance.Theme.SlightShade,
		//			Height = 4
		//		};
		//		column.AddChild(hline);

		//		TabControl sideTabs = CreateSideTabsAndPages(category, this.ShowHelpControls);
		//		sideTabBarsListForLayout.Add(sideTabs.TabBar);
		//		column.AddChild(sideTabs);

		//		categoryPage.AddChild(column);
		//	}

		//	primaryTabControl.TabBar.AddChild(new HorizontalSpacer());

		//	if (settingsContext.IsPrimarySettingsView)
		//	{
		//		// Add the Overflow menu
		//		primaryTabControl.TabBar.AddChild(new SliceSettingsOverflowMenu(printer, this));
		//	}

		//	FindWidestTabAndSetAllMinimumSize(sideTabBarsListForLayout);

		//	// check if there is only one left side tab. If so hide the left tabs and expand the content.
		//	foreach (var tabList in sideTabBarsListForLayout)
		//	{
		//		if (tabList.CountVisibleChildren() == 1)
		//		{
		//			tabList.MinimumSize = new Vector2(0, 0);
		//			tabList.Width = 0;
		//		}
		//	}

		//	this.AddChild(primaryTabControl);

		//	// Restore the last selected tab
		//	primaryTabControl.SelectTab(UserSettings.Instance.get(UserSettingsKey.SliceSettingsWidget_CurrentTab));

		//	// Store the last selected tab on change
		//	primaryTabControl.TabBar.TabIndexChanged += (s, e) =>
		//	{
		//		if (!string.IsNullOrEmpty(primaryTabControl.TabBar.SelectedTabName)
		//			&& settingsContext.IsPrimarySettingsView)
		//		{
		//			UserSettings.Instance.set(UserSettingsKey.SliceSettingsWidget_CurrentTab, primaryTabControl.TabBar.SelectedTabName);
		//		}
		//	};
		//}

	}
}
