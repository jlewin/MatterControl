/*
Copyright (c) 2014, Lars Brubaker
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

using MatterHackers.Agg.Image;

namespace MatterHackers.MatterControl.Extensibility
{
    public static class ImageIO
    {
        static ImageIOPlugin imageIOPlugin = null;
        static ImageIOPlugin AvailableImageIOPlugin
        {
            get
            {
                if (imageIOPlugin == null)
                {
                    imageIOPlugin = Configuration.LoadProviderFromAssembly("MatterHackers.Agg.Image.ImageIOWindowsPlugin, agg_platform_win32") as ImageIOPlugin;
                    if (imageIOPlugin == null)
                    {
                        throw new Exception(string.Format("Unable to load the ImageIO provider"));
                    }
                }

                return imageIOPlugin;
            }
        }

        static public bool LoadImageData(String pathToGifFile, ImageSequence destImageSequence)
        {
            return AvailableImageIOPlugin.LoadImageData(pathToGifFile, destImageSequence);
        }

        static public bool LoadImageData(String fileName, ImageBuffer destImage)
        {
            return AvailableImageIOPlugin.LoadImageData(fileName, destImage);
        }

        static public bool LoadImageData(String filename, ImageBufferFloat destImage)
        {
            return AvailableImageIOPlugin.LoadImageData(filename, destImage);
        }

        static public bool SaveImageData(String filename, IImageByte sourceImage)
        {
            return AvailableImageIOPlugin.SaveImageData(filename, sourceImage);
        }
    }
}
