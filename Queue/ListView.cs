/*
Copyright (c) 2016, John Lewin
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
using System.IO;
using LibraryProviders;
using MatterHackers.Agg;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.VectorMath;
/*
namespace MatterHackers.MatterControl.PrintQueue
{
	public interface IHoveredItem
	{
		bool IsHovered { get; set; }
		void Open();
		string Title { get; set; }
		string HelpText { get; set; }
	}

	public class ListView : ScrollableWidget
	{
		private event EventHandler unregisterEvents;

		private bool mouseDownWithinContainer = false;

		private bool editMode = false;

		internal FlowLayoutWidget itemsContainer;

		public ListView()
		{
			// Set Display Attributes
			this.MinimumSize = new Vector2(0, 200);
			this.AnchorAll();
			this.BackgroundColor = ActiveTheme.Instance.SecondaryBackgroundColor;
			this.AutoScroll = true;
			this.ScrollArea.Padding = new BorderDouble(3);

			// AddWatermark
			string imagePath = Path.Combine("OEMSettings", "watermark.png");
			if (StaticData.Instance.FileExists(imagePath))
			{
				this.AddChildToBackground(new ImageWidget(StaticData.Instance.LoadImage(imagePath))
				{
					VAnchor = VAnchor.ParentCenter,
					HAnchor = HAnchor.ParentCenter
				});
			}

			this.ScrollArea.HAnchor = HAnchor.ParentLeftRight;

			AutoScroll = true;
			itemsContainer = new FlowLayoutWidget(FlowDirection.TopToBottom)
			{
				Name = "PrintQueueControl TopToBottom",
				HAnchor = HAnchor.ParentLeftRight
			};
			base.AddChild(itemsContainer);

			for (int i = 0; i < QueueData.Instance.Count; i++)
			{
				itemsContainer.AddChild(new WrappedQueueRowItem(null, QueueData.Instance.GetPrintItemWrapper(i)));
			}

			this.MouseLeaveBounds += (sender, e) =>
			{
				if (HoverItem != null)
				{
					HoverItem.IsHoverItem = false;
				}
			};
		}

		public bool EditMode
		{
			get { return editMode; }
			set
			{
				if (this.editMode != value)
				{
					this.editMode = value;
					if (!this.editMode)
					{
						this.ClearSelectedItems();
						this.SelectedIndex = -1;
					}
					else
					{
						foreach (var item in SelectedItems)
						{
							item.isSelectedItem = true;
							item.selectionCheckBox.Checked = true;
						}
					}
				}
			}
		}

		public List<ListViewItem> SelectedItems = new List<ListViewItem>();

		public ILibraryItem DragSourceRowItem { get; internal set; }

		public ILibraryItem HoverItem { get; internal set; }

		public int SelectedIndex { get; set; }

		public void ClearSelectedItems()
		{
			foreach (var item in SelectedItems)
			{
				item.isSelectedItem = false;
				item.selectionCheckBox.Checked = false;
			}
			this.SelectedItems.Clear();
		}

		public override void OnClosed(EventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		public override void OnMouseDown(MouseEventArgs mouseEvent)
		{
			mouseDownWithinContainer = itemsContainer.LocalBounds.Contains(mouseEvent.Position);
			base.OnMouseDown(mouseEvent);
		}

		public override void OnMouseUp(MouseEventArgs mouseEvent)
		{
			mouseDownWithinContainer = false;
			this.SuppressScroll = false;
			base.OnMouseUp(mouseEvent);
		}

		public override void OnMouseMove(MouseEventArgs mouseEvent)
		{
			this.SuppressScroll = mouseDownWithinContainer && !PositionWithinLocalBounds(mouseEvent.X, 20);
			base.OnMouseMove(mouseEvent);
		}
	}
}*/