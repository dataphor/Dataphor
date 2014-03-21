/*
	Alphora Dataphor
	© Copyright 2000-2008 Alphora
	This file is licensed under a modified BSD-license which can be found here: http://dataphor.org/dataphor_license.txt
*/

using System;
using System.IO;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Net;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

using Alphora.Dataphor.BOP;

namespace Alphora.Dataphor.Frontend.Client.Web
{
    /// <summary> Maintains and caches images. </summary>
    /// <remarks> There are two methods for working with the ImageCache:
    ///		<list type="bullet">
    ///		<item><term>Allocation/Deallocate</term><description>This method caches the images when they are first read.</description></item>
    ///		<item><term>RegisterImageHandler/UnregisterImageHandler</term><description>This method does not cache the images, but rather passes the request to a specified handler.</description></item>
    ///		</list>
    /// </remarks>
    public class ImageCache
    {
        public const string CacheFileExtension = ".dfc";

        /// <summary> Instantiates a new DocumentCache. </summary>
        /// <param name="cachePath"> Specifies the path to use to store cached items.  </param>
        public ImageCache(string cachePath, Session session)
        {
            // Prepare the folder
            Directory.CreateDirectory(cachePath);
            _cachePath = cachePath;
            _session = session;
            _identifiers = new Hashtable();
        }

        public void Dispose()
        {
            if (_identifiers != null)
            {
                string fileName;
                foreach (DictionaryEntry entry in _identifiers)
                {
                    fileName = BuildFileName((string)entry.Key);
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                }
                _identifiers = null;
            }
        }

        // FImageHandlers

        private Hashtable _imageHandlers = new Hashtable();

        public LoadImageHandler GetImageHandler(string iD)
        {
            return (LoadImageHandler)_imageHandlers[iD];
        }

        public string RegisterImageHandler(LoadImageHandler handler)
        {
            string iD = Session.GenerateID();
            _imageHandlers[iD] = handler;
            return iD;
        }

        public void UnregisterImageHandler(string iD)
        {
            _imageHandlers.Remove(iD);
        }

        // Allocate / Deallocate

        private Hashtable _identifiers;

        private string _cachePath;
        public string CachePath { get { return _cachePath; } }

        private Session _session;

        private string BuildFileName(string imageID)
        {
            return String.Format("{0}\\{1}{2}", _cachePath, imageID, CacheFileExtension);
        }

        private string GetImageID()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace('/', '_').Replace('+', '~');
        }

        public string Allocate(string imageExpression)
        {
            if (imageExpression != String.Empty)
            {
                string imageID = GetImageID();
                _identifiers.Add(imageID, imageExpression);
                return imageID;
            }
            else
                return String.Empty;
        }

        public string Allocate(GetImageHandler getImage)
        {
            if (getImage != null)
            {
                string imageID = GetImageID();
                _identifiers.Add(imageID, getImage);
                return imageID;
            }
            else
                return String.Empty;
        }

        public void Deallocate(string imageID)
        {
            if (imageID != String.Empty)
            {
                string fileName = BuildFileName(imageID);
                if (File.Exists(fileName))
                    File.Delete(fileName);
                _identifiers.Remove(imageID);
            }
        }

        public Stream Read(string imageID)
        {
            string fileName = BuildFileName(imageID);
            if (File.Exists(fileName))
                return File.OpenRead(fileName);
            else
            {
                FileStream fileStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite);
                try
                {
                    object tempValue = _identifiers[imageID];
                    if (tempValue is String)
                    {
                        using (DAE.Runtime.Data.Scalar scalar = (DAE.Runtime.Data.Scalar)_session.Pipe.RequestDocument((string)tempValue))
                            StreamUtility.CopyStream(scalar.OpenStream(), fileStream);
                    }
                    else
                        ((GetImageHandler)tempValue)(imageID, fileStream);
                    fileStream.Position = 0;
                    return fileStream;
                }
                catch
                {
                    fileStream.Close();
                    if (File.Exists(fileName))
                        File.Delete(fileName);
                    throw;
                }
            }
        }
    }

    public delegate void GetImageHandler(string AImageID, Stream AStream);

    public delegate void LoadImageHandler(HttpContext AContext, string AID, Stream AStream);
}
