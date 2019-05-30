﻿/*
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
using MatterHackers.MatterControl.DesignTools;
using MatterHackers.MeshVisualizer;
using MatterHackers.RayTracer;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;
using MatterHackers.VectorMath;

namespace MatterHackers.Plugins.EditorTools
{
	public class PathControl : IGLInteractionElement
	{
		private IInteractionVolumeContext context;

		private ThemeConfig theme;

		private WorldView world;

		private IObject3D lastItem;

		private List<PointWidget> targets = new List<PointWidget>();

		private bool controlsRegistered = false;
		private IEnumerable<VertexData> activePoints;
		private FlattenCurves flattened;
		private bool m_visible;
		private PointWidget _activeItem;

		public PathControl(IInteractionVolumeContext context)
		{
			this.context = context;
			theme = MatterControl.AppContext.Theme;

			world = context.World;
		}

		public string Name => "Path Control";

		public bool Visible
		{
			get => m_visible;
			set
			{
				if (m_visible != value)
				{
					m_visible = value;

					foreach (var widget in targets)
					{
						widget.Visible = m_visible && !(widget is CurveControlPoint);
					}
				}
			}
		}

		public PointWidget ActiveItem
		{
			get => _activeItem;
			set
			{
				if (_activeItem != null)
				{
					_activeItem.Selected = false;
				}

				_activeItem = value;
				_activeItem.Selected = true;
			}
		}

		public bool DrawOnTop => false;

		public void CancelOperation()
		{
		}

		public void DrawGlContent(DrawGlContentEventArgs e)
		{
			if (flattened != null
				&& e.Graphics2D is Graphics2DOpenGL glGraphics)
			{
				var pixelWidth = world.GetWorldUnitsPerScreenPixelAtPosition(new Vector3(activePoints.First().position));

				//glGraphics.RenderTransformedPath(
				//	Matrix4X4.Identity,
				//	new Stroke(flattened, pixelWidth * 10),
				//	theme.PrimaryAccentColor,
				//	false);

				var stroke1 = new Stroke(flattened, pixelWidth);
				var stroke2 = new Stroke(flattened, 1);

				var vertices1 = stroke1.Vertices().ToList();
				var vertices2 = stroke2.Vertices().ToList();

				glGraphics.RenderTransformedPath(
					Matrix4X4.Identity,
					stroke2,
					Color.Black,
					false);

				foreach(var point in vertices2)
				{
					glGraphics.Circle(point.position, 0.2, Color.Red);
				}

				GL.Begin(BeginMode.Lines);
				{
					GL.Color4(Color.DarkGray);

					PointWidget lastPoint = null;

					for (int i = 0; i < targets.Count; i++)
					{
						var widget = targets[i];

						if (widget == this.ActiveItem
							&& lastPoint != null
							&& widget is Curve4AnchorWidget curveWidget)
						{
							curveWidget.ControlPoint1.Visible = true;
							curveWidget.ControlPoint2.Visible = true;

							GL.Vertex3(new Vector3(widget.Point));
							GL.Vertex3(new Vector3(curveWidget.ControlPoint2.Point));

							GL.Vertex3(new Vector3(lastPoint.Point));
							GL.Vertex3(new Vector3(curveWidget.ControlPoint1.Point));

							break;
						}

						if (!(widget is CurveControlPoint))
						{
							lastPoint = widget;
						}
					}
				}
				GL.End();

			}
		}

		public void LostFocus()
		{
			this.Reset();
		}

		private void Reset()
		{
			// Clear and close selection targets
			foreach (var widget in targets)
			{
				widget.Close();
			}

			targets.Clear();

			lastItem = null;
		}

		public void SetPosition(IObject3D selectedItem)
		{
			if (selectedItem != lastItem)
			{
				this.Reset();

				lastItem = selectedItem;

				if (selectedItem is PathObject3D pathObject)
				{
					var vertexStorage = pathObject.VertexSource as VertexStorage;

					activePoints = vertexStorage.Vertices();

					flattened = new FlattenCurves(vertexStorage);

					VertexPointWidget widget = null;

					for (var i = 0; i < vertexStorage.Count; i++)
					{
						var command = vertexStorage.vertex(i, out double x, out double y);

						if (ShapePath.is_vertex(command))
						{
							if (command == ShapePath.FlagsAndCommand.Curve4)
							{
								//vertexDataManager.AddVertex(x_ctrl1, y_ctrl1, ShapePath.FlagsAndCommand.Curve4);
								//vertexDataManager.AddVertex(x_ctrl2, y_ctrl2, ShapePath.FlagsAndCommand.Curve4);
								//vertexDataManager.AddVertex(x_to, y_to, ShapePath.FlagsAndCommand.Curve4);

								var controlPoint1 = new CurveControlPoint(context, this, vertexStorage, new Vector3(x, y, 0), command, i);
								context.GuiSurface.AddChild(controlPoint1);
								targets.Add(controlPoint1);

								command = vertexStorage.vertex(i + 1, out x, out y);
								var controlPoint2 = new CurveControlPoint(context, this, vertexStorage, new Vector3(x, y, 0), command, i + 1);
								context.GuiSurface.AddChild(controlPoint2);
								targets.Add(controlPoint2);

								command = vertexStorage.vertex(i + 2, out x, out y);
								var curveWidget = new Curve4AnchorWidget(context, this, vertexStorage, new Vector3(x, y, 0), command, i + 2)
								{
									ControlPoint1 = controlPoint1,
									ControlPoint2 = controlPoint2,
								};

								//controlPoint1.ParentPoint = curveWidget;
								//controlPoint2.ParentPoint = curveWidget;

								widget = curveWidget;

								// Advance to account for 3 commands in Curve4
								i += 2;
							}
							else
							{
								widget = new VertexPointWidget(context, this, vertexStorage, new Vector3(x, y, 0), command, i);
							}
							// widget.Click += (s, e) =>

							targets.Add(widget);

							context.GuiSurface.AddChild(widget);
						}
					}

					// Highlight last
					if (widget != null)
					{
						widget.PointColor = Color.Red;
					}
				}
			}

			foreach (var item in targets)
			{
				item.UpdatePosition();
			}
		}

		private class CurveControlPoint : VertexPointWidget
		{
			public CurveControlPoint(IInteractionVolumeContext context, PathControl interactionControl, VertexStorage vertexStorage, Vector3 point, ShapePath.FlagsAndCommand flagsandCommand, int index)
				: base(context, interactionControl, vertexStorage, point, flagsandCommand, index)
			{
				this.ClaimSelection = false;
				this.PointColor = Color.DarkBlue;
				this.Visible = false;
				this.HandleStyle = HandleStyle.Circle;
			}

			//public Curve4PointWidget ParentPoint { get; internal set; }
		}

		private class Curve4AnchorWidget : VertexPointWidget
		{
			private bool _focused;

			public Curve4AnchorWidget(IInteractionVolumeContext context, PathControl interactionControl,  VertexStorage vertexStorage, Vector3 point, ShapePath.FlagsAndCommand flagsandCommand, int index)
				: base (context, interactionControl, vertexStorage, point, flagsandCommand, index)
			{
			}

			public override void OnFocusChanged(EventArgs e)
			{
				if (this.Focused)
				{
					this.ControlPoint1.Visible = this.ControlPoint2.Visible = true;
				}
				else
				{
					UiThread.RunOnIdle(() =>
					{
						bool eitherFocused = this.ControlPoint1.Focused || this.ControlPoint2.Focused;
						this.ControlPoint1.Visible = this.ControlPoint2.Visible = eitherFocused;
					}, .1);
				}

				base.OnFocusChanged(e);
			}

			public VertexPointWidget ControlPoint1 { get; set; }

			public VertexPointWidget ControlPoint2 { get; set; }
		}

		private class VertexPointWidget : PointWidget
		{
			public PathControl PathInteractionControl { get; }

			private ShapePath.FlagsAndCommand command;
			private int index;
			private VertexStorage vertexStorage;

			public VertexPointWidget(IInteractionVolumeContext context, PathControl interactionControl, VertexStorage vertexStorage, Vector3 point, ShapePath.FlagsAndCommand flagsandCommand, int index)
				: base(context, point)
			{
				this.PathInteractionControl = interactionControl;
				this.command = flagsandCommand;
				this.index = index;
				this.vertexStorage = vertexStorage;
			}

			public bool ClaimSelection { get; protected set; } = true;

			public override void OnClick(MouseEventArgs mouseEvent)
			{
				if (this.ClaimSelection)
				{
					this.Selected = true;
					this.PathInteractionControl.ActiveItem = this;
				}

				base.OnClick(mouseEvent);
			}

			protected override void OnDragTo(IntersectInfo info)
			{
				this.Point = info.HitPosition;
				vertexStorage.modify_vertex(index, info.HitPosition.X, info.HitPosition.Y);
				this.Invalidate();
				base.OnDragTo(info);
			}
		}

		public class PointWidget : GuiWidget
		{
			private WorldView world;
			private GuiWidget guiSurface;
			private bool mouseInBounds;
			private GuiWidget systemWindow;
			private bool mouseDownOnWidget;
			private IInteractionVolumeContext interactionContext;
			private static PlaneShape bedPlane = new PlaneShape(Vector3.UnitZ, 0, null);
			private ThemeConfig theme;

			public PointWidget(IInteractionVolumeContext interactionContext, Vector3 point)
			{
				this.theme = MatterControl.AppContext.Theme;
				this.interactionContext = interactionContext;
				this.HAnchor = HAnchor.Absolute;
				this.VAnchor = VAnchor.Absolute;
				this.Width = 8;
				this.Height = 8;
				this.Point = point;

				world = interactionContext.World;
				guiSurface = interactionContext.GuiSurface;
			}

			public Vector3 Point { get; protected set; }

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

			public override void OnMouseDown(MouseEventArgs mouseEvent)
			{
				mouseDownOnWidget = mouseEvent.Button == MouseButtons.Left && this.PositionWithinLocalBounds(mouseEvent.Position);
				base.OnMouseDown(mouseEvent);
			}

			public override void OnMouseUp(MouseEventArgs mouseEvent)
			{
				mouseDownOnWidget = false;
				base.OnMouseUp(mouseEvent);
			}

			public override void OnMouseMove(MouseEventArgs mouseEvent)
			{
				// Drag item
				if (mouseDownOnWidget)
				{
					var localMousePosition = mouseEvent.Position;

					Vector2 meshViewerWidgetScreenPosition = this.TransformToParentSpace(guiSurface, localMousePosition);
					Ray ray = world.GetRayForLocalBounds(meshViewerWidgetScreenPosition);

					if (bedPlane.GetClosestIntersection(ray) is IntersectInfo info)
					{
						this.OnDragTo(info);
					}
				}

				base.OnMouseMove(mouseEvent);
			}

			protected virtual void OnDragTo(IntersectInfo info)
			{
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
					e.Graphics2D.Circle(position, 9, Color.Blue.WithAlpha(80));
				}
			}

			public Color PointColor { get; set; } = Color.Black;

			protected HandleStyle HandleStyle { get; set; } = HandleStyle.Square;

			public bool Selected { get; set; }

			public override void OnDraw(Graphics2D graphics2D)
			{
				if (this.HandleStyle == HandleStyle.Square)
				{
					if (this.Selected)
					{
						graphics2D.FillRectangle(0, 0, this.Width, this.Height, theme.PrimaryAccentColor);
						graphics2D.Rectangle(0, 0, this.Width, this.Height, Color.White);
					}
					else
					{
						graphics2D.Rectangle(0, 0, this.Width, this.Height, this.PointColor);
					}
				}
				else
				{
					graphics2D.Circle(this.LocalBounds.Center, 3.5, this.PointColor);
				}

				base.OnDraw(graphics2D);
			}

			public void UpdatePosition()
			{
				this.Position = world.GetScreenPosition(Point) - new Vector2(this.LocalBounds.Width / 2, this.LocalBounds.Height / 2);
			}
		}

		public enum HandleStyle
		{
			Square,
			Circle
		}
	}
}