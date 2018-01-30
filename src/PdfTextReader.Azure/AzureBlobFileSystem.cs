﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using PdfTextReader;
using PdfTextReader.Azure.Blob;

namespace PdfTextReader.Azure
{
    public class AzureBlobFileSystem
    {
        AzureBlobFS _root = new AzureBlobFS();
        string _workingFolder;
        IAzureBlobFolder _currentFolder;

        public void AddStorageAccount(string name, string connectionString)
        {
            _root.AddStorage(name, connectionString);
        }

        public void SetWorkingFolder(string path)
        {
            _currentFolder = (path != null) ? GetAbsoluteFolder(path) : _root;

            _workingFolder = path;
        }

        public string GetWorkingFolder() => _workingFolder;

        IAzureBlobFolder GetAbsoluteFolder(string path)
        {
            string name = RemoveProtocol(path);

            return _root.GetFolder(name);
        }

        public IAzureBlobFolder GetFolder(string name)
        {
            return _currentFolder.GetFolder(name);
        }

        public IAzureBlobFile GetFile(string name)
        {
            return _currentFolder.GetFile(name);
        }

        public IEnumerable<IAzureBlob> EnumItems()
        {
            return _currentFolder.EnumItems();
        }        

        string RemoveProtocol(string path)
        {
            if (_root.Path == path)
                return "";

            if (path.StartsWith(_root.Path))
                return path.Substring(_root.Path.Length+1);

            throw new System.IO.DirectoryNotFoundException($"Invalid protocol '{path}'");
        }
    }
}