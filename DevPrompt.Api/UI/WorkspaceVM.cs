﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevPrompt.Api
{
    /// <summary>
    /// View model to wrap IWorkspace model
    /// </summary>
    public class WorkspaceVM : PropertyNotifier, IWorkspaceVM
    {
        public string Title => this.Workspace?.Title ?? string.Empty;
        public bool CreatedWorkspace => this.workspace != null;
        public UIElement ViewElement => this.Workspace?.ViewElement;
        public IEnumerable<MenuItem> MenuItems => this.workspace?.MenuItems ?? Enumerable.Empty<MenuItem>();

        private IWindow window;
        private IWorkspace workspace;
        private IWorkspaceProvider provider;
        private IWorkspaceSnapshot snapshot;
        private ActiveState activeState;

        private WorkspaceVM(IWindow window)
        {
            this.window = window;
        }

        public WorkspaceVM(IWindow window, IWorkspace workspace)
            : this(window)
        {
            this.InitWorkspace(workspace);
        }

        public WorkspaceVM(IWindow window, IWorkspaceProvider provider, IWorkspaceSnapshot snapshot = null)
            : this(window)
        {
            this.provider = provider;
            this.snapshot = snapshot;
        }

        public void Dispose()
        {
            if (this.workspace != null)
            {
                this.workspace.PropertyChanged -= this.OnWorkspacePropertyChanged;
                (this.workspace as IDisposable)?.Dispose();
            }
        }

        public ICommand ActivateCommand => new DelegateCommand((object arg) =>
        {
            if (this.Workspace != null)
            {
                this.window.ActiveWorkspace = this;
            }
        });

        public Guid Id
        {
            get
            {
                if (this.workspace != null)
                {
                    return this.workspace.Id;
                }

                if (this.provider != null)
                {
                    return this.provider.WorkspaceId;
                }

                return Guid.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (this.workspace != null)
                {
                    return this.workspace.Name;
                }

                if (this.provider != null)
                {
                    return this.provider.WorkspaceName;
                }

                return string.Empty;
            }
        }

        public string Tooltip
        {
            get
            {
                if (this.workspace != null)
                {
                    return this.workspace.Tooltip;
                }

                if (this.provider != null)
                {
                    return this.provider.WorkspaceTooltip;
                }

                return string.Empty;
            }
        }

        public IWorkspaceSnapshot Snapshot
        {
            get
            {
                return this.workspace?.Snapshot ?? this.snapshot;
            }
        }

        public IWorkspace Workspace
        {
            get
            {
                if (this.workspace == null && this.provider != null)
                {
                    IWorkspaceProvider provider = this.provider;
                    this.provider = null;

                    if (this.snapshot != null)
                    {
                        IWorkspaceSnapshot snapshot = this.snapshot;
                        this.snapshot = null;

                        this.InitWorkspace(snapshot.Restore(this.window));
                    }
                    else
                    {
                        this.InitWorkspace(provider.CreateWorkspace(this.window));
                    }

                    this.OnPropertyChanged(nameof(this.Workspace));
                    this.OnPropertyChanged(nameof(this.Id));
                    this.OnPropertyChanged(nameof(this.Name));
                    this.OnPropertyChanged(nameof(this.Tooltip));
                    this.OnPropertyChanged(nameof(this.MenuItems));
                }

                return this.workspace;
            }
        }

        private void InitWorkspace(IWorkspace workspace)
        {
            this.workspace = workspace;

            if (this.workspace != null)
            {
                this.workspace.PropertyChanged += this.OnWorkspacePropertyChanged;
            }
        }

        private void OnWorkspacePropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspace.Name))
            {
                this.OnPropertyChanged(nameof(this.Name));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspace.Tooltip))
            {
                this.OnPropertyChanged(nameof(this.Tooltip));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspace.Title))
            {
                this.OnPropertyChanged(nameof(this.Title));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspace.ViewElement))
            {
                this.OnPropertyChanged(nameof(this.ViewElement));
            }

            if (string.IsNullOrEmpty(args.PropertyName) || args.PropertyName == nameof(IWorkspace.MenuItems))
            {
                this.OnPropertyChanged(nameof(this.MenuItems));
            }
        }

        public ActiveState ActiveState
        {
            get
            {
                return this.activeState;
            }

            set
            {
                if (this.SetPropertyValue(ref this.activeState, value))
                {
                    if (this.activeState == ActiveState.Hidden)
                    {
                        this.Workspace.OnHiding();
                    }
                    else
                    {
                        this.Workspace.OnShowing();
                        this.Workspace.Focus();
                    }
                }
            }
        }
    }
}
