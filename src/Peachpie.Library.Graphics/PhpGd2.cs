using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using ImageSharp;
using System.IO;

namespace Peachpie.Library.Graphics
{
    [PhpExtension("gd")]
    public static class PhpGd2
    {
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

        /// <summary>
        /// Create a new image
        /// </summary> 
        [return: CastToFalse]
        public static PhpResource imagecreate(int x_size, int y_size)
        {
            if (x_size <= 0 || y_size <= 0)
            {
                PhpException.Throw(PhpError.Warning, string.Format(Resources.invalid_image_dimensions));
                return null;
            }

            var img = new PhpGdImageResource(x_size, y_size);

            // Draw white background
            img.Image.BackgroundColor(Color.White);

            //
            return img;
        }

        /// <summary>
        /// Create a new image from the image stream in the string
        /// </summary> 
        [return: CastToFalse]
        public static PhpResource imagecreatefromstring(byte[] image)
        {
            if (image == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.empty_string_or_invalid_image);
                return null;
            }

            try
            {
                return new PhpGdImageResource(new Image(image));
            }
            catch
            {
                PhpException.Throw(PhpError.Warning, Resources.empty_string_or_invalid_image);
                return null;
            }
        }

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
            img.AlphaBlending = blendmode;

            return true;
        }

        /// <summary>
        /// Output JPEG image to browser or a file.
        /// </summary> 
        public static bool imagejpeg(Context ctx, PhpResource im, string filename = null, int quality = 75)
        {
            var img = PhpGdImageResource.ValidImage(im);
            if (img == null)
            {
                return false;
            }

            if (quality < 0) quality = 75;
            if (quality > 100) quality = 100;

            if (filename == null)
            {
                img.Image.SaveAsJpeg(ctx.OutputStream, quality);
            }
            else
            {
                using (var stream = File.OpenWrite(Path.Combine(ctx.WorkingDirectory, filename)))
                {
                    img.Image.SaveAsJpeg(stream, quality);
                }
            }

            return true;
        }
    }
}
