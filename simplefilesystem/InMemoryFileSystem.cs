using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace simplefilesystem
{
    public class InMemoryFileSystem : IFileSystem
    {
        public InMemoryFileSystem()
        {
            Directories.Add(FilePath.Root, new HashSet<FilePath>());
        }

        private IDictionary<FilePath, ISet<FilePath>> Directories { get; } =
                            new Dictionary<FilePath, ISet<FilePath>>();

        private IDictionary<FilePath, MemoryFile> Files { get; } =
            new Dictionary<FilePath, MemoryFile>();

        public void CreateDirectory(FilePath path)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is no directory.", nameof(path));
            if (Directories.ContainsKey(path))
                return;
            if (!Directories.TryGetValue(path.ParentPath, out var subentities))
                throw new DirectoryNotFoundException();
            subentities.Add(path);
            Directories[path] = new HashSet<FilePath>();
        }

        public Stream CreateFile(FilePath path)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is no file.", nameof(path));
            if (!Directories.ContainsKey(path.ParentPath))
                throw new DirectoryNotFoundException();
            Directories[path.ParentPath].Add(path);
            var file = Files[path] = new MemoryFile();
            return new MemoryFileStream(file);
        }

        public FilePath CreateFullPath(string path)
        {
            var root = FilePath.Root;
            var folders = path.Split(FilePath.DirectorySeparator);
            foreach (var folder in folders)
            {
                root = root.AppendDirectory(folder);
                CreateDirectory(root);
            }
            return root;
        }

        public void CreateTextFile(FilePath path, string contents)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is no file.", nameof(path));
            if (!Directories.ContainsKey(path.ParentPath))
                throw new DirectoryNotFoundException();
            Directories[path.ParentPath].Add(path);
            var file = Files[path] = new MemoryFile();
            var stream = new MemoryFileStream(file);
            var bytes = new UTF8Encoding().GetBytes(contents);
            stream.Write(bytes, 0, contents.Length);
            stream.Close();
        }

        public void Delete(FilePath path)
        {
            if (path.IsRoot)
                throw new ArgumentException("The root cannot be deleted.");
            var removed = path.IsDirectory
                ? Directories.Remove(path)
                : Files.Remove(path);
            if (!removed)
                throw new ArgumentException("The specified path does not exist.");
            var parent = Directories[path.ParentPath];
            parent.Remove(path);
        }

        public override bool Equals(object obj)
        {
            return obj is InMemoryFileSystem system &&
                   EqualityComparer<IDictionary<FilePath, ISet<FilePath>>>.Default.Equals(Directories, system.Directories) &&
                   EqualityComparer<IDictionary<FilePath, MemoryFile>>.Default.Equals(Files, system.Files);
        }

        public bool Exists(FilePath path)
        {
            return path.IsDirectory
                ? Directories.ContainsKey(path)
                : Files.ContainsKey(path);
        }

        public FilePath GetCurrentDirectory()
        {
            var path = Directory.GetCurrentDirectory();
            var folders = path.Split(FilePath.DirectorySeparator);
            var root = FilePath.Root;
            foreach (var folder in folders)
            {
                root = root.AppendDirectory(folder);
                CreateDirectory(root);
            }
            return root;
        }

        public ICollection<FilePath> GetEntities(FilePath path)
        {
            if (!path.IsDirectory)
                throw new ArgumentException("The specified path is no directory.", nameof(path));
            if (!Directories.TryGetValue(path, out var subentities))
                throw new DirectoryNotFoundException();
            return subentities;
        }

        public override int GetHashCode()
        {
            var hashCode = 1865982412;
            hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<FilePath, ISet<FilePath>>>.Default.GetHashCode(Directories);
            hashCode = hashCode * -1521134295 + EqualityComparer<IDictionary<FilePath, MemoryFile>>.Default.GetHashCode(Files);
            return hashCode;
        }

        public Stream OpenFile(FilePath path, FileAccess access)
        {
            if (!path.IsFile)
                throw new ArgumentException("The specified path is no file.", nameof(path));
            if (!Files.TryGetValue(path, out var file))
                throw new FileNotFoundException();
            return new MemoryFileStream(file);
        }

        public string ReadAllText(FilePath path)
        {
            var stream = OpenFile(path, FileAccess.Read);
            var bytes = new byte[300000];
            stream.Read(bytes, 0, 300000);
            return Encoding.UTF8.GetString(bytes);
        }

        public class MemoryFile
        {
            public MemoryFile()
                : this(new byte[0])
            {
            }

            public MemoryFile(byte[] content)
            {
                Content = content;
            }

            public byte[] Content { get; set; }
        }

        public class MemoryFileStream : Stream
        {
            private readonly MemoryFile _file;

            public MemoryFileStream(MemoryFile file)
            {
                _file = file;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public byte[] Content
            {
                get => _file.Content;
                set => _file.Content = value;
            }

            public override long Length => _file.Content.Length;

            public override long Position { get; set; }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int mincount = Math.Min(count, Math.Abs((int)(Length - Position)));
                Buffer.BlockCopy(Content, (int)Position, buffer, offset, mincount);
                Position += mincount;
                return mincount;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                    return Position = offset;
                if (origin == SeekOrigin.Current)
                    return Position += offset;
                return Position = Length - offset;
            }

            public override void SetLength(long value)
            {
                int newLength = (int)value;
                byte[] newContent = new byte[newLength];
                Buffer.BlockCopy(Content, 0, newContent, 0, Math.Min(newLength, (int)Length));
                Content = newContent;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (Length - Position < count)
                    SetLength(Position + count);
                Buffer.BlockCopy(buffer, offset, Content, (int)Position, count);
                Position += count;
            }
        }
    }
}