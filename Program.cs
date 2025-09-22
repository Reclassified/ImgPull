using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "ImgPull by Masterblastr";

        // Destination folder in current working directory
        string destinationDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output");
        Directory.CreateDirectory(destinationDirectory);

        Console.WriteLine("Copying image files from all accessible drives...");

        // Enumerate all accessible fixed drives
        var drives = DriveInfo.GetDrives()
                              .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                              .Select(d => d.RootDirectory.FullName);

        var tasks = drives.Select(d => TraverseAndCopyImagesAsync(d, destinationDirectory));
        await Task.WhenAll(tasks);

        Console.WriteLine("Image files copied successfully to the 'output' folder.");
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }

    // ✅ Only allow common photo formats
    static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    static bool IsImageFile(string filePath) =>
        ImageExtensions.Contains(Path.GetExtension(filePath));

    static async Task TraverseAndCopyImagesAsync(string sourceDir, string destDir)
    {
        try
        {
            // Copy all image files in this directory
            var files = Directory.EnumerateFiles(sourceDir);
            var copyTasks = files
                .Where(IsImageFile)
                .Select(f => CopyImageFileAsync(f, destDir));

            await Task.WhenAll(copyTasks);

            // Recurse into subdirectories in parallel
            var subDirs = Directory.EnumerateDirectories(sourceDir);
            var subTasks = subDirs.Select(sd => TraverseAndCopyImagesAsync(sd, destDir));
            await Task.WhenAll(subTasks);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"Access denied: {sourceDir}");
        }
        catch (PathTooLongException)
        {
            Console.WriteLine($"Path too long: {sourceDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {sourceDir}: {ex.Message}");
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

            // Ensure unique filename
            while (File.Exists(destFilePath))
            {
                string newFileName = $"{fileNameOnly}_{count}{extension}";
                destFilePath = Path.Combine(destDir, newFileName);
                count++;
            }

            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var destinationStream = new FileStream(destFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);

            Console.WriteLine($"Copied: {filePath} -> {destFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to copy {filePath}: {ex.Message}");
        }
    }
}
