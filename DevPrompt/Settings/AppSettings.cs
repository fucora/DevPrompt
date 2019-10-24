﻿using DevPrompt.Plugins;
using DevPrompt.ProcessWorkspace.Utility;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves/loads application settings
    /// </summary>
    [DataContract]
    internal class AppSettings : PropertyNotifier, INotifyCollectionChanged, Api.IAppSettings
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public ObservableCollection<ConsoleSettings> ObservableConsoles { get; private set; }
        public ObservableCollection<GrabConsoleSettings> ObservableGrabConsoles { get; private set; }
        public ObservableCollection<LinkSettings> ObservableLinks { get; private set; }
        public ObservableCollection<ToolSettings> ObservableTools { get; private set; }
        public ObservableCollection<PluginDirectorySettings> ObservableUserPluginDirectories { get; private set; }
        public ObservableCollection<NuGetPluginSettings> ObservableNuGetPlugins { get; private set; }

        private Dictionary<string, object> customProperties;
        private bool consoleGrabEnabled;
        private bool saveTabsOnExit;
        private bool telemetryEnabled;
        private bool pluginsChanged;
        private static readonly object fileLock = new object();

        public AppSettings()
        {
            this.Initialize();
        }

        public AppSettings(AppSettings copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        public AppSettings Clone()
        {
            return new AppSettings(this);
        }

        public void CopyFrom(AppSettings copyFrom)
        {
            this.ConsoleGrabEnabled = copyFrom.ConsoleGrabEnabled;
            this.SaveTabsOnExit = copyFrom.SaveTabsOnExit;
            this.TelemetryEnabled = copyFrom.TelemetryEnabled;
            this.PluginsChanged = copyFrom.PluginsChanged;

            this.ObservableConsoles.Clear();
            this.ObservableGrabConsoles.Clear();
            this.ObservableLinks.Clear();
            this.ObservableTools.Clear();
            this.ObservableUserPluginDirectories.Clear();
            this.ObservableNuGetPlugins.Clear();
            this.customProperties.Clear();

            foreach (ConsoleSettings console in copyFrom.Consoles)
            {
                this.ObservableConsoles.Add(console.Clone());
            }

            foreach (GrabConsoleSettings console in copyFrom.GrabConsoles)
            {
                this.ObservableGrabConsoles.Add(console.Clone());
            }

            foreach (LinkSettings link in copyFrom.Links)
            {
                this.ObservableLinks.Add(link.Clone());
            }

            foreach (ToolSettings tool in copyFrom.Tools)
            {
                this.ObservableTools.Add(tool.Clone());
            }

            foreach (PluginDirectorySettings pluginDir in copyFrom.ObservableUserPluginDirectories)
            {
                this.ObservableUserPluginDirectories.Add(pluginDir.Clone());
            }

            foreach (NuGetPluginSettings nuget in copyFrom.ObservableNuGetPlugins)
            {
                this.ObservableNuGetPlugins.Add(nuget.Clone());
            }

            foreach (KeyValuePair<string, object> pair in copyFrom.CustomProperties)
            {
                this.customProperties[pair.Key] = (pair.Value is ICloneable cloneable)
                    ? cloneable.Clone()
                    : pair.Value;
            }

            this.EnsureValid();
        }

        public void CopyFrom(AppCustomSettings copyFrom)
        {
            foreach (KeyValuePair<string, object> pair in copyFrom.CustomProperties)
            {
                this.customProperties[pair.Key] = (pair.Value is ICloneable cloneable)
                    ? cloneable.Clone()
                    : pair.Value;
            }

            this.EnsureValid(DefaultSettingsFilter.Custom);
        }

        public static string AppDataPath
        {
            get
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(appDataPath, AppSettings.ExeName);
            }
        }

        private static string ExeName => Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        public static string DefaultPath => Path.Combine(AppSettings.AppDataPath, "Settings.xml");
        public static string DefaultCustomPath => Path.Combine(AppSettings.AppDataPath, "Settings.Custom.xml");
        public static string DefaultNuGetPath => Path.Combine(AppSettings.AppDataPath, "Plugins.NuGet");

        [Flags]
        public enum DefaultSettingsFilter
        {
            None = 0,
            DevPrompts = 0x01,
            RawPrompts = 0x02,
            Grabs = 0x04,
            Links = 0x08,
            Tools = 0x10,
            PluginDirs = 0x20,
            Custom = 0x40,
            All = 0xFF,
        }

        public static async Task<AppSettings> GetDefaultSettings(DefaultSettingsFilter filter)
        {
            AppSettings settings = new AppSettings();

            if ((filter & DefaultSettingsFilter.DevPrompts) != 0)
            {
                settings.AddVisualStudioEnlistments();
                await settings.AddVisualStudioDevPrompts();
            }

            if ((filter & DefaultSettingsFilter.RawPrompts) != 0)
            {
                settings.AddRawCommandPrompts();
            }

            if ((filter & DefaultSettingsFilter.Grabs) != 0)
            {
                settings.AddDefaultGrabs();
            }

            if ((filter & DefaultSettingsFilter.Links) != 0)
            {
                settings.AddDefaultLinks();
            }

            if ((filter & DefaultSettingsFilter.Tools) != 0)
            {
                settings.AddDefaultTools();
            }

            if (settings.ObservableConsoles.Count > 0 && !settings.ObservableConsoles.Any(c => c.RunAtStartup))
            {
                settings.ObservableConsoles[0].RunAtStartup = true;
            }

            settings.EnsureValid(filter);

            return settings;
        }

        private void AddVisualStudioEnlistments()
        {
            if (!Program.IsMicrosoftDomain)
            {
                return;
            }

            // Try to detect VS enlistments on all drives
            foreach (DriveInfo info in DriveInfo.GetDrives())
            {
                if (info.DriveType != DriveType.Fixed)
                {
                    continue;
                }

                for (int i = 0; i < 10; i++)
                {
                    string enlistment = Path.Combine(info.RootDirectory.FullName, "VS" + ((i == 0) ? string.Empty : i.ToString()));
                    if (Directory.Exists(enlistment) && File.Exists(Path.Combine(enlistment, "init.cmd")))
                    {
                        this.Consoles.Add(new ConsoleSettings()
                        {
                            MenuName = $"cmd.exe ({enlistment})",
                            TabName = $"%BaseDir,{enlistment.Substring(Path.GetPathRoot(enlistment).Length)}%, %_ParentBranch,...%",
                            StartingDirectory = enlistment,
                            Arguments = "/k init.cmd -skipexportsprune",
                        });
                    }
                }
            }
        }

        private async Task AddVisualStudioDevPrompts()
        {
            List<VisualStudioSetup.Instance> instances = new List<VisualStudioSetup.Instance>(await VisualStudioSetup.GetInstances());
            instances.Sort((x, y) =>
            {
                if (!Version.TryParse(x.Version, out Version v1))
                {
                    v1 = new Version(0, 0);
                }

                if (!Version.TryParse(y.Version, out Version v2))
                {
                    v2 = new Version(0, 0);
                }

                return v2.CompareTo(v1);
            });

            foreach (VisualStudioSetup.Instance instance in instances)
            {
                string file = Path.Combine(instance.Path, "Common7", "Tools", "VsDevCmd.bat");
                if (File.Exists(file))
                {
                    int dotIndex = instance.DisplayName.IndexOf('.');
                    string name = (dotIndex != -1)
                        ? instance.DisplayName.Substring(0, dotIndex)
                        : instance.DisplayName;

                    this.ObservableConsoles.Add(new ConsoleSettings()
                    {
                        MenuName = string.Format(CultureInfo.CurrentCulture, Resources.Menu_VsPromptName, name),
                        TabName = string.Format(CultureInfo.CurrentCulture, Resources.Menu_VsPromptTabName, name),
                        Arguments = $"/k \"{file}\"",
                    });
                }
            }
        }

        private void AddRawCommandPrompts()
        {
            this.ObservableConsoles.Add(new ConsoleSettings()
            {
                ConsoleType = ConsoleType.Cmd,
                MenuName = Resources.Menu_RawCommandName,
                TabName = Resources.Menu_RawCommandTabName,
            });

            this.ObservableConsoles.Add(new ConsoleSettings()
            {
                ConsoleType = ConsoleType.PowerShell,
                MenuName = Resources.Menu_RawPowerShellName,
                TabName = Resources.Menu_RawPowerShellTabName,
            });
        }

        private void AddDefaultGrabs()
        {
            this.GrabConsoles.Add(new GrabConsoleSettings()
            {
                ExeName = "cmd.exe",
                TabName = Resources.Menu_RawCommandTabName,
                TabActivate = true,
            });

            this.GrabConsoles.Add(new GrabConsoleSettings()
            {
                ExeName = "powershell.exe",
                TabName = Resources.Menu_RawPowerShellTabName,
                TabActivate = true,
            });
        }

        private void AddDefaultLinks()
        {
            foreach (string entry in Resources.Links_Default.Split('\r', '\n'))
            {
                int i = entry.IndexOf('=');
                if (i >= 0)
                {
                    this.Links.Add(new LinkSettings()
                    {
                        Name = entry.Substring(0, i),
                        Address = entry.Substring(i + 1),
                    });
                }
            }
        }

        private void AddDefaultTools()
        {
            foreach (string entry in Resources.Tools_Default.Split('\r', '\n'))
            {
                int i = entry.IndexOf('=');
                if (i >= 0)
                {
                    this.Tools.Add(new ToolSettings()
                    {
                        Name = entry.Substring(0, i),
                        Command = entry.Substring(i + 1),
                    });
                }
            }
        }

        public IEnumerable<PluginDirectorySettings> PluginDirectories
        {
            get
            {
                PluginDirectorySettings appPlugins = new PluginDirectorySettings()
                {
                    Directory = @".\Plugins",
                    ReadOnly = true,
                };

                PluginDirectorySettings userPlugins = new PluginDirectorySettings()
                {
                    Directory = $@"%LocalAppData%\{AppSettings.ExeName}\Plugins",
                    ReadOnly = true,
                };

                yield return appPlugins;

                if (!string.Equals(appPlugins.ExpandedDirectory, userPlugins.ExpandedDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    yield return userPlugins;
                }

                foreach (PluginDirectorySettings userDir in this.UserPluginDirectories)
                {
                    yield return userDir;
                }
            }
        }

        public static async Task<T> UnsafeLoad<T>(App app, string path)
        {
            return await Task.Run(() =>
            {
                XmlReaderSettings xmlSettings = new XmlReaderSettings()
                {
                    XmlResolver = null
                };

                lock (AppSettings.fileLock)
                {
                    using (Stream stream = File.OpenRead(path))
                    using (XmlReader reader = XmlReader.Create(stream, xmlSettings))
                    {
                        return (T)AppSettings.GetDataContractSerializer<T>(app).ReadObject(reader, verifyObjectName: false);
                    }
                }
            });
        }

        public static async Task<AppSettings> Load(App app, string path)
        {
            AppSettings settings = null;

            try
            {
                if (File.Exists(path))
                {
                    settings = await AppSettings.UnsafeLoad<AppSettings>(app, path);
                    settings.EnsureValid();
                }
            }
            catch
            {
            }

            if (settings == null)
            {
                settings = await AppSettings.GetDefaultSettings(DefaultSettingsFilter.All);
                await settings.Save(app, path);
            }

            return settings;
        }

        public static async Task<AppCustomSettings> LoadCustom(App app, string path)
        {
            AppCustomSettings settings = null;

            try
            {
                if (File.Exists(path))
                {
                    settings = await AppSettings.UnsafeLoad<AppCustomSettings>(app, path);
                }
            }
            catch
            {
            }

            return settings ?? new AppCustomSettings();
        }

        public Task<Exception> Save(App app, string path = null)
        {
            AppSettings clone = this.Clone();

            Task<Exception> task = Task.Run(() =>
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                lock (AppSettings.fileLock)
                {
                    try
                    {
                        string customPath = null;

                        if (string.IsNullOrEmpty(path))
                        {
                            path = AppSettings.DefaultPath;
                            customPath = AppSettings.DefaultCustomPath;
                        }

                        if (Directory.CreateDirectory(Path.GetDirectoryName(path)) != null)
                        {
                            using (Stream stream = File.Create(path))
                            using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                            {
                                AppSettings.GetDataContractSerializer<AppSettings>(app).WriteObject(writer, clone);
                            }

                            if (!string.IsNullOrEmpty(customPath))
                            {
                                using (Stream stream = File.Create(customPath))
                                using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                                {
                                    AppCustomSettings customSettings = new AppCustomSettings(clone);
                                    AppSettings.GetDataContractSerializer<AppCustomSettings>(app).WriteObject(writer, customSettings);
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        return exception;
                    }
                }

                return null;
            });

            app.AddCriticalTask(task);
            return task;
        }

        [DataMember]
        public IList<ConsoleSettings> Consoles => this.ObservableConsoles;
        IEnumerable<Api.IConsoleSettings> Api.IAppSettings.ConsoleSettings => this.Consoles;

        [DataMember]
        public IList<GrabConsoleSettings> GrabConsoles => this.ObservableGrabConsoles;

        [DataMember]
        public IList<LinkSettings> Links => this.ObservableLinks;

        [DataMember]
        public IList<ToolSettings> Tools => this.ObservableTools;

        [DataMember]
        public IList<PluginDirectorySettings> UserPluginDirectories => this.ObservableUserPluginDirectories;

        [DataMember]
        public IList<NuGetPluginSettings> NuGetPlugins => this.ObservableNuGetPlugins;

        [DataMember]
        public bool ConsoleGrabEnabled
        {
            get => this.consoleGrabEnabled;
            set => this.SetPropertyValue(ref this.consoleGrabEnabled, value);
        }

        [DataMember]
        public bool SaveTabsOnExit
        {
            get => this.saveTabsOnExit;
            set => this.SetPropertyValue(ref this.saveTabsOnExit, value);
        }

        [DataMember]
        public bool TelemetryEnabled
        {
            get => this.telemetryEnabled;
            set => this.SetPropertyValue(ref this.telemetryEnabled, value);
        }

        // Do not persist this
        public bool PluginsChanged
        {
            get => this.pluginsChanged;
            set => this.SetPropertyValue(ref this.pluginsChanged, value);
        }

        // Saved from AppCustomSettings, not from here
        public ICollection<KeyValuePair<string, object>> CustomProperties => this.customProperties;

        public bool TryGetProperty<T>(string name, out T value)
        {
            if (!string.IsNullOrEmpty(name) && this.customProperties.TryGetValue(name, out object objectValue) && objectValue is T)
            {
                value = (T)objectValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public void SetProperty<T>(string name, T value)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                this.customProperties[name] = value;
                this.OnPropertyChanged($"Custom.{name}");
            }
        }

        public bool RemoveProperty(string name)
        {
            if (!string.IsNullOrWhiteSpace(name) && this.customProperties.Remove(name))
            {
                this.OnPropertyChanged($"Custom.{name}");
                return true;
            }

            return false;
        }

        string Api.IAppSettings.GetDefaultTabName(string path)
        {
            foreach (GrabConsoleSettings grab in this.GrabConsoles)
            {
                if (grab.CanGrab(path))
                {
                    return grab.TabName;
                }
            }

            return !string.IsNullOrEmpty(path) ? Path.GetFileName(path) : "Tab";
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.ObservableConsoles = new ObservableCollection<ConsoleSettings>();
            this.ObservableGrabConsoles = new ObservableCollection<GrabConsoleSettings>();
            this.ObservableLinks = new ObservableCollection<LinkSettings>();
            this.ObservableTools = new ObservableCollection<ToolSettings>();
            this.ObservableUserPluginDirectories = new ObservableCollection<PluginDirectorySettings>();
            this.ObservableNuGetPlugins = new ObservableCollection<NuGetPluginSettings>();

            this.ObservableConsoles.CollectionChanged += this.OnObservableCollectionChanged;
            this.ObservableGrabConsoles.CollectionChanged += this.OnObservableCollectionChanged;
            this.ObservableLinks.CollectionChanged += this.OnObservableCollectionChanged;
            this.ObservableTools.CollectionChanged += this.OnObservableCollectionChanged;
            this.ObservableUserPluginDirectories.CollectionChanged += this.OnObservableCollectionChanged;
            this.ObservableNuGetPlugins.CollectionChanged += this.OnObservableCollectionChanged;

            this.customProperties = new Dictionary<string, object>();
            this.saveTabsOnExit = true;
            this.telemetryEnabled = true;
        }

        private void OnObservableCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            this.CollectionChanged?.Invoke(sender, args);
        }

        private static DataContractSerializer GetDataContractSerializer<T>(App app)
        {
            List<Type> knownTypes = new List<Type>(AppSettings.CollectionTypes);
            DataContractSerializerSettings serializerSettings = new DataContractSerializerSettings()
            {
                KnownTypes = knownTypes.Distinct(),
                DataContractResolver = new SettingTypeResolver(app),
            };

            return new DataContractSerializer(typeof(T), serializerSettings);
        }

        private static IEnumerable<Type> CollectionTypes
        {
            get
            {
                yield return typeof(ConsoleSettings);
                yield return typeof(GrabConsoleSettings);
                yield return typeof(InstalledPluginAssemblyInfo);
                yield return typeof(InstalledPluginInfo);
                yield return typeof(LinkSettings);
                yield return typeof(NuGetPluginSettings);
                yield return typeof(PluginDirectorySettings);
                yield return typeof(ToolSettings);
            }
        }

        public void EnsureValid(DefaultSettingsFilter filter = DefaultSettingsFilter.All)
        {
            if ((filter & (DefaultSettingsFilter.DevPrompts | DefaultSettingsFilter.RawPrompts)) != 0)
            {
                if (this.ObservableConsoles.Count == 0)
                {
                    // Have to have at least one console that can be created
                    this.ObservableConsoles.Add(new ConsoleSettings()
                    {
                        RunAtStartup = true
                    });
                }
            }
        }
    }
}
