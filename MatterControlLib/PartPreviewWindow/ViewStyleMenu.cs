/*
Copyright (c) 2026, John Lewin
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
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.RenderOpenGl;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	public static class ViewStyleMenu
	{
		public static void Extend(PopupMenu popupMenu, ThemeConfig menuTheme, ISceneContext sceneContext)
		{ 
			var actions = buildActions(sceneContext);
			var renderType = sceneContext.ViewState.RenderType;

			// Create items in a 'Modify' sub-menu
			popupMenu.CreateSubMenu(
				"Renderer".Localize(),
				menuTheme,
				(modifyMenu) =>
				{
					var siblingList = new List<GuiWidget>();

					foreach (var action in actions.OfType<NamedToggleAction>())
					{
						modifyMenu.CreateBoolMenuItem(
							action.Title,
							action.Icon,
							() => action.IsActive(),
							(isChecked) => UiThread.RunOnIdle(() => {
								action.Action();
								// Unclear why the menu stays open here
								modifyMenu.Close();
							}),
							useRadioStyle: true,
							siblingRadioButtonList: siblingList);
					}
				});
		}

		private static NamedAction[] buildActions(ISceneContext sceneContext)
		{
			return new NamedAction[]
			{
				new NamedToggleAction()
				{
					Title = "Shaded".Localize(),
					Icon = StaticData.Instance.LoadIcon("view_shaded.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.Shaded,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.Shaded,
				},
				new NamedToggleAction()
				{
					Title = "Outlines (default)".Localize(),
					Icon = StaticData.Instance.LoadIcon("view_outlines.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.Outlines,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.Outlines,
				},

#if DEBUG
				new NamedToggleAction()
				{
					Title = "Non-Manifold".Localize(),
					Icon =  StaticData.Instance.LoadIcon("view_polygons.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.NonManifold,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.NonManifold,
				},
#endif
				new NamedToggleAction()
				{
					Title = "Polygons".Localize(),
					Icon = StaticData.Instance.LoadIcon("view_polygons.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.Polygons,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.Polygons,
				},
				new NamedToggleAction()
				{
					Title = "Materials".Localize(),
					Icon = StaticData.Instance.LoadIcon("view_materials.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.Materials,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.Materials,
				},
				new NamedToggleAction()
				{
					Title = "Overhang".Localize(),
					Icon = StaticData.Instance.LoadIcon("view_overhang.png", 16, 16),
					IsActive = () => sceneContext.ViewState.RenderType == RenderTypes.Overhang,
					Action = () => sceneContext.ViewState.RenderType = RenderTypes.Overhang,
				},
			};
		}
	}
}
