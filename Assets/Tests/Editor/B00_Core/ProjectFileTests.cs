using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Bagel.B00_Core
{
    // The game is made of code and text: meshes come from .procmesh inputs, images are vector (SVGs drawn
    // for the game, or Painter2D) and sounds are synthesized. Fonts are the only binary files.
    public class ProjectFileTests
    {
        static readonly string[] s_Models = { ".fbx", ".obj", ".blend", ".dae", ".3ds", ".max", ".ma", ".mb", ".usd", ".usda", ".usdc", ".usdz", ".gltf", ".glb", ".ply", ".stl" };
        static readonly string[] s_Images = { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".hdr", ".tif", ".tiff", ".bmp", ".gif", ".iff", ".pict", ".dds", ".ktx", ".webp" };
        static readonly string[] s_Sounds = { ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac", ".mod", ".it", ".s3m", ".xm" };

        static readonly Regex s_MaterialTexture = new(@"m_Texture: \{fileID: (-?\d+)");
        // What icon sites and drawing apps leave in a file they export.
        static readonly Regex s_DownloadedSvg = new(@"svgrepo|flaticon|freepik|iconify|Uploaded to|Generator:|<!DOCTYPE", RegexOptions.IgnoreCase);

        static IEnumerable<string> FilesEndingIn(string[] extensions) =>
            Directory.GetFiles("Assets", "*", SearchOption.AllDirectories).Where(file => extensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

        [Test]
        public void ThereAreNoModelFiles()
        {
            Assert.IsEmpty(FilesEndingIn(s_Models));
        }

        [Test]
        public void ThereAreNoRasterImages()
        {
            Assert.IsEmpty(FilesEndingIn(s_Images));
        }

        [Test]
        public void ThereAreNoSoundFiles()
        {
            Assert.IsEmpty(FilesEndingIn(s_Sounds));
        }

        // Every icon is drawn for the game, by hand: nothing downloaded, so nothing to credit.
        [Test]
        public void EverySvgIsDrawnForTheGame()
        {
            var problems = Directory.GetFiles("Assets", "*.svg", SearchOption.AllDirectories)
                .Where(path => s_DownloadedSvg.IsMatch(Path.GetFileName(path)) || s_DownloadedSvg.IsMatch(File.ReadAllText(path)))
                .ToList();
            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.IsFalse(File.ReadAllText("Third Party Notices.md").Contains("Icons"), "no icon set to credit");
        }

        // The SVG importer draws shapes and paths only: words are drawn as paths, or left to a Label.
        [Test]
        public void SvgsHaveNoText()
        {
            var problems = Directory.GetFiles("Assets", "*.svg", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("<text"))
                .ToList();
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void MaterialsAndShadersUseNoTextures()
        {
            var problems = new List<string>();
            foreach (var path in Directory.GetFiles("Assets", "*.mat", SearchOption.AllDirectories))
            {
                if (s_MaterialTexture.Matches(File.ReadAllText(path)).Any(match => match.Groups[1].Value != "0"))
                    problems.Add(path);
            }
            foreach (var path in Directory.GetFiles("Assets", "*.shadergraph", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(path).Contains("\\\"texture\\\":{\\\"fileID\\\""))
                    problems.Add(path);
            }
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }
    }
}
