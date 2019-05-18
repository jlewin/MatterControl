/*
Copyright (c) 2019, Lars Brubaker, John Lewin
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
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.DataConverters3D;
using MatterHackers.MatterControl;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.DesignTools;
using MatterHackers.MeshVisualizer;
using MatterHackers.VectorMath;

namespace MatterHackers.Plugins.EditorTools
{
	public class PathControl : InteractionVolume
	{
		private ThemeConfig theme;
		private WorldView world;

		private bool controlsRegistered = false;

		public PathControl(IInteractionVolumeContext context)
			: base(context)
		{
			theme = MatterControl.AppContext.Theme;

			world = InteractionContext.World;
		}

		public override void DrawGlContent(DrawGlContentEventArgs e)
		{
			//lines.Add(InteractionContext.World.GetScreenPosition(bottomPosition + new Vector3(distToStart * distBetweenPixelsWorldSpace, 0, 0)));

			var xxx = world.GetScreenPosition(new Vector3(5, 0, 0));

			base.DrawGlContent(e);
		}

		private IObject3D lastItem;

		private List<PointWidget> targets = new List<PointWidget>();

		public override void SetPosition(IObject3D selectedItem)
		{
			if (selectedItem != lastItem)
			{
				lastItem = selectedItem;

				foreach (var x in targets)
				{
					x.Close();
				}

				targets.Clear();

				if (selectedItem is PathObject3D pathObject)
				{
					foreach (var v in pathObject.VertexSource.Vertices())
					{
						var widget = new PointWidget(world, v);
						widget.Click += (s, e) =>
						{
							Console.WriteLine("Hello World!");
						};

						targets.Add(widget);

						InteractionContext.GuiSurface.AddChild(widget);
					}
				}
			}

			foreach(var item in targets)
			{
				item.UpdatePosition();
			}

			base.SetPosition(selectedItem);
		}

		private class PointWidget : GuiWidget
		{
			private VertexData vertexData;
			private Vector3 point;
			private WorldView world;
			private bool mouseInBounds;
			private GuiWidget systemWindow;

			public PointWidget(WorldView world, VertexData vertexData)
			{
				this.vertexData = vertexData;
				this.point = new Vector3(vertexData.position);
				this.world = world;
				HAnchor = HAnchor.Absolute;
				VAnchor = VAnchor.Absolute;
				Width = 12;
				Height = 12;
			}

			public override void OnLoad(EventArgs args)
			{
				// Register listeners
				systemWindow = this.Parents<SystemWindow>().First();
				systemWindow.AfterDraw += this.Parent_AfterDraw;

				base.OnLoad(args);
			}

			public override void OnClosed(EventArgs e)
			{
				// Unregister listeners
				if (systemWindow != null)
				{
					systemWindow.AfterDraw -= this.Parent_AfterDraw;
				}

				base.OnClosed(e);
			}

			public override void OnMouseEnterBounds(MouseEventArgs mouseEvent)
			{
				mouseInBounds = true;
				base.OnMouseEnterBounds(mouseEvent);
				this.Invalidate();
			}

			public override void OnMouseLeaveBounds(MouseEventArgs mouseEvent)
			{
				mouseInBounds = false;
				base.OnMouseLeaveBounds(mouseEvent);
				this.Invalidate();
			}

			private void Parent_AfterDraw(object sender, DrawEventArgs e)
			{
				// AfterDraw listener registered on parent to draw outside of bounds
				if (mouseInBounds)
				{
					var position = this.TransformToScreenSpace(LocalBounds.Center);
					e.Graphics2D.Circle(position, 7, Color.Blue.WithAlpha(80));
				}
			}

			public override void OnDraw(Graphics2D graphics2D)
			{
				graphics2D.Circle(6, 6, 3, Color.Black);
				base.OnDraw(graphics2D);
			}

			public void UpdatePosition()
			{
				this.Position = world.GetScreenPosition(point);
			}
		}
	}
}