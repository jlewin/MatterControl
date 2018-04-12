﻿/*
Copyright (c) 2017, John Lewin
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
using System.IO;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.MatterControl.Library;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.CustomWidgets
{
	public class ListViewItemBase : GuiWidget
	{
		private static ImageBuffer defaultFolderIcon = AggContext.StaticData.LoadIcon(Path.Combine("FileDialog", "folder.png"));
		private static ImageBuffer defaultItemIcon = AggContext.StaticData.LoadIcon(Path.Combine("FileDialog", "file.png"));
		private static ImageBuffer generatingThumbnailIcon = AggContext.StaticData.LoadIcon(Path.Combine("building_thumbnail_40x40.png"));

		protected ListViewItem listViewItem;
		protected View3DWidget view3DWidget;
		protected bool mouseInBounds = false;
		private bool mouseDownInBounds = false;
		private Vector2 mouseDownAt;

		protected ImageWidget imageWidget;
		protected int thumbWidth;
		protected int thumbHeight;

		public ListViewItemBase(ListViewItem listViewItem, int width, int height)
		{
			this.listViewItem = listViewItem;
			this.thumbWidth = width;
			this.thumbHeight = height;
		}

		private static bool WidgetOnScreen(GuiWidget widget, RectangleDouble bounds)
		{
			if (!widget.Visible)
			{
				return false;
			}
			else
			{
				if (widget.Parent != null)
				{
					var boundsInParentSpace = widget.TransformToParentSpace(widget.Parent, bounds);
					var intersects = boundsInParentSpace.IntersectRectangles(boundsInParentSpace, widget.Parent.LocalBounds);
					if (!intersects
						|| boundsInParentSpace.Width <= 0
						|| boundsInParentSpace.Height <= 0
						|| !WidgetOnScreen(widget.Parent, boundsInParentSpace))
					{
						return false;
					}
				}
			}

			return true;
		}

		public async Task LoadItemThumbnail()
		{
			var listView = listViewItem.ListView;

			var thumbnail = listView.LoadCachedImage(listViewItem, thumbWidth, thumbHeight);
			if (thumbnail != null)
			{
				SetItemThumbnail(thumbnail);
				return;
			}

			var itemModel = listViewItem.Model;

			if (thumbnail == null)
			{
				// Ask the container - allows the container to provide its own interpretation of the item thumbnail
				thumbnail = await listView.ActiveContainer.GetThumbnail(itemModel, thumbWidth, thumbHeight);
			}

			if (thumbnail == null && itemModel is IThumbnail)
			{
				// If the item provides its own thumbnail, try to collect it
				thumbnail = await (itemModel as IThumbnail).GetThumbnail(thumbWidth, thumbHeight);
			}

			if (thumbnail == null)
			{
				// Ask content provider - allows type specific thumbnail creation
				var contentProvider = ApplicationController.Instance.Library.GetContentProvider(itemModel);
				if (contentProvider is MeshContentProvider)
				{
					// Before we have a thumbnail set to the content specific thumbnail
					thumbnail = contentProvider.DefaultImage;

					ApplicationController.Instance.QueueForGeneration(async () =>
					{
						// When this widget is dequeued for generation, validate before processing. Off-screen widgets should be skipped and will requeue next time they become visible
						if (ListViewItemBase.WidgetOnScreen(this, this.LocalBounds))
						{
							SetItemThumbnail(generatingThumbnailIcon);

							// Then try to load a content specific thumbnail
							await contentProvider.GetThumbnail(
								itemModel,
								thumbWidth,
								thumbHeight,
								(image) =>
								{
									// Use the content providers default image if an image failed to load
									SetItemThumbnail(image ?? contentProvider.DefaultImage, true);
								});
						}
					});
				}
				else if (contentProvider != null)
				{
					// Then try to load a content specific thumbnail
					await contentProvider.GetThumbnail(
						itemModel,
						thumbWidth,
						thumbHeight,
						(image) => thumbnail = image);
				}
			}

			if (thumbnail == null)
			{
				// Use the listview defaults
				thumbnail = ((itemModel is ILibraryContainerLink) ? defaultFolderIcon : defaultItemIcon).AlphaToPrimaryAccent();
			}

			SetItemThumbnail(thumbnail);
		}

		internal void EnsureSelection()
		{
			if (this.IsSelectableContent)
			{
				// Existing selection only survives with ctrl->click
				if (!Keyboard.IsKeyDown(Keys.ControlKey))
				{
					listViewItem.ListView.SelectedItems.Clear();
				}

				// Any mouse down ensures selection - mouse up will evaluate if DragDrop occurred and toggle selection if not
				if (!listViewItem.ListView.SelectedItems.Contains(listViewItem))
				{
					listViewItem.ListView.SelectedItems.Add(listViewItem);
				}

				Invalidate();
			}
		}

		internal void OnItemSelect()
		{
			if (this.IsSelectableContent
				&& !hitDragThreshold)
			{
				if (wasSelected)
				{
					listViewItem.ListView.SelectedItems.Remove(listViewItem);
				}

				Invalidate();
			}
		}

		private bool IsSelectableContent
		{
			get
			{
				bool isContentItem = listViewItem.Model is ILibraryObject3D;
				bool isValidStream = (listViewItem.Model is ILibraryAssetStream stream
					&& ApplicationController.Instance.Library.IsContentFileType(stream.FileName));
				bool isContainerLink = listViewItem.Model is ILibraryContainerLink;

				bool isGCode = listViewItem.Model is FileSystemFileItem item && Path.GetExtension(item.FileName.ToUpper()) == ".GCODE"
					|| listViewItem.Model is SDCardFileItem sdItem && Path.GetExtension(sdItem.Name.ToUpper()) == ".GCODE";

				return isContentItem || isValidStream || isContainerLink || isGCode;
			}
		}

		protected void SetItemThumbnail(ImageBuffer thumbnail, bool colorize = false)
		{
			if (thumbnail != null)
			{
				// Resize canvas to target as fallback
				if (thumbnail.Width < thumbWidth || thumbnail.Height < thumbHeight)
				{
					thumbnail = listViewItem.ListView.ResizeCanvas(thumbnail, thumbWidth, thumbHeight);
				}
				else if (thumbnail.Width > thumbWidth || thumbnail.Height > thumbHeight)
				{
					thumbnail = LibraryProviderHelpers.ResizeImage(thumbnail, thumbWidth, thumbHeight);
				}

				// TODO: Resolve and implement
				// Allow the container to draw an overlay - use signal interface or add method to interface?
				//var iconWithOverlay = ActiveContainer.DrawOverlay()

				this.imageWidget.Image = thumbnail;

				this.Invalidate();
			}
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			base.OnDraw(graphics2D);

			var widgetBorder = new RoundedRect(LocalBounds, 0);

			// Draw the hover border if the mouse is in bounds or if its the ActivePrint item
			if (mouseInBounds || (this.IsActivePrint && !this.EditMode))
			{
				//Draw interior border
				graphics2D.Render(new Stroke(widgetBorder, 3), ActiveTheme.Instance.PrimaryAccentColor);
			}

			if (this.IsHoverItem)
			{
				RectangleDouble Bounds = LocalBounds;
				RoundedRect rectBorder = new RoundedRect(Bounds, 0);

				this.BackgroundColor = Color.White;

				graphics2D.Render(new Stroke(rectBorder, 3), ActiveTheme.Instance.PrimaryAccentColor);
			}
		}

		private bool hitDragThreshold = false;

		private bool wasSelected = false;

		public override void OnMouseDown(MouseEventArgs mouseEvent)
		{
			mouseDownInBounds = true;
			mouseDownAt = mouseEvent.Position;
			hitDragThreshold = false;

			wasSelected = this.IsSelected;

			this.EnsureSelection();

			if (IsDoubleClick(mouseEvent))
			{
				listViewItem.OnDoubleClick();
			}

			// On mouse down update the view3DWidget reference that will be used in MouseMove and MouseUp
			view3DWidget = ApplicationController.Instance.DragDropData.View3DWidget;

			base.OnMouseDown(mouseEvent);
		}

		public override void OnMouseMove(MouseEventArgs mouseEvent)
		{
			var delta = mouseDownAt - mouseEvent.Position;

			// If mouseDown on us and we've moved past are drag determination threshold, notify view3DWidget
			if (mouseDownInBounds && delta.Length > 40
				&& !(listViewItem.Model is MissingFileItem))
			{
				hitDragThreshold = true;

				// Performs move and possible Scene add in View3DWidget
				view3DWidget.ExternalDragOver(screenSpaceMousePosition: this.TransformToScreenSpace(mouseEvent.Position));
			}

			base.OnMouseMove(mouseEvent);
		}

		public override void OnMouseUp(MouseEventArgs mouseEvent)
		{
			this.OnItemSelect();

			var dropData = ApplicationController.Instance.DragDropData;
			if (dropData.View3DWidget?.DragOperationActive == true)
			{
				// Mouse and widget positions
				var screenSpaceMousePosition = this.TransformToScreenSpace(mouseEvent.Position);
				var meshViewerPosition = this.view3DWidget.meshViewerWidget.TransformToScreenSpace(view3DWidget.meshViewerWidget.LocalBounds);

				// Notify of drag operation complete
				view3DWidget.FinishDrop(mouseUpInBounds: meshViewerPosition.Contains(screenSpaceMousePosition));
			}

			mouseDownInBounds = false;
			base.OnMouseUp(mouseEvent);
		}

		public override void OnMouseEnterBounds(MouseEventArgs mouseEvent)
		{
			base.OnMouseEnterBounds(mouseEvent);
			mouseInBounds = true;
			UpdateHoverState();
			Invalidate();
		}

		public override void OnMouseLeaveBounds(MouseEventArgs mouseEvent)
		{
			mouseInBounds = false;
			base.OnMouseLeaveBounds(mouseEvent);
			UpdateHoverState();
			Invalidate();
		}

		protected virtual void UpdateColors()
		{
		}

		protected virtual void UpdateHoverState()
		{
		}

		public virtual bool IsHoverItem { get; set; }
		public virtual bool EditMode { get; set; }

		private bool isActivePrint = false;
		public bool IsActivePrint
		{
			get
			{
				return isActivePrint;
			}
			set
			{
				if (isActivePrint != value)
				{
					isActivePrint = value;
					UpdateColors();
				}
			}
		}

		private bool isSelected = false;

		public bool IsSelected
		{
			get
			{
				return isSelected;
			}
			set
			{
				if (isSelected != value)
				{
					//selectionCheckBox.Checked = value;

					isSelected = value;
					UpdateColors();
				}
			}
		}
	}
}