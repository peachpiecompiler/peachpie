using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using ImageSharp;
using ImageSharp.Formats;
using Pchp.Core;
using Pchp.Library.Streams;

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

        #endregion

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
        /// Return the types of images supported in a bitfield - 1=GIF, 2=JPEG, 4=PNG, 8=WBMP, 16=XPM
        /// IMG_GIF | IMG_JPG | IMG_PNG | IMG_WBMP | IMG_XPM
        /// </summary> 
        public static int imagetypes()
        {
            return (int)ImgType.Supported;
        }

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

        static PhpGdImageResource imagecreatecommon(int x_size, int y_size, IConfigurationModule configuration, IImageFormat format)
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

        static PhpGdImageResource imagercreatefromfile(Context ctx, string filename, IConfigurationModule formatOpt = null)
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

        /// <summary>
        /// Output WBMP image to browser or file
        /// </summary> 
        public static bool image2wbmp(Context ctx, PhpResource im, PhpValue to, int threshold = 0)
        {
            throw new NotImplementedException();
            //return imagesave(ctx, im, filename, (img, stream) => img.SaveAsWirelessBmp(stream));
        }

        public static bool image2wbmp(Context ctx, PhpResource im) => image2wbmp(ctx, im, PhpValue.Null);

        /// <summary>
        /// Output JPEG image to browser or a file.
        /// </summary> 
        public static bool imagejpeg(Context ctx, PhpResource im, PhpValue to, int quality = 75)
        {
            var jpegoptions = new JpegEncoder() { Quality = Math.Min(Math.Max(quality, 0), 100) };
            return imagesave(ctx, im, to, (img, stream) => img.SaveAsJpeg(stream, jpegoptions));
        }

        public static bool imagejpeg(Context ctx, PhpResource im) => imagejpeg(ctx, im, PhpValue.Null);

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

        /// <summary>
        /// Output PNG image to browser or file or a stream.
        /// </summary> 
        public static bool imagepng(Context ctx, PhpResource im, PhpValue to, int quality = 6, int filters = 0)
        {
            quality = Math.Min(Math.Max(quality, 0), 9);    // compression level 0 - 9

            return imagesave(ctx, im, to, (img, stream) =>
            {
                img.SaveAsPng(stream, new PngEncoder(){ CompressionLevel = quality });
            });
        }

        public static bool imagepng(Context ctx, PhpResource im) => imagepng(ctx, im, PhpValue.Null);

        /// <summary>
        /// Internal image save.
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="im">Image resource.</param>
        /// <param name="to">Optional. Filename or stream. If not specified the functiona saves the image to output stream.</param>
        /// <param name="saveaction">Callback that actually save the image to given stream. Called when all checks pass.</param>
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
                var filename = to.ToStringOrNull();
                PhpStream phpstream;

                // either save to a file or to output stream
                if (filename == null)
                {
                    saveaction(img.Image, ctx.OutputStream);
                }
                else if (to.IsEmpty)
                {
                    using (var stream = File.OpenWrite(Path.Combine(ctx.WorkingDirectory, filename)))
                    {
                        saveaction(img.Image, stream);
                    }
                }
                else
                {
                    // validate the stream resource, outputs warning in case of invalid resource
                    phpstream = PhpStream.GetValid(to.AsObject() as PhpResource, FileAccess.Write);
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
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
