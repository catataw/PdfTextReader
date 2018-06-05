﻿using ParserFrontend.Infra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParserFrontend.Logic
{
    public class DownloadFolder
    {
        readonly IVirtualFS2 _vfs;

        public DownloadFolder(IVirtualFS2 vfs)
        {
            this._vfs = vfs;
        }

        Regex _quickFix = new Regex(@"pubDate=""(\d\d\d\d)_(\d\d)_(\d\d)");
        string QuickFix(string input)
        {
            return _quickFix.Replace(input, @"pubDate=""{3}/{2}/{1}");
        }

        public Stream DownloadQuickFix(string path)
        {
            var filenames = _vfs.ListFolderContent(path);

            using (var zip = new ZipCompression())
            {
                foreach (var filename in filenames)
                {
                    string basename = GetFilename(filename);
                    using (var file = _vfs.OpenReader(filename))
                    using (var txtFile = new StreamReader(file))
                    {
                        var text = txtFile.ReadToEnd();
                        var newtext = QuickFix(text);
                        var newstream = new MemoryStream();
                        var memwrite = new StreamWriter(newstream);
                        memwrite.Write(newtext);
                        newstream.Seek(0, SeekOrigin.Begin);
                        zip.Add(basename, newstream);
                    }
                }

                return zip.DownloadStream();
            }
        }

        public Stream Download2(string path)
        {
            var filenames = _vfs.ListFolderContent(path);

            using (var zip = new ZipCompression())
            {
                foreach(var filename in filenames)
                {
                    string basename = GetFilename(filename);
                    using (var file = _vfs.OpenReader(filename))
                    {
                        zip.Add(basename, file);
                    }                        
                }

                return zip.DownloadStream();
            }
        }

        string GetFilename(string filename)
        {
            string[] components = filename.Split('/');
            return components[components.Length - 1];
        }
    }
}
