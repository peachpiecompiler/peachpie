using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Pchp.Core;
using ImageSharp;

namespace Peachpie.Library.Graphics
{
    /// <summary>
    /// Implements PHP functions provided by EXIF extension.
    /// </summary>
    [PhpExtension("exif")]
    public static class Exif
    {
        #region TagType

        private enum TagValueType
        {
            UShort, UInt, ULong, URational, String, Unicode, Unknown
        }

        private struct Tag : IComparable
        {
            public int id;
            public string name;
            public TagValueType type;

            public Tag(int i, string n, TagValueType t)
            {
                id = i;
                name = n;
                type = t;
            }

            /// <summary>
            /// IComparable.CompareTo implementation.
            /// </summary>
            public int CompareTo(object obj)
            {
                if (obj is Tag)
                {
                    Tag temp = (Tag)obj;

                    return id.CompareTo(temp.id);
                }

                throw new ArgumentException("object is not a Tag");
            }
        }

        #endregion

        #region IFDTagTable

        private static Tag[] IFDTagTable =
        {
            new Tag( 0x000B, "ACDComment", TagValueType.Unknown),
            new Tag( 0x00FE, "NewSubFile", TagValueType.Unknown), /* better name it 'ImageType' ? */
            new Tag( 0x00FF, "SubFile", TagValueType.Unknown),
            new Tag( 0x0100, "ImageWidth", TagValueType.Unknown),
            new Tag( 0x0101, "ImageLength", TagValueType.Unknown),
            new Tag( 0x0102, "BitsPerSample", TagValueType.Unknown),
            new Tag( 0x0103, "Compression", TagValueType.Unknown),
            new Tag( 0x0106, "PhotometricInterpretation", TagValueType.Unknown),
            new Tag( 0x010A, "FillOrder", TagValueType.Unknown),
            new Tag( 0x010D, "DocumentName", TagValueType.Unknown),
            new Tag( 0x010E, "ImageDescription", TagValueType.String),
            new Tag( 0x010F, "Make", TagValueType.String),
            new Tag( 0x0110, "Model", TagValueType.String),
            new Tag( 0x0111, "StripOffsets", TagValueType.Unknown),
            new Tag( 0x0112, "Orientation", TagValueType.Unknown),
            new Tag( 0x0115, "SamplesPerPixel", TagValueType.Unknown),
            new Tag( 0x0116, "RowsPerStrip", TagValueType.Unknown),
            new Tag( 0x0117, "StripByteCounts", TagValueType.Unknown),
            new Tag( 0x0118, "MinSampleValue", TagValueType.Unknown),
            new Tag( 0x0119, "MaxSampleValue", TagValueType.Unknown),
            new Tag( 0x011A, "XResolution", TagValueType.Unknown),
            new Tag( 0x011B, "YResolution", TagValueType.Unknown),
            new Tag( 0x011C, "PlanarConfiguration", TagValueType.Unknown),
            new Tag( 0x011D, "PageName", TagValueType.Unknown),
            new Tag( 0x011E, "XPosition", TagValueType.Unknown),
            new Tag( 0x011F, "YPosition", TagValueType.Unknown),
            new Tag( 0x0120, "FreeOffsets", TagValueType.Unknown),
            new Tag( 0x0121, "FreeByteCounts", TagValueType.Unknown),
            new Tag( 0x0122, "GrayResponseUnit", TagValueType.Unknown),
            new Tag( 0x0123, "GrayResponseCurve", TagValueType.Unknown),
            new Tag( 0x0124, "T4Options", TagValueType.Unknown),
            new Tag( 0x0125, "T6Options", TagValueType.Unknown),
            new Tag( 0x0128, "ResolutionUnit", TagValueType.Unknown),
            new Tag( 0x0129, "PageNumber", TagValueType.Unknown),
            new Tag( 0x012D, "TransferFunction", TagValueType.Unknown),
            new Tag( 0x0131, "Software", TagValueType.String),
            new Tag( 0x0132, "DateTime", TagValueType.String),
            new Tag( 0x013B, "Artist", TagValueType.String),
            new Tag( 0x013C, "HostComputer", TagValueType.String),
            new Tag( 0x013D, "Predictor", TagValueType.Unknown),
            new Tag( 0x013E, "WhitePoint", TagValueType.Unknown),
            new Tag( 0x013F, "PrimaryChromaticities", TagValueType.Unknown),
            new Tag( 0x0140, "ColorMap", TagValueType.Unknown),
            new Tag( 0x0141, "HalfToneHints", TagValueType.Unknown),
            new Tag( 0x0142, "TileWidth", TagValueType.Unknown),
            new Tag( 0x0143, "TileLength", TagValueType.Unknown),
            new Tag( 0x0144, "TileOffsets", TagValueType.Unknown),
            new Tag( 0x0145, "TileByteCounts", TagValueType.Unknown),
            new Tag( 0x014A, "SubIFD", TagValueType.Unknown),
            new Tag( 0x014C, "InkSet", TagValueType.Unknown),
            new Tag( 0x014D, "InkNames", TagValueType.Unknown),
            new Tag( 0x014E, "NumberOfInks", TagValueType.Unknown),
            new Tag( 0x0150, "DotRange", TagValueType.Unknown),
            new Tag( 0x0151, "TargetPrinter", TagValueType.Unknown),
            new Tag( 0x0152, "ExtraSample", TagValueType.Unknown),
            new Tag( 0x0153, "SampleFormat", TagValueType.Unknown),
            new Tag( 0x0154, "SMinSampleValue", TagValueType.Unknown),
            new Tag( 0x0155, "SMaxSampleValue", TagValueType.Unknown),
            new Tag( 0x0156, "TransferRange", TagValueType.Unknown),
            new Tag( 0x0157, "ClipPath", TagValueType.Unknown),
            new Tag( 0x0158, "XClipPathUnits", TagValueType.Unknown),
            new Tag( 0x0159, "YClipPathUnits", TagValueType.Unknown),
            new Tag( 0x015A, "Indexed", TagValueType.Unknown),
            new Tag( 0x015B, "JPEGTables", TagValueType.Unknown),
            new Tag( 0x015F, "OPIProxy", TagValueType.Unknown),
            new Tag( 0x0200, "JPEGProc", TagValueType.Unknown),
            new Tag( 0x0201, "JPEGInterchangeFormat", TagValueType.Unknown),
            new Tag( 0x0202, "JPEGInterchangeFormatLength", TagValueType.Unknown),
            new Tag( 0x0203, "JPEGRestartInterval", TagValueType.Unknown),
            new Tag( 0x0205, "JPEGLosslessPredictors", TagValueType.Unknown),
            new Tag( 0x0206, "JPEGPointTransforms", TagValueType.Unknown),
            new Tag( 0x0207, "JPEGQTables", TagValueType.Unknown),
            new Tag( 0x0208, "JPEGDCTables", TagValueType.Unknown),
            new Tag( 0x0209, "JPEGACTables", TagValueType.Unknown),
            new Tag( 0x0211, "YCbCrCoefficients", TagValueType.Unknown),
            new Tag( 0x0212, "YCbCrSubSampling", TagValueType.Unknown),
            new Tag( 0x0213, "YCbCrPositioning", TagValueType.Unknown),
            new Tag( 0x0214, "ReferenceBlackWhite", TagValueType.Unknown),
            new Tag( 0x02BC, "ExtensibleMetadataPlatform", TagValueType.Unknown), /* XAP: Extensible Authoring Publishing, obsoleted by XMP: Extensible Metadata Platform */
            new Tag( 0x0301, "Gamma", TagValueType.Unknown),
            new Tag( 0x0302, "ICCProfileDescriptor", TagValueType.Unknown),
            new Tag( 0x0303, "SRGBRenderingIntent", TagValueType.Unknown),
            new Tag( 0x0320, "ImageTitle", TagValueType.Unknown),
            new Tag( 0x5001, "ResolutionXUnit", TagValueType.Unknown),
            new Tag( 0x5002, "ResolutionYUnit", TagValueType.Unknown),
            new Tag( 0x5003, "ResolutionXLengthUnit", TagValueType.Unknown),
            new Tag( 0x5004, "ResolutionYLengthUnit", TagValueType.Unknown),
            new Tag( 0x5005, "PrintFlags", TagValueType.Unknown),
            new Tag( 0x5006, "PrintFlagsVersion", TagValueType.Unknown),
            new Tag( 0x5007, "PrintFlagsCrop", TagValueType.Unknown),
            new Tag( 0x5008, "PrintFlagsBleedWidth", TagValueType.Unknown),
            new Tag( 0x5009, "PrintFlagsBleedWidthScale", TagValueType.Unknown),
            new Tag( 0x500A, "HalftoneLPI", TagValueType.Unknown),
            new Tag( 0x500B, "HalftoneLPIUnit", TagValueType.Unknown),
            new Tag( 0x500C, "HalftoneDegree", TagValueType.Unknown),
            new Tag( 0x500D, "HalftoneShape", TagValueType.Unknown),
            new Tag( 0x500E, "HalftoneMisc", TagValueType.Unknown),
            new Tag( 0x500F, "HalftoneScreen", TagValueType.Unknown),
            new Tag( 0x5010, "JPEGQuality", TagValueType.Unknown),
            new Tag( 0x5011, "GridSize", TagValueType.Unknown),
            new Tag( 0x5012, "ThumbnailFormat", TagValueType.Unknown),
            new Tag( 0x5013, "ThumbnailWidth", TagValueType.Unknown),
            new Tag( 0x5014, "ThumbnailHeight", TagValueType.Unknown),
            new Tag( 0x5015, "ThumbnailColorDepth", TagValueType.Unknown),
            new Tag( 0x5016, "ThumbnailPlanes", TagValueType.Unknown),
            new Tag( 0x5017, "ThumbnailRawBytes", TagValueType.Unknown),
            new Tag( 0x5018, "ThumbnailSize", TagValueType.Unknown),
            new Tag( 0x5019, "ThumbnailCompressedSize", TagValueType.Unknown),
            new Tag( 0x501A, "ColorTransferFunction", TagValueType.Unknown),
            new Tag( 0x501B, "ThumbnailData", TagValueType.Unknown),
            new Tag( 0x5020, "ThumbnailImageWidth", TagValueType.Unknown),
            new Tag( 0x5021, "ThumbnailImageHeight", TagValueType.Unknown),
            new Tag( 0x5022, "ThumbnailBitsPerSample", TagValueType.Unknown),
            new Tag( 0x5023, "ThumbnailCompression", TagValueType.Unknown),
            new Tag( 0x5024, "ThumbnailPhotometricInterp", TagValueType.Unknown),
            new Tag( 0x5025, "ThumbnailImageDescription", TagValueType.Unknown),
            new Tag( 0x5026, "ThumbnailEquipMake", TagValueType.Unknown),
            new Tag( 0x5027, "ThumbnailEquipModel", TagValueType.Unknown),
            new Tag( 0x5028, "ThumbnailStripOffsets", TagValueType.Unknown),
            new Tag( 0x5029, "ThumbnailOrientation", TagValueType.Unknown),
            new Tag( 0x502A, "ThumbnailSamplesPerPixel", TagValueType.Unknown),
            new Tag( 0x502B, "ThumbnailRowsPerStrip", TagValueType.Unknown),
            new Tag( 0x502C, "ThumbnailStripBytesCount", TagValueType.Unknown),
            new Tag( 0x502D, "ThumbnailResolutionX", TagValueType.Unknown),
            new Tag( 0x502E, "ThumbnailResolutionY", TagValueType.Unknown),
            new Tag( 0x502F, "ThumbnailPlanarConfig", TagValueType.Unknown),
            new Tag( 0x5030, "ThumbnailResolutionUnit", TagValueType.Unknown),
            new Tag( 0x5031, "ThumbnailTransferFunction", TagValueType.Unknown),
            new Tag( 0x5032, "ThumbnailSoftwareUsed", TagValueType.Unknown),
            new Tag( 0x5033, "ThumbnailDateTime", TagValueType.Unknown),
            new Tag( 0x5034, "ThumbnailArtist", TagValueType.Unknown),
            new Tag( 0x5035, "ThumbnailWhitePoint", TagValueType.Unknown),
            new Tag( 0x5036, "ThumbnailPrimaryChromaticities", TagValueType.Unknown),
            new Tag( 0x5037, "ThumbnailYCbCrCoefficients", TagValueType.Unknown),
            new Tag( 0x5038, "ThumbnailYCbCrSubsampling", TagValueType.Unknown),
            new Tag( 0x5039, "ThumbnailYCbCrPositioning", TagValueType.Unknown),
            new Tag( 0x503A, "ThumbnailRefBlackWhite", TagValueType.Unknown),
            new Tag( 0x503B, "ThumbnailCopyRight", TagValueType.Unknown),
            new Tag( 0x5090, "LuminanceTable", TagValueType.Unknown),
            new Tag( 0x5091, "ChrominanceTable", TagValueType.Unknown),
            new Tag( 0x5100, "FrameDelay", TagValueType.Unknown),
            new Tag( 0x5101, "LoopCount", TagValueType.Unknown),
            new Tag( 0x5110, "PixelUnit", TagValueType.Unknown),
            new Tag( 0x5111, "PixelPerUnitX", TagValueType.Unknown),
            new Tag( 0x5112, "PixelPerUnitY", TagValueType.Unknown),
            new Tag( 0x5113, "PaletteHistogram", TagValueType.Unknown),
            new Tag( 0x1000, "RelatedImageFileFormat", TagValueType.Unknown),
            new Tag( 0x800D, "ImageID", TagValueType.Unknown),
            new Tag( 0x80E3, "Matteing", TagValueType.Unknown),   /* obsoleted by ExtraSamples */
            new Tag( 0x80E4, "DataType", TagValueType.Unknown),   /* obsoleted by SampleFormat */
            new Tag( 0x80E5, "ImageDepth", TagValueType.Unknown),
            new Tag( 0x80E6, "TileDepth", TagValueType.Unknown),
            new Tag( 0x828D, "CFARepeatPatternDim", TagValueType.Unknown),
            new Tag( 0x828E, "CFAPattern", TagValueType.Unknown),
            new Tag( 0x828F, "BatteryLevel", TagValueType.Unknown),
            new Tag( 0x8298, "Copyright", TagValueType.String),
            new Tag( 0x829A, "ExposureTime", TagValueType.URational),
            new Tag( 0x829D, "FNumber", TagValueType.URational),
            new Tag( 0x83BB, "IPTC/NAA", TagValueType.Unknown),
            new Tag( 0x84E3, "IT8RasterPadding", TagValueType.Unknown),
            new Tag( 0x84E5, "IT8ColorTable", TagValueType.Unknown),
            new Tag( 0x8649, "ImageResourceInformation", TagValueType.Unknown), /* PhotoShop */
            new Tag( 0x8769, "Exif_IFD_Pointer", TagValueType.Unknown),
            new Tag( 0x8773, "ICC_Profile", TagValueType.Unknown),
            new Tag( 0x8822, "ExposureProgram", TagValueType.Unknown),
            new Tag( 0x8824, "SpectralSensity", TagValueType.Unknown),
            new Tag( 0x8828, "OECF", TagValueType.Unknown),
            new Tag( 0x8825, "GPS_IFD_Pointer", TagValueType.Unknown),
            new Tag( 0x8827, "ISOSpeedRatings", TagValueType.UShort),
            new Tag( 0x8828, "OECF", TagValueType.Unknown),
            new Tag( 0x9000, "ExifVersion", TagValueType.Unknown),
            new Tag( 0x9003, "DateTimeOriginal", TagValueType.String),
            new Tag( 0x9004, "DateTimeDigitized", TagValueType.String),
            new Tag( 0x9101, "ComponentsConfiguration", TagValueType.Unknown),
            new Tag( 0x9102, "CompressedBitsPerPixel", TagValueType.Unknown),
            new Tag( 0x9201, "ShutterSpeedValue", TagValueType.Unknown),
            new Tag( 0x9202, "ApertureValue", TagValueType.Unknown),
            new Tag( 0x9203, "BrightnessValue", TagValueType.Unknown),
            new Tag( 0x9204, "ExposureBiasValue", TagValueType.Unknown),
            new Tag( 0x9205, "MaxApertureValue", TagValueType.Unknown),
            new Tag( 0x9206, "SubjectDistance", TagValueType.Unknown),
            new Tag( 0x9207, "MeteringMode", TagValueType.Unknown),
            new Tag( 0x9208, "LightSource", TagValueType.Unknown),
            new Tag( 0x9209, "Flash", TagValueType.Unknown),
            new Tag( 0x920A, "FocalLength", TagValueType.URational),
            new Tag( 0x920B, "FlashEnergy", TagValueType.Unknown),                 /* 0xA20B  in JPEG   */
            new Tag( 0x920C, "SpatialFrequencyResponse", TagValueType.Unknown),    /* 0xA20C    -  -    */
            new Tag( 0x920D, "Noise", TagValueType.Unknown),
            new Tag( 0x920E, "FocalPlaneXResolution", TagValueType.Unknown),       /* 0xA20E    -  -    */
            new Tag( 0x920F, "FocalPlaneYResolution", TagValueType.Unknown),       /* 0xA20F    -  -    */
            new Tag( 0x9210, "FocalPlaneResolutionUnit", TagValueType.Unknown),    /* 0xA210    -  -    */
            new Tag( 0x9211, "ImageNumber", TagValueType.Unknown),
            new Tag( 0x9212, "SecurityClassification", TagValueType.Unknown),
            new Tag( 0x9213, "ImageHistory", TagValueType.Unknown),
            new Tag( 0x9214, "SubjectLocation", TagValueType.Unknown),             /* 0xA214    -  -    */
            new Tag( 0x9215, "ExposureIndex", TagValueType.Unknown),               /* 0xA215    -  -    */
            new Tag( 0x9216, "TIFF/EPStandardID", TagValueType.Unknown),
            new Tag( 0x9217, "SensingMethod", TagValueType.Unknown),               /* 0xA217    -  -    */
            new Tag( 0x923F, "StoNits", TagValueType.Unknown),
            new Tag( 0x927C, "MakerNote", TagValueType.Unknown),
            new Tag( 0x9286, "UserComment", TagValueType.Unknown),
            new Tag( 0x9290, "SubSecTime", TagValueType.Unknown),
            new Tag( 0x9291, "SubSecTimeOriginal", TagValueType.Unknown),
            new Tag( 0x9292, "SubSecTimeDigitized", TagValueType.Unknown),
            new Tag( 0x935C, "ImageSourceData", TagValueType.Unknown),             /* "Adobe Photoshop Document Data Block": 8BIM... */
            new Tag( 0x9c9b, "Title" , TagValueType.Unicode),                      /* Win XP specific, Unicode  */
            new Tag( 0x9c9c, "Comments" , TagValueType.Unicode),                   /* Win XP specific, Unicode  */
            new Tag( 0x9c9d, "Author" , TagValueType.Unicode),                     /* Win XP specific, Unicode  */
            new Tag( 0x9c9e, "Keywords" , TagValueType.Unicode),                   /* Win XP specific, Unicode  */
            new Tag( 0x9c9f, "Subject" , TagValueType.Unicode),                    /* Win XP specific, Unicode, not to be confused with SubjectDistance and SubjectLocation */
            new Tag( 0xA000, "FlashPixVersion", TagValueType.Unknown),
            new Tag( 0xA001, "ColorSpace", TagValueType.Unknown),
            new Tag( 0xA002, "ExifImageWidth", TagValueType.Unknown),
            new Tag( 0xA003, "ExifImageLength", TagValueType.Unknown),
            new Tag( 0xA004, "RelatedSoundFile", TagValueType.Unknown),
            new Tag( 0xA005, "InteroperabilityOffset", TagValueType.Unknown),
            new Tag( 0xA20B, "FlashEnergy", TagValueType.Unknown),                 /* 0x920B in TIFF/EP */
            new Tag( 0xA20C, "SpatialFrequencyResponse", TagValueType.Unknown),    /* 0x920C    -  -    */
            new Tag( 0xA20D, "Noise", TagValueType.Unknown),
            new Tag( 0xA20E, "FocalPlaneXResolution", TagValueType.Unknown),    	/* 0x920E    -  -    */
            new Tag( 0xA20F, "FocalPlaneYResolution", TagValueType.Unknown),       /* 0x920F    -  -    */
            new Tag( 0xA210, "FocalPlaneResolutionUnit", TagValueType.Unknown),    /* 0x9210    -  -    */
            new Tag( 0xA211, "ImageNumber", TagValueType.Unknown),
            new Tag( 0xA212, "SecurityClassification", TagValueType.Unknown),
            new Tag( 0xA213, "ImageHistory", TagValueType.Unknown),
            new Tag( 0xA214, "SubjectLocation", TagValueType.Unknown),             /* 0x9214    -  -    */
            new Tag( 0xA215, "ExposureIndex", TagValueType.Unknown),               /* 0x9215    -  -    */
            new Tag( 0xA216, "TIFF/EPStandardID", TagValueType.Unknown),
            new Tag( 0xA217, "SensingMethod", TagValueType.Unknown),               /* 0x9217    -  -    */
            new Tag( 0xA300, "FileSource", TagValueType.Unknown),
            new Tag( 0xA301, "SceneType", TagValueType.Unknown),
            new Tag( 0xA302, "CFAPattern", TagValueType.Unknown),
            new Tag( 0xA401, "CustomRendered", TagValueType.Unknown),
            new Tag( 0xA402, "ExposureMode", TagValueType.Unknown),
            new Tag( 0xA403, "WhiteBalance", TagValueType.Unknown),
            new Tag( 0xA404, "DigitalZoomRatio", TagValueType.Unknown),
            new Tag( 0xA405, "FocalLengthIn35mmFilm", TagValueType.Unknown),
            new Tag( 0xA406, "SceneCaptureType", TagValueType.Unknown),
            new Tag( 0xA407, "GainControl", TagValueType.Unknown),
            new Tag( 0xA408, "Contrast", TagValueType.Unknown),
            new Tag( 0xA409, "Saturation", TagValueType.Unknown),
            new Tag( 0xA40A, "Sharpness", TagValueType.Unknown),
            new Tag( 0xA40B, "DeviceSettingDescription", TagValueType.Unknown),
            new Tag( 0xA40C, "SubjectDistanceRange", TagValueType.Unknown),
            new Tag( 0xA420, "ImageUniqueID", TagValueType.Unknown)
        };

        #endregion

        #region read_exif_data

        /// <summary>
        /// This is alternative alias of <see cref="exif_read_data(string,string,bool,bool)"/>.
        /// </summary>
        [return: CastToFalse]
        public static PhpArray read_exif_data(Context ctx, string filename, string sections = null, bool arrays = false, bool thumbnail = false)
        {
            return exif_read_data(ctx, filename, sections, arrays, thumbnail);
        }

        #endregion

        #region exif_read_data

        /// <summary>
        /// Reads the EXIF headers from JPEG or TIFF
        /// </summary>
        /// <param name="ctx">Runtime context.</param>
        /// <param name="filename">The name of the image file being read. This cannot be an URL.</param>
        /// <param name="sections">Is a comma separated list of sections that need to be present in file to produce a result array. If none of the requested sections could be found the return value is FALSE.
        /// 
        /// FILE:	FileName, FileSize, FileDateTime, SectionsFound
        /// COMPUTED:	 html, Width, Height, IsColor, and more if available. Height and Width are computed the same way getimagesize() does so their values must not be part of any header returned. Also, html is a height/width text string to be used inside normal HTML.
        /// ANY_TAG:	Any information that has a Tag e.g. IFD0, EXIF, ...
        /// IFD0:	 All tagged data of IFD0. In normal imagefiles this contains image size and so forth.
        /// THUMBNAIL:	 A file is supposed to contain a thumbnail if it has a second IFD. All tagged information about the embedded thumbnail is stored in this section.
        /// COMMENT:	Comment headers of JPEG images.
        /// EXIF:	 The EXIF section is a sub section of IFD0. It contains more detailed information about an image. Most of these entries are digital camera related.</param>
        /// <param name="arrays">Specifies whether or not each section becomes an array. The sections COMPUTED, THUMBNAIL, and COMMENT always become arrays as they may contain values whose names conflict with other sections.</param>
        /// <param name="thumbnail">When set to <c>TRUE</c> the thumbnail itself is read. Otherwise, only the tagged data is read.</param>
        /// <returns>It returns an associative array where the array indexes are the header names and the array values are the values associated with those headers.
        /// If no data can be returned, <c>FALSE</c> is returned.</returns>
        [return: CastToFalse]
        public static PhpArray exif_read_data(Context ctx, string filename, string sections = null, bool arrays = false, bool thumbnail = false)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return null;
            }

            if (!string.IsNullOrEmpty(sections))
            {
                PhpException.ArgumentValueNotSupported("sections", sections);
            }

            if (arrays)
            {
                PhpException.ArgumentValueNotSupported("arrays", arrays);
            }

            if (thumbnail)
            {
                PhpException.ArgumentValueNotSupported("thumbnail", thumbnail);
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

            Image image;

            using (var ms = new MemoryStream(bytes))
            {
                try
                {
                    image = new Image(ms);
                }
                catch
                {
                    return null;
                }

                var encoding = System.Text.Encoding.ASCII;
                var unicode = System.Text.Encoding.Unicode;

                foreach (var item in image.MetaData.Properties)
                {
                    //int i = Array.BinarySearch(IFDTagTable, new Tag(item.Id, null, TagValueType.Unknown));

                    //if (i > 0)
                    //{
                    //    var tag = IFDTagTable[i];

                    //    switch (tag.type)
                    //    {
                    //        case TagValueType.String:
                    //            array.Add(tag.name, encoding.GetString(item.Value));
                    //            break;
                    //        case TagValueType.UShort:
                    //            array.Add(tag.name, (int)BitConverter.ToInt16(item.Value, 0));
                    //            break;
                    //        case TagValueType.UInt:
                    //            array.Add(tag.name, BitConverter.ToInt32(item.Value, 0));
                    //            break;
                    //        case TagValueType.ULong:
                    //            array.Add(tag.name, BitConverter.ToInt64(item.Value, 0));
                    //            break;
                    //        case TagValueType.URational:
                    //            array.Add(tag.name,
                    //                BitConverter.ToUInt16(item.Value.Take(4).ToArray(), 0).ToString()
                    //                + "/" + BitConverter.ToUInt16(item.Value.Skip(4).ToArray(), 0).ToString()
                    //            );
                    //            break;
                    //        case TagValueType.Unicode:
                    //            array.Add(tag.name, unicode.GetString(item.Value));
                    //            break;
                    //        case TagValueType.Unknown:
                    //        default:
                    //            array.Add(tag.namem, new PhpBytes(item.Value));
                    //            break;
                    //    }
                    //}
                    //else
                    //{
                    //    array.Add(item.Id.ToString(), new PhpBytes(item.Value));
                    //}

                    array.Add(item.Name, item.Value);
                }

                image.Dispose();
            }

            return array;
        }

        #endregion

        #region exif_tagname

        /// <summary>
        /// Get the header name for an index
        /// </summary>
        /// <returns></returns>
        [return: CastToFalse]
        public static string exif_tagname(int index)
        {
            int i = Array.BinarySearch(IFDTagTable, new Tag(index, null, TagValueType.Unknown));

            if (i > 0)
            {
                return IFDTagTable[i].name;
            }

            return null;
        }

        #endregion

        #region exif_imagetype

        /// <summary>
        /// Determine the type of an image
        /// </summary>
        /// <returns></returns>
        [return: CastToFalse]
        public static int exif_imagetype(Context ctx, string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                PhpException.Throw(PhpError.Warning, Resources.filename_cannot_be_empty);
                return -1;
            }

            var stream = Utils.OpenStream(ctx, filename);
            if (stream == null)
            {
                PhpException.Throw(PhpError.Warning, Resources.read_error);
                return -1;
            }

            PhpImage.ImageType type;
            try
            {
                PhpImage.ImageSignature.ImageInfo info;
                type = PhpImage.ImageSignature.ProcessImageType(stream, true, out info, false, false);
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
                return null;
            }

            if (imagetype != null)
            {
                PhpException.ArgumentValueNotSupported(nameof(imagetype), "!=null");
            }

            Image thumbnail = null;
            byte[] result;

            var bytes = Utils.ReadPhpBytes(ctx, filename);

            if (bytes == null)
                return null;

            // get thumbnail from <filename>'s content:
            using (var ms = new MemoryStream(bytes))
            {
                try
                {
                    using (var image = new Image(ms))
                    {
                        //thumbnail = image.GetThumbnailImage(0, 0, () => true, IntPtr.Zero);
                    }
                }
                catch
                {
                    return null;
                }
            }

            if (thumbnail == null)
            {
                return null;
            }

            //
            if (width != null)
                width.Value = (PhpValue)thumbnail.Width;

            if (height != null)
                height.Value = (PhpValue)thumbnail.Height;

            using (var ms2 = new MemoryStream())
            {
                thumbnail.Save(ms2, new ImageSharp.Formats.PngEncoder());
                result = ms2.ToArray();
            }

            thumbnail.Dispose();

            //
            return new PhpString(result);
        }

        #endregion
    }
}
