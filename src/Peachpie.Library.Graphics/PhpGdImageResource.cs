using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using Pchp.Core;
using TImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Drawing.Brushes;

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

        /// <summary>
        /// Image format.
        /// </summary>
        public IImageFormat Format { get { return _format; } }
        IImageFormat _format;

        /// <summary>
        /// Determine if the pixel format is indexed.
        /// </summary>
        public bool IsIndexed =>
            _format == ImageFormats.Gif ||
            _format == ImageFormats.Bmp;
        
        internal bool AlphaBlending = false;
        internal bool SaveAlpha = false;
        internal bool AntiAlias = false;

        internal Rgba32 transparentColor;
        internal bool IsTransparentColSet = false;

        internal IBrush<Rgba32> styled = null;
        internal IBrush<Rgba32> brushed = null;
        internal IBrush<Rgba32> tiled = null;

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
