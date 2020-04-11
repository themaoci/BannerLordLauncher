﻿using System;
using System.IO;
using System.Reactive.Linq;
using System.Windows.Input;
using BannerLord.Common;
using BannerLordLauncher.Views;
using ReactiveUI;
using Steam.Common;
using Ookii.Dialogs.Wpf;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GongSolutions.Wpf.DragDrop;

namespace BannerLordLauncher.ViewModels
{
    using System.Linq;

    public sealed class MainWindowViewModel : ViewModelBase, IDropTarget
    {
        public ModManager Manager { get; }
        private readonly MainWindow _window;
        private int _selectedIndex = -1;

        public int SelectedIndex
        {
            get => this._selectedIndex;
            set => this.RaiseAndSetIfChanged(ref this._selectedIndex, value);
        }

        public MainWindowViewModel(MainWindow window)
        {
            this._window = window;
            this.Manager = new ModManager();

            var moveUp = this.WhenAnyValue(x => x.SelectedIndex).Select(x => x > 0);
            var moveDown = this.WhenAnyValue(x => x.SelectedIndex).Select(x => x >= 0 && x < this.Manager.Mods.Count - 1);

            this.Save = ReactiveCommand.Create(this.SaveDialog);
            this.AlphaSort = ReactiveCommand.Create(() => this.Manager.AlphaSort());
            this.ReverseOrder = ReactiveCommand.Create(() => this.Manager.ReverseOrder());
            this.ExperimentalSort = ReactiveCommand.Create(() => this.Manager.TopologicalSort());
            this.MoveToTop = ReactiveCommand.Create(() => this.Manager.MoveToTop(this.SelectedIndex), moveUp.Select(x => x));
            this.MoveUp = ReactiveCommand.Create(() => this.Manager.MoveUp(this.SelectedIndex), moveUp.Select(x => x));
            this.MoveDown = ReactiveCommand.Create(() => this.Manager.MoveDown(this.SelectedIndex), moveDown.Select(x => x));
            this.MoveToBottom = ReactiveCommand.Create(() => this.Manager.MoveToBottom(this.SelectedIndex), moveDown.Select(x => x));
            this.CheckAll = ReactiveCommand.Create(() => this.Manager.CheckAll());
            this.UncheckAll = ReactiveCommand.Create(() => this.Manager.UncheckAll());
            this.InvertCheck = ReactiveCommand.Create(() => this.Manager.InvertCheck());
            this.Run = ReactiveCommand.Create(() => this.Manager.RunGame());
            this.Config = ReactiveCommand.Create(() => this.Manager.OpenConfig());
        }

        public void Initialize()
        {
            var game = string.Empty;

            if (!string.IsNullOrEmpty(this._window.Configuration.GamePath) &&
                Directory.Exists(this._window.Configuration.GamePath))
            {
                game = this._window.Configuration.GamePath;
            }
            else
            {
                var steamFinder = new SteamFinder();
                if (steamFinder.FindSteam())
                {
                    game = steamFinder.FindGameFolder(261550);
                    if (string.IsNullOrEmpty(game) || !Directory.Exists(game))
                    {
                        game = null;
                    }
                }

                if (string.IsNullOrEmpty(game))
                {
                    game = this.FindGameFolder();
                }

                this._window.Configuration.GamePath = game;
            }

            this.Manager.Initialize(game);
        }

        private string FindGameFolder()
        {
            while (true)
            {
                var dialog = new VistaFolderBrowserDialog
                {
                    Description = "Select game root folder",
                    UseDescriptionForTitle = true
                };
                var result = dialog.ShowDialog(this._window);
                if (result is null) Environment.Exit(0);
                if (!Directory.Exists(dialog.SelectedPath) || !File.Exists(Path.Combine(dialog.SelectedPath, "bin", "Win64_Shipping_Client", "Bannerlord.exe"))) continue;
                return dialog.SelectedPath;
            }
        }

        public void SaveDialog()
        {
            if (this.Manager.Mods.Any(x => x.HasConflicts))
            {
                if (MessageBox.Show(
                        this._window,
                        "Your mod list has existing conflicts, are you sure that you want to save it?",
                        "Warning",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }
            this.Manager.Save();
        }

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public ICommand Config { get; }
        public ICommand Run { get; }
        public ICommand Save { get; }
        public ICommand AlphaSort { get; }
        public ICommand ReverseOrder { get; }
        public ICommand ExperimentalSort { get; }
        public ICommand MoveToTop { get; }
        public ICommand MoveUp { get; }
        public ICommand MoveDown { get; }
        public ICommand MoveToBottom { get; }
        public ICommand CheckAll { get; }
        public ICommand UncheckAll { get; }
        public ICommand InvertCheck { get; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore MemberCanBePrivate.Global

        #region Implementation of IDropTarget

        public void DragOver(IDropInfo dropInfo)
        {
            var sourceItem = dropInfo.Data as ModEntry;
            var targetItem = dropInfo.TargetItem as ModEntry;

            if (sourceItem != null && targetItem != null)
            {
                dropInfo.Effects = DragDropEffects.Move;
                dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            }
        }

        public void Drop(IDropInfo dropInfo)
        {
            if (dropInfo?.DragInfo == null)
            {
                return;
            }

            var insertIndex = dropInfo.UnfilteredInsertIndex;
            if (dropInfo.VisualTarget is ItemsControl itemsControl)
            {
                if (itemsControl.Items is IEditableCollectionView editableItems)
                {
                    var newItemPlaceholderPosition = editableItems.NewItemPlaceholderPosition;
                    if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning && insertIndex == 0)
                    {
                        ++insertIndex;
                    }
                    else if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd && insertIndex == itemsControl.Items.Count)
                    {
                        --insertIndex;
                    }
                }
            }
            var sourceItem = dropInfo.Data as ModEntry;
            var index = this.Manager.Mods.IndexOf(sourceItem);
            if (index < insertIndex) insertIndex--;
            this.Manager.Mods.Remove(sourceItem);
            this.Manager.Mods.Insert(insertIndex, sourceItem);
            this.Manager.Validate();
        }

        #endregion
    }
}
