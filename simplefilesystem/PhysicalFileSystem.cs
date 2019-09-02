using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace simplefilesystem
{
    /// <summary>borrowed heavily from https://github.com/bobvanderlinden/sharpfilesystem</summary>
    public class PhysicalFileSystem : IFileSystem
    {
        public PhysicalFileSystem(string physicalRoot)
        {
            if (!Path.IsPathRooted(physicalRoot))
                physicalRoot = Path.GetFullPath(physicalRoot);
            if (physicalRoot[physicalRoot.Length - 1] != Path.DirectorySeparatorChar)
                physicalRoot = physicalRoot + Path.DirectorySeparatorChar;
            PhysicalRoot = physicalRoot;
        }

        public string PhysicalRoot { get; private set; }

        public void CreateDirectory(FilePath path)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is not a directory.", nameof(path));
            Directory.CreateDirectory(GetPhysicalPath(path));
        }

        public Stream CreateFile(FilePath path)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", nameof(path));
            return File.Create(GetPhysicalPath(path));
        }

        public FilePath CreateFullPath(string path)
        {
            return GetVirtualDirectoryPath(path);
        }

        public void CreateTextFile(FilePath path, string contents)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", nameof(path));
            File.WriteAllText(GetPhysicalPath(path), contents);
        }

        public void Delete(FilePath path)
        {
            if (path.IsFile)
                File.Delete(GetPhysicalPath(path));
            else
                Directory.Delete(GetPhysicalPath(path), true);
        }

        public void Dispose()
        {
            PhysicalRoot = null;
        }

        public bool Exists(FilePath path)
        {
            return path.IsFile
                ? File.Exists(GetPhysicalPath(path))
                : Directory.Exists(GetPhysicalPath(path));
        }

        public FilePath GetCurrentDirectory()
        {
            var path = Directory.GetCurrentDirectory();
            return GetVirtualDirectoryPath(path);
        }

        public ICollection<FilePath> GetEntities(FilePath path)
        {
            var physicalPath = GetPhysicalPath(path);
            var directories = Directory.GetDirectories(physicalPath);
            var files = Directory.GetFiles(physicalPath);
            var virtualDirectories =
                directories.Select(GetVirtualDirectoryPath);
            var virtualFiles =
                files.Select(GetVirtualFilePath);
            return new EnumerableCollection<FilePath>(virtualDirectories.Concat(virtualFiles), directories.Length + files.Length);
        }

        public string GetPhysicalPath(FilePath path)
        {
            return Path.Combine(PhysicalRoot, path.ToString().Remove(0, 1).Replace(FilePath.DirectorySeparator, Path.DirectorySeparatorChar));
        }

        public FilePath GetVirtualDirectoryPath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", nameof(physicalPath));
            var virtualPath = FilePath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FilePath.DirectorySeparator);
            if (virtualPath[virtualPath.Length - 1] != FilePath.DirectorySeparator)
                virtualPath += FilePath.DirectorySeparator;
            return FilePath.Parse(virtualPath);
        }

        public FilePath GetVirtualFilePath(string physicalPath)
        {
            if (!physicalPath.StartsWith(PhysicalRoot, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The specified path is not member of the PhysicalRoot.", nameof(physicalPath));
            var virtualPath = FilePath.DirectorySeparator + physicalPath.Remove(0, PhysicalRoot.Length).Replace(Path.DirectorySeparatorChar, FilePath.DirectorySeparator);
            return FilePath.Parse(virtualPath);
        }

        public Stream OpenFile(FilePath path, FileAccess access)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is not a file.", nameof(path));
            return File.Open(GetPhysicalPath(path), FileMode.Open, access);
        }

        public string ReadAllText(FilePath path)
        {
            return File.ReadAllText(GetPhysicalPath(path));
        }
    }
}