﻿using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Pchp.Core;
using Pchp.Library.Streams;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

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
        /// When the bundled version of GD is used, this is 1 otherwise it's set to 0.
        /// </summary>
        public const int GD_BUNDLED = 1;

        #endregion

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

            WEBP = 32,

            BMP = 64,

            TGA = 128,

            /// <summary>
            /// A combination of IMG_ constants that are supported.
            /// </summary>
            Supported = GIF | JPEG | PNG,

            /// <summary>
            /// Unknown image type.
            /// </summary>
            Unknown = -1
        }

        public const int IMG_JPEG = (int)ImgType.JPEG;
        public const int IMG_GIF = (int)ImgType.GIF;
        public const int IMG_JPG = (int)ImgType.JPG;
        public const int IMG_PNG = (int)ImgType.PNG;
        public const int IMG_WBMP = (int)ImgType.WBMP;
        public const int IMG_XPM = (int)ImgType.XPM;
        public const int IMG_WEBP = (int)ImgType.WEBP;
        public const int IMG_BMP = (int)ImgType.BMP;
        public const int IMG_TGA = (int)ImgType.TGA;

        #endregion

        #region IMG_GD2_*

        /// <summary>
        /// A type constant used by the imagegd2() function.
        /// </summary>
        public const int IMG_GD2_RAW = 1;

        /// <summary>
        /// A type constant used by the imagegd2() function.
        /// </summary>
        public const int IMG_GD2_COMPRESSED = 2;

        #endregion

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

        #endregion

        #region ColorValues

        /// <summary>
        /// Special Image Color values enumeration.
        /// </summary>
        public enum ColorValues
        {
            /// <summary>
            /// Special color option which can be used instead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            STYLED = -2,

            /// <summary>
            /// Special color option which can be used instead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
            /// </summary>
            BRUSHED = -3,

            /// <summary>
            /// Special color option which can be used instead of color allocated with <see cref="imagecolorallocate"/> or <see cref="imagecolorallocatealpha"/>.
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

        #endregion

        #region FilterTypes

        /// <summary>
        /// Filled Arc Style types enumeration
        /// </summary>
        public enum FilterTypes
        {
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            NEGATE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            GRAYSCALE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            BRIGHTNESS,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            CONTRAST,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            COLORIZE,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            EDGEDETECT,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            EMBOSS,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            GAUSSIAN_BLUR,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            SELECTIVE_BLUR,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            MEAN_REMOVAL,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
            /// </summary>
            SMOOTH,
            /// <summary>
            /// Special GD filter used by the <see cref="imagefilter"/> function.
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

        #endregion

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
        /// Return the types of images supported in a bitfield of <c>IMG_*</c> constants.
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

        static bool ImageCopyAndResize(PhpResource dst_im, PhpResource src_im,
            int dst_x, int dst_y, int src_x, int src_y, int dst_w, int dst_h,
            int src_w, int src_h, IResampler resampler)
        {
            var dst_img = PhpGdImageResource.ValidImage(dst_im);
            var src_img = PhpGdImageResource.ValidImage(src_im);

            if (dst_img == null || src_img == null)
            {
                return false;
            }

            if (dst_w == 0 || dst_h == 0)
            {
                return true;
            }

            bool flip_w = false;
            bool flip_h = false;

            // normalize position
            if (dst_w < 0)
            {
                dst_x += dst_w + 1;
                dst_w = -dst_w;
                flip_w = !flip_w;
            }

            if (dst_h < 0)
            {
                dst_y += dst_h + 1;
                dst_h = -dst_h;
                flip_h = !flip_h;
            }

            if (src_w < 0)
            {
                src_x += src_w + 1;
                src_w = -src_w;
                flip_w = !flip_w;
            }

            if (src_h < 0)
            {
                src_y += src_h + 1;
                src_h = -src_h;
                flip_h = !flip_h;
            }

            //
            using (var cropped = src_img.Image.Clone(o =>
            {
                o = o.Crop(new Rectangle(src_x, src_y, src_w, src_h));
                o = o.Resize(dst_w, dst_h, resampler);

                if (flip_w) o = o.Flip(FlipMode.Horizontal);
                if (flip_h) o = o.Flip(FlipMode.Vertical);
            }))
            {
                dst_img.Image.Mutate(o => o.DrawImage(cropped, new Point(dst_x, dst_y), new GraphicsOptions { /* GraphicsOptions */ }));
            }

            return true;
        }

        #endregion

        #region imagecreate*

        /// <summary>
        /// Create a new image
        /// </summary> 
        [return: CastToFalse]
        public static PhpResource imagecreate(int x_size, int y_size)
        {
            var img = imagecreatecommon(x_size, y_size, new BmpConfigurationModule(), BmpFormat.Instance);

            img.Image.Mutate(o => o.BackgroundColor(Color.White));
            img.AlphaBlending = true;

            return img;
        }

        /// <summary>
        /// Create a new true color image
        /// </summary> 
        [return: CastToFalse]
        public static PhpResource imagecreatetruecolor(int x_size, int y_size)
        {
            var img = imagecreatecommon(x_size, y_size, new PngConfigurationModule(), PngFormat.Instance);

            img.Image.Mutate(o => o.BackgroundColor(Color.Black));
            img.AlphaBlending = true;

            return img;
        }

        static PhpGdImageResource imagecreatecommon(int x_size, int y_size, IImageFormatConfigurationModule configuration, IImageFormat format)
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
                return new PhpGdImageResource(
                    Image.Load<Rgba32>(image.AsSpan())
                );
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

        static PhpGdImageResource imagercreatefromfile(Context ctx, string filename, IImageFormatConfigurationModule formatOpt = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            var decoderOptions =  (formatOpt == null)
                ? new DecoderOptions()
                : new DecoderOptions() { Configuration = new Configuration(formatOpt) }
                ;

            Image<Rgba32> img = null;
            
            using (var stream = Utils.OpenStream(ctx, filename))
            {
                if (stream != null)
                {
                    try
                    {
                        img = Image.Load<Rgba32>(decoderOptions, stream);
                    }
                    catch { }
                }
            }

            return (img != null)
                ? new PhpGdImageResource(img)
                : null;
        }

        #endregion

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

        #endregion

        #region Helpers

        /// <summary>
        /// RGBA values to PHP Color format.
        /// </summary>
        static long RGBA(long red, long green, long blue, long transparency = 0)
        {
            return (transparency << 24)
                | ((red & 0x0000FF) << 16)
                | ((green & 0x0000FF) << 8)
                | (blue & 0x0000FF);
        }

        static Rgba32 FromRGB(long color) => new Rgba32((uint)color | 0xff000000u);

        /// <summary>
        /// Php Alpha is reversed map. PHP: 0 -> PNG,ImageSharp:255, PHP: 127 -> PNG,ImageSharp: 0. This method converts an alpha part of a color from PHP/ImageSharp to ImageSharp/PHP.
        /// </summary>
        /// <param name="color"></param>
        /// <returns>Converted color</returns>
        static Rgba32 FromRGBA(long color) => (color != (long)ColorValues.TRANSPARENT) ? PHPColorToRgba((uint)color) : (Rgba32)Color.Transparent;

        /// <summary>
        /// Converts Rgba32 to PHP color with PHP's alpha format (in range 0-127).
        /// </summary>
        /// <param name="rgba">The color.</param>
        static long RgbaToPHPColor(this Rgba32 rgba)
        {
            byte transparency = GetPhpTransparency(rgba);

            return (rgba.Rgba & 0x00ffffffu) | ((uint)transparency << 24);
        }

        /// <summary>
        /// Gets transparency in range of [0..127] where 127 is fully transparent color.
        /// </summary>
        static byte GetPhpTransparency(this Rgba32 rgba)
        {
            return (byte)((0xff - rgba.A) / 2);
        }

        static Rgba32 PHPColorToRgba(uint color)
        {
            byte transparency = (byte)((color >> 24) & 0xffu);
            byte alpha = (transparency >= 0x7f) ? (byte)0 : (byte)(255 - transparency * 2);

            return new Rgba32((color & 0x00ffffffu) | ((uint)alpha << 24));
        }

        //private static int PHPColorToPHPAlpha(int color) => FromRGBA(color).A; // not correct
        //private static int PHPColorToRed(int color) => FromRGBA(color).R;
        //private static int PHPColorToGreen(int color) => FromRGBA(color).G;
        //private static int PHPColorToBlue(long color) => FromRGBA(color).B;

        private static Rgba32 GetAlphaColor(PhpGdImageResource img, long col)
        {
            return img.AlphaBlending ? FromRGBA(col) : FromRGB(col);
        }

        /// <summary>
        /// Can be 1, 2, 3, 4, 5 for built-in fonts in latin2 encoding (where higher numbers corresponding to larger fonts) or any of your own font identifiers registered with imageloadfont().
        /// </summary>
        static Font CreateFontById(int fontInd)
        {
            // TODO: cache statically

            // Get the first available of specified sans serif system fonts
            var result = SystemFonts.TryGet("Consolas", out var fontFamily) || SystemFonts.TryGet("Lucida Console", out fontFamily) || SystemFonts.TryGet("Arial", out fontFamily) || SystemFonts.TryGet("Verdana", out fontFamily) || SystemFonts.TryGet("Tahoma", out fontFamily);

            // Couldn't find the system font.
            if (!result)
                return null;

            // Use Bold if required and available
            var fontStyle = FontStyle.Regular;
            if (fontInd == 3 || fontInd >= 5)
            {
                if (fontFamily.TryGetMetrics(FontStyle.Bold, out _))
                {
                    fontStyle = FontStyle.Bold;
                }
            }

            // Make the font size equivalent to the original PHP version
            int fontSize = 8;
            if (fontInd > 1) fontSize += 4;
            if (fontInd > 3) fontSize += 4;

            return fontFamily.CreateFont(fontSize, fontStyle);
        }

        static Font CreateFontByFontFile(Context ctx, string font_file, double size)
        {
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
                family = new FontCollection().Add(font_stream.RawStream); // TODO: perf: global font collection cache
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

            if (family.TryGetMetrics(FontStyle.Regular, out _))
            {
                style = FontStyle.Regular;
            }
            else if (family.TryGetMetrics(FontStyle.Bold, out _))
            {
                style = FontStyle.Bold;
            }
            else if (family.TryGetMetrics(FontStyle.Italic, out _))
            {
                style = FontStyle.Italic;
            }
            else if (family.TryGetMetrics(FontStyle.BoldItalic, out _))
            {
                style = FontStyle.BoldItalic;
            }
            else
            {
                throw new InvalidDataException();
            }

            //
            return family.CreateFont((float)size, style);
        }

        #endregion

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
            return image[x, y].RgbaToPHPColor();
        }

        /// <summary>
        /// Enable or disable interlace.
        /// </summary>
        public static int imageinterlace(PhpResource image, bool interlace = false)
        {
            PhpException.FunctionNotSupported("imageinterlace");
            return 0; // false
        }

        /// <summary>
        /// Should antialias functions be used or not.
        /// </summary>
        /// <remarks>Always enabled.</remarks>
        public static bool imageantialias(PhpResource image, bool enabled)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img != null)
            {
                // always enabled.
                return true;
            }

            //
            return false;
        }

        #region imagefilter

        /// <summary>
        /// Applies a filter to an image.
        /// </summary>
        public static bool imagefilter(PhpResource image, FilterTypes filtertype, int arg1 = 0, int arg2 = 0, int arg3 = 0, int arg4 = 0)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img != null)
            {
                switch (filtertype)
                {
                    case FilterTypes.GRAYSCALE:
                        img.Image.Mutate(o => o.Grayscale());
                        return true;

                    case FilterTypes.CONTRAST:
                        // -100 = max contrast, 0 = no change, +100 = min contrast (note the direction!)
                        img.Image.Mutate(o => o.Contrast(arg1 / 100.0f));
                        return true;

                    case FilterTypes.BRIGHTNESS:
                        // -255 = min brightness, 0 = no change, +255 = max brightness
                        img.Image.Mutate(o => o.Brightness(arg1 / 255.0f));
                        return true;

                    case FilterTypes.NEGATE:
                        img.Image.Mutate(o => o.Invert());
                        return true;

                    case FilterTypes.GAUSSIAN_BLUR:
                        img.Image.Mutate(o => o.BoxBlur(arg1));
                        return true;

                    case FilterTypes.COLORIZE:
                        // Adds(subtracts) specified RGB values to each pixel.
                        // The valid range for each color is -255...+ 255, not 0...255.The correct order is red, green, blue.
                        // -255 = min, 0 = no change, +255 = max
                        return false;

                    case FilterTypes.SMOOTH:
                        // Applies a 9 - cell convolution matrix where center pixel has the weight arg1 and others weight of 1.0.
                        // The result is normalized by dividing the sum with arg1 + 8.0(sum of the matrix).
                        // Any float is accepted, large value(in practice: 2048 or more) = no change
                        return false;

                    default:
                        // argument exception
                        Debug.Fail("Not Implemented: imagefilter(" + filtertype.ToString() + ")");
                        break;
                }
            }

            return false;
        }

        #endregion

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

        #endregion

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
            var rotated = img.Image.Clone(o => o.Rotate((float)-angle));
            return new PhpGdImageResource(rotated, img.Format);
        }

        #endregion

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

            var opt = new DrawingOptions
            {
                GraphicsOptions =
                {
                    Antialias = img.AntiAlias
                }
            };

            img.Image.Mutate(o => o.Draw(opt, FromRGBA(col), 1.0f, rect));

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
                    img.Image.Mutate(o => o.Fill(img.tiled, rect));
                }
            }
            else
            {
                img.Image.Mutate(o => o.Fill(FromRGBA(col), rect));
            }

            return true;
        }

        #endregion

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

            img.tiled = new ImageBrush(imgTile.Image);

            return false;
        }

        #endregion

        #region imagettftext

        static PhpArray/*[8]*/imagettf(Context ctx, PhpGdImageResource img, double size, double angle, int x, int y, long color, string font_file, string text)
        {
            var font = CreateFontByFontFile(ctx, font_file, size);

            if (font == null)
            {
                return null;
            }

            var rendererOptions = new TextOptions(font);
            var textsize = TextMeasurer.MeasureSize(text, rendererOptions);

            // text transformation:
            var matrix = (angle == 0.0) ? Matrix3x2.Identity : Matrix3x2.CreateRotation((float)(angle * -2.0 * Math.PI / 360.0f));
            matrix.Translation = new Vector2(x, y);

            // draw the text:
            if (img != null)
            {
                // TODO: col < 0 => turn off antialiasing
                var rgbaColor = FromRGBA(Math.Abs(color));
                if (angle == 0.0)
                {
                    // Horizontal version is optimized by ImageSharp
                    img.Image.Mutate(o => o.DrawText(text, font, rgbaColor, new PointF(x, y - font.Size)));
                }
                else
                {
                    // Obtain and fill the particular glyphs if rotated
                    var path = new PathBuilder(matrix).AddLine(0, 0, textsize.Width, 0).Build();
                    var glyphs = TextBuilder.GenerateGlyphs(text, path, rendererOptions);
                    img.Image.Mutate(o => o.Fill(rgbaColor, glyphs));
                }
            }

            // calculate drawn text boundaries:
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

        /// <summary>
        /// Write text to the image using a TrueType font
        /// </summary> 
        [return: CastToFalse]
        public static PhpArray imagettftext(Context ctx, PhpResource im, double size, double angle, int x, int y, long color, string font_file, string text)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img != null)
            {
                return imagettf(ctx, img, size, angle, x, y, color, font_file, text);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Give the bounding box of a markerName using fonts via freetype2
        /// </summary>
        public static PhpArray imageftbbox(Context ctx, double size, double angle, string font_file, string text, PhpArray extrainfo = null)
        {
            if (extrainfo != null && extrainfo.TryGetValue("linespacing", out var linespacing))
            {
                PhpException.ArgumentValueNotSupported(nameof(extrainfo), "linespacing");
            }

            return imagettf(ctx, null, size, angle, 0, 0, 0, font_file, text) ?? throw new ArgumentException();
        }

        /// <summary>
        /// Give the bounding box of a markerName using fonts via freetype2
        /// </summary>
        public static PhpArray imagettfbbox(Context ctx, double size, double angle, string font_file, string text)
        {
            return imagettf(ctx, null, size, angle, 0, 0, 0, font_file, text) ?? throw new ArgumentException();
        }

        #endregion

        /// <summary>
        /// Draws a line between the two given points.
        /// </summary>
        public static bool imageline(PhpResource im, int x1, int y1, int x2, int y2, int color)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img != null)
            {
                var opt = new DrawingOptions();
                opt.GraphicsOptions.Antialias = img.AntiAlias;
                img.Image.Mutate(
                    o => o.DrawLine(opt, GetAlphaColor(img, color), 1.0f, new PointF[] { new PointF(x1, y1), new PointF(x2, y2) })
                );

                return true;
            }
            else
            {
                return false;
            }
        }

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

        static bool imagecopy(PhpResource dst_im, PhpResource src_im, int dst_x, int dst_y, int src_x, int src_y, int src_w, int src_h, float opacity = 1.0f)
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
                using (var cropped = src.Image.Clone(o => o
                        .Crop(new Rectangle(src_x, src_y, src_w, src_h))
                        .Resize(new Size(src_w, src_h))))
                {
                    dst.Image.Mutate<Rgba32>(
                        o => o.DrawImage(
                            cropped,
                            opacity: opacity,
                            backgroundLocation: new Point(dst_x, dst_y)
                        )
                    );
                }
            }
            catch (Exception ex)
            {
                PhpException.Throw(PhpError.Warning, ex.Message);
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        public static bool image2wbmp(Context ctx, PhpResource im, PhpValue to = default(PhpValue), int threshold = 0)
        {
            throw new NotImplementedException();
            //return imagesave(ctx, im, filename, (img, stream) => img.SaveAsWirelessBmp(stream));
        }

        /// <summary>
        /// Output JPEG image to browser or a file.
        /// </summary> 
        public static bool imagejpeg(Context ctx, PhpResource im, PhpValue to = default(PhpValue), int quality = 75)
        {
            var jpegoptions = new JpegEncoder() { Quality = Math.Min(Math.Max(quality, 0), 100) };
            return imagesave(ctx, im, to, (img, stream) => img.SaveAsJpeg(stream, jpegoptions));
        }

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
        public static bool imagegd2(Context ctx, PhpResource im, PhpValue to = default(PhpValue), int chunk_size = 128, int type = IMG_GD2_RAW)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Output GIF image to browser or file
        /// </summary> 
        public static bool imagegif(Context ctx, PhpResource im, PhpValue to = default(PhpValue))
        {
            return imagesave(ctx, im, to, (img, stream) =>
            {
                // use the source's encoder:
                var encoder = img.Configuration.ImageFormatsManager.GetEncoder(GifFormat.Instance) as GifEncoder;

                // or use default encoding options
                encoder ??= new GifEncoder(); // TODO: ColorTableMode from allocated colors count?

                img.Mutate(o => o.BackgroundColor(Color.Transparent));
                img.SaveAsGif(stream, encoder);
            });
        }

        /// <summary>
        /// Output PNG image to browser or file or a stream.
        /// </summary> 
        public static bool imagepng(Context ctx, PhpResource im, PhpValue to = default(PhpValue), int quality = 6, int filters = 0)
        {
            var compressionlevel = (PngCompressionLevel)Math.Min(Math.Max(quality, 0), 9);    // compression level 0 - 9

            return imagesave(ctx, im, to, (img, stream) =>
            {
                img.SaveAsPng(stream, new PngEncoder() { CompressionLevel = compressionlevel });
            });
        }

        /// <summary>
        /// Internal image save.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="im">Image resource.</param>
        /// <param name="to">Optional. Filename or stream. If not specified, the function saves the image to output stream.</param>
        /// <param name="saveaction">Callback that actually saves the image to given stream. Called when all checks pass.</param>
        /// <returns>True if save succeeded.</returns>
        static bool imagesave(Context ctx, PhpResource im, PhpValue to/* = null*/, Action<Image<Rgba32>, Stream> saveaction)
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
                if (Operators.IsEmpty(to)) // ~ is default or empty
                {
                    using (var ms = new MemoryStream())
                    {
                        saveaction(img.Image, ms);

                        // use WriteAsync() always
                        ms.Position = 0;
                        ms.CopyToAsync(ctx.OutputStream).GetAwaiter().GetResult();
                    }
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

                using (var ms = new MemoryStream())
                {
                    saveaction(img.Image, ms);

                    phpstream.WriteBytes(ms.ToArray());
                    phpstream.Flush();
                }

                // stream is closed after the operation
                phpstream.Dispose();
            }
            catch
            {
                return false;
            }

            return true;
        }

        #region imageconvolution

        /// <summary>
        /// Apply a 3x3 convolution matrix, using coefficient div and offset
        /// </summary>
        public static PhpResource imageconvolution(PhpResource src_im, PhpArray matrix3x3, double div, double offset)
        {
            PhpException.FunctionNotSupported("imageconvolution");
            return null;
        }

        #endregion

        #region imagecolortransparent

        /// <summary>
        /// Define a color as transparent
        /// </summary>
        public static long imagecolortransparent(PhpResource im)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;

            if (img.IsTransparentColSet == false)
            {
                return -1;
            }

            return img.transparentColor.Rgba;
        }

        /// <summary>
        /// Define a color as transparent
        /// </summary>
        public static long imagecolortransparent(PhpResource im, long col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return -1;

            img.transparentColor = FromRGBA(col);
            img.IsTransparentColSet = true;

            return col;
        }

        #endregion

        #region imagecolorsforindex

        /// <summary>
        /// Get the colors for an index
        /// </summary>
        public static PhpArray imagecolorsforindex(PhpResource im, long col)
        {
            var rgba = FromRGBA(col);

            return new PhpArray(4)
            {
                { "red", rgba.R },
                { "green", rgba.G },
                { "blue", rgba.B },
                { "alpha", rgba.GetPhpTransparency() },
            };
        }

        #endregion

        #region imagecolorset

        /// <summary>
        /// Set the color for the specified palette index
        /// </summary>
        public static void imagecolorset(PhpResource im, long col, int red, int green, int blue)
        {
            PhpException.FunctionNotSupported("imagecolorset");
        }

        #endregion

        #region imageellipse, imagefilledellipse

        /// <summary>
        /// Draw an ellipse
        /// </summary>
        /// <remarks>
        /// <see cref="imageellipse"/> ignores imagesetthickness().
        /// </remarks>
        public static bool imageellipse(PhpResource im, int cx, int cy, int w, int h, long col)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            var ellipse = new EllipsePolygon(cx, cy, w, h);

            var opt = new DrawingOptions();
            opt.GraphicsOptions.Antialias = img.AntiAlias;

            img.Image.Mutate(o => o.Draw(opt, GetAlphaColor(img, col), 1.0f, ellipse));

            return true;
        }

        /// <summary>
        /// Draw filled ellipse
        /// </summary>
        public static bool imagefilledellipse(PhpResource im, int cx, int cy, int w, int h, long col)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            var ellipse = new EllipsePolygon(cx, cy, w, h);
            var opt = new DrawingOptions();
            opt.GraphicsOptions.Antialias = img.AntiAlias;

            if (img.tiled != null)
            {
                img.Image.Mutate(o => o.Fill(opt, img.tiled, ellipse));
            }
            else
            {
                var brush = new SolidBrush(GetAlphaColor(img, col));
                img.Image.Mutate(o => o.Fill(opt, brush, ellipse));
            }

            return true;
        }

        #endregion

        #region imagecolorexact

        /// <summary>
        /// Get the index of the specified color
        /// </summary>
        public static int imagecolorexact(PhpResource im, int red, int green, int blue)
        {
            PhpException.FunctionNotSupported("imagecolorexact");
            return -1;
        }

        #endregion

        #region imagecolorexactalpha

        /// <summary>
        /// Find exact match for colour with transparency
        /// </summary>
        public static int imagecolorexactalpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            PhpException.FunctionNotSupported("imagecolorexactalpha");
            return -1;
        }

        #endregion

        #region imagecolormatch

        /// <summary>
        /// Makes the colors of the palette version of an image more closely match the true color version
        /// </summary>
        public static bool imagecolormatch(PhpResource im1, PhpResource im2)
        {
            PhpException.FunctionNotSupported("imagecolormatch");
            return false;
        }

        #endregion

        #region imagecolorresolve

        /// <summary>
        /// Get the index of the specified color or its closest possible alternative
        /// </summary>
        public static int imagecolorresolve(PhpResource im, int red, int green, int blue)
        {
            PhpException.FunctionNotSupported("imagecolorresolve");
            return -1;
        }

        #endregion

        #region imagecolorresolvealpha

        /// <summary>
        /// Resolve/Allocate a colour with an alpha level.  Works for true colour and palette based images
        /// </summary>
        public static int imagecolorresolvealpha(PhpResource im, int red, int green, int blue, int alpha)
        {
            PhpException.FunctionNotSupported("imagecolorresolvealpha");
            return -1;
        }

        #endregion

        #region imagestring, imagestringup, imagechar, imagecharup

        /// <summary>
        /// Draw a character horizontally
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="font">Can be 1, 2, 3, 4, 5 for built-in fonts in latin2 encoding (where higher numbers corresponding to larger fonts) or any of your own font identifiers registered with imageloadfont().</param>
        /// <param name="x">x-coordinate of the start.</param>
        /// <param name="y">y-coordinate of the start.</param>
        /// <param name="c">The character to draw.</param>
        /// <param name="color">A color identifier created with imagecolorallocate().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool imagechar(PhpResource image, int font, int x, int y, string c, long color) => DrawText(image, font, x, y, c[0].ToString(), color);

        /// <summary>
        /// Draw a character vertically
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="font">Can be 1, 2, 3, 4, 5 for built-in fonts in latin2 encoding (where higher numbers corresponding to larger fonts) or any of your own font identifiers registered with imageloadfont().</param>
        /// <param name="x">x-coordinate of the start.</param>
        /// <param name="y">y-coordinate of the start.</param>
        /// <param name="c">The character to draw.</param>
        /// <param name="color">A color identifier created with imagecolorallocate().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool imagecharup(PhpResource image, int font, int x, int y, string c, long color) => DrawText(image, font, x, y, c[0].ToString(), color, true);

        /// <summary>
        /// A function to write simple text to a gd2 image. 
        /// 
        /// NOTICE: Only system font are supported (indexes 1-5).
        ///         Loading custom fonts with loadfont is currently not supported.
        /// </summary>
        /// <param name="im"></param>
        /// <param name="fontInd">Font index - Only system fonts are supported currently, numbered 1-5. Anything higher than 5 will work as 5.</param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="text"></param>
        /// <param name="col">Packed color number</param>
        /// <returns></returns>
        public static bool imagestring(PhpResource im, int fontInd, int x, int y, string text, long col) => DrawText(im, fontInd, x, y, text, col);

        /// <summary>
        /// Draw a string vertically
        /// </summary>
        /// <param name="im">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="fontInd">Can be 1, 2, 3, 4, 5 for built-in fonts in latin2 encoding (where higher numbers corresponding to larger fonts) or any of your own font identifiers registered with imageloadfont().</param>
        /// <param name="x">x-coordinate of the bottom left corner.</param>
        /// <param name="y">y-coordinate of the bottom left corner.</param>
        /// <param name="text">The string to be written.</param>
        /// <param name="col">A color identifier created with imagecolorallocate().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool imagestringup(PhpResource im, int fontInd, int x, int y, string text, long col) => DrawText(im, fontInd, x, y, text, col, true);

        static bool DrawText(PhpResource im, int fontInd, int x, int y, string text, long col, bool up = false)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            if (x < 0 || y < 0) return true;
            if (x > img.Image.Width || y > img.Image.Height) return true;

            var font = CreateFontById(fontInd);
            var color = FromRGBA(col);

            var opt = new DrawingOptions();
            opt.GraphicsOptions.Antialias = img.AntiAlias;

            if (up)
                img.Image.Mutate(o => o.Rotate(90).DrawText(opt, text, font, new Color(color), new PointF(img.Image.Height - y, x)).Rotate(-90));
            else
                img.Image.Mutate<Rgba32>(context => context.DrawText(opt, text, font, color, new PointF(x, y)));


            return true;
        }

        /// <summary>
        /// Returns the pixel width of a character in the specified font.
        /// </summary>
        public static int imagefontwidth(int fontInd)
        {
            return (int)imagefontsize(fontInd).Width;
        }

        /// <summary>
        /// Returns the pixel height of a character in the specified font.
        /// </summary>
        public static int imagefontheight(int fontInd)
        {
            return (int)imagefontsize(fontInd).Height;
        }

        /// <summary>
        /// Returns the pixel size of a character in the specified font.
        /// </summary>
        static FontRectangle imagefontsize(int fontInd)
        {
            if (fontInd <= 0)
            {
                return FontRectangle.Empty;
            }

            var arr = _imagefontsize;
            if (arr != null && fontInd < arr.Length && arr[fontInd] != default)
            {
                return arr[fontInd];
            }

            // measure
            var font = CreateFontById(fontInd);
            if (font != null)
            {
                var size = TextMeasurer.MeasureSize("X", new TextOptions(font));

                if (arr == null || arr.Length <= fontInd)
                {
                    Array.Resize(ref arr, fontInd + 1);
                    _imagefontsize = arr;
                }

                arr[fontInd] = size;

                return size;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Cached pixel sizes of a built-in font character.
        /// </summary>
        static FontRectangle[] _imagefontsize;

        #endregion

        #region imagefill, image filltoborder

        /// <summary>
        /// Flood fill
        /// </summary>
        public static bool imagefill(PhpResource im, int x, int y, long col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            if (x < 0 || y < 0) return true;
            if (x > img.Image.Width || y > img.Image.Height) return true;

            img.Image.Mutate(o => o.ApplyProcessor(new FloodFillProcessor(new Point(x, y), FromRGBA(col), false, Color.Red)));

            return true;
        }

        /// <summary>
        /// Flood fill
        /// </summary>
        public static bool imagefilltoborder(PhpResource im, int x, int y, long border, long col)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            if (x < 0 || y < 0) return true;
            if (x > img.Image.Width || y > img.Image.Height) return true;

            img.Image.Mutate(o => o.ApplyProcessor(new FloodFillProcessor(new Point(x, y), FromRGBA(col), true, FromRGBA(border))));

            return true;
        }

        #endregion

        #region imagearc, imagefilledarc

        /// <summary>
        /// Draws an arc of circle centered at the given coordinates.
        /// </summary>
        public static bool imagearc(PhpResource image, long cx, long cy, long width, long height, int start, int end, long color)
        {
            return imagefilledarc(image, cx, cy, width, height, start, end, color, FilledArcStyles.PIE | FilledArcStyles.NOFILL);
        }

        /// <summary>
        /// Draw a filled partial ellipse.
        /// </summary>
        public static bool imagefilledarc(PhpResource im, long cx, long cy, long w, long h, int s, long e, long col, FilledArcStyles style)
        {
            PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            if (img == null)
                return false;

            var image = img.Image;

            if (cx < 0 || cy < 0) return true;
            if (cx > image.Width || cy > image.Height) return true;

            long range = 0;
            AdjustAnglesAndSize(ref w, ref h, ref s, ref e, ref range);

            // Path Builder object to be used in all the branches
            var pathBuilder = new PathBuilder();
            var color = FromRGBA(col);

            var pen = new SolidPen(color, 1);

            // edge points, used for both pie and chord
            PointF startingPoint = new PointF(cx + (int)(Math.Cos(s * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(s * Math.PI / 180) * (h / 2.0)));
            PointF endingPoint = new PointF(cx + (int)(Math.Cos(e * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(e * Math.PI / 180) * (h / 2.0)));

            image.Mutate<Rgba32>(context =>
            {
                // All PIE variants - IMG_ARC_PIE = 0
                if ((style & FilledArcStyles.CHORD) == 0)
                {
                    // Negative range means that starting point is greater than the ending one. Then we will create a correct arc by using the rest to 360 from range from the starting point
                    while (range < 0)
                        range += 360;

                    // Calculate the arc points, one point per degree
                    var lastPoint = startingPoint;
                    for (int angle = s + 1; angle < s + range; angle++)
                    {
                        var nextPoint = new PointF(cx + (int)(Math.Cos(angle * Math.PI / 180) * (w / 2.0)), cy + (int)(Math.Sin(angle * Math.PI / 180) * (h / 2.0)));

                        pathBuilder.AddLine(lastPoint, nextPoint);
                        lastPoint = nextPoint;
                    }
                    pathBuilder.AddLine(lastPoint, endingPoint);

                    // Draw the prepared lines or fill the pie
                    if (style == (FilledArcStyles.PIE | FilledArcStyles.NOFILL))
                    {
                        context.Draw(pen, pathBuilder.Build());
                    }
                    else
                    {
                        //Add the lines to the center
                        pathBuilder.AddLine(endingPoint, new PointF(cx, cy));
                        pathBuilder.AddLine(new PointF(cx, cy), startingPoint);

                        if (style == (FilledArcStyles.PIE | FilledArcStyles.NOFILL | FilledArcStyles.EDGED))
                        {
                            context.Draw(pen, pathBuilder.Build());
                        }
                        else
                        {
                            //Last not-excluded option for PIE, a simple filled PIE
                            context.Fill(color, pathBuilder.Build());
                        }
                    }
                }
                //The other exclusive branch - IMG_ARC_CHORD
                else
                {
                    pathBuilder.AddLine(startingPoint, endingPoint);

                    if (style == (FilledArcStyles.CHORD | FilledArcStyles.NOFILL))
                    {
                        context.Draw(pen, pathBuilder.Build());
                    }
                    else
                    {
                        //Add the lines to the center
                        pathBuilder.AddLine(endingPoint, new PointF(cx, cy));
                        pathBuilder.AddLine(new PointF(cx, cy), startingPoint);

                        if (style == (FilledArcStyles.CHORD | FilledArcStyles.NOFILL | FilledArcStyles.EDGED))
                        {
                            context.Draw(pen, pathBuilder.Build());
                        }
                        else
                        {
                            // Last remaining option is to fill the chord arc
                            context.Fill(color, pathBuilder.Build());
                        }
                    }
                }
            });
            return true;
        }

        #endregion

        /// <summary>
        /// Adjust angles and size for same behavior as in PHP
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="range"></param>
        private static void AdjustAnglesAndSize(ref long w, ref long h, ref int s, ref long e, ref long range)
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
            if (s > 360) s = s - (s / 360) * 360;
        }

        #region TODO (Convert from System.Drawing to ImageSharp)

        #region imagecolorstotal

        // NOTE: See https://github.com/SixLabors/ImageSharp/issues/488
        /// <summary>
        /// Find out the number of colors in an image's palette
        /// </summary>
        public static int imagecolorstotal(PhpResource im)
        {
            throw new NotImplementedException();

            //PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            //if (img == null)
            //    return 0;

            //var format = img.Image.PixelFormat;

            //if ((format & PixelFormat.Format1bppIndexed) != 0)
            //    return 2;
            //if ((format & PixelFormat.Format4bppIndexed) != 0)
            //    return 16;
            //if ((format & PixelFormat.Format8bppIndexed) != 0)
            //    return 256;

            //if ((format & PixelFormat.Indexed) != 0)
            //{
            //    // count the palette
            //    try
            //    {
            //        return img.Image.Palette.Entries.Length;
            //    }
            //    catch
            //    {
            //        // ignored, some error during SafeNativeMethods.Gdip.GdipGetImagePalette
            //    }
            //}

            //// non indexed image
            //return 0;
        }

        #endregion

        #region imagetruecolortopalette

        /// <summary>
        /// Convert a true colour image to a palette based image with a number of colours, optionally using dithering.
        /// </summary>
        public static bool imagetruecolortopalette(PhpResource im, bool ditherFlag, int colorsWanted)
        {
            throw new NotImplementedException();

            //if (colorsWanted <= 0)
            //    return false;

            //PhpGdImageResource img = PhpGdImageResource.ValidImage(im);
            //if (img == null)
            //    return false;

            //if (img.IsIndexed)
            //    return true;     // already indexed

            //// determine new pixel format:
            //PixelFormat newformat;
            //if (colorsWanted <= 2)
            //    newformat = PixelFormat.Format1bppIndexed;
            //else if (colorsWanted <= 16)
            //    newformat = PixelFormat.Format4bppIndexed;
            //else if (colorsWanted <= 256)
            //    newformat = PixelFormat.Format8bppIndexed;
            //else
            //    newformat = PixelFormat.Indexed;

            //// clone the image as indexed:
            //var image = img.Image;
            //var newimage = image.Clone(new Rectangle(0, 0, image.Width, image.Height), newformat);

            //if (newimage == null)
            //    return false;

            //img.Image = newimage;
            //return true;
        }

        #endregion

        #region imagefilledpolygon, imagepolygon 

        /// <summary>
        /// Draws a polygon.
        /// </summary>
        public static bool imagepolygon(PhpResource im, PhpArray point, int num_points, long col)
            => Polygon(im, point, num_points, col, filled: false);

        /// <summary>
        /// Draw a filled polygon
        /// </summary>
        public static bool imagefilledpolygon(PhpResource im, PhpArray point, int num_points, long col)
            => Polygon(im, point, num_points, col, filled: true);

        /// <summary>
        /// Draws an open polygon on the given image. Contrary to imagepolygon(), no line is drawn between the last and the first point.
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions.</param>
        /// <param name="points">An array containing the polygon's vertices.</param>
        /// <param name="num_points">Total number of points (vertices), which must be at least 3.</param>
        /// <param name="color">A color identifier created with imagecolorallocate().</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool imageopenpolygon(PhpResource image, PhpArray points, int num_points, long color)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return false;
            }

            if (points == null)
            {
                PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.unexpected_arg_given, nameof(points), PhpArray.PhpTypeName, PhpVariable.TypeNameNull);
                return false;
            }

            if (points.Count < num_points * 2)
            {
                return false;
            }

            if (num_points <= 0)
            {
                PhpException.Throw(PhpError.Warning, Resources.must_be_positive_number_of_points);
                return false;
            }

            var pointsF = GetPointFsFromArray(points, num_points);

            img.Image.Mutate(o => o.DrawLine(new SolidPen(FromRGBA(color), 1.0f), pointsF));

            return true;
        }

        static bool Polygon(PhpResource im, PhpArray point, int num_points, long col, bool filled)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null || point == null)
            {
                return false;
            }

            if (point == null)
            {
                PhpException.Throw(PhpError.Warning, Pchp.Library.Resources.Resources.unexpected_arg_given, nameof(point), PhpArray.PhpTypeName, PhpVariable.TypeNameNull);
                return false;
            }

            if (point.Count < num_points * 2)
            {
                return false;
            }

            if (num_points <= 0)
            {
                PhpException.Throw(PhpError.Warning, Resources.must_be_positive_number_of_points);
                return false;
            }

            var points = GetPointFsFromArray(point, num_points);

            if (filled)
            {
                var brush = col switch
                {
                    (long)ColorValues.TILED => img.tiled,
                    (long)ColorValues.STYLED => img.styled,
                    (long)ColorValues.BRUSHED => img.brushed,
                    _ => new SolidBrush(FromRGBA(col))
                };

                img.Image.Mutate(o => o.FillPolygon(brush, points));
            }
            else
            {
                img.Image.Mutate(o => o.DrawPolygon(new SolidPen(FromRGBA(col), 1.0f), points));
            }

            return true;
        }

        /// <summary>
        /// Gets a SixLabors representation of point from PhpArray
        /// </summary>
        private static PointF[] GetPointFsFromArray(PhpArray point, int num_points)
        {
            var enumerator = point.GetFastEnumerator();
            var points = new PointF[num_points];
            for (int i = 0; i < points.Length; i++)
            {
                enumerator.MoveNext();
                var x = (float)enumerator.CurrentValue.ToDouble();
                enumerator.MoveNext();
                var y = (float)enumerator.CurrentValue.ToDouble();

                points[i] = new PointF(x, y);
            }
            return points;
        }

        #endregion

        #endregion

        //imagegrabscreen is only available on Windows.

        #region imageflip, imagecrop, imagescale, imageaffine, imageaffinematrixget, imageaffinematrixconcat, imageresolution

        public const int IMG_FLIP_HORIZONTAL = 1;
        public const int IMG_FLIP_VERTICAL = 2;
        public const int IMG_FLIP_BOTH = 3;
        public const int IMG_NEAREST_NEIGHBOUR = 16;
        public const int IMG_BILINEAR_FIXED = 3;
        public const int IMG_BICUBIC = 4;
        public const int IMG_BICUBIC_FIXED = 5;

        public const int IMG_AFFINE_TRANSLATE = 0;
        public const int IMG_AFFINE_SCALE = 1;
        public const int IMG_AFFINE_ROTATE = 2;
        public const int IMG_AFFINE_SHEAR_HORIZONTAL = 3;
        public const int IMG_AFFINE_SHEAR_VERTICAL = 4;

        /// <summary>
        /// Flips an image using a given mode
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="mode">Flip mode, this can be one of the IMG_FLIP_* constants:</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        public static bool imageflip(PhpResource image, int mode)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return false;
            }

            switch (mode)
            {
                case IMG_FLIP_HORIZONTAL:
                    img.Image.Mutate(o => o.Flip(FlipMode.Horizontal));
                    break;
                case IMG_FLIP_VERTICAL:
                    img.Image.Mutate(o => o.Flip(FlipMode.Vertical));
                    break;
                case IMG_FLIP_BOTH:
                    img.Image.Mutate(o => o.Flip(FlipMode.Horizontal));
                    img.Image.Mutate(o => o.Flip(FlipMode.Vertical));
                    break;
                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Crops an image to the given rectangular area and returns the resulting image. The given image is not modified.
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="rect">The cropping rectangle as array with keys x, y, width and height.</param>
        /// <returns>Return cropped image resource on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpResource imagecrop(PhpResource image, PhpArray rect)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return null;
            }

            if (!TryGetRectangle(rect, out Rectangle rectangle))
            {
                PhpException.Throw(PhpError.Warning, Resources.array_expected, "rect");
                return null;
            }

            // Makes bigger image and then crops it. 
            if (rectangle.X + rectangle.Width > img.Image.Width ||
                rectangle.Y + rectangle.Height > img.Image.Height)
            {
                var resized = new PhpGdImageResource(new Image<Rgba32>(
                    Math.Max(rectangle.X + rectangle.Width, img.Image.Width),
                    Math.Max(rectangle.Y + rectangle.Height, img.Image.Height)),
                    img.Format);
                resized.Image.Mutate(o => o.DrawImage(img.Image, 1).Crop(rectangle));
                return resized;
            }

            return new PhpGdImageResource(img.Image.Clone(o => o.Crop(rectangle)), img.Format);
        }

        /// <summary>
        /// Scale an image using the given new width and height
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="new_width">The width to scale the image to.</param>
        /// <param name="new_height">The height to scale the image to.If omitted or negative, the aspect ratio will be preserved.</param>
        /// <param name="mode"></param>
        /// <returns>Return the scaled image resource on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpResource imagescale(PhpResource image, int new_width, int new_height = -1, int mode = IMG_BILINEAR_FIXED)
        {
            // TODO: Description mode
            // TODO: modes fixed
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return null;
            }

            if (new_height < 0 && new_width < 0)
            {
                return null;
            }

            new_height = new_height < 0 ? (img.Image.Height / img.Image.Width) * new_width : new_height;
            new_width = new_width < 0 ? (img.Image.Width / img.Image.Height) * new_height : new_width;

            IResampler res = null;
            switch (mode)
            {
                case IMG_NEAREST_NEIGHBOUR:
                    res = new NearestNeighborResampler();
                    break;
                case IMG_BICUBIC:
                    res = new BicubicResampler();
                    break;
                case IMG_BILINEAR_FIXED:
                    res = new TriangleResampler();
                    break;
                case IMG_BICUBIC_FIXED:
                    throw new NotSupportedException();
                default:
                    return null;
            }

            return new PhpGdImageResource(img.Image.Clone(o => o.Resize(new_width, new_height, res)), img.Format);
        }

        /// <summary>
        /// Return an image containing the affine transformed src image, using an optional clipping area
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions, such as imagecreatetruecolor().</param>
        /// <param name="affine">Array with keys 0 to 5.</param>
        /// <param name="clip">Array with keys "x", "y", "width" and "height".</param>
        /// <returns>Return affined image resource on success or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpResource imageaffine(PhpResource image, PhpArray affine, PhpArray clip = null)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return null;
            }

            // Check Arguments and get Matrix
            if (!TryGetTransformMatrix(affine, out Matrix3x2 affineMatrix))
            {
                PhpException.Throw(PhpError.Warning, Resources.array_expected, "affine");
                return null;
            }


            // Check Arguments if clip exists
            if (!TryGetRectangle(clip, out Rectangle sourceBox))
                sourceBox = new Rectangle(0, 0, img.Image.Width, img.Image.Height);

            // Calculate translation
            var extent = new PointF[4]
            {
                new PointF(0, 0),
                new PointF(sourceBox.Width, 0),
                new PointF(sourceBox.Width, sourceBox.Height),
                new PointF(0, sourceBox.Height)
            };

            for (int i = 0; i < extent.Length; i++)
                extent[i] = ApplyAffineToPointF(extent[i], affineMatrix);

            PointF min = extent[0];
            for (int i = 1; i < 4; i++)
            {
                if (min.X > extent[i].X)
                    min.X = extent[i].X;
                if (min.Y > extent[i].Y)
                    min.Y = extent[i].Y;
            }

            var translationMatrix = new Matrix3x2(1, 0, 0, 1, -min.X, -min.Y);

            AffineTransformBuilder builder =
                new AffineTransformBuilder().AppendMatrix(affineMatrix).AppendMatrix(translationMatrix);

            var transformed = img.Image.Clone(o => o.Crop(sourceBox).Transform(builder, new TriangleResampler()));

            return new PhpGdImageResource(transformed, img.Format);
        }

        static bool TryGetFloat(PhpValue value, out float number)
        {
            switch (value.TypeCode)
            {
                case PhpTypeCode.Boolean: number = value.Boolean ? 1 : 0; return true;
                case PhpTypeCode.Long: number = value.Long; return true;
                case PhpTypeCode.Double: number = (float)value.Double; return true;
                //PhpTypeCode.Null : 0,
                //PhpTypeCode.String
                case PhpTypeCode.Alias: return TryGetFloat(value.Alias.Value, out number);
                default:
                    PhpException.Throw(PhpError.Warning, Resources.wrong_type);
                    number = default;
                    return false;
            }
        }

        private static PointF ApplyAffineToPointF(PointF point, Matrix3x2 affine)
        {
            var x = point.X;
            var y = point.Y;
            return new PointF(x * affine.M11 + y * affine.M21 + affine.M31, x * affine.M12 + y * affine.M22 + affine.M32);
        }

        /// <summary>
        /// Get an affine transformation matrix
        /// </summary>
        /// <param name="type">One of the IMG_AFFINE_* constants.</param>
        /// <param name="options">If type is IMG_AFFINE_TRANSLATE or IMG_AFFINE_SCALE, options has to be an array with keys x and y, both having float values.
        /// If type is IMG_AFFINE_ROTATE, IMG_AFFINE_SHEAR_HORIZONTAL or IMG_AFFINE_SHEAR_VERTICAL, options has to be a float specifying the angle.</param>
        /// <returns>Returns an affine transformation matrix.</returns>
        [return: CastToFalse]
        public static PhpArray imageaffinematrixget(int type, PhpValue options)
        {
            switch (type)
            {
                case IMG_AFFINE_TRANSLATE:
                case IMG_AFFINE_SCALE:
                    if (options.IsArray)
                    {
                        var arr = options.AsArray();
                        if (!arr.TryGetItemValue("x", out PhpValue xVal))
                        {
                            PhpException.Throw(PhpError.Warning, Resources.missing_param, "x");
                            break;
                        }

                        if (!arr.TryGetItemValue("y", out PhpValue yVal))
                        {
                            PhpException.Throw(PhpError.Warning, Resources.missing_param, "y");
                            break;
                        }

                        //double x = 0;
                        //double y = 0;

                        // If there is another type, coordinates are zero.
                        TryGetFloat(xVal, out float x);
                        TryGetFloat(yVal, out float y);


                        return (type == IMG_AFFINE_TRANSLATE) ? GetPhpMatrix(m00: 1, m11: 1, m20: x, m21: y) : GetPhpMatrix(m00: x, m11: y);
                    }

                    PhpException.Throw(PhpError.Warning, Resources.array_expected, "options");
                    break;
                case IMG_AFFINE_ROTATE:
                case IMG_AFFINE_SHEAR_HORIZONTAL:
                case IMG_AFFINE_SHEAR_VERTICAL:
                    double angle = 0;

                    // If there is another type, coordinates are zero.
                    if (TryGetFloat(options, out float res))
                        angle = Math.PI * res / 180.0;

                    if (type == IMG_AFFINE_ROTATE)
                    {
                        double cos = Math.Cos(angle);
                        double sin = Math.Sin(angle);
                        return GetPhpMatrix(m00: cos, m01: sin, m10: -sin, m11: cos);
                    }
                    else
                    {
                        double tan = Math.Tan(angle);
                        return (type == IMG_AFFINE_SHEAR_HORIZONTAL) ? GetPhpMatrix(m00: 1, m10: tan, m11: 1) : GetPhpMatrix(m00: 1, m01: tan, m11: 1);
                    }
                default:
                    PhpException.Throw(PhpError.Warning, Resources.invalid_type, type.ToString());
                    break;
            }

            return null;
        }

        private static PhpArray GetPhpMatrix(double m00 = 0, double m01 = 0, double m10 = 0, double m11 = 0, double m20 = 0, double m21 = 0)
            => new PhpArray(6) { m00, m01, m10, m11, m20, m21 };

        /// <summary>
        /// Get Matrix 3x2 from a PhpArray. Entries are indexed with integer numbers in range of 0..5.
        /// </summary>
        /// <param name="matrix">Php matrix representation.</param>
        /// <param name="result">Resulting matrix.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        private static bool TryGetTransformMatrix(PhpArray matrix, out Matrix3x2 result)
        {
            result = default;

            if (matrix == null)
            {
                return false;
            }

            if (matrix.TryGetValue(0, out var n_11) & matrix.TryGetValue(1, out var n_12) |
                matrix.TryGetValue(2, out var n_21) & matrix.TryGetValue(3, out var n_22) |
                matrix.TryGetValue(4, out var n_31) & matrix.TryGetValue(5, out var n_32))
            {
                if (TryGetFloat(n_11, out result.M11) &
                    TryGetFloat(n_12, out result.M12) &
                    TryGetFloat(n_21, out result.M21) &
                    TryGetFloat(n_22, out result.M22) &
                    TryGetFloat(n_31, out result.M31) &
                    TryGetFloat(n_32, out result.M32))
                {
                    return true;
                }
                else
                {
                    PhpException.Throw(PhpError.Warning, Resources.wrong_type);
                    return false;
                }
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.affine_array_number_of_params);
                return false;
            }
        }

        /// <summary>
        /// Get Rectangle from a PhpArray.
        /// </summary>
        /// <param name="rectangle">Php rectangle representation.</param>
        /// <param name="result">Rectangle.</param>
        /// <returns>Returns TRUE on success or FALSE on failure.</returns>
        private static bool TryGetRectangle(PhpArray rectangle, out Rectangle result)
        {
            result = default;

            if (rectangle == null)
            {
                return false;
            }

            if (rectangle.TryGetValue("x", out var x) &
                rectangle.TryGetValue("y", out var y) &
                rectangle.TryGetValue("width", out var width) &
                rectangle.TryGetValue("height", out var height))
            {
                try
                {
                    result = new Rectangle()
                    {
                        X = (int)StrictConvert.ToLong(x),
                        Y = (int)StrictConvert.ToLong(y),
                        Width = (int)StrictConvert.ToLong(width),
                        Height = (int)StrictConvert.ToLong(height)
                    };

                    return true;
                }
                catch // (Pchp.Library.Spl.TypeError)
                {
                    PhpException.Throw(PhpError.Warning, Resources.wrong_type);
                    return false;
                }
            }
            else
            {
                PhpException.Throw(PhpError.Warning, Resources.missing_params);
                return false;
            }
        }

        /// <summary>
        /// Concatenate two affine transformation matrices
        /// </summary>
        /// <param name="m1">An affine transformation matrix (an array with keys 0 to 5 and float values).</param>
        /// <param name="m2">An affine transformation matrix (an array with keys 0 to 5 and float values).</param>
        /// <returns>An affine transformation matrix (an array with keys 0 to 5 and float values) or FALSE on failure.</returns>
        [return: CastToFalse]
        public static PhpArray imageaffinematrixconcat(PhpArray m1, PhpArray m2)
        {
            if (!TryGetTransformMatrix(m1, out Matrix3x2 matrix1))
            {
                PhpException.Throw(PhpError.Warning, Resources.array_expected, "m1");
                return null;
            }

            if (!TryGetTransformMatrix(m2, out Matrix3x2 matrix2))
            {
                PhpException.Throw(PhpError.Warning, Resources.array_expected, "m2");
                return null;
            }

            PhpArray result = new PhpArray(6) {
            matrix1.M11 * matrix2.M11 + matrix1.M21 * matrix2.M12, // 0
            matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22, // 1
            matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21, // 2
            matrix1.M12 * matrix2.M21 + matrix1.M22 * matrix2.M22, // 3
            matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M12 + matrix2.M31, // 4
            matrix1.M31 * matrix2.M21 + matrix1.M32 * matrix2.M22 + matrix2.M32 // 5
            };

            return result;
        }

        /// <summary>
        /// Allows to get the resolution of an image in DPI. The resolution is only used as meta information. It does not affect any drawing operations.
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions.</param>
        /// <returns>It returns an indexed array of the horizontal and vertical resolution on success, or FALSE on failure. </returns>
        [return: CastToFalse]
        public static PhpArray imageresolution(PhpResource image)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
            {
                return null;
            }

            return new PhpArray(2) { img.Image.Metadata.HorizontalResolution, img.Image.Metadata.VerticalResolution };
        }

        /// <summary>
        ///  Allows to set the resolution of an image in DPI. The resolution is only used as meta information. It does not affect any drawing operations.
        /// </summary>
        /// <param name="image">An image resource, returned by one of the image creation functions.</param>
        /// <param name="res_x">The horizontal resolution in DPI.</param>
        /// <param name="res_y">The vertical resolution in DPI.</param>
        /// <returns>It returns TRUE on success, or FALSE on failure.</returns>
        public static bool imageresolution(PhpResource image, int res_x, int res_y)
        {
            var img = PhpGdImageResource.ValidImage(image);
            if (img == null)
                return false;

            if (res_x < 0 || res_y < 0)
                return false;

            if (res_x != 0)
            {
                img.Image.Metadata.HorizontalResolution = res_x;

                if (res_y != 0)
                    img.Image.Metadata.VerticalResolution = res_y;
            }
            else if (res_y != 0)
            {
                img.Image.Metadata.VerticalResolution = res_y;
            }

            return true;
        }

        public static bool imageresolution(PhpResource image, int res_x) => imageresolution(image, res_x, res_x);

        #endregion
    }
}
