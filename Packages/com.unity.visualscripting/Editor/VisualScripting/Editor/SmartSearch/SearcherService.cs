using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public static class SearcherService
    {
        static readonly SearcherWindow.Alignment k_FindAlignment = new SearcherWindow.Alignment(
            SearcherWindow.Alignment.Vertical.Top, SearcherWindow.Alignment.Horizontal.Center);
        static readonly TypeSearcherAdapter k_TypeAdapter =  new TypeSearcherAdapter("Pick a type");
        static readonly Comparison<SearcherItem> k_GraphElementSort = (x, y) =>
        {
            var xRoot = GetRoot(x);
            var yRoot = GetRoot(y);

            if (xRoot == yRoot)
                return x.Id - y.Id;

            if (xRoot.HasChildren == yRoot.HasChildren)
                return string.Compare(xRoot.Name, yRoot.Name, StringComparison.Ordinal);

            return xRoot.HasChildren ? 1 : -1;
        };
        static readonly Comparison<SearcherItem> k_TypeSort = (x, y) =>
        {
            var xRoot = GetRoot(x);
            var yRoot = GetRoot(y);

            if (xRoot == yRoot)
                return x.Id - y.Id;

            if (xRoot.HasChildren == yRoot.HasChildren)
            {
                const string lastItemName = "Advanced";

                if (xRoot.Name == lastItemName) return 1;
                if (yRoot.Name == lastItemName) return -1;
                return string.Compare(xRoot.Name, yRoot.Name, StringComparison.Ordinal);
            }

            return xRoot.HasChildren ? 1 : -1;
        };

        internal static Task<Func<GraphNodeCreationData, IGraphElementModel[]>> ShowGraphNodes(
            List<SearcherDatabase> databases,
            SearcherFilter filter,
            ISearcherAdapter adapter,
            Vector2 position,
            CancellationToken cancellationToken
            )
        {
            ApplyDatabasesFilter<GraphNodeModelSearcherItem>(databases, filter);
            var tcs = CreateAsyncTask<GraphNodeCreationData>(cancellationToken);
            var searcher = new Searcher.Searcher(databases, adapter) { SortComparison = k_GraphElementSort };

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item is GraphNodeModelSearcherItem graphNodeModelSearcherItem)
                {
                    tcs.SetResult(graphNodeModelSearcherItem.CreateElements);
                    return true;
                }

                tcs.TrySetCanceled(cancellationToken);
                return true;
            }, position, e => AnalyticsDataDelegate(e, SearcherContext.Graph));

            return tcs.Task;
        }

        internal static Task<Func<StackNodeCreationData, IGraphElementModel[]>> ShowStackNodes(
            List<SearcherDatabase> databases,
            SearcherFilter filter,
            ISearcherAdapter adapter,
            Vector2 position,
            CancellationToken cancellationToken
            )
        {
            ApplyDatabasesFilter<StackNodeModelSearcherItem>(databases, filter);
            var tcs = CreateAsyncTask<StackNodeCreationData>(cancellationToken);
            var searcher = new Searcher.Searcher(databases, adapter) { SortComparison = k_GraphElementSort };

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item is StackNodeModelSearcherItem nodeModelSearcherItem)
                {
                    tcs.SetResult(nodeModelSearcherItem.CreateElements);
                    return true;
                }

                tcs.TrySetCanceled();
                return true;
            }, position, e => AnalyticsDataDelegate(e, SearcherContext.Stack));

            return tcs.Task;
        }

        static SearcherItem GetRoot(SearcherItem item)
        {
            if (item.Parent == null)
                return item;

            SearcherItem parent = item.Parent;
            while (true)
            {
                if (parent.Parent == null)
                    break;

                parent = parent.Parent;
            }

            return parent;
        }

        static void ApplyDatabasesFilter<T>(IEnumerable<SearcherDatabase> databases, SearcherFilter filter)
            where T : ISearcherItemDataProvider
        {
            foreach (var database in databases)
            {
                database.MatchFilter = (query, item) =>
                {
                    if (!(item is T dataProvider))
                        return false;

                    if (filter == null || filter == SearcherFilter.Empty)
                        return true;

                    return filter.ApplyFilters(dataProvider.Data);
                };
            }
        }

        static TaskCompletionSource<Func<T, IGraphElementModel[]>> CreateAsyncTask<T>(
            CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Func<T, IGraphElementModel[]>>();
            cancellationToken.Register(() =>
            {
                // Prevents Exception when changing window while searcher is open
                if (tcs.Task.IsCompleted)
                    return;

                tcs.SetCanceled();
                EditorWindow.GetWindow<SearcherWindow>()?.Close();
            });

            return tcs;
        }

        // Used to display data that is not meant to be persisted. The database will be overwritten after each call to SearcherWindow.Show(...).
        internal static void ShowTransientData(EditorWindow host, IEnumerable<SearcherItem> items, ISearcherAdapter adapter, Action<SearcherItem> selectionDelegate, Vector2 pos)
        {
            var database = SearcherDatabase.Create(items.ToList(), "", false);
            var searcher = new Searcher.Searcher(database, adapter);

            SearcherWindow.Show(host, searcher, x =>
                {
                    host.Focus();
                    selectionDelegate(x);

                    return !(Event.current?.modifiers.HasFlag(EventModifiers.Control)).GetValueOrDefault();
                }, pos, null);
        }

        internal static void FindInGraph(
            EditorWindow host,
            VSGraphModel graph,
            Action<FindInGraphAdapter.FindSearcherItem> highlightDelegate,
            Action<FindInGraphAdapter.FindSearcherItem> selectionDelegate
            )
        {
            var items = graph.GetAllNodes()
                .Where(x => !string.IsNullOrEmpty(x.Title))
                .Select(MakeFindItems)
                .ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new FindInGraphAdapter(highlightDelegate));
            var position = new Vector2(host.rootVisualElement.layout.center.x, 0);

            SearcherWindow.Show(host, searcher, item =>
                {
                    selectionDelegate(item as FindInGraphAdapter.FindSearcherItem);
                    return true;
                },
                position, null, k_FindAlignment);
        }

        internal static void ShowEnumValues(string title, Type enumType, Vector2 position, Action<Enum, int> callback)
        {
            var items = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(v => new EnumValuesAdapter.EnumValueSearcherItem(v) as SearcherItem)
                .ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new EnumValuesAdapter(title));

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
                {
                    if (item == null)
                        return false;

                    callback(((EnumValuesAdapter.EnumValueSearcherItem)item).value, 0);
                    return true;
                }, position, null);
        }

        internal static void ShowValues(string title, IEnumerable<string> values, Vector2 position, Action<string, int> callback)
        {
            var items = values.Select(v => new SearcherItem(v)).ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new SimpleSearcherAdapter(title));

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(item.Name, item.Id);
                return true;
            }, position, null);
        }

        public static void ShowTypes(Stencil stencil, Vector2 position, Action<TypeHandle, int> callback,
            SearcherFilter userFilter = null)
        {
            var databases = stencil.GetSearcherDatabaseProvider().GetTypesSearcherDatabases();
            foreach (var database in databases)
            {
                database.MatchFilter = (query, item) =>
                {
                    if (!(item is TypeSearcherItem typeItem))
                        return false;

                    var filter = stencil.GetSearcherFilterProvider()?.GetTypeSearcherFilter();
                    var res = true;

                    if (filter != null)
                        res &= GetFilterResult(filter, typeItem.Data);

                    if (userFilter != null)
                        res &= GetFilterResult(userFilter , typeItem.Data);

                    return res;
                };
            }

            var searcher = new Searcher.Searcher(databases, k_TypeAdapter) { SortComparison = k_TypeSort };
            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, position, null);
        }

        static bool GetFilterResult(SearcherFilter filter, ISearcherItemData data)
        {
            return filter == SearcherFilter.Empty || filter.ApplyFilters(data);
        }

        static void AnalyticsDataDelegate(Searcher.Searcher.AnalyticsEvent obj, SearcherContext context)
        {
            var userActionKind = obj.eventType == Searcher.Searcher.AnalyticsEvent.EventType.Picked
                ? AnalyticsHelper.UserActionKind.WaitForConfirmation // wait for next action or window close to know if it was cancelled
                : AnalyticsHelper.UserActionKind.SendImmediately; // no need to wait
            AnalyticsHelper.Instance.AddUserActionEvent(obj.currentSearchFieldText, context, userActionKind);
        }

        static SearcherItem MakeFindItems(INodeModel node)
        {
            List<SearcherItem> children = null;
            string title = node.Title;

            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find window
                case IConstantNodeModel _:
                {
                    var nodeTitle = node is StringConstantModel ? $"\"{node.Title}\"" : node.Title;
                    title = $"Const {((ConstantNodeModel)node).Type.Name} {nodeTitle}";
                    break;
                }

                case PropertyGroupBaseNodeModel prop:
                {
                    title = node is GetPropertyGroupNodeModel ? "Get " : "Set ";
                    children = prop.Members.Select(m =>
                        (SearcherItem)new FindInGraphAdapter.FindSearcherItem(node, m.ToString())).ToList();
                    break;
                }
            }

            // find current function/event
            string method = node.ParentStackModel?.OwningFunctionModel?.Title;
            title = method == null ? title : ($"{title} ({method})");

            return new FindInGraphAdapter.FindSearcherItem(node, title, children: children);
        }
    }
}
