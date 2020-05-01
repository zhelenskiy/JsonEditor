﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JsonEditor.DataClasses;

namespace JsonEditor
{
    namespace DataClasses
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        public interface INamed
        {
            string name { get; set; }
            string type { get; }

            INamed CreateChild(string _id, string _name);

            bool RemoveSubItem(INamed missing_subItem);
            bool AddSubItem(INamed subItem, int? index);

            IEnumerable<INamed> NamedSubItems();
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        public class Station : INamed
        {
            public Station(string id, string name)
            {
                this.id = id;
                this.name = name;
            }

            public string type => GetType().Name.ToLower();
            public string id { get; }
            public string name { get; set; }

            public INamed CreateChild(string _id, string _name) => new Arm(_id, _name);

            public bool RemoveSubItem(INamed subItem) => subItem is Arm arm && items.Remove(arm);

            public bool AddSubItem(INamed subItem, int? index) =>
                subItem is Arm arm && items.AddWithNullableIndex(arm, index);

            public IEnumerable<INamed> NamedSubItems() => items;

            public List<Arm> items { get; } = new List<Arm>();
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        public class Arm : INamed
        {
            public Arm(string id, string name)
            {
                this.id = id;
                this.name = name;
            }

            public string type => GetType().Name.ToLower();
            public string id { get; }
            public string name { get; set; }

            public INamed CreateChild(string _id, string _name) => new Device( _id, _name);

            public bool RemoveSubItem(INamed subItem) => subItem is Device device && items.Remove(device);

            public bool AddSubItem(INamed subItem, int? index) =>
                subItem is Device device && items.AddWithNullableIndex(device, index);

            public IEnumerable<INamed> NamedSubItems() => items;

            public List<Device> items { get; } = new List<Device>();
        }


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        // ReSharper disable once ClassNeverInstantiated.Global
        public class Device : INamed
        {
            public Device(string id, string name)
            {
                this.id = id;
                this.name = name;
            }
            public string type => GetType().Name.ToLower();
            public string id { get; }
            public string name { get; set; }

            public INamed CreateChild(string missing_id, string missing_name) => null;

            public bool RemoveSubItem(INamed missing_subItem) => false;

            public bool AddSubItem(INamed missing_subItem, int? missing_index) => false;

            public IEnumerable<INamed> NamedSubItems() => Enumerable.Empty<INamed>();
        }
    }

    public interface INode
    {
        bool RemoveSubItem(NestedNode subItem);

        bool AddSubItem(INamed subItem, int? index);

        INamed CreateChild(string _id, string _name);

        String NodeName { get; set; }

        IEnumerable<INamed> NamedSubItems();
    }

    public class NestedNode : TreeViewItem, INode
    {
        private string _name;
        private MainWindow Window { get; }

        public INamed JsonObject { get; }

        public NestedNode(MainWindow window, INamed jsonObject)
        {
            Window = window;
            JsonObject = jsonObject;
        }

        public INamed CreateChild(string _id, string _name) => JsonObject.CreateChild(_id, _name);

        public string NodeName
        {
            get => _name;
            set
            {
                Header = value;
                JsonObject.name = value;
                _name = value;
            }
        }

        public bool RemoveSubItem(NestedNode subItem)
        {
            if (JsonObject.RemoveSubItem(subItem.JsonObject))
            {
                Items.Remove(subItem);
                return true;
            }

            return false;
        }

        public bool AddSubItem(INamed subItem, int? index) =>
            JsonObject.AddSubItem(subItem, index)
            && CommonMethods.AddInterfaceNodeFromINamed(Window, this, subItem, index);

        public IEnumerable<INamed> NamedSubItems() => JsonObject.NamedSubItems();
    }

    internal class JsonTreeViewMenuItem : MenuItem
    {
        public NestedNode Source { get; set; }
    }

    internal static class CommonMethods
    {
        internal static bool AddWithNullableIndex<T>(this IList<T> list, T item, int? index)
        {
            if (index == null)
            {
                list.Add(item);
            }
            else
            {
                list.Insert((int) index, item);
            }

            return true;
        }

        private static bool AddWithNullableIndex(this IList list, object item, int? index)
        {
            if (index == null)
            {
                list.Add(item);
            }
            else
            {
                list.Insert((int) index, item);
            }

            return true;
        }

        internal static INode GetSelectedTreeViewItemParent(MainWindow window, DependencyObject item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView || parent == null))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as NestedNode as INode ?? window.Root;
        }


        internal static bool AddInterfaceNodeFromINamed(MainWindow window, ItemsControl parent, INamed item,
            int? index = null)
        {
            var v = JsonTreeViewItemFromINamed(window, item);
            foreach (var property in item.NamedSubItems())
            {
                AddInterfaceNodeFromINamed(window, v, property);
            }

            return parent.Items.AddWithNullableIndex(v, index);
        }

        private static NestedNode JsonTreeViewItemFromINamed(MainWindow window, INamed item)
        {
            var contextMenu = new ContextMenu();
            var v = new NestedNode(window, item) {Header = item.name, ContextMenu = contextMenu, IsExpanded = true};
            var create = new JsonTreeViewMenuItem {Header = "Create", Source = v};
            create.Click += window.Create_Click;
            contextMenu.Items.Add(create);
            var rename = new JsonTreeViewMenuItem {Header = "Rename", Source = v};
            rename.Click += MainWindow.Rename_Click;
            contextMenu.Items.Add(rename);
            var remove = new JsonTreeViewMenuItem {Header = "Remove", Source = v};
            remove.Click += window.Remove_Click;
            contextMenu.Items.Add(remove);
            var copy = new JsonTreeViewMenuItem {Header = "Copy", Source = v};
            copy.Click += window.Copy_Click;
            contextMenu.Items.Add(copy);
            var paste = new JsonTreeViewMenuItem {Header = "Paste", Source = v};
            paste.Click += window.Paste_Click;
            contextMenu.Items.Add(paste);
            return v;
        }

        internal static int IndexOf<T>(this IEnumerable<T> enumerable, T item)
        {
            int i = 0;
            foreach (var other in enumerable)
            {
                if (Equals(other, item)) return i;
                i++;
            }

            return -1;
        }
    }

    public class RootNode : INode
    {
        public RootNode(MainWindow window, List<Station> stations)
        {
            Window = window;
            Stations = stations;
            Window.JsonTree.Items.Clear();
            stations.ForEach(station => CommonMethods.AddInterfaceNodeFromINamed(Window, Window.JsonTree, station));
        }

        private MainWindow Window { get; }
        public List<Station> Stations { get; }

        public bool RemoveSubItem(NestedNode subItem)
        {
            if (subItem.JsonObject is Station station && Stations.Remove(station))
            {
                Window.JsonTree.Items.Remove(subItem);
                return true;
            }

            return false;
        }

        public bool AddSubItem(INamed subItem, int? index)
        {
            if (subItem is Station station)
            {
                Stations.AddWithNullableIndex(station, index);
                CommonMethods.AddInterfaceNodeFromINamed(Window, Window.JsonTree, subItem, index);
                return true;
            }

            return false;
        }

        public INamed CreateChild(string _id, string _name) => new Station(_id, _name);

        public string NodeName { get; set; }

        public IEnumerable<INamed> NamedSubItems() => Stations;
    }
}