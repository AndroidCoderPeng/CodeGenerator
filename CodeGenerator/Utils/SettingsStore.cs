using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodeGenerator.Utils;

public class SettingsStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CodeGenerator"
    );

    private static readonly object Lock = new();

    private static string GetFilePath(string fileName) => Path.Combine(Dir, fileName);
    
    // 读取整个文件根对象，文件不存在或损坏时返回空对象
    private static JObject ReadRoot(string file)
    {
        if (!File.Exists(file)) return new JObject();
        try
        {
            return JObject.Parse(File.ReadAllText(file));
        }
        catch (Exception e)
        {
            Console.WriteLine($@"[SettingsStore] 读取失败：{e.Message}");
            return new JObject();
        }
    }

    // 按分组加载
    public static T Load<T>(string fileName, string section) where T : class, new()
    {
        var node = ReadRoot(GetFilePath(fileName))[section];
        if (node == null) return new T();
        
        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace
        });
        return node.ToObject<T>(serializer) ?? new T();
    }

    // 按分组保存：只更新该 section，其他 section 原样保留
    public static void Save<T>(string fileName, string section, T value) where T : class
    {
        var file = GetFilePath(fileName);
        lock (Lock)
        {
            var root = ReadRoot(file);
            root[section] = JToken.FromObject(value);
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(file, root.ToString(Formatting.Indented));
            }
            catch (Exception e)
            {
                Console.WriteLine($@"[SettingsStore] 保存失败：{e.Message}");
            }
        }
    }
}