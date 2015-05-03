using System.IO;

// This is a subset of the full Palaso.IO.TempFile

namespace NetSparkle
{
    /// <summary>
    ///     This is useful a temporary file is needed. When it is disposed, it will delete the file.
    /// </summary>
    /// <example>using(f = new TempFile())</example>
    public class TempFile
    {
        internal TempFile(string existingPath)
        {
            Path = existingPath;
        }

        internal string Path { get; }

        internal void Delete()
        {
            File.Delete(Path);
        }

        /// <summary>
        ///     Create a TempFile based on a pre-existing file, which will be deleted when this is disposed.
        /// </summary>
        internal static TempFile TrackExisting(string path)
        {
            return new TempFile(path);
        }

        /// <summary>
        ///     Use this one when it's important to have a certain file extension
        /// </summary>
        /// <param name="extension">with or with out '.', will work the same</param>
        internal static TempFile WithExtension(string extension)
        {
            extension = extension.TrimStart('.');
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName() + "." + extension);
            File.Create(path).Close();
            return TrackExisting(path);
        }
    }
}