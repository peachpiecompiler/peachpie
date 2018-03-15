using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using ImageSharp;
using ImageSharp.Drawing.Brushes;
using ImageSharp.Formats;
using ImageSharp.Processing;
using Pchp.Core;
using Pchp.Library.Streams;
using SixLabors.Fonts;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace Peachpie.Library.Graphics
{
    [PhpExtension("gd")]
    public static class PhpGd2
    {
        #region GDVersionConstants

        /// <summary>
        /// The GD version PHP was compiled against.
        /// </summary>
        public const string GD_VERSION = "2.0.35";

        /// <summary>
        /// The GD major version PHP was compiled against.
        /// </summary>
        public const int GD_MAJOR_VERSION = 2;

        /// <summary>
        /// The GD minor version PHP was compiled against.
        /// </summary>
        public const int GD_MINOR_VERSION = 0;

        /// <summary>
        /// The GD release version PHP was compiled against.
        /// </summary>
        public const int GD_RELEASE_VERSION = 35;

        /// <summary>
        /// The GD "extra" version (beta/rc..) PHP was compiled against.
        /// </summary>
        public const string GD_EXTRA_VERSION = ""; //"beta";

        /// <summary>
        /// When the bundled version of GD is used this is 1 otherwise its set to 0.
        /// </summary>
        public const int GD_BUNDLED = 1;

        #endregion GDVersionConstants

        #region ImgType

        /// <summary>
        /// Image types enumeration, corresponds to IMGTYPE_ PHP constants.
        /// </summary>
        [Flags, PhpHidden]
        public enum ImgType
        {
            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            GIF = 1,

            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            JPG = JPEG,

            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            JPEG = 2,

            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            PNG = 4,

            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            WBMP = 8,

            /// <summary>
            /// Used as a return value by <see cref="imagetypes"/>.
            /// </summary>
            XPM = 16,

            /// <summary>
            /// A combinanation of IMG_ constants that are supported.
            /// </summary>
            Supported = GIF | JPEG | PNG,

            /// <summary>
            /// UNknown image type.
            /// </summary>
            Unknown = -1
        }

        public const int IMG_JPEG = (int)ImgType.JPEG;
        public const int IMG_GIF = (int)ImgType.GIF;
        public const int IMG_JPG = (int)ImgType.JPG;
        public const int IMG_PNG = (int)ImgType.PNG;
        public const int IMG_WBMP = (int)ImgType.WBMP;
        public const int IMG_XPM = (int)ImgType.XPM;

        #endregion ImgType

        #region IMG_GD2_*

        /// <summary>
        /// A type constant used by the imagegd2() function.
        /// </summary>
        public const int IMG_GD2_RAW = 1;

        /// <summary>
        /// A type constant used by the imagegd2() function.
        /// </summary>
        public const int IMG_GD2_COMPRESSED = 2;

        #endregion IMG_GD2_*

        #region FilledArcStyles

        /// <summary>
        /// Filled Arc Style types enumeration
        /// </summary>
        [Flags]
        public enum FilledArcStyles
        {
            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// This constant has the same value as IMG_ARC_PIE.
            /// </summary>
            ROUNDED = PIE,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            PIE = 0,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            CHORD = 1,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            NOFILL = 2,

            /// <summary>
            /// A style constant used by the <see cref="imagefilledarc"/> function.
            /// </summary>
            EDGED = 4,
        }

        public const int IMG_ARC_ROUNDED = (int)FilledArcStyles.ROUNDED;
        public const int IMG_ARC_PIE = (int)FilledArcStyles.PIE;
        public const int IMG_ARC_CHORD = (int)FilledArcStyles.CHORD;
        public const int IMG_ARC_NOFILL = (int)FilledArcStyles.NOFILL;
        public const int IMG_ARC_EDGED = (int)FilledArcStyles.EDGED;

        #endregion FilledArcStyles

        #region ColorValues

        /// <summary>
        /// Special Image Color values enumeration.
        /// </summary>
        public enum ColorValues
        {
            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            STYLED = -2,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            BRUSHED = -3,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            STYLEDBRUSHED = -4,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            TILED = -5,

            /// <summary>
            /// Special color option which can be used in stead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            TRANSPARENT = -6
        }

        public const int IMG_COLOR_STYLED = (int)ColorValues.STYLED;
        public const int IMG_COLOR_BRUSHED = (int)ColorValues.BRUSHED;
        public const int IMG_COLOR_STYLEDBRUSHED = (int)ColorValues.STYLEDBRUSHED;
        public const int IMG_COLOR_TILED = (int)ColorValues.TILED;
        public const int IMG_COLOR_TRANSPARENT = (int)ColorValues.TRANSPARENT;

        #endregion ColorValues

        #region FilterTypes

        /// <summary>
        /// Filled Arc Style types enumeration
        /// </summary>
        public enum FilterTypes
        {
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            NEGATE,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            GRAYSCALE,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            BRIGHTNESS,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            CONTRAST,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            COLORIZE,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            EDGEDETECT,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            EMBOSS,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            GAUSSIAN_BLUR,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            SELECTIVE_BLUR,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            MEAN_REMOVAL,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            SMOOTH,

            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter(PhpResource,int)"/> function.
            /// </summary>
            PIXELATE,
        }

        public const int IMG_FILTER_NEGATE = (int)FilterTypes.NEGATE;
        public const int IMG_FILTER_GRAYSCALE = (int)FilterTypes.GRAYSCALE;
        public const int IMG_FILTER_BRIGHTNESS = (int)FilterTypes.BRIGHTNESS;
        public const int IMG_FILTER_CONTRAST = (int)FilterTypes.CONTRAST;
        public const int IMG_FILTER_COLORIZE = (int)FilterTypes.COLORIZE;
        public const int IMG_FILTER_EDGEDETECT = (int)FilterTypes.EDGEDETECT;
        public const int IMG_FILTER_EMBOSS = (int)FilterTypes.EMBOSS;
        public const int IMG_FILTER_GAUSSIAN_BLUR = (int)FilterTypes.GAUSSIAN_BLUR;
        public const int IMG_FILTER_SELECTIVE_BLUR = (int)FilterTypes.SELECTIVE_BLUR;
        public const int IMG_FILTER_MEAN_REMOVAL = (int)FilterTypes.MEAN_REMOVAL;
        public const int IMG_FILTER_SMOOTH = (int)FilterTypes.SMOOTH;
        public const int IMG_FILTER_PIXELATE = (int)FilterTypes.PIXELATE;

        #endregion FilterTypes

        /// <summary>
        /// Retrieve information about the currently installed GD library
        /// </summary>
        /// <returns></returns>
        public static PhpArray gd_info()
        {
            var array = new PhpArray(13);

            array.Add("GD Version", "bundled (2.0 compatible)");
            array.Add("FreeType Support", true);
            array.Add("FreeType Linkage", "with TTF library");
            array.Add("T1Lib Support", false);
            array.Add("GIF Read Support", true);
            array.Add("GIF Create Support", true);
            array.Add("JPEG Support", true);
            array.Add("JPG Support", true);
            array.Add("PNG Support", true);
            array.Add("WBMP Support", false);
            array.Add("XPM Support", false);
            array.Add("XBM Support", false);
            array.Add("JIS-mapped Japanese Font Support", false); // Maybe is true because of .net unicode strings?

            return array;
        }

        /// <summary>
        /// Return the types of images supported in a bitfield - 1=GIF, 2=JPEG, 4=PNG, 8=WBMP, 16=XPM
        /// IMG_GIF | IMG_JPG | IMG_PNG | IMG_WBMP | IMG_XPM
        /// </summary>
        public static int imagetypes()
        {
            return (int)ImgType.Supported;
        }

        #region imagecopyresampled, imagecopyresized

        /// <summary>
        /// Copy and resize part of an image using resampling to help ensure clarity.
        /// </summary>
        public static bool imagecopyresampled(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h)
        {
            return ImageCopyAndResize(dst_im, src_im,
                dst_x, dst_y, src_x, src_y,
                dst_w, dst_h, src_w, src_h,
                new BicubicResampler());
        }

        /// <summary>
        /// Copy and resize part of an image.
        /// </summary>
        public static bool imagecopyresized(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h, int src_w, int src_h)
        {
            return ImageCopyAndResize(dst_im, src_im,
                dst_x, dst_y, src_x, src_y,
                dst_w, dst_h, src_w, src_h,
                new NearestNeighborResampler());
        }

        private static bool ImageCopyAndResize(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h,
            int src_w, int src_h, IResampler resampler)
        {
            var dst_img = PhpGdImageResource.ValidImage(dst_im);
            var src_img = PhpGdImageResource.ValidImage(src_im);

            if (dst_img == null || src_img == null)
            {
                return false;
            }

            //if (src_w == 0 && src_h == 0)
            //    return true;

            if (dst_w == 0 || dst_h == 0)
                return true;

            //if (dst_w < 0) dst_w = 0;
            //if (dst_h < 0) dst_h = 0;

            var src = src_img.Image
                .Crop(new Rectangle(src_x, src_y, src_w, src_h))
                .Resize(dst_w, dst_h, resampler);

            dst_img.Image.DrawImage(src, new Size(dst_w, dst_h), new Point(dst_x, dst_y), GraphicsOptions.Default);

            return true;
        }

        #endregion imagecopyresampled, imagecopyresized

        #region imagecreate*

        /// <summary>
        /// Create a new image
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreate(int x_size, int y_size)
        {
            var img = imagecreatecommon(x_size, y_size, new BmpConfigurationModule(), ImageFormats.Bitmap);

            img.Image.BackgroundColor(Rgba32.White);
            img.AlphaBlending = true;

            return img;
        }

        /// <summary>
        /// Create a new true color image
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatetruecolor(int x_size, int y_size)
        {
            var img = imagecreatecommon(x_size, y_size, new PngConfigurationModule(), ImageFormats.Png);

            img.Image.BackgroundColor(Rgba32.Black);
            img.AlphaBlending = true;

            return img;
        }

        private static PhpGdImageResource imagecreatecommon(int x_size, int y_size, IConfigurationModule configuration, IImageFormat format)
        {
            if (x_size <= 0 || y_size <= 0)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Resources.invalid_image_dimensions));
                return null;
            }

            return new PhpGdImageResource(x_size, y_size, configuration, format);
        }

        /// <summary>
        /// Create a new image from the image stream in the string
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromstring(byte[] image)
        {
            if (image == null || image.Length == 0)
            {
                PhpException.Throw(PhpError.Warning, Resources.empty_string_or_invalid_image);
                return null;
            }

            try
            {
                return new PhpGdImageResource(Image.Load(image, out IImageFormat format), format);
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, Resources.empty_string_or_invalid_image);
                return null;
            }
        }

        /// <summary>
        /// Create a new image from GD file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromgd(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename);
        }

        /// <summary>
        /// Create a new image from GD2 file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromgd2(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename);
        }

        /// <summary>
        /// Create a new image from a given part of GD2 file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromgd2part(Context ctx, string filename, int srcX, int srcY, int width, int height)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Create a new image from GIF file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromgif(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename, new GifConfigurationModule());
        }

        /// <summary>
        /// Create a new image from JPEG file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromjpeg(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename, new JpegConfigurationModule());
        }

        /// <summary>
        /// Create a new image from PNG file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefrompng(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename, new PngConfigurationModule());
        }

        /// <summary>
        /// Create a new image from WBMP file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromwbmp(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename);
        }

        /// <summary>
        /// Create a new image from XBM file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromxbm(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename);
        }

        /// <summary>
        /// Create a new image from XPM file or URL.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagecreatefromxpm(Context ctx, string filename)
        {
            return imagercreatefromfile(ctx, filename);
        }

        private static PhpGdImageResource imagercreatefromfile(Context ctx, string filename, IConfigurationModule formatOpt = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            var configuration = (formatOpt == null)
                ? Configuration.Default
                : new Configuration(formatOpt);

            Image<Rgba32> img = null;
            IImageFormat format = null;

            using (var stream = Utils.OpenStream(ctx, filename))
            {
                if (stream != null)
                {
                    try { img = Image.Load(configuration, stream, out format); }
                    catch { }
                }
            }

            return (img != null)
                ? new PhpGdImageResource(img, format)
                : null;
        }

        #endregion imagecreate*

        /// <summary>
        /// Destroy an image
        /// </summary>
        public static bool imagedestroy(PhpResource im)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }
            else
            {
                img.Dispose();
                return true;
            }
        }

        #region imagesx, imagesy

        /// <summary>
        /// Gets image width.
        /// </summary>
        [return: CastToFalse]
        public static int imagesx(PhpResource im)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;

            return img.Image.Width;
        }

        /// <summary>
        /// Gets image height.
        /// </summary>
        [return: CastToFalse]
        public static int imagesy(PhpResource im)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;

            return img.Image.Height;
        }

        #endregion imagesx, imagesy

        /// <summary>
        /// Turn alpha blending mode on or off for the given image
        /// </summary>
        public static bool imagealphablending(PhpResource im, bool blendmode)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null || img.IsIndexed)
            {
                return false;
            }

            // In PHP AlphaBlending is supported only in True color images
            if (!img.IsIndexed)
            {
                img.AlphaBlending = blendmode;
            }

            return true;
        }

        /// <summary>
        /// return true if the image uses truecolor
        /// </summary>
        public static bool imageistruecolor(PhpResource im)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            return !img.IsIndexed;
        }

        #region imagecolorallocate, imagecolorallocatealpha

        /// <summary>
        /// Allocate a color for an image
        /// </summary>
        public static long imagecolorallocate(PhpResource im, int red, int green, int blue)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1; // TODO: false

            //TODO: In non-truecolor images allocate the color
            return RGBA(red, green, blue);
        }

        /// <summary>
        /// Allocate a color with an alpha level.  Works for true color and palette based images.
        /// </summary>
        public static long imagecolorallocatealpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;// TODO: false

            //TODO: In non-truecolor images allocate the color
            return RGBA(red, green, blue, alpha);
        }

        /// <summary>
        /// RGBA values to PHP Color format.
        /// </summary>
        private static long RGBA(long red, long green, long blue, long alpha = 0xff)
        {
            return (alpha << 24)
                | ((red & 0x0000FF) << 16)
                | ((green & 0x0000FF) << 8)
                | (blue & 0x0000FF);
        }

        private static Rgba32 FromRGBA(long color) => new Rgba32((uint)color);

        #endregion imagecolorallocate, imagecolorallocatealpha

        /// <summary>
        /// Draws a pixel at the specified coordinate.
        /// </summary>
        public static bool imagesetpixel(PhpResource im, int x, int y, long color)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            var image = img.Image;

            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height)
            {
                return false;
            }

            image[x, y] = FromRGBA(color);

            return true;
        }

        /// <summary>
        /// Returns the index of the color of the pixel at the specified location in the image specified by image.
        /// </summary>
        public static long imagecolorat(PhpResource im, int x, int y)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return -1;
            }

            var image = img.Image;

            return (long)image[x, y].Rgba;
        }

        #region imagesavealpha

        /// <summary>
        /// Include alpha channel to a saved image
        /// </summary>
        public static bool imagesavealpha(PhpResource im, bool on)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            img.SaveAlpha = on;

            return true;
        }

        #endregion imagesavealpha

        #region imagerotate

        /// <summary>
        /// Rotates the image image using the given angle in degrees.
        /// The center of rotation is the center of the image, and the rotated image may have different dimensions than the original image.
        /// </summary>
        [return: CastToFalse]
        public static PhpResource imagerotate(PhpResource im, double angle, int bgcolor, bool ignore_transparent = false)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return null;
            }

            //
            return new PhpGdImageResource(new Image<Rgba32>(img.Image).Rotate((float)(angle * (-Math.PI / 180.0)), true), img.Format);
        }

        #endregion imagerotate

        #region imagerectangle, imagefilledrectangle

        /// <summary>
        /// Draw a rectangle
        /// </summary>
        public static bool imagerectangle(PhpResource im, int x1, int y1, int x2, int y2, long col)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            var rect = new RectangleF(x1, y1, x2 - x1, y2 - y1);

            img.Image.Draw(FromRGBA(col), 1.0f, rect);

            return true;
        }

        /// <summary>
        /// Draw a filled rectangle
        /// </summary>
        public static bool imagefilledrectangle(PhpResource im, int x1, int y1, int x2, int y2, long col)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            var rect = new RectangleF(x1, y1, x2 - x1 + 1, y2 - y1 + 1);

            if (col == (int)ColorValues.TILED)
            {
                if (img.tiled != null)
                {
                    img.Image.Fill(img.tiled, rect);
                }
            }
            else
            {
                img.Image.Fill(FromRGBA(col), rect);
            }

            return true;
        }

        #endregion imagerectangle, imagefilledrectangle

        #region imagesettile

        /// <summary>
        /// Set the tile image to $tile when filling $image with the "IMG_COLOR_TILED" color
        /// </summary>
        public static bool imagesettile(PhpResource image, PhpResource tile)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return false;
            }

            var imgTile = PhpGdImageResource.ValidImage(tile);
            if (imgTile == null)
            {
                return false;
            }

            img.tiled = new ImageBrush<Rgba32>(imgTile.Image);

            return false;
        }

        #endregion imagesettile

        #region imagettftext

        /// <summary>
        /// Write text to the image using a TrueType font
        /// </summary>
        [return: CastToFalse]
        public static PhpArray imagettftext(Context ctx, PhpResource im, double size, double angle, int x, int y, long color, string font_file, string text)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(font_file))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            var font_stream = PhpStream.Open(ctx, font_file, "rb");
            if (font_stream == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.invalid_font_filename, font_file);
                return null;
            }

            // Font preparation
            FontFamily family;

            try
            {
                family = new FontCollection().Install(font_stream.RawStream); // TODO: perf: global font collection cache

                if (ReferenceEquals(family, null))
                {
                    throw new InvalidOperationException();
                }
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, Resources.invalid_font_filename, font_file);
                return null;
            }
            finally
            {
                font_stream.Dispose();
            }

            FontStyle style;

            if (family.IsStyleAvailible(FontStyle.Regular))
            {
                style = FontStyle.Regular;
            }
            else if (family.IsStyleAvailible(FontStyle.Bold))
            {
                style = FontStyle.Bold;
            }
            else if (family.IsStyleAvailible(FontStyle.Italic))
            {
                style = FontStyle.Italic;
            }
            else if (family.IsStyleAvailible(FontStyle.BoldItalic))
            {
                style = FontStyle.BoldItalic;
            }
            else
            {
                return null;
            }

            var font = new Font(family, (float)size, style);
            var textsize = TextMeasurer.Measure(text, new RendererOptions(font));

            // text transformation:
            var matrix = (angle == 0.0) ? Matrix3x2.Identity : Matrix3x2.CreateRotation((float)(angle * -2.0 * Math.PI / 360.0f));
            matrix.Translation = new Vector2(x, y);

            var path = new SixLabors.Shapes.PathBuilder(matrix).AddLine(0, 0, textsize.Width, 0).Build();

            // draw the text:
            // TODO: col < 0 => turn off antialiasing
            img.Image.DrawText(text, font, FromRGBA(Math.Abs(color)), path);

            // calculate drawen text boundaries:
            var pts = new Vector2[]
            {
                new Vector2(0, textsize.Height), // lower left
                new Vector2(textsize.Width, textsize.Height), // lower right
                new Vector2(textsize.Width, 0), // upper right
                new Vector2(0, 0), // upper left
            };

            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = Vector2.Transform(pts[i], matrix);
            }

            return new PhpArray(8)
            {
                pts[0].X,
                pts[0].Y,

                pts[1].X,
                pts[1].Y,

                pts[2].X,
                pts[2].Y,

                pts[3].X,
                pts[3].Y,
            };
        }

        #endregion imagettftext

        #region imagecopy, imagecopymerge

        /// <summary>
        /// Copy a part of <paramref name="src_im"/> onto <paramref name="dst_im"/> starting at the x,y coordinates src_x, src_y with a width of src_w and a height of src_h.
        /// The portion defined will be copied onto the x,y coordinates, dst_x and dst_y.
        /// </summary>
        public static bool imagecopy(PhpResource dst_im, PhpResource src_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h)
        {
            return imagecopy(dst_im, src_im, dst_x, dst_y, src_x, src_y, src_w, src_h, 1.0f);
        }

        /// <summary>
        /// Merge one part of an image with another.
        /// </summary>
        public static bool imagecopymerge(PhpResource dst_im, PhpResource src_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, int pct)
        {
            return imagecopy(dst_im, src_im, dst_x, dst_y, src_x, src_y, src_w, src_h, pct * 0.01f);
        }

        private static bool imagecopy(PhpResource dst_im, PhpResource src_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, float opacity = 1.0f)
        {
            var dst = PhpGdImageResource.ValidImage(dst_im);
            var src = PhpGdImageResource.ValidImage(src_im);

            if (src == null || dst == null)
            {
                return false;
            }

            if (src_w <= 0 || src_h <= 0 || opacity <= 0)
            {
                // nothing to do
                return true;
            }

            try
            {
                dst.Image.DrawImage(src.Image.Crop(new Rectangle(src_x, src_y, src_w, src_h)), opacity, new Size(src_w, src_h), new Point(dst_x, dst_y));
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }

            return true;
        }

        #endregion imagecopy, imagecopymerge

        #region image2wbmp

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary>
        public static bool image2wbmp(Context ctx, PhpResource im, PhpValue to, int threshold = 0)
        {
            throw new NotImplementedException();
            //return imagesave(ctx, im, filename, (img, stream) => img.SaveAsWirelessBmp(stream));
        }

        public static bool image2wbmp(Context ctx, PhpResource im) => image2wbmp(ctx, im, PhpValue.Null);

        #endregion image2wbmp

        #region imagejpeg

        /// <summary>
        /// Output JPEG image to browser or a file.
        /// </summary>
        public static bool imagejpeg(Context ctx, PhpResource im, PhpValue to, int quality = 75)
        {
            var jpegoptions = new JpegEncoder() { Quality = Math.Min(Math.Max(quality, 0), 100) };
            return imagesave(ctx, im, to, (img, stream) => img.SaveAsJpeg(stream, jpegoptions));
        }

        public static bool imagejpeg(Context ctx, PhpResource im) => imagejpeg(ctx, im, PhpValue.Null);

        #endregion imagejpeg

        #region imagegd, imagegd2

        /// <summary>
        /// Output GD image to browser or file
        /// </summary>
        public static bool imagegd(PhpResource im)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Output GD2 image to browser or file
        /// </summary>
        public static bool imagegd2(Context ctx, PhpResource im, [Optional]PhpValue to, int chunk_size = 128, int type = IMG_GD2_RAW)
        {
            throw new NotImplementedException();
        }

        #endregion imagegd, imagegd2

        #region imagegif

        /// <summary>
        /// Output GIF image to browser or file
        /// </summary>
        public static bool imagegif(Context ctx, PhpResource im, [Optional]PhpValue to)
        {
            return imagesave(ctx, im, to, (img, stream) =>
            {
                img.BackgroundColor(Rgba32.Transparent);
                img.SaveAsGif(stream);
            });
        }

        public static bool imagegif(Context ctx, PhpResource im) => imagegif(ctx, im, PhpValue.Null);

        #endregion imagegif

        #region imagepng

        /// <summary>
        /// Output PNG image to browser or file or a stream.
        /// </summary>
        public static bool imagepng(Context ctx, PhpResource im, PhpValue to, int quality = 6, int filters = 0)
        {
            quality = Math.Min(Math.Max(quality, 0), 9);    // compression level 0 - 9

            return imagesave(ctx, im, to, (img, stream) =>
            {
                img.SaveAsPng(stream, new PngEncoder() { CompressionLevel = quality });
            });
        }

        public static bool imagepng(Context ctx, PhpResource im) => imagepng(ctx, im, PhpValue.Null);

        #endregion imagepng

        #region imagesave

        /// <summary>
        /// Internal image save.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="im">Image resource.</param>
        /// <param name="to">Optional. Filename or stream. If not specified the functiona saves the image to output stream.</param>
        /// <param name="saveaction">Callback that actually save the image to given stream. Called when all checks pass.</param>
        /// <returns>True if save succeeded.</returns>
        private static bool imagesave(Context ctx, PhpResource im, PhpValue to/* = null*/, Action<Image<Rgba32>, Stream> saveaction)
        {
            Debug.Assert(saveaction != null);

            // check the gd2 resource
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            try
            {
                // not specified stream or filename -> save to the output stream
                if (to.IsEmpty)
                {
                    saveaction(img.Image, ctx.OutputStream);
                    return true;
                }

                // filename specified?
                var filename = to.ToStringOrNull();
                if (filename != null)
                {
                    using (var stream = File.OpenWrite(System.IO.Path.Combine(ctx.WorkingDirectory, filename)))
                    {
                        saveaction(img.Image, stream);
                    }

                    return true;
                }

                // to a PHP stream ?
                // validate the stream resource, outputs warning in case of invalid resource
                var phpstream = PhpStream.GetValid(to.AsObject() as PhpResource, FileAccess.Write);
                if (phpstream == null)
                {
                    return false;
                }

                // save image to byte[] and pass it to php stream

                var ms = new MemoryStream();

                saveaction(img.Image, ms);

                phpstream.WriteBytes(ms.ToArray());
                phpstream.Flush();

                // stream is closed after the operation
                phpstream.Dispose();
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion imagesave

        // NEW FUNCTIONS

        #region imagefilter

        /// <summary>
        /// Applies a filter to an image
        /// </summary>
        public static bool imagefilter(PhpResource src_im, int filtertype) => imagefilter(src_im, filtertype, -1, -1, -1, -1);

        /// <summary>
        /// Applies a filter to an image
        /// </summary>
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1) => imagefilter(src_im, filtertype, arg1, -1, -1, -1);

        /// <summary>
        /// Applies a filter to an image
        /// </summary>
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1, int arg2) => imagefilter(src_im, filtertype, arg1, arg2, -1, -1);

        /// <summary>
        /// Applies a filter to an image
        /// </summary>
        public static bool imagefilter(PhpResource src_im, int filtertype, int arg1, int arg2, int arg3) => imagefilter(src_im, filtertype, arg1, arg2, arg3, -1);

        /// <summary>
        /// Applies a filter to an image
        /// </summary>
        public static bool imagefilter(PhpResource im, int filtertype, int arg1, int arg2, int arg3, int arg4)
        {
            if (arg1 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg1", arg1);
            }

            if (arg2 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg2", arg2);
            }

            if (arg3 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg3", arg3);
            }

            if (arg4 != -1)
            {
                PhpException.ArgumentValueNotSupported("arg4", arg4);
            }

            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            //TODO: (Maros) Not all filters are added here.

            switch (filtertype)
            {
                case (int)FilterTypes.NEGATE:
                    InvertColors(img.Image);
                    break;

                case (int)FilterTypes.GRAYSCALE:
                    MakeGrayscale(img.Image);
                    break;

                default:
                    return false;
            }

            return true;
        }

        #endregion imagefilter

        #region imageconvolution

        /// <summary>
        /// Apply a 3x3 convolution matrix, using coefficient div and offset
        /// </summary>
        public static PhpResource imageconvolution(PhpResource src_im, PhpArray matrix3x3, double div, double offset)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion imageconvolution

        #region imagecolortransparent

        /// <summary>
        /// Define a color as transparent
        /// </summary>
        public static int imagecolortransparent(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1; // TODO: (maros) check if it is -1 in PHP

            if (img.IsTransparentColSet == false)
            {
                return -1;
            }

            return Rgba32ColorToPHPColor(img.transparentColor);
        }

        /// <summary>
        /// Define a color as transparent
        /// </summary>
        public static int imagecolortransparent(PhpResource im, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1; // TODO: (maros) chekc if it is -1 in PHP

            img.transparentColor = PHPColorToRgba32Color(col);
            img.IsTransparentColSet = true;

            return col;
        }

        #endregion imagecolortransparent

        #region imagecolorsforindex

        /// <summary>
        /// Get the colors for an index
        /// </summary>
        public static PhpArray imagecolorsforindex(PhpResource im, int col)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return null;
        }

        #endregion imagecolorsforindex

        #region imagecolorset

        /// <summary>
        /// Set the color for the specified palette index
        /// </summary>
        public static void imagecolorset(PhpResource im, int col, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
        }

        #endregion imagecolorset

        #region imagefilledellipse

        /// <summary>
        /// Draw an ellipse
        /// </summary>
        public static bool imagefilledellipse(PhpResource im, int cx, int cy, int w, int h, int col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            if (img.tiled != null)
            {
                img.Image.Fill(img.tiled, new EllipsePolygon(cx - (w / 2), cy - (h / 2), w, h));
            }
            else
            {
                var brush = new SolidBrush<Rgba32>(GetAlphaColor(img, col));
                img.Image.Fill(brush, new EllipsePolygon(cx - (w / 2), cy - (h / 2), w, h));
            }

            return true;
        }

        #endregion imagefilledellipse
        
        /// <summary>
        /// Give the bounding box of a markerName using fonts via freetype2
        /// </summary>
        public static PhpArray imageftbbox(double size, double angle, string font_file, string text/*, PhpArray extrainfo*/)
        {
            //return imagettfbbox(size, angle, font_file, text);
            return null;
        }

        #region imageinterlace

        /// <summary>
        /// Enable or disable interlace
        /// </summary>
        public static int imageinterlace(PhpResource im, int interlace)
        {
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion imageinterlace

        #region imagecolorexact

        /// <summary>
        /// Get the index of the specified color
        /// </summary>
        public static int imagecolorexact(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion imagecolorexact

        #region imagecolorexactalpha

        /// <summary>
        /// Find exact match for colour with transparency
        /// </summary>
        public static int imagecolorexactalpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion imagecolorexactalpha

        #region imagecolormatch

        /// <summary>
        /// Makes the colors of the palette version of an image more closely match the true color version
        /// </summary>
        public static bool imagecolormatch(PhpResource im1, PhpResource im2)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return false;
        }

        #endregion imagecolormatch

        #region imagecolorresolve

        /// <summary>
        /// Get the index of the specified color or its closest possible alternative
        /// </summary>
        public static int imagecolorresolve(PhpResource im, int red, int green, int blue)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion imagecolorresolve

        #region imagecolorresolvealpha

        /// <summary>
        /// Resolve/Allocate a colour with an alpha level.  Works for true colour and palette based images
        /// </summary>
        public static int imagecolorresolvealpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            //TODO: (Maros) Used in non-truecolor images (palette images).
            //PhpException.FunctionNotSupported(PhpError.Warning);
            return -1;
        }

        #endregion imagecolorresolvealpha

        /// <summary>
        /// Inverts colors in specified Bitmap
        /// </summary>
        /// <param name="bitmap">processed bitmap</param>
        /// <returns>success indication</returns>
        private static bool InvertColors(Image<Rgba32>/*!*/bitmap)
        {
            Debug.Assert(bitmap != null);

            try
            {
                bitmap = bitmap.Invert(); // TODO: Test
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Makes specified Bitmap grayscaled
        /// </summary>
        /// <param name="bitmap">processed bitmap</param>
        /// <returns>success indication</returns>
        private static bool MakeGrayscale(Image<Rgba32>/*!*/bitmap)
        {
            Debug.Assert(bitmap != null);

            try
            {
                bitmap = bitmap.Grayscale(); // TODO: Test
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts .NET Color format to PHP Color format
        /// </summary>
        /// <param name="col">.NET Color</param>
        /// <returns>PHP Color</returns>
        private static int Rgba32ColorToPHPColor(Rgba32 col)
        {
            int alpha = col.A;

            int color = RGBToPHPColor(col.R, col.G, col.B);

            // PHP Alpha format to .NET Alpha format
            alpha = (byte)((1.0f - ((float)alpha / 255.0f)) * 127.0f);
            alpha = alpha << 24;

            return color | alpha;
        }

        /// <summary>
        /// RGB values to PHP Color format
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        private static int RGBToPHPColor(int red, int green, int blue)
        {
            int color = 0; // = 0x00 << 24;

            color = color | blue & 0x0000FF;
            color = color | ((green & 0x0000FF) << 8);
            color = color | ((red & 0x0000FF) << 16);
            return color;
        }

        /// <summary>
        /// Converts PHP Color format to .NET Color format (different alpha meaning)
        /// </summary>
        /// <param name="color">PHP Color</param>
        /// <returns>.NET Color</returns>
        private static Rgba32 PHPColorToRgba32Color(int color)
        {
            if (color == (int)ColorValues.TRANSPARENT)
            {
                return Rgba32.Transparent;
            }

            Rgba32 col;

            byte alpha = (byte)PHPColorToPHPAlpha(color);
            byte red = (byte)PHPColorToRed(color);
            byte green = (byte)PHPColorToGreen(color);
            byte blue = (byte)PHPColorToBlue(color);

            // PHP Alpha format to .NET Alpha format
            alpha = (byte)((1.0f - ((float)alpha / 127.0f)) * 255.0f);

            col = new Rgba32(red, green, blue, alpha);

            return col;
        }

        private static int PHPColorToPHPAlpha(int color)
        {
            int ret = (color & 0x0000FF << 24);
            ret = (ret >> 24);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToRed(int color)
        {
            int ret = (color & 0x0000FF << 16);
            ret = (ret >> 16);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToGreen(int color)
        {
            int ret = (color & 0x0000FF << 8);
            ret = (ret >> 8);
            ret = ret & (0x0000FF);

            return ret;
        }

        private static int PHPColorToBlue(int color)
        {
            return (color & 0x0000FF);
        }

        private static Rgba32 GetAlphaColor(PhpGdImageResource img, int col)
        {
            Rgba32 color;
            if (!img.AlphaBlending)
            {
                color = new Rgba32(PHPColorToRed(col), PHPColorToGreen(col), PHPColorToBlue(col), 255);
            }
            else
            {
                color = PHPColorToRgba32Color(col);
            }
            return color;
        }

        /// <summary>
        /// Adjust angles and size for same behavior as in PHP
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="range"></param>
        private static void AdjustAnglesAndSize(ref int w, ref int h, ref int s, ref int e, ref int range)
        {
            if (w < 0) w = 0;
            if (h < 0) h = 0;

            if (w > 1 && w <= 4) w -= 1;
            if (h > 1 && h <= 4) h -= 1;
            if (w > 4) w -= 2;
            if (h > 4) h -= 2;

            range = e - s;
            if (range < 360) range = range + (range / 360) * 360;
            if (range > 360) range = range - (range / 360) * 360;

            if (s < 360) s = s + (s / 360) * 360;
            if (e < 360) e = e + (e / 360) * 360;

            if (s < 0) s = 360 + s;
            if (e < 0) e = 360 + e;

            if (e > 360) e = e - (e / 360) * 360;
            if (s > 360) e = e - (e / 360) * 360;
        }

        #region TODO

        #region imagecolorstotal

        //TODO: See: https://github.com/SixLabors/ImageSharp/issues/488
        ///// <summary>
        ///// Find out the number of colors in an image's palette
        ///// </summary>
        //public static int imagecolorstotal(PhpResource im)
        //{
        //    PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
        //    if (img == null)
        //        return 0;

        //    var format = img.Image.PixelFormat;

        //    if ((format & PixelFormat.Format1bppIndexed) != 0)
        //        return 2;
        //    if ((format & PixelFormat.Format4bppIndexed) != 0)
        //        return 16;
        //    if ((format & PixelFormat.Format8bppIndexed) != 0)
        //        return 256;

        //    if ((format & PixelFormat.Indexed) != 0)
        //    {
        //        // count the palette
        //        try
        //        {
        //            // TODO: optimize, cache ?
        //            return img.Image.Palette.Entries.Length;
        //        }
        //        catch
        //        {
        //            // ignored, some error during SafeNativeMethods.Gdip.GdipGetImagePalette
        //        }
        //    }

        //    // non indexed image
        //    return 0;
        //}

        #endregion imagecolorstotal

        #region imagetruecolortopalette

        //TODO
        ///// <summary>
        ///// Convert a true colour image to a palette based image with a number of colours, optionally using dithering.
        ///// </summary>
        //public static bool imagetruecolortopalette(PhpResource im, bool ditherFlag, int colorsWanted)
        //{
        //    if (colorsWanted <= 0)
        //        return false;

        //    PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
        //    if (img == null)
        //        return false;

        //    if (img.IsIndexed)
        //        return true;     // already indexed

        //    // determine new pixel format:
        //    PixelFormat newformat;
        //    if (colorsWanted <= 2)
        //        newformat = PixelFormat.Format1bppIndexed;
        //    else if (colorsWanted <= 16)
        //        newformat = PixelFormat.Format4bppIndexed;
        //    else if (colorsWanted <= 256)
        //        newformat = PixelFormat.Format8bppIndexed;
        //    else
        //        newformat = PixelFormat.Indexed;

        //    // clone the image as indexed:
        //    var image = img.Image;
        //    var newimage = image.Clone(new Rectangle(0, 0, image.Width, image.Height), newformat);

        //    if (newimage == null)
        //        return false;

        //    img.Image = newimage;
        //    return true;
        //}

        #endregion imagetruecolortopalette

        #region imagefill

        //TODO
        ///// <summary>
        ///// Flood fill
        ///// </summary>
        //public static bool imagefill(PhpResource im, int x, int y, int col)
        //{
        //    PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
        //    if (img == null)
        //        return false;

        //    if (x < 0 || y < 0) return true;
        //    if (x > img.Image.Width || x > img.Image.Height) return true;

        //    //TODO: (Maros) COLOR_TILED is not implemented.

        //    //TODO: Can be optimized.
        //    FloodFill(img.Image, x, y, PHPColorToRgba32Color(col), false, Rgba32.Red);

        //    return true;
        //}

        #endregion imagefill

        #region imagefilledarc

        //TODO
        ///// <summary>
        ///// Draw a filled partial ellipse
        ///// </summary>
        //public static bool imagefilledarc(PhpResource im, int cx, int cy, int w, int h, int s, int e, int col, int style)
        //{
        //    PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
        //    if (img == null)
        //        return false;

        //    using (var g = Graphics.FromImage(img.Image))
        //    {
        //        g.SmoothingMode = SmoothingMode.None;
        //        Pen pen = CreatePen(col, img, false);

        //        int range = 0;
        //        AdjustAnglesAndSize(ref w, ref h, ref s, ref e, ref range);

        //        // IMG_ARC_PIE
        //        if (style == (int)FilledArcStyles.PIE || style == (int)FilledArcStyles.EDGED)
        //        {
        //            g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
        //            var brush = new SolidBrush<Rgba32>(GetAlphaColor(img, col));
        //            g.FillPie(brush, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
        //        }

        //        if (style == (int)FilledArcStyles.NOFILL)
        //        {
        //            g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
        //        }

        //        if (style == ((int)FilledArcStyles.EDGED | (int)FilledArcStyles.NOFILL))
        //        {
        //            Point[] points =
        //            {
        //                new Point(cx+(int)(Math.Cos(s*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(s*Math.PI/180) * (h / 2.0))),
        //                new Point(cx, cy),
        //                new Point(cx+(int)(Math.Cos(e*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(e*Math.PI/180) * (h / 2.0)))
        //            };

        //            g.DrawLines(pen, points);
        //            g.DrawArc(pen, new Rectangle(cx - (w / 2), cy - (h / 2), w, h), s, range);
        //        }

        //        // IMG_ARC_CHORD
        //        if (style == ((int)FilledArcStyles.CHORD) || style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.EDGED))
        //        {
        //            var brush = new SolidBrush<Rgba32>(GetAlphaColor(img, col));

        //            Point point1 = new Point(cx + (int)(Math.Cos(s * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(s * Math.PI / 180) * (h / 2.0)));
        //            Point point2 = new Point(cx + (int)(Math.Cos(e * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(e * Math.PI / 180) * (h / 2.0)));

        //            Point[] points = { new Point(cx, cy), point1, point2 };

        //            g.FillPolygon(brush, points);

        //        }

        //        if (style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.NOFILL))
        //        {
        //            g.DrawLine(pen,
        //                new Point(cx + (int)(Math.Cos(s * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(s * Math.PI / 180) * (h / 2.0))),
        //                new Point(cx + (int)(Math.Cos(e * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(e * Math.PI / 180) * (h / 2.0)))
        //                );
        //        }

        //        if (style == ((int)FilledArcStyles.CHORD | (int)FilledArcStyles.NOFILL | (int)FilledArcStyles.EDGED))
        //        {
        //            Point[] points =
        //            {
        //                new Point(cx, cy),
        //                new Point(cx+(int)(Math.Cos(s*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(s*Math.PI/180) * (h / 2.0))),
        //                new Point(cx+(int)(Math.Cos(e*Math.PI/180) * (w / 2.0)), cy+(int)(Math.Sin(e*Math.PI/180) * (h / 2.0)))
        //            };

        //            g.DrawPolygon(pen, points);
        //        }

        //        pen.Dispose();
        //    }

        //    return true;
        //}

        #endregion imagefilledarc

        //TODO
        //private static void FloodFill(Image<Rgba32>/*!*/image, int x, int y, Rgba32 color, bool toBorder, Rgba32 border)
        //{
        //    Debug.Assert(image != null);

        //    BitmapData data = image.LockBits(
        //        new Rectangle(0, 0, image.Width, image.Height),
        //        ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

        //    int[] bits = new int[data.Stride / 4 * data.Height];
        //    Marshal.Copy(data.Scan0, bits, 0, bits.Length);

        //    LinkedList<Point> check = new LinkedList<Point>();
        //    //int floodTo = color.ToArgb();
        //    uint floodTo = color.Rgba; // Correct?
        //    int floodFrom = bits[x + y * data.Stride / 4];
        //    bits[x + y * data.Stride / 4] = floodTo;

        //    //int floodBorder = border.ToArgb();
        //    uint floodBorder = border.Rgba; // Correct?

        //    if (floodFrom != floodTo)
        //    {
        //        check.AddLast(new Point(x, y));
        //        while (check.Count > 0)
        //        {
        //            Point cur = check.First.Value;
        //            check.RemoveFirst();

        //            foreach (Point off in new Point[]{
        //                new Point(0, -1), new Point(0, 1),
        //                new Point(-1, 0), new Point(1, 0)})
        //            {
        //                Point next = new Point(cur.X + off.X, cur.Y + off.Y);
        //                if (next.X >= 0 && next.Y >= 0 &&
        //                    next.X < data.Width &&
        //                    next.Y < data.Height)
        //                {
        //                    if (toBorder == false)
        //                    {
        //                        if (bits[next.X + next.Y * data.Stride / 4] == floodFrom)
        //                        {
        //                            check.AddLast(next);
        //                            bits[next.X + next.Y * data.Stride / 4] = floodTo;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        if ((bits[next.X + next.Y * data.Stride / 4] != floodBorder && bits[next.X + next.Y * data.Stride / 4] != floodTo))
        //                        {
        //                            check.AddLast(next);
        //                            bits[next.X + next.Y * data.Stride / 4] = floodTo;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    Marshal.Copy(bits, 0, data.Scan0, bits.Length);
        //    image.UnlockBits(data);
        //}

        #region imagefilledpolygon

        //TODO
        ///// <summary>
        ///// Draw a filled polygon
        ///// </summary>
        //public static bool imagefilledpolygon(PhpResource im, PhpArray point, int num_points, int col)
        //{
        //    return DrawPoly(im, point, num_points, col, true);
        //}

        //TODO
        ///// <summary>
        ///// Draws normal or filled polygon
        ///// </summary>
        ///// <param name="im"></param>
        ///// <param name="point"></param>
        ///// <param name="num_points"></param>
        ///// <param name="col"></param>
        ///// <param name="filled"></param>
        ///// <returns></returns>
        //private static bool DrawPoly(PhpResource im, PhpArray point, int num_points, int col, bool filled)
        //{
        //    if (im == null)
        //    {
        //        PhpException.Throw(PhpError.Warning, LibResources.GetString("unexpected_arg_given", 1, PhpResource.PhpTypeName, PhpVariable.TypeNameNull.ToLowerInvariant()));
        //        return false;
        //    }
        //    PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
        //    if (img == null)
        //        return false;

        //    if (point == null)
        //    {
        //        PhpException.Throw(PhpError.Warning, LibResources.GetString("unexpected_arg_given", 2, PhpArray.PhpTypeName, PhpVariable.TypeNameNull.ToLowerInvariant()));
        //        return false;
        //    }

        //    if (point.Count < num_points * 2)
        //        return false;

        //    if (num_points <= 0)
        //    {
        //        PhpException.Throw(PhpError.Warning, Utils.Resources.GetString("must_be_positive_number_of_points"));
        //        return false;
        //    }

        //    Point[] points = new Point[num_points];

        //    for (int i = 0, j = 0; i < num_points; i++, j += 2)
        //        points[i] = new Point(Pchp.Core.Convert.ObjectToInteger(point[j]), Pchp.Core.Convert.ObjectToInteger(point[j + 1]));

        //    using (Graphics g = Graphics.FromImage(img.Image))
        //    {
        //        g.SmoothingMode = SmoothingMode.None;

        //        if (filled)
        //        {
        //            if (col < 0)
        //            {
        //                if (col == (int)ColorValues.TILED)
        //                {
        //                    if (img.tiled != null)
        //                    {
        //                        //TODO: (Maros) TILED filles little more width in PHP (pixel wider).
        //                        g.FillPolygon(img.tiled, points);
        //                    }
        //                    return true;
        //                }
        //                //TODO: (Maros) BRUSHED_STYLED missing, BRUSHED and BRUSHED_STYLED has different behavior in PHP (brush image draw for every pixel drawn)
        //                //TODO: (Maros) STYLED has little different look (different angle of lines etc.)
        //                else if (col == -2 && img.styled != null)
        //                {
        //                    g.FillPolygon(img.styled, points);
        //                }
        //                else if (col == -3 && img.brushed != null)
        //                {
        //                    g.FillPolygon(img.brushed, points);
        //                }
        //                else
        //                {
        //                    return true;
        //                }
        //            }
        //            else
        //            {
        //                var brush = new SolidBrush<Rgba32>(PHPColorToRgba32Color(col));
        //                g.FillPolygon(brush, points);
        //            }

        //            using (Pen pen = CreatePen(col, img, false))
        //            {
        //                pen.LineJoin = LineJoin.Bevel;
        //                g.DrawPolygon(pen, points);
        //            }
        //        }
        //        else
        //        {
        //            using (Pen pen = CreatePen(col, img, true))
        //            {
        //                SetAntiAlias(img, g);

        //                pen.LineJoin = LineJoin.Bevel;
        //                g.DrawPolygon(pen, points);
        //            }
        //        }
        //    }

        //    return true;
        //}

        #endregion imagefilledpolygon

        #endregion TODO
        
        // END: NEW FUNCTIONS
    }
}