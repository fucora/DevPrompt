﻿using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace DevPrompt.Settings
{
    /// <summary>
    /// A plugin that's downloaded from NuGet
    /// </summary>
    [DataContract]
    [DebuggerDisplay("{Id}")]
    internal class NuGetPluginSettings : Api.PropertyNotifier
    {
        private string id;
        private string title;
        private string description;
        private string summary;
        private string projectUrl;
        private string iconUrl;
        private string authors;
        private string latestVersion;
        private string latestVersionUrl;
        private string latestVersionPackageUrl;
        private DateTime latestVersionDate;
        private string installedVersion;

        public NuGetPluginSettings()
        {
            this.id = string.Empty;
            this.title = string.Empty;
            this.description = string.Empty;
            this.summary = string.Empty;
            this.projectUrl = string.Empty;
            this.iconUrl = string.Empty;
            this.authors = string.Empty;
            this.latestVersion = string.Empty;
            this.latestVersionUrl = string.Empty;
            this.latestVersionPackageUrl = string.Empty;
            this.latestVersionDate = DateTime.MinValue;
            this.installedVersion = string.Empty;
        }

        public NuGetPluginSettings(NuGetPluginSettings copyFrom)
        {
            this.CopyFrom(copyFrom);
        }

        public void CopyFrom(NuGetPluginSettings copyFrom)
        {
            this.Id = copyFrom.id;
            this.Title = copyFrom.title;
            this.Description = copyFrom.description;
            this.Summary = copyFrom.summary;
            this.ProjectUrl = copyFrom.projectUrl;
            this.IconUrl = copyFrom.iconUrl;
            this.Authors = copyFrom.authors;
            this.LatestVersion = copyFrom.latestVersion;
            this.LatestVersionUrl = copyFrom.latestVersionUrl;
            this.latestVersionPackageUrl = copyFrom.latestVersionPackageUrl;
            this.latestVersionDate = copyFrom.latestVersionDate;
            this.InstalledVersion = copyFrom.installedVersion;
        }

        public NuGetPluginSettings Clone()
        {
            return new NuGetPluginSettings(this);
        }

        [DataMember]
        public string Id
        {
            get => this.id;
            set
            {
                if (this.SetPropertyValue(ref this.id, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.InstalledRootPath));
                }
            }
        }

        [DataMember]
        public string Title
        {
            get => this.title;
            set => this.SetPropertyValue(ref this.title, value ?? string.Empty);
        }

        [DataMember]
        public string Description
        {
            get => this.description;
            set => this.SetPropertyValue(ref this.description, value ?? string.Empty);
        }

        [DataMember]
        public string Summary
        {
            get => this.summary;
            set => this.SetPropertyValue(ref this.summary, value ?? string.Empty);
        }

        [DataMember]
        public string ProjectUrl
        {
            get => this.projectUrl;
            set => this.SetPropertyValue(ref this.projectUrl, value ?? string.Empty);
        }

        [DataMember]
        public string IconUrl
        {
            get => this.iconUrl;
            set => this.SetPropertyValue(ref this.iconUrl, value ?? string.Empty);
        }

        [DataMember]
        public string Authors
        {
            get => this.authors;
            set => this.SetPropertyValue(ref this.authors, value ?? string.Empty);
        }

        [DataMember]
        public string LatestVersion
        {
            get => this.latestVersion;
            set => this.SetPropertyValue(ref this.latestVersion, value ?? string.Empty);
        }

        [DataMember]
        public string LatestVersionUrl
        {
            get => this.latestVersionUrl;
            set => this.SetPropertyValue(ref this.latestVersionUrl, value ?? string.Empty);
        }

        [DataMember]
        public string LatestVersionPackageUrl
        {
            get => this.latestVersionPackageUrl;
            set => this.SetPropertyValue(ref this.latestVersionPackageUrl, value ?? string.Empty);
        }

        [DataMember]
        public DateTime LatestVersionDate
        {
            get => this.latestVersionDate;
            set => this.SetPropertyValue(ref this.latestVersionDate, value);
        }

        [DataMember]
        public string InstalledVersion
        {
            get => this.installedVersion;
            set
            {
                if (this.SetPropertyValue(ref this.installedVersion, value ?? string.Empty))
                {
                    this.OnPropertyChanged(nameof(this.IsInstalled));
                    this.OnPropertyChanged(nameof(this.InstalledVersionPath));
                }
            }
        }

        public bool IsInstalled => !string.IsNullOrEmpty(this.InstalledVersionPath);
        public string InstalledRootPath => Path.Combine(AppSettings.DefaultNuGetPath, this.Id);
        public string InstalledVersionPath => this.GetInstallPath(this.InstalledVersion);

        public string GetInstallPath(string version)
        {
            if (!string.IsNullOrEmpty(version) && !string.IsNullOrEmpty(this.id))
            {
                return Path.Combine(this.InstalledRootPath, version);
            }

            return string.Empty;
        }
    }
}
