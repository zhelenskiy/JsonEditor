using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            string type { get; }
            string id { get; }
            string name { get; set; }

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

            public INamed CreateChild(string _id, string _name) => new Device(_id, _name);

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

        INode AddSubItem(INamed subItem, int? index);

        INamed CreateChild(string id, string name);

        string NodeName { get; set; }

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

        public INamed CreateChild(string id, string name) => JsonObject.CreateChild(id, name);

        public string NodeName
        {
            get => _name;
            set
            {
                Header = value;
                JsonObject.name = value;
                _name = value;
                if (IsSelected)
                {
                    Window.CurrentName.Text = value;
                }
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

        public INode AddSubItem(INamed subItem, int? index)
        {
            CommonMethods.CheckAndAddIds(Window.UsedIds, subItem);
            return JsonObject.AddSubItem(subItem, index)
                ? CommonMethods.AddInterfaceNodeFromINamed(Window, this, subItem, index)
                : null;
        }

        public IEnumerable<INamed> NamedSubItems() => JsonObject.NamedSubItems();
    }

    internal class JsonTreeViewMenuItem : MenuItem
    {
        internal enum OperationType
        {
            Create, Rename, Remove, Copy, Paste, Cut
        }

        public OperationType Operation { get; set; }
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


        internal static INode AddInterfaceNodeFromINamed(MainWindow window, ItemsControl parent, INamed item,
            int? index = null)
        {
            var v = JsonTreeViewItemFromINamed(window, item);
            foreach (var property in item.NamedSubItems())
            {
                AddInterfaceNodeFromINamed(window, v, property);
            }

            return parent.Items.AddWithNullableIndex(v, index) ? v : null;
        }

        private static NestedNode JsonTreeViewItemFromINamed(MainWindow window, INamed item)
        {
            var contextMenu = new ContextMenu();
            var v = new NestedNode(window, item) {Header = item.name, ContextMenu = contextMenu, IsExpanded = true};
            var create = new JsonTreeViewMenuItem {Operation = JsonTreeViewMenuItem.OperationType.Create, Header = "Create", Source = v};
            create.Click += window.Create_Click;
            var rename = new JsonTreeViewMenuItem { Operation = JsonTreeViewMenuItem.OperationType.Rename, Header = "Rename", Source = v};
            rename.Click += MainWindow.Rename_Click;
            var remove = new JsonTreeViewMenuItem { Operation = JsonTreeViewMenuItem.OperationType.Remove, Header = "Remove", Source = v};
            remove.Click += window.Remove_Click;
            var copy = new JsonTreeViewMenuItem { Operation = JsonTreeViewMenuItem.OperationType.Copy, Header = "Copy", Source = v};
            copy.Click += window.Copy_Click;
            var paste = new JsonTreeViewMenuItem { Operation = JsonTreeViewMenuItem.OperationType.Paste, Header = "Paste", Source = v};
            paste.Click += window.Paste_Click;
            var cut = new JsonTreeViewMenuItem { Operation = JsonTreeViewMenuItem.OperationType.Cut, Header = "Cut", Source = v};
            cut.Click += window.Cut_Click;
            contextMenu.Items.Add(create);
            contextMenu.Items.Add(rename);
            contextMenu.Items.Add(remove);
            contextMenu.Items.Add(cut);
            contextMenu.Items.Add(copy);
            contextMenu.Items.Add(paste);
            return v;
        }

        

        internal static int IndexOf<T>(this IEnumerable<T> enumerable, T item)
        {
            var i = 0;
            foreach (var other in enumerable)
            {
                if (Equals(other, item)) return i;
                i++;
            }

            return -1;
        }

        private static Random RandomGenerator { get; } = new Random();

        private static ulong RandomUlong()
        {
            var buf = new byte[8];
            RandomGenerator.NextBytes(buf);
            return BitConverter.ToUInt64(buf, 0);
        }

        private static void CheckIds(ISet<string> ids, ISet<string> newIds, INamed currentNode)
        {
            if (ids.Contains(currentNode.id))
            {
                throw new JsonEditorException($"Id \"{currentNode.id}\" is already used!");
            }

            if (newIds.Contains(currentNode.id))
            {
                throw new JsonEditorException($"Id \"{currentNode.id}\" is used more than once!");
            }

            newIds.Add(currentNode.id);
            foreach (var subItem in currentNode.NamedSubItems())
            {
                CheckIds(ids, newIds, subItem);
            }
        }

        internal static void CheckAndAddIds(ISet<string> ids, INamed currentNode)
        {
            var newIds = new HashSet<string>();
            CheckIds(ids, newIds, currentNode);
            ids.UnionWith(newIds);
        }

        internal static string GenerateUniqueId(MainWindow window)
        {
            string newId;
            do
            {
                newId = RandomUlong().ToString();
            } while (window.UsedIds.Contains(newId));

            return newId;
        }
    }

    public class RootNode : INode
    {
        public RootNode(MainWindow window, List<Station> stations)
        {
            Window = window;
            Stations = stations;
            var oldItems = Window.JsonTree.Items.Cast<object>().ToList();
            Window.JsonTree.Items.Clear();

            var usedIds = new HashSet<string>();
            try
            {
                stations.ForEach(station => CommonMethods.CheckAndAddIds(usedIds, station));
            }
            catch (JsonEditorException)
            {
                oldItems.ForEach(oldItem => Window.JsonTree.Items.Add(oldItem));
                throw;
            }

            Window.UsedIds = usedIds;

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

        public INode AddSubItem(INamed subItem, int? index)
        {
            CommonMethods.CheckAndAddIds(Window.UsedIds, subItem);
            if (subItem is Station station)
            {
                Stations.AddWithNullableIndex(station, index);
                return CommonMethods.AddInterfaceNodeFromINamed(Window, Window.JsonTree, subItem, index);
            }

            return null;
        }

        public INamed CreateChild(string id, string name) => new Station(id, name);

        public string NodeName { get; set; }

        public IEnumerable<INamed> NamedSubItems() => Stations;
    }
    internal class NameNode
    {
        private MainWindow Window { get; }

        private NameNode(NameNode[] items, string name, MainWindow window)
        {
            Items = items;
            Name = name;
            Window = window;
        }

        public NameNode(INamed fullNode, MainWindow window)
            : this(fullNode.NamedSubItems().Select(t => new NameNode(t, window)).ToArray(), fullNode.name, window)
        {
        }

        private string Name { get; }
        private NameNode[] Items { get; }

        internal enum PasteStatus
        {
            FullyAdded,
            PartiallyAdded,
            NotAdded
        }

        public PasteStatus AddToINode(INode destination, int? index)
        {
            var named = destination.CreateChild(CommonMethods.GenerateUniqueId(Window), Name);
            if (named == null) return PasteStatus.NotAdded;
            var node = destination.AddSubItem(named, index);
            if (node == null) return PasteStatus.NotAdded;
            bool isFullyAdded = Items.Aggregate(true,
                (current, childNameNode) => current & childNameNode.AddToINode(node, null) == PasteStatus.FullyAdded);
            return isFullyAdded ? PasteStatus.FullyAdded : PasteStatus.PartiallyAdded;
        }
    }
}