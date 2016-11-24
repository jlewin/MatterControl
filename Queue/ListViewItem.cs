/*
Copyright (c) 2014, Kevin Pope
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
using MatterHackers.Agg.VertexSource;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrinterCommunication;
using MatterHackers.VectorMath;
using System;
using System.Globalization;
using System.Threading.Tasks;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.DataConverters3D;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using LibraryProviders;
using System.Collections.Generic;

namespace MatterHackers.MatterControl.PrintQueue
{
	public class ListViewItem : GuiWidget
	{
		private CheckBox selectionCheckBox;

		private SlideWidget actionButtonContainer;

		private ConditionalClickWidget conditionalClickContainer;

		private TextWidget partLabel;

		private TextWidget partStatus;

		private GuiWidget selectionCheckBoxContainer;

		private FatFlatClickWidget viewButton;

		private TextWidget viewButtonLabel;

		private event EventHandler unregisterEvents;

		private int thumbWidth = 50;
		private int thumbHeight = 50;

		private bool imageRequested = false;

		public ILibraryItem ListItemModel { get; private set; }

		private QueueDataView queueDataView;

		public ListViewItem(ILibraryItem listItemData, QueueDataView queueDataView)
		{
			this.queueDataView = queueDataView;

			this.ListItemModel = listItemData;

			// Set Display Attributes
			this.VAnchor = VAnchor.FitToChildren;
			this.HAnchor = HAnchor.ParentLeftRight | HAnchor.FitToChildren;
			this.Height = 50;
			this.BackgroundColor = RGBA_Bytes.White;
			this.Padding = new BorderDouble(0);
			this.Margin = new BorderDouble(6, 0, 6, 6);

			var topToBottomLayout = new FlowLayoutWidget(FlowDirection.TopToBottom, HAnchor.ParentLeftRight);

			var topContentsFlowLayout = new FlowLayoutWidget(FlowDirection.LeftToRight, HAnchor.ParentLeftRight);
			{
				selectionCheckBoxContainer = new GuiWidget()
				{
					VAnchor = VAnchor.ParentBottomTop,
					Width = 40,
					Visible = false,
					Margin = new BorderDouble(left: 6)
				};

				selectionCheckBox = new CheckBox("")
				{
					Name = "List Item Checkbox",
					VAnchor = VAnchor.ParentCenter,
					HAnchor = HAnchor.ParentCenter
				};
				selectionCheckBoxContainer.AddChild(selectionCheckBox);

				var leftColumn = new FlowLayoutWidget(FlowDirection.LeftToRight, vAnchor: VAnchor.ParentTop | VAnchor.FitToChildren);
				topContentsFlowLayout.AddChild(leftColumn);

				// TODO: add in default thumbnail handling from parent or IListItem
				Thumbnail = new ImageWidget(thumbWidth, thumbHeight)
				{
					Name = "List Item Thumbnail",
					Image = StaticData.Instance.LoadIcon("140.png"),
					BackgroundColor = ActiveTheme.Instance.PrimaryAccentColor
				};
				leftColumn.AddChild(Thumbnail);

				// TODO: Move to caller
				// TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
				// textInfo.ToTitleCase(PrintItemWrapper.Name).Replace('_', ' ')

				partLabel = new TextWidget(listItemData.Name, pointSize: 14)
				{
					TextColor = RGBA_Bytes.Black,
					MinimumSize = new Vector2(1, 16)
				};

				partStatus = new TextWidget("{0}: {1}".FormatWith("Status".Localize().ToUpper(), "Queued to Print".Localize()), pointSize: 10)
				{
					AutoExpandBoundsToText = true,
					TextColor = RGBA_Bytes.Black,
					MinimumSize = new Vector2(50, 12)
				};

				var middleColumn = new FlowLayoutWidget(FlowDirection.TopToBottom)
				{
					VAnchor = VAnchor.ParentTop | VAnchor.FitToChildren,
					HAnchor = HAnchor.ParentLeftRight,
					Padding = new BorderDouble(8),
					Margin = new BorderDouble(10, 0)
				};
				middleColumn.AddChild(partLabel);
				middleColumn.AddChild(partStatus);

				topContentsFlowLayout.AddChild(middleColumn);
			}

			// The ConditionalClickWidget supplies a user driven Enabled property based on a delegate of your choosing
			conditionalClickContainer = new ConditionalClickWidget(() => this.EditMode)
			{
				HAnchor = HAnchor.ParentLeftRight,
				VAnchor = VAnchor.ParentBottomTop
			};
			conditionalClickContainer.Click += onQueueItemClick;

			topToBottomLayout.AddChild(topContentsFlowLayout);
			this.AddChild(topToBottomLayout);

			actionButtonContainer = getItemActionButtons();
			actionButtonContainer.Visible = false;
			this.AddChild(conditionalClickContainer);

			this.AddChild(actionButtonContainer);
		}

		public ImageWidget Thumbnail { get; set; }

		public event EventHandler<MouseEventArgs> DoubleClick;
		
		public object Tag { get; set; }

		private bool editMode = false;
		public bool EditMode
		{
			get
			{
				return editMode;
			}
			set
			{
				if (editMode != value)
				{
					editMode = value;
					UpdateColors();

					if (editMode)
					{
						selectionCheckBoxContainer.Visible = true;
						actionButtonContainer.Visible = false;
					}
					else
					{
						selectionCheckBoxContainer.Visible = false;
					}
				}
			}
		}

		private bool isHoverItem = false;
		public bool IsHoverItem
		{
			get { return isHoverItem; }
			set
			{
				if (this.isHoverItem != value)
				{
					this.isHoverItem = value;
					if (value && !this.EditMode)
					{
						this.actionButtonContainer.SlideIn();
					}
					else
					{
						this.actionButtonContainer.SlideOut();
					}

					UpdateColors();
				}
			}
		}

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
					isSelected = value;
					UpdateColors();
				}
			}
		}

		public bool IsViewHelper { get; set; }

		public override void OnDraw(Graphics2D graphics2D)
		{
			base.OnDraw(graphics2D);

			var widgetBorder = new RoundedRect(LocalBounds, 0);

			// Draw the hover border if the mouse is in bounds or if its the ActivePrint item
			if (mouseInBounds || (this.IsActivePrint && !this.EditMode))
			{
				//Draw interior border
				graphics2D.Render(new Stroke(widgetBorder, 3), ActiveTheme.Instance.SecondaryAccentColor);
			}

			if (!imageRequested)
			{
				imageRequested = true;
				//RequestImage();
			}
		}

		private bool mouseDownInBounds = false;
		private Vector2 mouseDownAt;

		public override void OnMouseDown(MouseEventArgs mouseEvent)
		{
			mouseDownInBounds = true;
			mouseDownAt = mouseEvent.Position;

			if (IsDoubleClick(mouseEvent))
			{
				DoubleClick?.Invoke(this, mouseEvent);
				//UiThread.RunOnIdle(ChangeCollection);
			}
			base.OnMouseDown(mouseEvent);
		}

		public override void OnMouseMove(MouseEventArgs mouseEvent)
		{
			var delta = mouseDownAt - mouseEvent.Position;
			if (mouseDownInBounds && delta.Length > 50)
			{
				// Set the QueueRowItem child as the DragSourceRowItem for use in drag/drop
				queueDataView.DragSourceRowItem = this;
			}

			base.OnMouseMove(mouseEvent);
		}

		public override void OnMouseUp(MouseEventArgs mouseEvent)
		{
			// If a valid click event occurs then set the selected index in our parent
			if (mouseDownInBounds &&
				mouseEvent.X > 56 && // Disregard clicks within the thumbnail region (x < 56)
				PositionWithinLocalBounds(mouseEvent.X, mouseEvent.Y))
			{
				queueDataView.SelectedItem = this;
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

		public override void OnClosed(EventArgs e)
		{
			unregisterEvents?.Invoke(this, null);
			base.OnClosed(e);
		}

		private async Task RequestImage()
		{
			var itemThumbnail = await ListItemModel?.GetThumbnail(thumbWidth, thumbHeight);
			if (itemThumbnail != null)
			{
				Thumbnail.Image = itemThumbnail;
			}
		}

		private void UpdateColors()
		{
			if (this.IsActivePrint && !this.EditMode)
			{
				this.BackgroundColor = ActiveTheme.Instance.SecondaryAccentColor;
				this.partLabel.TextColor = RGBA_Bytes.White;
				this.partStatus.TextColor = RGBA_Bytes.White;
				this.viewButton.BackgroundColor = RGBA_Bytes.White;
				this.viewButtonLabel.TextColor = ActiveTheme.Instance.SecondaryAccentColor;
			}
			else if (this.IsSelected)
			{
				this.BackgroundColor = ActiveTheme.Instance.PrimaryAccentColor;
				this.partLabel.TextColor = RGBA_Bytes.White;
				this.partStatus.TextColor = RGBA_Bytes.White;
				this.selectionCheckBox.TextColor = RGBA_Bytes.White;
				this.viewButton.BackgroundColor = RGBA_Bytes.White;
				this.viewButtonLabel.TextColor = ActiveTheme.Instance.SecondaryAccentColor;
			}
			else if (this.IsHoverItem)
			{
				this.BackgroundColor = RGBA_Bytes.White;
				this.partLabel.TextColor = RGBA_Bytes.Black;
				this.selectionCheckBox.TextColor = RGBA_Bytes.Black;
				this.partStatus.TextColor = RGBA_Bytes.Black;
				this.viewButton.BackgroundColor = ActiveTheme.Instance.SecondaryAccentColor;
				this.viewButtonLabel.TextColor = RGBA_Bytes.White;
			}
			else
			{
				this.BackgroundColor = new RGBA_Bytes(255, 255, 255, 255);
				this.partLabel.TextColor = RGBA_Bytes.Black;
				this.partStatus.TextColor = RGBA_Bytes.Black;
				this.selectionCheckBox.TextColor = RGBA_Bytes.Black;
				this.partStatus.TextColor = RGBA_Bytes.Black;
				this.viewButton.BackgroundColor = ActiveTheme.Instance.SecondaryAccentColor;
				this.viewButtonLabel.TextColor = RGBA_Bytes.White;
			}
		}

		private bool mouseInBounds = false;

		private SlideWidget getItemActionButtons()
		{
			var removeLabel = new TextWidget("Remove".Localize())
			{
				Name = "Queue Item " + ListItemModel.Name + " Remove",
				TextColor = RGBA_Bytes.White,
				VAnchor = VAnchor.ParentCenter,
				HAnchor = HAnchor.ParentCenter
			};

			var removeButton = new FatFlatClickWidget(removeLabel)
			{
				VAnchor = VAnchor.ParentBottomTop,
				BackgroundColor = ActiveTheme.Instance.PrimaryAccentColor,
				Width = 100
			};
			removeButton.Click += onRemovePartClick;

			viewButtonLabel = new TextWidget("View".Localize())
			{
				Name = "Queue Item " + ListItemModel.Name + " View",
				TextColor = RGBA_Bytes.White,
				VAnchor = VAnchor.ParentCenter,
				HAnchor = HAnchor.ParentCenter
			};

			viewButton = new FatFlatClickWidget(viewButtonLabel)
			{
				VAnchor = VAnchor.ParentBottomTop,
				BackgroundColor = ActiveTheme.Instance.SecondaryAccentColor,
				Width = 100
			};
			viewButton.Click += onViewPartClick;

			var buttonFlowContainer = new FlowLayoutWidget(FlowDirection.LeftToRight)
			{
				VAnchor = VAnchor.ParentBottomTop
			};
			buttonFlowContainer.AddChild(viewButton);
			buttonFlowContainer.AddChild(removeButton);

			var buttonContainer = new SlideWidget()
			{
				VAnchor = VAnchor.ParentBottomTop,
				HAnchor = HAnchor.ParentRight
			};
			buttonContainer.AddChild(buttonFlowContainer);
			buttonContainer.Width = 200;

			return buttonContainer;
		}

		private async void UpdateHoverState()
		{
			if(!mouseInBounds)
			{
				IsHoverItem = false;
				return;
			}

			// Hover only occurs after mouse is in bounds for a given period of time
			await Task.Delay(500);

			if (!mouseInBounds)
			{
				IsHoverItem = false;
				return;
			}

			switch (UnderMouseState)
			{
				case UnderMouseState.NotUnderMouse:
					IsHoverItem = false;
					break;

				case UnderMouseState.FirstUnderMouse:
					IsHoverItem = true;
					break;

				case UnderMouseState.UnderMouseNotFirst:
					IsHoverItem = ContainsFirstUnderMouseRecursive();
					break;
			}
		}

		private void onQueueItemClick(object sender, EventArgs e)
		{
			if (this.IsSelected)
			{
				this.IsSelected = false;
				this.selectionCheckBox.Checked = false;
			}
			else
			{
				this.IsSelected = true;
				this.selectionCheckBox.Checked = true;
			}
		}

		private void onRemovePartClick(object sender, EventArgs e)
		{
			this.actionButtonContainer.SlideOut();
			//UiThread.RunOnIdle(DeletePartFromQueue);
		}

		private void onViewPartClick(object sender, EventArgs e)
		{
			this.actionButtonContainer.SlideOut();
			//UiThread.RunOnIdle(() =>
			//{
			//	OpenPartViewWindow(View3DWidget.OpenMode.Viewing);
			//});
		}
	}
	/*
	public class FolderItem : IListItem
	{
		public string ID { get; private set; }
		public string Name { get; set; }
		public bool IsContainer { get; set; } = false;
		public string Category { get; set; }

		public Task<ImageBuffer> GetThumbnail(int width, int height)
		{
			return Task.FromResult(StaticData.Instance.LoadIcon(Path.Combine("FileDialog", "folder.png")));
		}

		public Task<IObject3D> GetContent()
		{
			throw new NotImplementedException();
		}

		public void SetContent(IObject3D item)
		{

		}
	}
 
	public class ListItem : IListItem
	{
		public string ID { get; private set; }
		public string Name { get; set; }
		public bool IsContainer { get; set; } = false;
		public string Category { get; set; }

		public Func<Task<IObject3D>> Collector { get; set; }
		
		public Task<ImageBuffer> GetThumbnail(int width, int height)
		{
			//return Collector?.Invoke();
			return null;
		}

		public Task<IObject3D> GetContent() => Collector?.Invoke();

		public void SetContent(IObject3D item)
		{
			throw new NotImplementedException();
		}
	} */

	/*
	public interface IListItem
	{
		string ID { get; }
		string Name { get; set; }
		bool IsContainer { get; set; }
		string Category { get; set; }
		Task<ImageBuffer> GetThumbnail(int width, int height);
		Task<IObject3D> GetContent();
		void SetContent(IObject3D item);
	}*/

	public class SimpleItem : ILibraryPrintItem
	{
		public SimpleItem(string name, Func<IObject3D> collector)
		{
			this.Name = name;
			this.Collector = collector;
		}

		public bool IsContainer { get; set; } = false;
		public string ID { get; set; }
		public string Category { get; set; }
		public string Name { get; set; }

		/// <summary>
		/// The delegate responsible for producing the item
		/// </summary>
		public Func<IObject3D> Collector { get; set; }

		public Task<IObject3D> GetContent() => Task.FromResult<IObject3D>(Collector?.Invoke());

		public Task<ImageBuffer> GetThumbnail(int height, int width)
		{
			var xxx = GetContent();
			return null;
		}

		public void SetContent(IObject3D item)
		{
		}

		public void SetThumbnail(int width, int height, ImageBuffer imageBuffer)
		{
			throw new NotImplementedException();
		}
	}
}