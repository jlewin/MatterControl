/*
Copyright (c) 2023, Lars Brubaker, John Lewin
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
using System.Threading.Tasks;
using MatterControlLib.DesignTools.Operations.Path;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.DataConverters3D;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.PolygonMesh.Processors;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.DesignTools.Primitives
{
    public class BoxPathObject3D : PathObject3DAbstract, IObject3DControlsProvider, IEditorDraw, IPropertyGridModifier, IStaticThumbnail
    {
        public BoxPathObject3D()
        {
            Name = "Box".Localize();
            Color = Operations.Object3DExtensions.PrimitiveColors["Cube"];
        }

        public override bool MeshIsSolidObject => false;

        public static double MinEdgeSize = .001;

        public string ThumbnailName => "Box";

        /// <summary>
        /// This is the actual serialized with that can use expressions
        /// </summary>
        [MaxDecimalPlaces(2)]
        [Slider(1, 400, Easing.EaseType.Quadratic, useSnappingGrid: true)]
        public double Width { get; set; } = 20;

        [MaxDecimalPlaces(2)]
        [Slider(1, 400, Easing.EaseType.Quadratic, useSnappingGrid: true)]
        public double Depth { get; set; } = 20;

        public bool Round { get; set; }

        [Slider(0, 20, Easing.EaseType.Quadratic, snapDistance: .1)]
        public double Radius { get; set; } = 3;

        [Slider(1, 30, Easing.EaseType.Quadratic, snapDistance: 1)]
        public int RoundSegments { get; set; } = 9;

        public static async Task<BoxPathObject3D> Create()
        {
            var item = new BoxPathObject3D();
            await item.Rebuild();
            return item;
        }

        public void AddObject3DControls(Object3DControlsLayer object3DControlsLayer)
        {
            var width = new Property<double>
            {
	            Get = () => Width,
	            Set = (value) => Width = value
            };

            var depth = new Property<double>
            {
	            Get = () => Depth,
	            Set = (value) => Depth = value
            };

            object3DControlsLayer.AddControls(ControlTypes.MoveInZ);
            object3DControlsLayer.AddWidthDepthControls(this, width, depth, null);

            object3DControlsLayer.AddControls(ControlTypes.MoveInZ);
            object3DControlsLayer.AddControls(ControlTypes.RotateXYZ);
        }

        public override async void OnInvalidate(InvalidateArgs invalidateArgs)
        {
            if (invalidateArgs.InvalidateType.HasFlag(InvalidateType.Properties) && invalidateArgs.Source == this)
            {
                await Rebuild();
            }
            else
            {
                base.OnInvalidate(invalidateArgs);
            }
        }

        public override Task Rebuild()
        {
            this.DebugDepth("Rebuild");

            using (RebuildLock())
            {
                using (new CenterAndHeightMaintainer(this))
                {
                    var width = double.Clamp(Width, MinEdgeSize, 1000000);
                    var depth = double.Clamp(Depth, MinEdgeSize, 1000000);
                    var roundSegments = int.Clamp(RoundSegments, 1, 90);
                    var roundRadius = double.Clamp(Radius, 0, Math.Min(width, depth) / 2);

                    if (Round)
                    {
                        var roundRect = new RoundedRect(-width / 2, -depth / 2, width / 2, depth / 2, roundRadius);
                        roundRect.NumSegments = roundSegments;
                        VertexStorage = new VertexStorage(roundRect);
                    }
                    else
                    {
                        VertexStorage = new VertexStorage(new RoundedRect(-width / 2, -depth / 2, width / 2, depth / 2, 0));
                    }

                    Mesh = VertexStorage.Extrude(Constants.PathPolygonsHeight);
                }
            }

            this.CancelAllParentBuilding();
            Parent?.Invalidate(new InvalidateArgs(this, InvalidateType.Path));
            return Task.CompletedTask;
        }

        public void UpdateControls(PublicPropertyChange change)
        {
            change.SetRowVisible(nameof(RoundSegments), () => Round);
            change.SetRowVisible(nameof(Radius), () => Round);
        }
    }
}