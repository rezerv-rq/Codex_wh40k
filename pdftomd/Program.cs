using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Введите путь к папке для сканирования:");
        string rootPath = Console.ReadLine();

        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine("Указанная папка не существует!");
            return;
        }

        var mdContent = new StringBuilder();
        mdContent.AppendLine("# codexes:");
        mdContent.AppendLine();

        var filesByCategory = ScanPdfFiles(rootPath);

        GenerateMarkdown(filesByCategory, mdContent);

        string outputPath = Path.Combine(rootPath, "codex_index.md");
        File.WriteAllText(outputPath, mdContent.ToString());

        Console.WriteLine($"Markdown файл создан: {outputPath}");
        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadLine();
    }

    static Dictionary<string, List<PdfFile>> ScanPdfFiles(string rootPath)
    {
        var filesByCategory = new Dictionary<string, List<PdfFile>>();

        foreach (var file in Directory.GetFiles(rootPath, "*.pdf", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(rootPath, file);
            string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);

            string category = "Other";
            string subcategory = null;
            string fileName = Path.GetFileNameWithoutExtension(file);

            if (pathParts.Length > 1)
            {
                category = pathParts[0];
                if (pathParts.Length > 2)
                {
                    subcategory = pathParts[1];
                }
            }

            if (!filesByCategory.ContainsKey(category))
            {
                filesByCategory[category] = new List<PdfFile>();
            }

            filesByCategory[category].Add(new PdfFile
            {
                Name = CleanFileName(fileName),
                Path = relativePath.Replace(Path.DirectorySeparatorChar, '/'),
                IsSupplement = IsSupplement(fileName),
                Subcategory = subcategory
            });
        }

        return filesByCategory;
    }

    static string CleanFileName(string fileName)
    {
        string[] suffixes = { "_10th", "10th", "_scan", "_photo", "_OCR" };
        foreach (var suffix in suffixes)
        {
            fileName = fileName.Replace(suffix, "");
        }

        return fileName.Replace("_", " ")
                      .Replace("  ", " ")
                      .Trim();
    }

    static bool IsSupplement(string fileName)
    {
        return fileName.IndexOf("supplement", StringComparison.OrdinalIgnoreCase) >= 0 ||
               fileName.IndexOf("suplement", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static void GenerateMarkdown(Dictionary<string, List<PdfFile>> filesByCategory, StringBuilder mdContent)
    {
        var sortedCategories = filesByCategory.Keys.OrderBy(k => k);

        foreach (var category in sortedCategories)
        {
            mdContent.AppendLine($"## {category}");
            mdContent.AppendLine();

            var filesBySubcategory = filesByCategory[category]
                .GroupBy(f => f.Subcategory)
                .OrderBy(g => g.Key ?? "");

            foreach (var subcategoryGroup in filesBySubcategory)
            {
                if (!string.IsNullOrEmpty(subcategoryGroup.Key))
                {
                    mdContent.AppendLine($"### {subcategoryGroup.Key}");
                    mdContent.AppendLine();
                }

                foreach (var file in subcategoryGroup.OrderBy(f => f.Name))
                {
                    string entry = file.IsSupplement 
                        ? $"- {file.Name} (supplement)" 
                        : $"- [{file.Name}]({file.Path})";

                    mdContent.AppendLine(entry);
                }

                mdContent.AppendLine();
            }
        }
    }
}

class PdfFile
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsSupplement { get; set; }
    public string Subcategory { get; set; }
}