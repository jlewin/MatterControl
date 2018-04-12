﻿using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
	public class LanguageSelector : DropDownList
	{
		private Dictionary<string, string> languageDict;

		public LanguageSelector(ThemeConfig theme)
			: base("Default", theme.Colors.PrimaryTextColor)
		{
			this.MinimumSize = new Vector2(this.LocalBounds.Width, this.LocalBounds.Height);
			this.BorderColor = theme.GetBorderColor(75);
			CreateLanguageDict();


			foreach (KeyValuePair<string, string> entry in languageDict)
			{
				AddItem(entry.Key, entry.Value);
			}

			string languageCode = UserSettings.Instance.get("Language");
			foreach (KeyValuePair<string, string> entry in languageDict)
			{
				if (languageCode == entry.Value)
				{
					SelectedLabel = entry.Key;
					break;
				}
			}
		}

		private void CreateLanguageDict()
		{
			languageDict = new Dictionary<string, string>
			{
				["Default"] = "EN",
				["English"] = "EN",
				["Čeština"] = "CS",
				["Dansk"] = "DA",
				["Deutsch"] = "DE",
				["Español"] = "ES",
				["ελληνικά"] = "EL",
				["Français"] = "FR",
				["Italiano"] = "IT",
				["Norsk"] = "NO",
				["Polski"] = "PL",
				["Português"] = "CR",
				["Русский"] = "RU",
				["Română"] = "RO",
				["Türkçe"] = "TR",
				["Vlaams"] = "NL",
			};

#if DEBUG
			languageDict["L10N"] = "L10N";
#endif
		}
	}
}