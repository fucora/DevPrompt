﻿using DevPrompt.Plugins;
using DevPrompt.Settings;
using DevPrompt.UI.Controls;
using DevPrompt.UI.ViewModels;
using DevPrompt.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DevPrompt.UI
{
    internal partial class MainWindow : Window, DragItemsControl.IDragHost
    {
        public MainWindowVM ViewModel { get; }
        public AppSettings AppSettings { get; }
        private bool systemShuttingDown;

        public MainWindow(AppSettings settings, string initialErrorText)
        {
            this.AppSettings = settings;
            this.ViewModel = new MainWindowVM(this)
            {
                ErrorText = initialErrorText
            };

            this.InitializeComponent();
        }

        public ProcessHostWindow ProcessHostWindow
        {
            get
            {
                return this.processHostHolder?.Child as ProcessHostWindow;
            }
        }

        public UIElement ViewElement
        {
            get
            {
                return this.viewElementHolder.Child;
            }

            set
            {
                this.viewElementHolder.Child = value;

                if (value == null)
                {
                    this.processHostHolder.Visibility = Visibility.Visible;
                    this.viewElementHolder.Visibility = Visibility.Collapsed;

                    this.ProcessHostWindow?.Show();
                }
                else
                {
                    this.ProcessHostWindow?.Hide();

                    this.processHostHolder.Visibility = Visibility.Collapsed;
                    this.viewElementHolder.Visibility = Visibility.Visible;
                }
            }
        }

        private void OnFileMenuOpened(object sender, RoutedEventArgs args)
        {
            MainWindow.UpdateMenu((MenuItem)sender, this.AppSettings.Consoles, (ConsoleSettings settings) =>
            {
                return new MenuItem()
                {
                    Header = settings.MenuName,
                    Command = this.ViewModel.ConsoleCommand,
                    CommandParameter = settings,
                };
            });
        }

        private void OnGrabMenuOpened(object sender, RoutedEventArgs args)
        {
            List<string> names = new List<string>(CommandHelpers.GetGrabProcesses());

            MainWindow.UpdateMenu((MenuItem)sender, names, (string name) =>
            {
                int id = 0;
                if (name.StartsWith("[", StringComparison.Ordinal))
                {
                    int end = name.IndexOf(']', 1);
                    if (end != -1 && int.TryParse(name.Substring(1, end - 1), out int tempId))
                    {
                        id = tempId;
                    }
                }

                return new MenuItem()
                {
                    Header = name,
                    Command = this.ViewModel.GrabConsoleCommand,
                    CommandParameter = id
                };
            });
        }

        private async void OnVsMenuOpened(object sender, RoutedEventArgs args)
        {
            IEnumerable<VisualStudioSetup.Instance> instances = await VisualStudioSetup.GetInstances();

            MainWindow.UpdateMenu((MenuItem)sender, instances.ToArray(), (VisualStudioSetup.Instance instance) =>
            {
                return new MenuItem()
                {
                    Header = instance.DisplayName,
                    Command = this.ViewModel.VisualStudioCommand,
                    CommandParameter = instance,
                };
            });
        }

        private void OnToolsMenuOpened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            MainWindow.UpdateMenu(menu, this.AppSettings.Tools, (ToolSettings settings) =>
            {
                if (string.IsNullOrEmpty(settings.Command))
                {
                    return new Separator();
                }

                return new MenuItem()
                {
                    Header = settings.Name,
                    Command = this.ViewModel.ToolCommand,
                    CommandParameter = settings,
                };
            });

            this.AddPluginMenuItems(menu, MenuType.Tools);
        }

        private void OnLinksMenuOpened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            MainWindow.UpdateMenu(menu, this.AppSettings.Links, (LinkSettings settings) =>
            {
                if (string.IsNullOrEmpty(settings.Address))
                {
                    return new Separator();
                }

                return new MenuItem()
                {
                    Header = settings.Name,
                    Command = this.ViewModel.LinkCommand,
                    CommandParameter = settings,
                };
            });

            this.AddPluginMenuItems(menu, MenuType.Links);
        }

        /// <summary>
        /// Replaces MenuItems up until the first separator with dynamic MenuItems.
        /// The dynamic MenuItems are generated by the createMenuItem Func
        /// </summary>
        private static void UpdateMenu<T>(MenuItem menu, IList<T> dynamicItems, Func<T, Control> createMenuItem) where T : class
        {
            for (int i = 0; i < dynamicItems.Count; i++)
            {
                FrameworkElement item = (FrameworkElement)menu.Items[i];

                if (item.Tag is string str && str == "[End]")
                {
                    // Reached the end separator
                    menu.Items.Insert(i, createMenuItem(dynamicItems[i]));
                }
                else if (!object.Equals(item.Tag, dynamicItems[i]))
                {
                    FrameworkElement elem = createMenuItem(dynamicItems[i]);
                    elem.Tag = dynamicItems[i];
                    menu.Items[i] = elem;
                }
            }

            // Delete old extra items
            for (int i = dynamicItems.Count; i < menu.Items.Count; i++)
            {
                FrameworkElement item = (FrameworkElement)menu.Items[i];

                if (item.Tag is string str && str == "[End]")
                {
                    item.Visibility = (dynamicItems.Count > 0) ? Visibility.Visible : Visibility.Collapsed;
                    break;
                }
                else
                {
                    menu.Items.RemoveAt(i--);
                }
            }
        }

        private void AddPluginMenuItems(MenuItem menu, MenuType menuType)
        {
            Separator separator = menu.Items.OfType<Separator>().Where(s => s.Tag is string name && name == "[Plugins]").FirstOrDefault();
            if (separator != null)
            {
                separator.Tag = null;
                int index = menu.Items.IndexOf(separator);

                foreach (IMenuItemProvider provider in App.Current.GetExports<IMenuItemProvider>())
                {
                    foreach (MenuItem item in provider.GetMenuItems(menuType, this.ViewModel))
                    {
                        if (item != null)
                        {
                            menu.Items.Insert(index, item);
                        }
                    }
                }

                if (menu.Items.IndexOf(separator) == index)
                {
                    // No plugins added anything
                    separator.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void OnActivated(object sender, EventArgs args)
        {
            App.Current.NativeApp?.Activate();
            this.ProcessHostWindow?.OnActivated();
        }

        private void OnDeactivated(object sender, EventArgs args)
        {
            App.Current.NativeApp?.Deactivate();
            this.ProcessHostWindow?.OnDeactivated();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            App.Current.NativeApp?.MainWindowProc(hwnd, msg, wParam, lParam);
            return IntPtr.Zero;
        }

        public void OnAppInitComplete()
        {
            if (App.Current.NativeApp != null)
            {
                this.processHostHolder.Child = new ProcessHostWindow();

                Action action = () => this.ViewModel.RunStartupConsoles();
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, action);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.AddHook(this.WindowProc);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                source.RemoveHook(this.WindowProc);
            }
        }

        private async void OnClosing(object sender, CancelEventArgs args)
        {
            if (!this.systemShuttingDown)
            {
                AppSnapshot snapshot = new AppSnapshot(this.ViewModel);
                await snapshot.Save();
            }
        }

        /// <summary>
        /// Last chance to save snapshot before restart
        /// </summary>
        public void OnSystemShutdown()
        {
            if (!this.systemShuttingDown)
            {
                this.systemShuttingDown = true;
                AppSnapshot snapshot = new AppSnapshot(this.ViewModel, force: true);
                snapshot.Save().Wait();
            }
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs args)
        {
            base.OnGotKeyboardFocus(args);

            if (args.NewFocus == this)
            {
                this.ViewModel.FocusActiveTab();
            }
        }

        public void OnAltLetter(int vk)
        {
            CommandHelpers.ShowMenuFromAltLetter(this.MainMenu, vk);
        }

        public void OnAlt()
        {
            if (!this.MainMenu.IsKeyboardFocusWithin)
            {
                CommandHelpers.FocusFirstMenuItem(this.MainMenu);
            }
        }

        private void OnTabButtonMouseDown(object sender, MouseButtonEventArgs args)
        {
            if (args.ChangedButton == MouseButton.Middle &&
                sender is Button button &&
                button.DataContext is ITabVM tab)
            {
                tab.CloseCommand?.Execute(null);
            }
        }

        private void OnTabButtonMouseMoveEvent(object sender, MouseEventArgs args)
        {
            if (sender is Button button)
            {
                this.tabItemsControl.NotifyMouseMove(button, args);
            }
        }

        private void OnTabButtonMouseCaptureEvent(object sender, MouseEventArgs args)
        {
            if (sender is Button button)
            {
                this.tabItemsControl.NotifyMouseCapture(button, args);
            }
        }

        private void OnTabContextMenuOpened(object sender, RoutedEventArgs args)
        {
            if (sender is ContextMenu menu)
            {
                CommandHelpers.FocusFirstMenuItem(menu);
            }
        }

        void DragItemsControl.IDragHost.OnDrop(ItemsControl source, object droppedModel, int droppedIndex, bool copy)
        {
            if (source == this.tabItemsControl && droppedModel is ITabVM tab)
            {
                this.ViewModel.OnDrop(tab, droppedIndex, copy);
            }
        }

        /// <summary>
        /// Allows cloning of a process
        /// </summary>
        bool DragItemsControl.IDragHost.CanDropCopy(object droppedModel)
        {
            return droppedModel is ITabVM tab && tab.CloneCommand?.CanExecute(null) == true;
        }
    }
}
