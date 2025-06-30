﻿using System;
using System.IO;
using Pchp.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Peachpie.Library.Graphics
{
    /// <summary>
    /// Implements PHP functions provided by EXIF extension.
    /// </summary>
    [PhpExtension("exif")]
    public static class Exif
    {
        #region Constants

        /// <summary>
        /// This constant has a value of <c>1</c> if the mbstring is enabled, otherwise it has a value of <c>0</c>.
        /// </summary>
        public const int EXIF_USE_MBSTRING = 1;

        #endregion

        #region read_exif_data

        /// <summary>
        /// This is alternative alias of <see cref="exif_read_data(string,string,bool,bool)"/>.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray read_exif_data(Context ctx, string filename, string sections_needed = null, bool sub_arrays = false, bool read_thumbnail = false)
        {
            return exif_read_data(ctx, filename, sections_needed, sub_arrays, read_thumbnail);
        }

        #endregion

        #region exif_read_data

        /// <summary>
        /// Reads the EXIF headers from JPEG or TIFF
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filename">The name of the image file being read. This cannot be an URL.</param>
        /// <param name="sections_needed">Is a comma separated list of sections that need to be present in file to produce a result array. If none of the requested sections could be found the return value is FALSE.
        /// 
        /// FILE:	FileName, FileSize, FileDateTime, SectionsFound
        /// COMPUTED:	 html, Width, Height, IsColor, and more if available. Height and Width are computed the same way getimagesize() does so their values must not be part of any header returned. Also, html is a height/width text string to be used inside normal HTML.
        /// ANY_TAG:	Any information that has a Tag e.g. IFD0, EXIF, ...
        /// IFD0:	 All tagged data of IFD0. In normal imagefiles this contains image size and so forth.
        /// THUMBNAIL:	 A file is supposed to contain a thumbnail if it has a second IFD. All tagged information about the embedded thumbnail is stored in this section.
        /// COMMENT:	Comment headers of JPEG images.
        /// EXIF:	 The EXIF section is a sub section of IFD0. It contains more detailed information about an image. Most of these entries are digital camera related.</param>
        /// <param name="sub_arrays">Specifies whether or not each section becomes an array. The sections COMPUTED, THUMBNAIL, and COMMENT always become arrays as they may contain values whose names conflict with other sections.</param>
        /// <param name="read_thumbnail">When set to <c>TRUE</c> the thumbnail itself is read. Otherwise, only the tagged data is read.</param>
        /// <returns>It returns an associative array where the array indexes are the header names and the array values are the values associated with those headers.
        /// If no data can be returned, <c>FALSE</c> is returned.</returns>
        [return: CastToFalse]
        public static PhpArray exif_read_data(Context ctx, string filename, string sections_needed = null, bool sub_arrays = false, bool read_thumbnail = false)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            if (!string.IsNullOrEmpty(sections_needed))
            {
                PhpException.ArgumentValueNotSupported(nameof(sections_needed), sections_needed);
            }

            if (sub_arrays)
            {
                PhpException.ArgumentValueNotSupported(nameof(sub_arrays), sub_arrays);
            }

            if (read_thumbnail)
            {
                PhpException.ArgumentValueNotSupported(nameof(read_thumbnail), read_thumbnail);
            }

            PhpArray array = new PhpArray();

            var bytes = Utils.ReadPhpBytes(ctx, filename);
            if (bytes == null)
            {
                return null;
            }

            array.Add("FileName", Path.GetFileName(filename));
            //array.Add("FileDateTime", (int)File.GetCreationTime(filename).ToOADate());
            array.Add("FileSize", (int)bytes.Length);

            ImageInfo image;

            using (var ms = new MemoryStream(bytes))
            {
                try
                {
                    image = Image.Identify(ms);
                }
                catch
                {
                    return null;
                }

                // TODO: image.MetaData.Properties, image.MetaData.IccProfile, image.MetaData.***Resolution

                if (image.Metadata.ExifProfile != null)
                {
                    foreach (var item in image.Metadata.ExifProfile.Values)
                    {
                        array.Add(item.Tag.ToString(), ExifValueToPhpValue(item.GetValue()));
                    }
                }
            }

            return array;
        }

        #region ExifValueToPhpValue

        static PhpValue ExifValueToPhpValue(object value)
        {
            if (value != null)
            {
                if (value is Array arr)
                {
                    var phparr = new PhpArray(arr.Length);

                    for (int i = 0; i < arr.Length; i++)
                    {
                        phparr.Add(ExifValueToPhpValue(arr.GetValue(i)));
                    }

                    return PhpValue.Create(phparr);
                }
                else
                {
                    double dval;
                    long ival;

                    if (TryAsDouble(value, out dval))
                    {
                        return PhpValue.Create(dval);
                    }
                    else if (TryAsLong(value, out ival))
                    {
                        return PhpValue.Create(ival);
                    }
                    else
                    {
                        return PhpValue.Create(value.ToString());
                    }
                }
            }
            else
            {
                return PhpValue.Null;
            }
        }

        static bool TryAsDouble(object value, out double dval)
        {
            dval = 0.0;

            if (value is float f)
            {
                dval = f;
                return true;
            }

            if (value is double d)
            {
                dval = d;
                return true;
            }

            return false;
        }

        static bool TryAsLong(object value, out long ival)
        {
            ival = 0;

            if (value is int i)
            {
                ival = i;
                return true;
            }

            if (value is long l)
            {
                ival = l;
                return true;
            }

            if (value is uint u)
            {
                ival = u;
                return true;
            }

            if (value is byte b)
            {
                ival = b;
                return true;
            }

            if (value is sbyte sb)
            {
                ival = sb;
                return true;
            }

            if (value is short s)
            {
                ival = s;
                return true;
            }

            if (value is ushort us)
            {
                ival = us;
                return true;
            }

            return false;
        }

        #endregion

        #endregion

        #region exif_tagname

        /// <summary>
        /// Get the header name for an index
        /// </summary>
        /// <returns></returns>
        [return: CastToFalse]
        public static string exif_tagname(int index)
        {
            if (index < 0 || index > ushort.MaxValue)
            {
                return null;
            }

            // var tag = (ExifTagValue)index; // INTERNAL
            //if (Enum.IsDefined(typeof(ExifTag), tag))
            //{
            //    return tag.ToString();
            //}
            //return null;

            return GetExifTagMap().GetValueOrDefault((ushort)index);
        }

        /// <summary>
        /// Lazily initialized immutable set of known exif tags.
        /// </summary>
        static Dictionary<ushort, string> s_exifmap;

        static Dictionary<ushort, string> GetExifTagMap()
        {
            Dictionary<ushort, string> BuildMap()
            {
                var map = new Dictionary<ushort, string>(256); // ~249

                var props = typeof(ExifTag).GetProperties();
                foreach (var p in props)
                {
                    if (p.GetMethod?.IsStatic == true && p.GetValue(null) is ExifTag exiftag)
                    {
                        map[(ushort)exiftag] = exiftag.ToString();
                    }
                }

                Debug.Assert(map.Count <= 256, "perf: initialize {map} with bigger capacity please");

                return map;
            }

            var map = s_exifmap;
            if (map == null)
            {
                Interlocked.CompareExchange(ref s_exifmap, map = BuildMap(), null);
            }

            return map;
        }

        #endregion

        #region exif_imagetype

        /// <summary>
        /// Determine the type of an image
        /// </summary>
        /// <returns></returns>
        [return: CastToFalse]
        public static int exif_imagetype(Context ctx, string imagefile)
        {
            if (string.IsNullOrEmpty(imagefile))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return -1;
            }

            var stream = Utils.OpenStream(ctx, imagefile);
            if (stream == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.read_error);
                return -1;
            }

            PhpImage.ImageType type;
            try
            {
                type = PhpImage.ImageSignature.ProcessImageType(stream, true, out _, false, false);
            }
            catch
            {
                /*rw error*/
                type = PhpImage.ImageType.Unknown;
            }
            finally
            {
                stream.Dispose();
            }

            return (type != PhpImage.ImageType.Unknown) ? (int)type : -1;
        }

        #endregion

        #region exif_thumbnail

        //string exif_thumbnail ( string $filename [, int &$width [, int &$height [, int &$imagetype ]]] )

        /// <summary>
        /// Retrieve the embedded thumbnail of a TIFF or JPEG image
        /// </summary>
        [return: CastToFalse]
        public static PhpString exif_thumbnail(Context ctx, string filename, PhpAlias width = null, PhpAlias height = null, PhpAlias imagetype = null)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return default(PhpString);
            }

            Image<Rgba32> thumbnail = null;
            IImageFormat format = null;
            byte[] result;

            var bytes = Utils.ReadPhpBytes(ctx, filename);

            if (bytes == null)
                return default(PhpString);

            // get thumbnail from <filename>'s content:
            try
            {
                //using (var image = Image.Load(bytes.AsSpan()))
                {
                    // TODO: Image.Identify needs a new overload that returns the format.
                    var imageInfo = Image.Identify(bytes.AsSpan());
                    // return byte[] ~ image.MetaData.ExifProfile{ this.data, this.thumbnailOffset, this.thumbnailLength }
                    imageInfo.Metadata.ExifProfile.TryCreateThumbnail<Rgba32>(out thumbnail);
                }
            }
            catch
            {
                return default(PhpString);
            }

            if (thumbnail == null)
            {
                return default(PhpString);
            }

            //
            if (width != null)
                width.Value = (PhpValue)thumbnail.Width;

            if (height != null)
                height.Value = (PhpValue)thumbnail.Height;

            if (imagetype != null)  // TODO: get thumbnail image format
                imagetype.Value = (PhpValue)(format == JpegFormat.Instance ? PhpImage.IMAGETYPE_JPEG : PhpImage.IMAGETYPE_TIFF_II);

            using (var ms2 = new MemoryStream())
            {
                thumbnail.Save(ms2, new PngEncoder());
                result = ms2.ToArray();
            }

            thumbnail.Dispose();

            //
            return new PhpString(result);
        }

        #endregion
    }
}
