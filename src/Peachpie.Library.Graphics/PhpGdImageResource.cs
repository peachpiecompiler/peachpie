using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ImageSharp;
using ImageSharp.Formats;
using Pchp.Core;
using TImage = ImageSharp.Image<ImageSharp.Rgba32>;

namespace Peachpie.Library.Graphics
{
    /// <summary>
    /// <see cref="PhpResource"/> representing PHP image.
    /// </summary>
    internal class PhpGdImageResource : PhpResource
    {
        /// <summary>
        /// Underlaying <see cref="Image"/> object.
        /// Cannot be <c>null</c> reference until it is not disposed.
        /// </summary>
        public TImage/*!*/Image
        {
            get
            {
                return _image;
            }
            internal set
            {
                _image = value ?? throw new ArgumentNullException();
            }
        }
        TImage/*!*/_image;
        IImageFormat _format;

        /// <summary>
        /// Determine if the pixel format is indexed.
        /// </summary>
        public bool IsIndexed =>
            _format == ImageFormats.Gif ||
            _format == ImageFormats.Bitmap;
        
        internal bool AlphaBlending = false;
        internal bool SaveAlpha = false;
        internal bool AntiAlias = false;

        //internal Rgba32 transparentColor;
        internal bool IsTransparentColSet = false;

        //internal TextureBrush styled;
        //internal TextureBrush brushed;
        //internal TextureBrush tiled;

        internal int LineThickness = 1;

        //public bool BackgroundAllocated { get; set; }

        private PhpGdImageResource()
            : base("GdImage")
        {
        }

        internal PhpGdImageResource(int x, int y, IConfigurationModule configuration, IImageFormat format)
            : this(new TImage(new Configuration(configuration), x, y), format)
        {
        }

        /// <summary>
        ///  Creates PhpGdImageResource without creating internal image
        /// </summary>
        internal PhpGdImageResource(TImage/*!*/image, IImageFormat format)
            : this()
        {
            Debug.Assert(image != null);
            _image = image;
            _format = format;
        }

        /// <summary>
        /// Free resources owned by this resource.
        /// </summary>
        protected override void FreeManaged()
        {
            if (_image != null)
            {
                _image.Dispose();
                _image = null;
            }

            //if (this.styled != null)
            //{
            //    this.styled.Dispose();
            //    this.styled = null;
            //}
            //if (this.brushed != null)
            //{
            //    this.brushed.Dispose();
            //    this.brushed = null;
            //}
            //if (this.tiled != null)
            //{
            //    this.tiled.Dispose();
            //    this.tiled = null;
            //}

            base.FreeManaged();
        }

        /// <summary>
        /// Checks if resource is really image and if it exists
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns> 
        /// <exception cref="PhpException">Warning when resource is not valid <see cref="PhpGdImageResource"/>.</exception>
        internal static PhpGdImageResource ValidImage(PhpResource handle)
        {
            var result = handle as PhpGdImageResource;
            if (result != null && result.IsValid && result._image != null)
            {
                return result;
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.image_resource_not_valid);
                return null;
            }
        }
    }
}
