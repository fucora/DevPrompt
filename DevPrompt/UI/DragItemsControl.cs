﻿using DevPrompt.Utility;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DevPrompt.UI
{
    /// <summary>
    /// ItemsControl that allows drag/drop of items
    /// </summary>
    internal class DragItemsControl : ItemsControl
    {
        public interface IDragHost
        {
            void OnDrop(ItemsControl source, object droppedModel, int droppedIndex, bool copy);
            bool CanDropCopy(object droppedModel);
        }

        private bool dragging;
        private Point? mouseCapturePoint;
        private ContentPresenter captureItem;
        private DragItemAdorner dropAdorner;
        private const string SingleItemDataFormat = "DragSingleItem";

        public DragItemsControl()
        {
            this.AllowDrop = true;
            this.Drop += this.OnDrop;
            this.DragOver += this.OnDragOver;
            this.DragLeave += this.OnDragLeave;
        }

        private IDragHost Host
        {
            get
            {
                return WpfHelpers.FindVisualAncestor<IDragHost>(this);
            }
        }

        private void OnDrop(object sender, DragEventArgs args)
        {
            this.RemoveDropAdorner();

            object dropModel = args.Data.GetData(DragItemsControl.SingleItemDataFormat);

            if (dropModel != null && this.GetDropTarget(args, out int index, out bool firstHalf))
            {
                bool copy =
                    (args.Effects & DragDropEffects.Copy) == DragDropEffects.Copy &&
                    (args.KeyStates & DragDropKeyStates.ControlKey) == DragDropKeyStates.ControlKey;

                if (!firstHalf)
                {
                    index++;
                }

                if (index >= 0 && index <= this.Items.Count)
                {
                    this.Host?.OnDrop(this, dropModel, index, copy);
                }
            }
        }

        private void OnDragOver(object sender, DragEventArgs args)
        {
            if (this.GetDropTarget(args, out int index, out bool left))
            {
                this.EnsureDropAdorner(this.ItemContainerGenerator.ContainerFromIndex(index) as ContentPresenter, left);
            }
            else
            {
                args.Effects = DragDropEffects.None;
                args.Handled = true;
            }
        }

        private void OnDragLeave(object sender, DragEventArgs args)
        {
            this.RemoveDropAdorner();
        }

        private bool GetDropTarget(DragEventArgs args, out int index, out bool left)
        {
            ItemContainerGenerator itemGen = this.ItemContainerGenerator;
            ItemCollection items = this.Items;
            Point point = args.GetPosition(this);
            IInputElement hit = this.InputHitTest(point);
            ContentPresenter item = WpfHelpers.FindItemContainer<ContentPresenter>(this, hit as DependencyObject, includeSelf: true);

            if (item == null)
            {
                // Use the last item
                item = this.HasItems
                    ? itemGen.ContainerFromIndex(items.Count - 1) as ContentPresenter
                    : null;
            }

            if (item != null)
            {
                index = itemGen.IndexFromContainer(item);
                Point itemPoint = args.GetPosition(item);
                left = itemPoint.X < item.ActualWidth / 2;
                return true;
            }

            index = 0;
            left = false;
            return false;
        }

        private void EnsureDropAdorner(ContentPresenter item, bool left)
        {
            if (this.dropAdorner == null || this.dropAdorner.AdornedElement != item || left != this.dropAdorner.Left)
            {
                this.RemoveDropAdorner();

                if (item != null)
                {
                    this.dropAdorner = new DragItemAdorner(item, left);

                    AdornerLayer layer = AdornerLayer.GetAdornerLayer(item);
                    if (layer != null)
                    {
                        layer.Add(this.dropAdorner);
                    }
                }
            }
        }

        private void RemoveDropAdorner()
        {
            if (this.dropAdorner != null)
            {
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(this.dropAdorner.AdornedElement);
                if (layer != null)
                {
                    layer.Remove(this.dropAdorner);
                }

                this.dropAdorner = null;
            }
        }

        public void NotifyMouseMove(object sender, MouseEventArgs args)
        {
            if (this.mouseCapturePoint.HasValue && this.captureItem != null)
            {
                object captureModel = this.ItemContainerGenerator.ItemFromContainer(this.captureItem);
                Point point = args.GetPosition(this);
                if (Math.Abs(this.mouseCapturePoint.Value.X - point.X) >= SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(this.mouseCapturePoint.Value.Y - point.Y) >= SystemParameters.MinimumVerticalDragDistance)
                {
                    try
                    {
                        if (this.CaptureMouse())
                        {
                            this.dragging = true;
                            bool canCopy = this.Host?.CanDropCopy(captureModel) == true;
                            DragDropEffects effects = canCopy
                                ? DragDropEffects.Move | DragDropEffects.Copy
                                : DragDropEffects.Move;

                            DataObject data = new DataObject(DragItemsControl.SingleItemDataFormat, captureModel);
                            DragDrop.DoDragDrop(this, data, effects);
                        }
                    }
                    finally
                    {
                        if (this.dragging)
                        {
                            this.ReleaseMouseCapture();
                            this.dragging = false;
                            this.mouseCapturePoint = null;
                            this.captureItem = null;
                        }
                    }
                }
            }
        }

        public void NotifyMouseCapture(UIElement sender, MouseEventArgs args)
        {
            if (args.MouseDevice.Captured != null)
            {
                ContentPresenter item = WpfHelpers.FindItemContainer<ContentPresenter>(this, sender, includeSelf: true);
                if (item != null && !this.dragging)
                {
                    this.mouseCapturePoint = args.GetPosition(this);
                    this.captureItem = item;
                }
            }
            else
            {
                this.mouseCapturePoint = null;
                this.captureItem = null;
            }
        }

        private class DragItemAdorner : Adorner
        {
            public bool Left { get; private set; }

            public DragItemAdorner(UIElement adornedElement, bool left)
                : base(adornedElement)
            {
                this.Left = left;
                this.IsHitTestVisible = false;
            }

            protected override void OnRender(DrawingContext drawing)
            {
                Size size = this.AdornedElement.RenderSize;
                double x = this.Left ? 0.0 : size.Width;

                drawing.DrawRectangle(SystemColors.HighlightBrush, null, new Rect(x - 1.5, 0, 3.0, size.Height));
            }
        }
    }
}