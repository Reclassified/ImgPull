using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "ImgPull by Masterblastr";
        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] defaultDirectories = { "Desktop", "Documents", "Downloads", "3D Objects" };

        // Ensure the destination directory exists
        string destinationDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        // Traverse the default directories and copy image files
        Console.WriteLine("Copying image files...");
        foreach (string directory in defaultDirectories)
        {
            string sourceDirectory = Path.Combine(userProfile, directory);
            if (Directory.Exists(sourceDirectory))
            {
                await TraverseAndCopyImagesAsync(sourceDirectory, destinationDirectory);
            }
        }

        Console.WriteLine("Image files copied successfully to the 'output' folder.");
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    static async Task TraverseAndCopyImagesAsync(string sourceDir, string destDir)
    {
        var directories = new Stack<string>();
        directories.Push(sourceDir);

        while (directories.Count > 0)
        {
            string currentDir = directories.Pop();
            Console.WriteLine($"Traversing directory: {currentDir}");

            try
            {
                string[] files = Directory.GetFiles(currentDir);
                var tasks = new List<Task>();

                foreach (string file in files)
                {
                    if (IsImageFile(file))
                    {
                        tasks.Add(CopyImageFileAsync(file, destDir));
                    }
                }

                await Task.WhenAll(tasks);

                string[] subDirs = Directory.GetDirectories(currentDir);
                foreach (string subDir in subDirs)
                {
                    directories.Push(subDir);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                // Log or display the error
                Console.WriteLine($"Access denied: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log or display other exceptions
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    static async Task CopyImageFileAsync(string filePath, string destDir)
    {
        try
        {
            string destFilePath = Path.Combine(destDir, Path.GetFileName(filePath));
            int count = 1;
            string fileNameOnly = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            while (File.Exists(destFilePath))
            {
                string newFileName = $"{fileNameOnly}_{count}{extension}";
                destFilePath = Path.Combine(destDir, newFileName);
                count++;
            }

            using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
            using (var destinationStream = new FileStream(destFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
        catch (Exception ex)
        {
            // Log the exception (e.g., to a log file or console)
            Console.WriteLine($"Failed to copy file: {filePath}. Error: {ex.Message}");
        }
    }

    static bool IsImageFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".bmp";
        // Add more extensions as needed
    }
}
