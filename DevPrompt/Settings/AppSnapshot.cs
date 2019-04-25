﻿using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace DevPrompt.Settings
{
    /// <summary>
    /// Saves the state of the app during shutdown so it can be restored on startup
    /// </summary>
    [DataContract]
    public class AppSnapshot : PropertyNotifier, ICloneable
    {
        private ObservableCollection<ITabSnapshot> tabs;
        private static readonly object fileLock = new object();

        public AppSnapshot()
            : this((MainWindowVM)null)
        {
        }

        internal AppSnapshot(MainWindowVM window, bool force = false)
        {
            this.Initialize();

            if (window != null && (force || window.AppSettings.SaveTabsOnExit))
            {
                foreach (ITabVM tab in window.Tabs)
                {
                    if (tab is ProcessVM process)
                    {
                        this.tabs.Add(new ConsoleSnapshot(process));
                    }
                }
            }
        }

        public AppSnapshot(AppSnapshot copyFrom)
        {
            this.Initialize();
            this.CopyFrom(copyFrom);
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public AppSnapshot Clone()
        {
            return new AppSnapshot(this);
        }

        public void CopyFrom(AppSnapshot copyFrom)
        {
            this.tabs.Clear();

            if (copyFrom != null)
            {
                foreach (ITabSnapshot tab in copyFrom.Tabs)
                {
                    this.tabs.Add(tab.Clone());
                }
            }
        }

        [DataMember]
        public IList<ITabSnapshot> Tabs
        {
            get
            {
                return this.tabs;
            }
        }

        [OnDeserializing]
        private void Initialize(StreamingContext context = default(StreamingContext))
        {
            this.tabs = new ObservableCollection<ITabSnapshot>();
        }

        private static DataContractSerializer DataContractSerializer
        {
            get
            {
                return new DataContractSerializer(typeof(AppSnapshot), AppSnapshot.CollectionTypes);
            }
        }

        private static IEnumerable<Type> CollectionTypes
        {
            get
            {
                yield return typeof(ConsoleSnapshot);
            }
        }

        public static string DefaultPath
        {
            get
            {
                return Path.Combine(AppSettings.AppDataPath, "Snapshot.xml");
            }
        }

        public static async Task<AppSnapshot> Load(string path)
        {
            AppSnapshot snapshot = null;

            try
            {
                snapshot = await Task.Run(() =>
                {
                    XmlReaderSettings xmlSettings = new XmlReaderSettings()
                    {
                        XmlResolver = null
                    };

                    lock (AppSnapshot.fileLock)
                    {
                        using (Stream stream = File.OpenRead(path))
                        using (XmlReader reader = XmlReader.Create(stream, xmlSettings))
                        {
                            return (AppSnapshot)AppSnapshot.DataContractSerializer.ReadObject(reader);
                        }
                    }
                });
            }
            catch
            {
            }

            return snapshot ?? new AppSnapshot();
        }

        public Task Save()
        {
            return this.Save(AppSnapshot.DefaultPath);
        }

        public Task Save(string path)
        {
            AppSnapshot clone = this.Clone();

            return Task.Run(() =>
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                lock (AppSnapshot.fileLock)
                {
                    using (Stream stream = File.Create(path))
                    using (XmlWriter writer = XmlWriter.Create(stream, xmlSettings))
                    {
                        AppSnapshot.DataContractSerializer.WriteObject(writer, clone);
                    }
                }
            });
        }
    }
}
