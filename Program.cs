using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using UAssetAPI;
using UAssetAPI.PropertyTypes;
using UAssetAPI.StructTypes;

var fNameBeatShooterSongInfo = new FName("BeatShooterSongInfo");
var fNameSongInfoProgress = new FName("SongInfoProgress");
var fNameSongFilePath = new FName("SongFilePath");
var fNameSongName = new FName("SongName");
var fNameArtistName = new FName("ArtistName");
var fNameAlbumName = new FName("AlbumName");
var fNameCreditsLine = new FName("CreditsLine");
var fNameAlbumCover = new FName("AlbumCover");
var fNameSongDuration = new FName("SongDuration");
var fNameOverrideLevelFilePath = new FName("OverrideLevelFilePath");
var fNameBAlwaysHidden = new FName("bAlwaysHidden");
var fNameBHiddenUntilCompleted = new FName("bHiddenUntilCompleted");
var fNameBeatsPerMinute = new FName("BeatsPerMinute");
var fNameGenre = new FName("Genre");
var fNameFamily = new FName("Family");
var fNameEnvironmentType = new FName("EnvironmentType");
var fNameLoudness = new FName("Loudness");
var fNameAudioAnalysisFilename = new FName("AudioAnalysisFilename");

var fNameFilePath = new FName("FilePath");
var fNameEBeatShooterSongInfoProgress = new FName("EBeatShooterSongInfoProgress");
var fNameEBeatShooterSongInfoProgressPostAudioAnalysis = new FName("EBeatShooterSongInfoProgress::PostAudioAnalysis");
var fNameEBeatShooterMusicGenre = new FName("EBeatShooterMusicGenre");
var fNameEBeatShooterMusicGenreValuePrefix = "EBeatShooterMusicGenre::";
var fNameEBeatShooterMusicFamily = new FName("EBeatShooterMusicFamily");
var fNameEBeatShooterMusicFamilyValuePrefix = "EBeatShooterMusicFamily::";
var fNameEBeatShooterEnvironmentType = new FName("EBeatShooterEnvironmentType");
var fNameEBeatShooterEnvironmentTypeValuePrefix = "EBeatShooterEnvironmentType::";

const string gameName = "BeatShooter";
const string packToolPath = "./u4pak.exe";
const string outputPath = "./output";
const string outputFilePath = $"{outputPath}/SoundFallSongsPack.pak";
const string runtimePath = "./runtime";
const string assetRuntimePath = $"{runtimePath}/{gameName}";
const string contentFolder = "Content/Music/Mods";
const string assetFolder = "Content/DataTables";
const string assetFileName = "AllSongsTable.uasset";
const string assetSrcPath = $"./unPatched/{assetFileName}";
var supportedSongExtensions = new[] { "ogg", "mp3" };
var songAssetsExtensions = new[]
{
    "txt",
    // "bsaa"
};

void IgnoreException(Action action)
{
    try
    {
        action.Invoke();
    }
    catch
    {
        // ignored
    }
}

void ExecuteAndPrint(string path, string args, string workingDirectory)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = path,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        }
    };
    process.Start();
    while (!process.StandardOutput.EndOfStream)
    {
        Console.WriteLine(process.StandardOutput.ReadLine());
    }
}

UAsset? assetInstance = null;
IgnoreException(() => assetInstance = new UAsset(assetSrcPath, UE4Version.VER_UE4_27));
if (assetInstance == null)
{
    Console.WriteLine($"can not found \"{assetSrcPath}\". program will exit.");
    return;
}

IgnoreException(() => Directory.Delete(runtimePath, true));
IgnoreException(() => Directory.Delete(outputPath, true));
Directory.CreateDirectory(outputPath);
Directory.CreateDirectory($"{assetRuntimePath}/{assetFolder}");
Directory.CreateDirectory($"{assetRuntimePath}/{contentFolder}");

var tableData = (assetInstance.Exports[0] as DataTableExport)?.Table.Data;

//build asset
Console.WriteLine("scanning songs...");
var songs = Directory.GetFiles("./songs", "*.json");
foreach (var songConfigFilePath in songs)
{
    var songFileName = Path.GetFileNameWithoutExtension(songConfigFilePath);
    var songFileFullName = (
        from extension in supportedSongExtensions
        where File.Exists($"./songs/{songFileName}.{extension}")
        select $"{songFileName}.{extension}"
    ).FirstOrDefault();
    if (songFileFullName == null)
    {
        Console.WriteLine($"song file for \"{songFileName}\" not exists. skipped.");
        continue;
    }

    JsonNode? songConfig = null;
    IgnoreException(() => songConfig = JsonNode.Parse(File.ReadAllText(songConfigFilePath)));

    if (songConfig == null)
    {
        Console.WriteLine($"song config for \"{songFileName}\" is not valid. skipped.");
        continue;
    }

    //copy files
    File.Copy($"./songs/{songFileFullName}", $"{assetRuntimePath}/{contentFolder}/{songFileFullName}");
    foreach (var extension in songAssetsExtensions)
    {
        if (File.Exists($"./songs/{songFileName}.{extension}"))
            File.Copy($"./songs/{songFileName}.{extension}", $"{assetRuntimePath}/{contentFolder}/{songFileName}.{extension}");
    }

    //add item to table
    var songPathText = $"{contentFolder}/{songFileFullName}";
    assetInstance.AddNameReference(new FString(songPathText, Encoding.Unicode));
    tableData?.Add(
        new StructPropertyData
        {
            Name = new FName(songPathText),
            StructType = fNameBeatShooterSongInfo,
            Value = new List<PropertyData>()
            {
                new BytePropertyData
                {
                    Name = fNameSongInfoProgress,
                    EnumType = fNameEBeatShooterSongInfoProgress,
                    EnumValue = fNameEBeatShooterSongInfoProgressPostAudioAnalysis,
                    ByteType = BytePropertyType.FName
                },
                new StructPropertyData
                {
                    Name = fNameSongFilePath,
                    StructType = fNameFilePath,
                    Value = new List<PropertyData>
                    {
                        new StrPropertyData
                        {
                            Name = fNameFilePath,
                            Value = new FString(songPathText, Encoding.Unicode)
                        }
                    }
                },
                new StrPropertyData
                {
                    Name = fNameSongName,
                    Value = new FString(songConfig["SongName"]?.ToString() ?? "", Encoding.Unicode)
                },
                new StrPropertyData
                {
                    Name = fNameArtistName,
                    Value = new FString(songConfig["ArtistName"]?.ToString() ?? "", Encoding.Unicode)
                },
                new StrPropertyData
                {
                    Name = fNameAlbumName,
                    Value = new FString(songConfig["AlbumName"]?.ToString() ?? "", Encoding.Unicode)
                },
                new StrPropertyData
                {
                    Name = fNameCreditsLine,
                    Value = new FString("", Encoding.Unicode)
                },
                new ObjectPropertyData
                {
                    Name = fNameAlbumCover,
                    Value = FPackageIndex.FromRawIndex(0)
                },
                new IntPropertyData
                {
                    Name = fNameSongDuration,
                    Value = Convert.ToInt32(songConfig["SongDuration"]?.ToString())
                },
                new StructPropertyData
                {
                    Name = fNameOverrideLevelFilePath,
                    StructType = fNameFilePath,
                    Value = new List<PropertyData>
                    {
                        new StrPropertyData
                        {
                            Name = fNameFilePath,
                            Value = new FString(null, Encoding.Unicode)
                        }
                    }
                },
                new BoolPropertyData
                {
                    Name = fNameBAlwaysHidden,
                    Value = false
                },
                new BoolPropertyData
                {
                    Name = fNameBHiddenUntilCompleted,
                    Value = false
                },
                new IntPropertyData
                {
                    Name = fNameBeatsPerMinute,
                    Value = Convert.ToInt32(songConfig["BeatsPerMinute"]?.ToString())
                },
                new BytePropertyData
                {
                    Name = fNameGenre,
                    EnumType = fNameEBeatShooterMusicGenre,
                    EnumValue = new FName($"{fNameEBeatShooterMusicGenreValuePrefix}{(string.IsNullOrEmpty(songConfig["Genre"]?.ToString()) ? "Pop" : songConfig["Genre"]?.ToString())}"),
                    ByteType = BytePropertyType.FName
                },
                new BytePropertyData
                {
                    Name = fNameFamily,
                    EnumType = fNameEBeatShooterMusicFamily,
                    EnumValue = new FName($"{fNameEBeatShooterMusicFamilyValuePrefix}{(string.IsNullOrEmpty(songConfig["Family"]?.ToString()) ? "Digital" : songConfig["Family"]?.ToString())}"),
                    ByteType = BytePropertyType.FName
                },
                new BytePropertyData
                {
                    Name = fNameEnvironmentType,
                    EnumType = fNameEBeatShooterEnvironmentType,
                    EnumValue = new FName($"{fNameEBeatShooterEnvironmentTypeValuePrefix}{(string.IsNullOrEmpty(songConfig["Environment"]?.ToString()) ? "Urban" : songConfig["Environment"]?.ToString())}"),
                    ByteType = BytePropertyType.FName
                },
                new FloatPropertyData
                {
                    Name = fNameLoudness,
                    Value = Convert.ToSingle(songConfig["Loudness"]?.ToString())
                },
                new StructPropertyData
                {
                    Name = fNameAudioAnalysisFilename,
                    StructType = fNameFilePath,
                    Value = new List<PropertyData>
                    {
                        new StrPropertyData
                        {
                            Name = fNameFilePath,
                            Value = new FString(
                                // File.Exists($"./songs/{songFileName}.bsaa") ? $"{contentFolder}/{songFileName}.bsaa" : null,
                                $"AudioAnalysisCache/{songFileName}.bsaa",
                                Encoding.Unicode
                            )
                        }
                    }
                }
            }
        }
    );
}

Console.WriteLine("building assets...");
assetInstance.Write($"{assetRuntimePath}/{assetFolder}/{assetFileName}");

//pack pak
if (!File.Exists(packToolPath))
{
    Console.WriteLine("u4pak not exists. auto packing skipped.");
    Console.WriteLine($"but you still can manually pack {assetRuntimePath} folder your self.");
    return;
}

Console.WriteLine("start packing...");
var runtimePackToolPath = $"{runtimePath}/{Path.GetFileName(packToolPath)}";
File.Copy(packToolPath, runtimePackToolPath);
ExecuteAndPrint(runtimePackToolPath, $"pack ../{outputFilePath} {gameName}", runtimePath);
ExecuteAndPrint(runtimePackToolPath, $"info ../{outputFilePath}", runtimePath);
ExecuteAndPrint(runtimePackToolPath, $"test ../{outputFilePath}", runtimePath);