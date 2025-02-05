using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ReferenceImages.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        var length = 0;
        var sb = new System.Text.StringBuilder(0);
        var result = GetCurrentPackageFullName(ref length, sb);
        if (result == 15700L)
        {
            // Not a packaged app. Configure file-based persistence instead
            WinUIEx.WindowManager.PersistenceStorage = new FilePersistence("WinUIExPersistence.json");
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, System.Text.StringBuilder packageFullName);


    private partial class FilePersistence : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> data = new();
        private readonly string file;

        public FilePersistence(string filename)
        {
            file = filename;
            try
            {
                if (!File.Exists(filename)) return;
                if (JsonNode.Parse(File.ReadAllText(filename)) is not JsonObject jsonObject) return;
                foreach (var node in jsonObject)
                {
                    if (node.Value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value))
                        data[node.Key] = value;
                }
            }
            catch
            {
                // ignored
            }
        }

        private void Save()
        {
            var jo = new JsonObject();
            foreach (var item in data)
            {
                if (item.Value is string s) // In this case we only need string support. TODO: Support other types
                    jo.Add(item.Key, s);
            }

            File.WriteAllText(file, jo.ToJsonString());
        }

        public object this[string key]
        {
            get => data[key];
            set
            {
                data[key] = value;
                Save();
            }
        }

        public ICollection<string> Keys => data.Keys;

        public ICollection<object> Values => data.Values;

        public int Count => data.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            data.Add(key, value);
            Save();
        }

        public void Add(KeyValuePair<string, object> item)
        {
            data.Add(item.Key, item.Value);
            Save();
        }

        public void Clear()
        {
            data.Clear();
            Save();
        }

        public bool Contains(KeyValuePair<string, object> item) => data.Contains(item);

        public bool ContainsKey(string key) => data.ContainsKey(key);

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotImplementedException(); // TODO

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => throw new NotImplementedException(); // TODO

        public bool Remove(string key) => throw new NotImplementedException(); // TODO

        public bool Remove(KeyValuePair<string, object> item) => throw new NotImplementedException(); // TODO

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => throw new NotImplementedException(); // TODO

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException(); // TODO
    }
}