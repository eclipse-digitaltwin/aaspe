﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

// #define TESTMODE

#pragma warning disable 1416

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using System.Windows;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System.Windows.Documents;
using System.Printing;
using Microsoft.Msagl.Layout.MDS;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Layout.Incremental;


namespace AasxPluginBomStructure
{

    /// <summary>
    /// This set of static functions lay-out a graph according to the package information.
    /// Right now, no domain-specific lay-out.
    /// </summary>
    public class GenericBomControl
    {
        protected static bool UseContextMenu = false;

        private AdminShellPackageEnv _package;
        private LogInstance _log;
        private Aas.Submodel _submodel;
        private PluginEventStack _eventStack = null;
        private PluginSessionBase _session = null;
        private bool _createOnPackage = false;

        private Microsoft.Msagl.Drawing.Graph _graph = null;
        private Microsoft.Msagl.WpfGraphControl.GraphViewer _viewer = null;
        private Aas.IReferable _referable = null;
        private DockPanel _insideDockPanel = null;

        private BomStructureOptionsRecordList _bomRecords = new BomStructureOptionsRecordList();

        private GenericBomCreatorOptions _creatorOptions = new GenericBomCreatorOptions();

        private Dictionary<Aas.IReferable, GenericBomCreatorOptions> preferredPreset =
            new Dictionary<Aas.IReferable, GenericBomCreatorOptions>();

        private BomStructureOptions _bomOptions = new BomStructureOptions();

        private GenericBomCreator _bomCreator = null;

        private Microsoft.Msagl.Core.Geometry.Point _rightClickCoordinates = 
            new Microsoft.Msagl.Core.Geometry.Point();

        private Microsoft.Msagl.Drawing.IViewerObject _objectUnderCursor = null;

        private TabControl _tabControlBottom = null;
        private TabItem _tabItemEdit = null;

        private bool _needsFinalize = false;
        private Button _buttonFinalize = null;

        public void SetEventStack(PluginEventStack es)
        {
            _eventStack = es;
        }

        protected WrapPanel CreateTopPanel()
        {
            // create TOP controls
            var wpTop = new WrapPanel()
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xf0)),
            };

            // style

            wpTop.Children.Add(new Label() { 
                Content = "Layout style: ",                
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var cbli = new ComboBox()
            {
                Margin = new Thickness(0, 2, 0, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            foreach (var psn in this.PresetSettingNames)
                cbli.Items.Add(psn);
            cbli.SelectedIndex = _creatorOptions.LayoutIndex;
            cbli.SelectedItem = cbli.Items[cbli.SelectedIndex];
            cbli.SelectionChanged += (s3, e3) =>
            {
                _creatorOptions.LayoutIndex = cbli.SelectedIndex;
                RememberSettings();
                RedrawGraph(resetTransform: true);
            };
            wpTop.Children.Add(cbli);

            // spacing

            wpTop.Children.Add(new Label() { 
                Content = "Spacing: ",
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var sli = new Slider()
            {
                Orientation = Orientation.Horizontal,
                Width = 100,
                Minimum = 1,
                Maximum = 100,
                TickFrequency = 10,
                IsSnapToTickEnabled = true,
                Value = _creatorOptions.LayoutSpacing,
                Margin = new Thickness(10, 2, 10, 2),
                VerticalAlignment = VerticalAlignment.Center
            };
            sli.ValueChanged += (s, e) =>
            {
                _creatorOptions.LayoutSpacing = e.NewValue;
                RememberSettings();
                RedrawGraph();
            };
            wpTop.Children.Add(sli);

            // Compact nodes

            var cbCompNodes = new CheckBox()
            {
                Content = "Compact nodes",
                Margin = new Thickness(10, 2, 10, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactNodes,
            };
            RoutedEventHandler cbcombN_changed = (s2, e2) =>
            {
                _creatorOptions.CompactNodes = cbCompNodes.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbCompNodes.Checked += cbcombN_changed;
            cbCompNodes.Unchecked += cbcombN_changed;
            wpTop.Children.Add(cbCompNodes);

            // Compact edges

            var cbCompEdges = new CheckBox()
            {
                Content = "Compact edges",
                Margin = new Thickness(10, 2, 10, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                IsChecked = _creatorOptions.CompactEdges,
            };
            RoutedEventHandler cbcombE_changed = (s2, e2) =>
            {
                _creatorOptions.CompactEdges = cbCompEdges.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbCompEdges.Checked += cbcombE_changed;
            cbCompEdges.Unchecked += cbcombE_changed;
            wpTop.Children.Add(cbCompEdges);

            // show asset ids

            var cbaid = new CheckBox()
            {
                Content = "Show Asset ids",
                Margin = new Thickness(10, 2, 10, 2),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsChecked = _creatorOptions.ShowAssetIds,
            };
            RoutedEventHandler cbaid_changed = (s2, e2) =>
            {
                _creatorOptions.ShowAssetIds = cbaid.IsChecked == true;
                RememberSettings();
                RedrawGraph();
            };
            cbaid.Checked += cbaid_changed;
            cbaid.Unchecked += cbaid_changed;
            wpTop.Children.Add(cbaid);

            // finalize button
            _buttonFinalize = new Button()
            {
                Content = "Finalize design",
                ToolTip = "Will reload all contents including redisplay of AAS tree of elements",
                IsEnabled = _needsFinalize,
                Padding = new Thickness(2, -2, 2, -1),
                Margin = new Thickness(2, 1, 2, 1),
                MinHeight = 24
            };
            _buttonFinalize.Click += (s3, e3) =>
            {
                // acknowledge
                SetNeedsFinalize(false);

                // send event to main application
                var evt = new AasxPluginResultEventRedrawAllElements()
                {
                };
                _eventStack.PushEvent(evt);
            };
            wpTop.Children.Add(_buttonFinalize);

            // return

            return wpTop;
        }

        protected void SetNeedsFinalize(bool state)
        {
            _needsFinalize = state;
            if (_buttonFinalize != null)
                _buttonFinalize.IsEnabled = state;

            if (state)
                _log?.Info(StoredPrint.Color.Blue, "At end of change session, activate \"Finalize\" tu update tree.");
        }

        /// <summary>
        /// This is the "normal" view of the BOM plugin
        /// </summary>
        public object FillWithWpfControls(
            BomStructureOptions bomOptions,
            LogInstance log,
            PluginEventStack eventStack,
            PluginSessionBase session,
            object opackage, object osm, object masterDockPanel)
        {
            // access
            _package = opackage as AdminShellPackageEnv;
            _log = log;
            _eventStack = eventStack;
            _session = session;
            _submodel = osm as Aas.Submodel;
            _createOnPackage = false;
            _bomOptions = bomOptions;
            var master = masterDockPanel as DockPanel;
            if (_bomOptions == null || _package == null || _submodel == null || master == null)
                return null;

            // set of records helping layouting
            _bomRecords = new BomStructureOptionsRecordList(
                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                    _submodel.SemanticId?.GetAsExactlyOneKey()));

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // apply some global options?
            foreach (var br in _bomRecords)
            {
                if (br.Layout >= 1 && br.Layout <= PresetSettingNames.Length)
                    _creatorOptions.LayoutIndex = br.Layout - 1;
                if (br.Compact.HasValue)
                    _creatorOptions.CompactEdges = br.Compact.Value;
            }

            // already user defined?
            if (preferredPreset != null && preferredPreset.ContainsKey(_submodel))
                _creatorOptions = preferredPreset[_submodel].Copy();

            // the Submodel elements need to have parents
            _submodel.SetAllParents();

            // create TOP controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();

            _tabControlBottom = new TabControl() { MinHeight = 100 };
            _tabControlBottom.Items.Add(new TabItem() { 
                Header = "", Name = "tabItemLegend", 
                Visibility = Visibility.Collapsed,
                Content = legend });

            _tabItemEdit = new TabItem() { 
                Header = "", Name = "tabItemEdit", 
                Visibility = Visibility.Collapsed,
                Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None }) };
            _tabControlBottom.Items.Add(_tabItemEdit);

            DockPanel.SetDock(_tabControlBottom, Dock.Bottom);
            master.Children.Add(_tabControlBottom);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, _submodel, _creatorOptions);

            // very important: first bind it, then add graph
            var viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            viewer.BindToPanel(dp);
            viewer.MouseDown += Viewer_MouseDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseUp += Viewer_MouseUp;
            viewer.ObjectUnderMouseCursorChanged += Viewer_ObjectUnderMouseCursorChanged;
            viewer.ViewChangeEvent += Viewer_ViewChangeEvent;
            viewer.Graph = graph;

            // test
                dp.ContextMenu = new ContextMenu();
            if (UseContextMenu)
            {
                dp.ContextMenu.Items.Add(new MenuItem() { Header = "Jump to selected ..", Tag = "JUMP" });
                dp.ContextMenu.Items.Add(new Separator());
                dp.ContextMenu.Items.Add(new MenuItem() { Header = "Edit Node / Edge ..", Tag = "EDIT" });
                dp.ContextMenu.Items.Add(new MenuItem() { Header = "Create Node (to selected) ..", Tag = "CREATE" });
                dp.ContextMenu.Items.Add(new MenuItem() { Header = "Delete (selected) Node(s) ..", Tag = "DELETE" });
                dp.ContextMenu.Items.Add(new Separator());
            }

            // SVG
            dp.ContextMenu.Items.Add(new MenuItem() { Header = "Export as SVG ..", Tag = "EXP-SVG" });

            foreach (var x in dp.ContextMenu.Items)
                    if (x is MenuItem mi)
                        mi.Click += ContextMenu_Click;

            // make it re-callable
            _graph = graph;
            _viewer = viewer;
            _referable = _submodel;
            _insideDockPanel = dp;

            // return viewer for advanced manipulation
            return viewer;
        }

        private Microsoft.Msagl.Core.Geometry.Curves.PlaneTransformation _savedTransform = null;

        private void Viewer_ViewChangeEvent(object sender, EventArgs e)
        {
            _savedTransform = _viewer.Transform;
        }

        protected bool IsViewerNode(Microsoft.Msagl.Drawing.IViewerNode vn)
        {
            foreach (var x in _viewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode xvn && xvn == vn)
                    return true;
            return false;
        }

        protected Microsoft.Msagl.Drawing.IViewerNode FindViewerNode(Microsoft.Msagl.Drawing.Node node)
        {
            if (_viewer == null || node == null)
                return null;
            foreach (var x in _viewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn
                    && vn.Node == node)
                    return vn;
            return null;
        }

        protected IEnumerable<Microsoft.Msagl.Drawing.IViewerNode> GetSelectedViewerNodes()
        {
            if (_viewer == null)
                yield break;

            foreach (var x in _viewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn)
                    if (vn.MarkedForDragging)
                        yield return vn;
        }

        protected IEnumerable<Aas.IReferable> GetSelectedViewerReferables()
        {
            if (_viewer == null)
                yield break;

            foreach (var x in _viewer.Entities)
                if (x is Microsoft.Msagl.Drawing.IViewerNode vn)
                    if (vn.MarkedForDragging && vn.Node?.UserData is Aas.IReferable rf)
                        yield return rf;
        }

        protected Tuple<Aas.Entity, Aas.RelationshipElement> CreateNodeAndRelationInBom(
            string nodeIdShort,
            string nodeSemId,
            string nodeSuppSemId,
            Aas.IReferable parent,
            string relSemId,
            string relSuppSemId)
        {
            // access
            if (_submodel == null)
                return null;

            // create
            var ent = new Aas.Entity(Aas.EntityType.CoManagedEntity, idShort: nodeIdShort);
            ent.Parent = parent;
            if (nodeSemId?.HasContent() == true)
                ent.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSemId) }).ToList());
            if (nodeSuppSemId?.HasContent() == true)
                ent.SupplementalSemanticIds = (new Aas.IReference[] {
                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSuppSemId) }).ToList())
                    }).ToList();

            // where to add?
            var contToAdd = ((parent as Aas.IEntity) as Aas.IReferable) ?? _submodel;
            contToAdd.Add(ent);

            // try build a relationship
            Aas.RelationshipElement rel = null;
            if (parent != null)
            {
                var klFirst = _submodel.BuildKeysToTop(parent as Aas.ISubmodelElement);
                if (klFirst.Count == 0)
                    klFirst.Add(new Aas.Key(Aas.KeyTypes.Submodel, _submodel.Id));
                var klSecond = _submodel.BuildKeysToTop(ent);

                if (klFirst.Count >= 1 && klSecond.Count >= 1)
                {
                    rel = new Aas.RelationshipElement(
                        idShort: "HasPart_" + nodeIdShort,
                        first: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klFirst),
                        second: new Aas.Reference(Aas.ReferenceTypes.ModelReference, klSecond));
                    if (relSemId?.HasContent() == true)
                        rel.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, relSemId) }).ToList());
                    if (relSuppSemId?.HasContent() == true)
                        rel.SupplementalSemanticIds = (new Aas.IReference[] {
                        new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                            (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, relSuppSemId) }).ToList())
                    }).ToList();
                    contToAdd.Add(rel);
                }
            }

            // ok
            return new Tuple<Aas.Entity, Aas.RelationshipElement>(ent, rel);
        }

        protected void AdjustNodeInBom(
            Aas.ISubmodelElement nodeSme,
            string nodeIdShort,
            string nodeSemId,
            string nodeSuppSemId)
        {
            // access
            if (_submodel == null || nodeSme == null)
                return;

            // we need to exchange node in References!
            var kl = _submodel?.BuildKeysToTop(nodeSme);
            var changeRels = kl.Count >= 2;
            var oldRefToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, kl);

            // write back new values
            nodeSme.IdShort = nodeIdShort;
            if (nodeSemId?.HasContent() == true)
                nodeSme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSemId) }).ToList());
            else
                nodeSme.SemanticId = null;

            if (nodeSuppSemId?.HasContent() == true)
                nodeSme.SupplementalSemanticIds = (new Aas.IReference[] {
                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                        (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, nodeSuppSemId) }).ToList())
                    }).ToList();
            else
                nodeSme.SupplementalSemanticIds = null;

            // use the same logic to make a replacement reference (needs no further check)
            var newRefToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, _submodel?.BuildKeysToTop(nodeSme));

            // now search recursively for all RefElems and RelElems referring to it
            _submodel?.RecurseOnSubmodelElements(null, (o, parents, sme) => {

                // figure out the last parent = container of SME
                Aas.IReferable cont = (parents.Count < 1) ? _submodel : parents.LastOrDefault();

                // to change?
                if (sme is Aas.IRelationshipElement relEl)
                {
                    relEl.First?.ReplacePartialHead(oldRefToNode, newRefToNode);
                    relEl.Second?.ReplacePartialHead(oldRefToNode, newRefToNode);
                }
                if (sme is Aas.IReferenceElement refEl)
                {
                    refEl.Value?.ReplacePartialHead(oldRefToNode, newRefToNode);
                }

                // always search further
                return true;
            });
        }

        protected void AdjustEdgeInBom(
            Aas.ISubmodelElement edgeSme,
            string edgeIdShort,
            string edgeSemId,
            string edgeSuppSemId)
        {
            // access
            if (_submodel == null || edgeSme == null)
                return;

            // write back new values
            edgeSme.IdShort = edgeIdShort;
            if (edgeSemId?.HasContent() == true)
                edgeSme.SemanticId = new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                    (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, edgeSemId) }).ToList());
            else
                edgeSme.SemanticId = null;

            if (edgeSuppSemId?.HasContent() == true)
                edgeSme.SupplementalSemanticIds = (new Aas.IReference[] {
                    new Aas.Reference(Aas.ReferenceTypes.ExternalReference,
                        (new Aas.IKey[] { new Aas.Key(Aas.KeyTypes.GlobalReference, edgeSuppSemId) }).ToList())
                    }).ToList();
            else
                edgeSme.SupplementalSemanticIds = null;
        }

        static Microsoft.Msagl.Core.Layout.Node GeometryNode(Microsoft.Msagl.Drawing.IViewerNode node)
        {
            Microsoft.Msagl.Core.Layout.Node geomNode = ((Microsoft.Msagl.Drawing.Node)node.DrawingObject).GeometryNode;
            return geomNode;
        }
        
        protected void ContextMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi 
                && mi.Tag is string miTag
                && miTag?.HasContent() == true)
            {
                if (UseContextMenu)
                {
                    if (miTag == "JUMP")
                    {
                        if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode)
                            NavigateTo(_objectUnderCursor?.DrawingObject);
                    }

                    if (miTag == "EDIT")
                    {
                        StartDialoguePanelEditFor(_objectUnderCursor);
                    }

                    if (miTag == "CREATE")
                    {
                        StartDialoguePanelCreateFor(_objectUnderCursor);
                    }

                    if (miTag == "DELETE")
                    {
                        // independent function
                        StartDialoguePanelDelete();
                    }
                }

                // https://github.com/microsoft/automatic-graph-layout/issues/372

                if (miTag == "EXP-SVG" && _viewer.Graph != null)
                {
                    // ask for file name
                    var dlg = new Microsoft.Win32.SaveFileDialog()
                    {
                        FileName = "new",
                        DefaultExt = ".svg",
                        Filter = "Scalable Vector Graphics (.svg)|*.svg|All files|*.*"
                    };
                    if (dlg.ShowDialog() != true)
                        return;

#if __old_version_of_MSAGL
                    theViewer.Graph.CreateGeometryGraph();
                    LayoutHelpers.CalculateLayout(theViewer.Graph.GeometryGraph, new SugiyamaLayoutSettings(), null);

                    foreach (var n in theViewer.Graph.Nodes)
                        if (n.Label != null)
                        {
                            n.Label.Width = 100;
                            n.Label.Height = 20;
                        }
#endif

                    // take care on resources
                    try
                    {
                        using (var stream = File.Create(dlg.FileName))
                        {
                            var svgWriter = new Microsoft.Msagl.Drawing.SvgGraphWriter(stream, _viewer.Graph);
                            svgWriter.Write();
                        }

                        _log.Info(StoredPrint.Color.Blue, "BOM plugin exported SVG: {0}", dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        _log?.Error(ex, "when creating SVG");
                    }

                    // toggle redisplay -> graph is renewed for display
                    RedrawGraph();
                }

            }
        }

        /// <summary>
        /// This is used by the menu option to create BOM overview on full package
        /// </summary>
        public object CreateViewPackageReleations(
            BomStructureOptions bomOptions,
            object opackage,
            DockPanel master)
        {
            // access
            _package = opackage as AdminShellPackageEnv;
            _submodel = null;
            _createOnPackage = true;
            _bomOptions = bomOptions;
            if (_bomOptions == null || _package?.AasEnv == null)
                return null;

            // new master panel
            // dead-csharp off
            // var master = new DockPanel();
            // dead-csharp on

            // clear some other members (GenericBomControl is not allways created new)
            _creatorOptions = new GenericBomCreatorOptions();

            // index all submodels
            foreach (var sm in _package.AasEnv.OverSubmodelsOrEmpty())
                sm.SetAllParents();

            // create controls
            var spTop = CreateTopPanel();
            DockPanel.SetDock(spTop, Dock.Top);
            master.Children.Add(spTop);

            // create BOTTOM controls
            var legend = GenericBomCreator.GenerateWrapLegend();
            DockPanel.SetDock(legend, Dock.Bottom);
            master.Children.Add(legend);

            // set default for very small edge label size
            Microsoft.Msagl.Drawing.Label.DefaultFontSize = 6;

            // make a Dock panel (within)
            var dp = new DockPanel();
            dp.ClipToBounds = true;
            dp.MinWidth = 10;
            dp.MinHeight = 10;

            // very important: add first the panel, then add graph
            master.Children.Add(dp);

            // graph
            var graph = CreateGraph(_package, null, _creatorOptions, createOnPackage: _createOnPackage);

            // very important: first bind it, then add graph
            var viewer = new Microsoft.Msagl.WpfGraphControl.GraphViewer();
            viewer.BindToPanel(dp);
            viewer.MouseDown += Viewer_MouseDown;
            viewer.MouseMove += Viewer_MouseMove;
            viewer.MouseUp += Viewer_MouseUp;
            viewer.ObjectUnderMouseCursorChanged += Viewer_ObjectUnderMouseCursorChanged;
            viewer.Graph = graph;

            // make it re-callable
            _graph = graph;
            _viewer = viewer;
            _referable = _submodel;

            // return viewer for advanced manipulation
            // dead-csharp off
            // return viewer;
            // dead-csharp on

            // return master
            return master;
        }

        private Microsoft.Msagl.Drawing.Graph CreateGraph(
            AdminShellPackageEnv env,
            Aas.Submodel sm,
            GenericBomCreatorOptions options,
            bool createOnPackage = false)
        {
            // access   
            if (env?.AasEnv == null || (sm == null && !createOnPackage) || options == null)
                return null;

            //create a graph object
            Microsoft.Msagl.Drawing.Graph graph = new Microsoft.Msagl.Drawing.Graph("BOM-graph");

#if TESTMODE
            //create the graph content
            graph.AddEdge("A", "B");
            var e1 = graph.AddEdge("B", "C");
            e1.Attr.ArrowheadAtSource = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.ArrowheadAtTarget = Microsoft.Msagl.Drawing.ArrowStyle.None;
            e1.Attr.Color = Microsoft.Msagl.Drawing.Color.Magenta;
            e1.GeometryEdge = new Microsoft.Msagl.Core.Layout.Edge();
            // e1.LabelText = "Dumpf!";
            e1.LabelText = "hbhbjhbjhb";
            // e1.Label = new Microsoft.Msagl.Drawing.Label("Dumpf!!");
            graph.AddEdge("A", "C").Attr.Color = Microsoft.Msagl.Drawing.Color.Green;
            graph.FindNode("A").Attr.FillColor = Microsoft.Msagl.Drawing.Color.Magenta;
            graph.FindNode("B").Attr.FillColor = Microsoft.Msagl.Drawing.Color.MistyRose;
            Microsoft.Msagl.Drawing.Node c = graph.FindNode("C");
            graph.FindNode("B").LabelText = "HalliHallo";
            c.Attr.FillColor = Microsoft.Msagl.Drawing.Color.PaleGreen;
            c.Attr.Shape = Microsoft.Msagl.Drawing.Shape.Diamond;
            c.Label.FontSize = 28;

#else

            _bomCreator = new GenericBomCreator(
                env?.AasEnv,
                _bomRecords,
                options);

            // Turn on logging if required
            //// using (var tw = new StreamWriter("bomgraph.log"))
            {
                if (!createOnPackage)
                {
                    // just one Submodel
                    _bomCreator.RecurseOnLayout(1, graph, null, sm.SubmodelElements, 1, null);
                    _bomCreator.RecurseOnLayout(2, graph, null, sm.SubmodelElements, 1, null);
                    _bomCreator.RecurseOnLayout(3, graph, null, sm.SubmodelElements, 1, null);
                }
                else
                {
                    for (int pass = 1; pass <= 3; pass++)
                        foreach (var sm2 in env.AasEnv.OverSubmodelsOrEmpty())
                        {
                            // create AAS and SM
                            if (pass == 1)
                                _bomCreator.CreateAasAndSubmodelNodes(graph, sm2);

                            // modify creator's bomRecords on the fly
                            var recs = new BomStructureOptionsRecordList(
                                _bomOptions.LookupAllIndexKey<BomStructureOptionsRecord>(
                                    sm2.SemanticId?.GetAsExactlyOneKey()));
                            _bomCreator.SetRecods(recs);

                            // graph itself
                            _bomCreator.RecurseOnLayout(pass, graph, null, sm2.SubmodelElements, 1, null,
                                entityParentRef: sm2);
                        }
                }
            }

            // make default or (already) preferred settings
            var settings = GivePresetSettings(options, graph.NodeCount);
            if (this.preferredPreset != null && sm != null
                && this.preferredPreset.ContainsKey(sm))
                settings = GivePresetSettings(this.preferredPreset[sm], graph.NodeCount);
            if (settings != null)
                graph.LayoutAlgorithmSettings = settings;

            // switching between LR and TB makes a lot of issues, therefore:
            // LR is the most useful one!
            graph.Attr.LayerDirection = Microsoft.Msagl.Drawing.LayerDirection.LR;

#endif
            return graph;
        }

        private void Viewer_ObjectUnderMouseCursorChanged(
            object sender, Microsoft.Msagl.Drawing.ObjectUnderMouseCursorChangedEventArgs e)
        {
            // this is called when the pointer is moved; no click is required
        }

        // mouse states from mouse down to mouse up
        protected bool _leftButtonIsPressed = false;

        private void Viewer_MouseDown(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
            // double click
            if (e != null && e.Clicks > 1 && e.LeftButtonIsPressed && _viewer != null && _eventStack != null)
            {
                // double-click detected, can access the viewer?
                try
                {
                    var x = _viewer.ObjectUnderMouseCursor;
                    NavigateTo(x?.DrawingObject);
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                }
            }

            // 1-click RIGHT opens context menu
            if (UseContextMenu)
            {
                if (e != null && e.Clicks == 1 && e.RightButtonIsPressed
                    && _insideDockPanel.ContextMenu != null
                    && _viewer != null)
                {
                    _objectUnderCursor = _viewer.ObjectUnderMouseCursor;
                    _rightClickCoordinates = _viewer.ScreenToSource(e);

                    _insideDockPanel.ContextMenu.IsOpen = true;
                }
            }

            // remember state for mouse up
            _leftButtonIsPressed = e.LeftButtonIsPressed;
        }

        private void Viewer_MouseUp(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
            // 1-click LEFT opens/ closes info panel
            if (!UseContextMenu)
            {
                if (e != null && e.Clicks == 1 && _leftButtonIsPressed
                    && _viewer != null)
                {
                    _objectUnderCursor = _viewer.ObjectUnderMouseCursor;
                    var noStat = _dialogueStatus == null || _dialogueStatus.Type == DialogueType.None;

                    // special rule: if no object under cursor (because there is no) and
                    // the Submodel has any entities, offer to create some
                    if (_objectUnderCursor == null)
                    {
                        var anyEntry = _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IEntity>(
                            AasxPredefinedConcepts.HierarchStructV11.Static.CD_EntryNode?.GetSingleKey(),
                            matchMode: MatchMode.Relaxed);
                        
                        var anyNode = _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IEntity>(
                            AasxPredefinedConcepts.HierarchStructV11.Static.CD_Node?.GetSingleKey(),
                            matchMode: MatchMode.Relaxed);

                        if (anyEntry == null && anyNode == null)
                        {
                            StartDialoguePanelNoNodesFor();
                        }
                    }

                    // if no object under cursor, show no panel
                    // (assume that any selected obect will be de-selected by the viewer)
                    if (_objectUnderCursor == null && !noStat)
                    {
                        // force close
                        HideDialoguePanel();
                        return;
                    }

                    // multiple selected? .. check if already some OTHER nodes are selected?
                    var multi = GetSelectedViewerNodes().ToList();

                    if (multi.Count > 1)
                    {
                        // open multi select panel
                        var copy = new List<Microsoft.Msagl.Drawing.IViewerNode>(multi);
                        if (!copy.Contains(_objectUnderCursor))
                            copy.Add(_objectUnderCursor);
                        StartDialoguePanelMultiSelectFor(copy);
                    }
                    else
                    {
                        // try to auto-open edit panel
                        StartDialoguePanelEditFor(_objectUnderCursor);
                    }
                }
            }
        }

        private void Viewer_MouseMove(object sender, Microsoft.Msagl.Drawing.MsaglMouseEventArgs e)
        {
        }

        protected void NavigateTo(Microsoft.Msagl.Drawing.DrawingObject obj)
        {
            if (obj != null && obj.UserData != null)
            {
                var us = obj.UserData;
                if (us is Aas.IReferable)
                {
                    // make event
                    var refs = new List<Aas.IKey>();
                    (us as Aas.IReferable).CollectReferencesByParent(refs);

                    // ok?
                    if (refs.Count > 0)
                    {
                        var evt = new AasxPluginResultEventNavigateToReference();
                        evt.targetReference = ExtendReference.CreateNew(refs);
                        _eventStack.PushEvent(evt);
                    }
                }

                if (us is Aas.Reference)
                {
                    var evt = new AasxPluginResultEventNavigateToReference();
                    evt.targetReference = (us as Aas.Reference);
                    _eventStack.PushEvent(evt);
                }
            }
        }

        private string[] PresetSettingNames =
        {
            "1 | Tree style layout",
            "2 | Round layout (variable)",
            "3 | MDS/round layout",
            "4 | MDS+fast/rectilinear layout"
        };

        private Microsoft.Msagl.Core.Layout.LayoutAlgorithmSettings GivePresetSettings(
            GenericBomCreatorOptions opt, int nodeCount)
        {
            var li = (opt == null || opt.LayoutIndex < 1 || opt.LayoutIndex >= 1 + PresetSettingNames.Length) 
                     ? 1 : opt.LayoutIndex + 1;

            switch (li)
            {
                default:
                case 1:
                    {
                        // Tree
                        var settings = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
                        return settings;
                    }

                case 2:
                    {
                        // Round
                        var settings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
                        settings.RepulsiveForceConstant = 8.0 / (1.0 + Math.Log(nodeCount)) * (5.0 + opt.LayoutSpacing);
                        return settings;
                    }

                case 3:
                    {
                        // MDS
                        var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
                        settings.EdgeRoutingSettings = new Microsoft.Msagl.Core.Routing.EdgeRoutingSettings();
                        settings.EdgeRoutingSettings.EdgeRoutingMode = 
                            Microsoft.Msagl.Core.Routing.EdgeRoutingMode.Spline;
                        return settings;
                    }

                case 4:
                    {
                        // MDS + Fast Incremental
                        var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings()
                        {
                            EdgeRoutingSettings = {
                                EdgeRoutingMode = EdgeRoutingMode.Rectilinear,
                                CornerRadius = 10
                            },
                            NodeSeparation = 50, // Minimum space between nodes                            
                        };
                        return settings;
                    }
            }
        }

        protected void RememberSettings()
        {
            // try to remember preferred setting
            if (_referable != null && preferredPreset != null && _creatorOptions != null)
                this.preferredPreset[_referable] = _creatorOptions.Copy();
        }

        protected void RedrawGraph(bool resetTransform = false)
        {
            // complete reset of viewport?
            if (resetTransform)
            {
                _savedTransform = null;
            }

            try
            {
                // re-draw (brutally)
                _graph = CreateGraph(_package, _submodel, _creatorOptions, createOnPackage: _createOnPackage);

                _viewer.Graph = null;
                _viewer.Graph = _graph;

                // may take over last view
                if (_savedTransform != null)
                    _viewer.Transform = _savedTransform;
            }
            catch (Exception ex)
            {
                AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
            }
        }

        //
        // Dialog pages
        //

        protected void ShowDialoguePanel(DialogueStatus stat)
        {
            _tabItemEdit.Content = CreateDialoguePanel(stat);
            _tabControlBottom.SelectedItem = _tabItemEdit;
        }

        protected void HideDialoguePanel()
        {
            _tabControlBottom.SelectedItem = _tabControlBottom.Items[0];
            _dialogueStatus = null;
        }

        protected T AddToGrid<T>(
            Grid grid,
            int row, int col,
            int rowSpan = 0, int colSpan = 0,
            T fe = null) where T : FrameworkElement
        {
            if (grid == null || fe == null)
                return null;

            Grid.SetRow(fe, row);
            Grid.SetColumn(fe, col);
            if (rowSpan > 0)
                Grid.SetRowSpan(fe, rowSpan);
            if (colSpan > 0)
                Grid.SetColumnSpan(fe, colSpan);
            grid.Children.Add(fe);

            return fe;
        }

        protected enum DialogueType { None = 0, EditNode, EditEdge, Create, Delete, MultiSelect, NoNodes }

        protected class DialogueStatus
        {
            public DialogueType Type = DialogueType.None;

            public Microsoft.Msagl.Drawing.IViewerNode ParentNode = null;
            public Aas.IReferable ParentReferable = null;

            public Aas.IReferable AasElem = null;
            public List<Aas.IReferable> Nodes = new List<Aas.IReferable>();

            public TextBox TextBoxIdShort = null;

            public ComboBox 
                ComboBoxNodeSemId = null, 
                ComboBoxNodeSupplSemId = null, 
                ComboBoxRelSemId = null,
                ComboBoxRelSupplSemId = null;

            public Action<DialogueStatus, string> Action = null;

            public bool IsEntryNode = false;
            public bool ReverseDir = false;
        }

        protected DialogueStatus _dialogueStatus = null;

        protected Panel CreateDialoguePanel(DialogueStatus stat)
        {
            // remember
            _dialogueStatus = stat;

            if (stat.Type == DialogueType.None)
            {
                // empty panel
                var grid = new Grid();
                return grid;
            }

            if (stat.Type == DialogueType.EditNode || stat.Type == DialogueType.EditEdge 
                || stat.Type == DialogueType.Create)
            {
                // add node
                var grid = new Grid();
                var prefHS = AasxPredefinedConcepts.HierarchStructV11.Static;
                var create = stat.Type == DialogueType.Create;
                var editNode = stat.Type == DialogueType.EditNode;
                var editEdge = stat.Type == DialogueType.EditEdge;

                // 4 rows (IdShort, Node.semId, Node.suppSemId, Rel.semId, Rel.suppSemId, expand, buttons)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });

                // 4 cols (auto, expand, small, expand)
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(20.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() {  Width = new GridLength(1.0, GridUnitType.Star) });

                // idShort
                AddToGrid(grid, 0, 0, fe: new Label() { 
                    Content = editEdge ? "Rel.idShort:" : "Node.idShort:" 
                });
                AddToGrid(grid, 0, 1, colSpan:1, fe: stat.TextBoxIdShort = new TextBox() { 
                    Text = (editNode || editEdge) ? stat.AasElem?.IdShort : "", 
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(0, -1, 0, -1),
                    Margin = new Thickness(0, 2, 0, 2),
                });
                AddToGrid(grid, 0, 3, fe: new Label() { Content = "(Enter confirms add)", Foreground = Brushes.DarkGray });

                // Node.semId
                if (create || editNode)
                {
                    AddToGrid(grid, 1, 0, fe: new Label() { Content = "Node.semanticId:" });
                    stat.ComboBoxNodeSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    stat.ComboBoxNodeSemId.Items.Add(prefHS.CD_EntryNode?.GetSingleKey()?.Value);
                    stat.ComboBoxNodeSemId.Items.Add(prefHS.CD_Node?.GetSingleKey()?.Value);

                    if (!editNode)
                    {
                        if (stat.IsEntryNode)
                            stat.ComboBoxNodeSemId.Text = stat.ComboBoxNodeSemId.Items[0].ToString();
                        else
                            stat.ComboBoxNodeSemId.Text = stat.ComboBoxNodeSemId.Items[1].ToString();
                    }
                    else
                    {
                        stat.ComboBoxNodeSemId.Text = "" + (stat.AasElem as Aas.IHasSemantics).
                            SemanticId.Keys?.FirstOrDefault()?.Value;
                    }                

                    AddToGrid(grid, 1, 1, colSpan: 3, fe: stat.ComboBoxNodeSemId);

                    // Node.supplSemId
                    AddToGrid(grid, 2, 0, fe: new Label() { Content = "Node.supplSemId:" });
                    stat.ComboBoxNodeSupplSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    if (_bomRecords != null)
                        foreach (var br in _bomRecords)
                            if (br.NodeSupplSemIds != null)
                                foreach (var nss in br.NodeSupplSemIds)
                                    if (!stat.ComboBoxNodeSupplSemId.Items.Contains(nss))
                                        stat.ComboBoxNodeSupplSemId.Items.Add(nss);

                    if (!editNode)
                    {
                        stat.ComboBoxNodeSupplSemId.Text = "";
                    }
                    else
                    {
                        stat.ComboBoxNodeSupplSemId.Text = 
                            "" + (stat.AasElem as Aas.IHasSemantics)?.SupplementalSemanticIds?
                                .FirstOrDefault()?.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 2, 1, colSpan: 3, fe: stat.ComboBoxNodeSupplSemId);

                }

                if (create || editEdge)
                {
                    // Rel.semId
                    AddToGrid(grid, 3, 0, fe: new Label() { Content = "Relation.semanticId:" });
                    stat.ComboBoxRelSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    stat.ComboBoxRelSemId.Items.Add(prefHS.CD_HasPart?.GetSingleKey()?.Value);
                    stat.ComboBoxRelSemId.Items.Add(prefHS.CD_IsPartOf?.GetSingleKey()?.Value);

                    if (!editEdge)
                    {
                        if (stat.ReverseDir)
                            stat.ComboBoxRelSemId.Text = stat.ComboBoxRelSemId.Items[1].ToString();
                        else
                            stat.ComboBoxRelSemId.Text = stat.ComboBoxRelSemId.Items[0].ToString();
                    } else
                    {
                        stat.ComboBoxRelSemId.Text = "" + (stat.AasElem as Aas.IHasSemantics)
                            .SemanticId.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 3, 1, colSpan: 3, fe: stat.ComboBoxRelSemId);

                    // Node.supplSemId
                    AddToGrid(grid, 4, 0, fe: new Label() { Content = "Rel.supplSemId:" });
                    stat.ComboBoxRelSupplSemId = new ComboBox()
                    {
                        IsEditable = true,
                        Padding = new Thickness(0, -1, 0, -1),
                        Margin = new Thickness(0, 2, 0, 2),
                        VerticalContentAlignment = VerticalAlignment.Center
                    };

                    if (_bomRecords != null)
                        foreach (var br in _bomRecords)
                            if (br.EdgeSupplSemIds != null)
                                foreach (var nss in br.EdgeSupplSemIds)
                                    if (!stat.ComboBoxRelSupplSemId.Items.Contains(nss))
                                        stat.ComboBoxRelSupplSemId.Items.Add(nss);

                    if (!editEdge)
                    {
                        stat.ComboBoxRelSupplSemId.Text = "";
                    }
                    else
                    {
                        stat.ComboBoxRelSupplSemId.Text =
                            "" + (stat.AasElem as Aas.IHasSemantics)?.SupplementalSemanticIds?
                                .FirstOrDefault()?.Keys?.FirstOrDefault()?.Value;
                    }

                    AddToGrid(grid, 4, 1, colSpan: 3, fe: stat.ComboBoxRelSupplSemId);
                }

                // Info
                if (editNode)
                {
                    var rtb = new RichTextBox()
                    {
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        IsDocumentEnabled = true,
                        MinHeight = 18, MaxHeight = 60
                    };

                    // line height
                    Paragraph p = rtb.Document.Blocks.FirstBlock as Paragraph;
                    p.LineHeight = 1;

                    // collect list of string infos
                    var strInfo = new List<string>();

                    // append bulk count?
                    var pbc = (stat.AasElem as Aas.IEntity)?.Statements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.HierarchStructV11.Static.CD_BulkCount, 
                        MatchMode.Relaxed)?.ValueAsText();
                    if (int.TryParse(pbc, out var i) && i > 1)
                        strInfo.Add($"bulk count = {i}");

                    // append manufacturer?
                    var manu = (stat.AasElem as Aas.IEntity)?.Statements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.DigitalNameplateV20.Static.CD_ManufacturerName, 
                        MatchMode.Relaxed)?.ValueAsText();
                    if (manu?.HasContent() == true)
                        strInfo.Add($"manufacturer = \"{manu}\"");

                    // append part designation?
                    var partDesig = (stat.AasElem as Aas.IEntity)?.Statements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static.CD_PartDesignation,
                        MatchMode.Relaxed)?.ValueAsText();
                    if (partDesig?.HasContent() == true)
                        strInfo.Add($"part designation = \"{partDesig}\"");

                    // append part order code?
                    var partOC = (stat.AasElem as Aas.IEntity)?.Statements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static.CD_PartOrderCode,
                        MatchMode.Relaxed)?.ValueAsText();
                    if (partOC?.HasContent() == true)
                        strInfo.Add($"part order code = \"{partOC}\"");

                    // turn list of string into text ranges
                    var anyInfo = false;
                    if (strInfo.Count > 0)
                    {
                        anyInfo = true;
                        TextRange tr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                        tr.Text = "" + string.Join(", ", strInfo);
                    }

                    // make another append for the URL?
                    var anyUrl = false;
                    var partURL = (stat.AasElem as Aas.IEntity)?.Statements?.FindFirstSemanticIdAs<Aas.IProperty>(
                        AasxPredefinedConcepts.DefinitionsExperimental.BomExtensions.Static.CD_PartUrl,
                        MatchMode.Relaxed)?.ValueAsText();
                    if (partURL?.HasContent() == true)
                    {
                        anyUrl = true;

                        if (anyInfo)
                        {
                            TextRange tr = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                            tr.Text = ", URL = ";
                        }

                        var link = new Hyperlink(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                        link.IsEnabled = true;

                        try
                        {
                            link.Inlines.Add(partURL);
                            link.NavigateUri = new Uri(partURL);

                            link.Click += (s, e) =>
                            {
                                // give over to event stack
                                _eventStack?.PushEvent(new AasxPluginResultEventDisplayContentFile()
                                {
                                    Session = _session,
                                    fn = partURL,
                                    mimeType = System.Net.Mime.MediaTypeNames.Text.Html
                                });
                            };
                        }
                        catch (Exception ex)
                        {
                            AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                        }
                    }

                    if (anyInfo || anyUrl)
                    {
                        AddToGrid(grid, 5, 0, fe: new Label() { Content = "Info (proprietary):" });
                        AddToGrid(grid, 5, 1, colSpan: 3, fe: rtb);
                    }
                }

                // Buttons
                var buttonGrid = new Grid();
                AddToGrid(grid, grid.RowDefinitions.Count - 1, 1, colSpan: 3, fe: buttonGrid);

                // 4 rows (IdShort, Node.semId, Node.suppSemId, Rel.semId, Rel.suppSemId, expand, buttons)
                buttonGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });

                // 5 cols (all star)
                for (int i=0; i<5; i++)
                    buttonGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(
                        ((i>=3) ? 1.5 : 1.0), GridUnitType.Star) 
                    });

                // when editing something existing: jump, delete, create
                if (stat.Type == DialogueType.EditNode || stat.Type == DialogueType.EditEdge)
                {
                    // Jump
                    var btnJump = new Button() { Content = "Jump", Padding = new Thickness(2), Margin = new Thickness(2) };
                    btnJump.Click += (s, e) =>
                    {
                        if (_objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode)
                            NavigateTo(_objectUnderCursor?.DrawingObject);
                    };
                    AddToGrid(buttonGrid, 0, 0, fe: btnJump);

                    // Delete
                    var btnDel = new Button() { Content = "Delete", Padding = new Thickness(2), Margin = new Thickness(2) };
                    btnDel.Click += (s, e) =>
                    {
                        // replace panel with "OK to proceed"
                        StartDialoguePanelDelete();
                    };
                    AddToGrid(buttonGrid, 0, 1, fe: btnDel);

                    // Create
                    var btnCreate = new Button() { Content = "Create child", Padding = new Thickness(2), Margin = new Thickness(2) };
                    btnCreate.Click += (s, e) =>
                    {
                        StartDialoguePanelCreateFor(null);
                    };
                    AddToGrid(buttonGrid, 0, 2, fe: btnCreate);
                }

                // Always: button Cancel
                if (true)
                {
                    var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                    btnCancel.Click += (s, e) =>
                    {
                        _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                        HideDialoguePanel();
                    };
                    AddToGrid(buttonGrid, 0, 3, fe: btnCancel);
                }

                // Add action
                Action lambdaAdd = () => {
                    // access
                    if (stat != null)
                    {
                        var idShort = "" + stat.TextBoxIdShort?.Text;
                        stat.Action?.Invoke(stat, "OK");
                    }

                    // done
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePanel();
                };

                stat.TextBoxIdShort.KeyDown += (s2, e2) =>
                {
                    if (e2.Key == System.Windows.Input.Key.Return)
                        lambdaAdd.Invoke();
                };

                var btnAdd = new Button() {  Content = "OK", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnAdd.Click += (s, e) => { lambdaAdd.Invoke(); } ;
                AddToGrid(buttonGrid, 0, 4, fe: btnAdd);                

                // return outer grid
                return grid;
            }

            if (stat.Type == DialogueType.MultiSelect)
            {
                // confirmation (delete)
                var grid = new Grid();

                // 5 rows (spacer, text, small gap, buttons, space)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10.0, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });

                // 5 cols (small, expand, small, expand, small)
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });

                // text
                AddToGrid(grid, 1, 1, colSpan: 3, fe: new TextBox()
                {
                    Text = "Multiple elements selected",
                    FontSize = 14.0,
                    TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsReadOnly = true
                });

                // Button: Delete
                var btnDel = new Button() { Content = "Delete selected", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnDel.Click += (s, e) => {
                    // access (the other panel will handle hiding)
                    stat.Action?.Invoke(stat, "DELETE-ALL");
                };
                AddToGrid(grid, 3, 1, fe: btnDel);

                // Button: Cancel
                var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCancel.Click += (s, e) => {
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePanel();
                };
                AddToGrid(grid, 3, 3, fe: btnCancel);

                return grid;
            }

            if (stat.Type == DialogueType.Delete)
            {
                // confirmation (delete)
                var grid = new Grid();
                
                // 5 rows (spacer, text, small gap, buttons, space)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10.0, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });

                // 5 cols (small, expand, small, expand, small)
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });

                // text
                AddToGrid(grid, 1, 1, colSpan: 3, fe: new TextBox() { 
                    Text = "Proceed with deleting selected nodes?", 
                    FontSize = 14.0, TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsReadOnly = true
                });

                // Button: Cancel
                var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCancel.Click += (s, e) => {
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePanel();
                };
                AddToGrid(grid, 3, 1, fe: btnCancel);

                // Button: Delete
                var btnDel = new Button() { Content = "Delete", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnDel.Click += (s, e) => {
                    // access
                    stat.Action?.Invoke(stat, "OK");

                    // done
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePanel();
                };
                AddToGrid(grid, 3, 3, fe: btnDel);

                return grid;
            }

            if (stat.Type == DialogueType.NoNodes)
            {
                // confirmation (delete)
                var grid = new Grid();

                // 5 rows (spacer, text, small gap, buttons, space)
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(10.0, GridUnitType.Pixel) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1.0, GridUnitType.Star) });

                // 5 cols (small, expand, small, expand, small)
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1.0, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(30.0, GridUnitType.Pixel) });

                // text
                AddToGrid(grid, 1, 1, colSpan: 3, fe: new TextBox()
                {
                    Text = "No nodes for hierarchical structures found!!",
                    FontSize = 14.0,
                    TextWrapping = TextWrapping.Wrap,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    IsReadOnly = true
                });

                // Button: Create
                var btnCreate = new Button() { Content = "Create", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCreate.Click += (s, e) => {
                    // access (the other panel will handle hiding)
                    stat.Action?.Invoke(stat, "CREATE");
                };
                AddToGrid(grid, 3, 1, fe: btnCreate);

                // Button: Cancel
                var btnCancel = new Button() { Content = "Cancel", Padding = new Thickness(2), Margin = new Thickness(2) };
                btnCancel.Click += (s, e) => {
                    _tabItemEdit.Content = CreateDialoguePanel(new DialogueStatus() { Type = DialogueType.None });
                    HideDialoguePanel();
                };
                AddToGrid(grid, 3, 3, fe: btnCancel);

                return grid;
            }

            // uuh?
            return new Grid();
        }

        protected void StartDialoguePanelEditFor(object objectUnderCursor)
        {
            if (objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode node
                     && node.Node?.UserData is Aas.ISubmodelElement nodeSme)
            {
                // create job
                var stat = new DialogueStatus() { Type = DialogueType.EditNode, AasElem = nodeSme };

                // set the action
                stat.Action = (st, action) =>
                {
                    // correct?
                    if (action != "OK" || !(st.AasElem is Aas.ISubmodelElement nodeSme))
                        return;

                    // modify
                    AdjustNodeInBom(
                        nodeSme,
                        nodeIdShort: st.TextBoxIdShort.Text,
                        nodeSemId: st.ComboBoxNodeSemId.Text,
                        nodeSuppSemId: st.ComboBoxNodeSupplSemId.Text);

                    // refresh
                    SetNeedsFinalize(true);
                    RedrawGraph();
                };

                // in any case, create a node
                ShowDialoguePanel(stat);
            }

            if (objectUnderCursor is Microsoft.Msagl.Drawing.IViewerEdge edge
             && edge.Edge?.UserData is Aas.ISubmodelElement edgeSme)
            {
                // create job
                var stat = new DialogueStatus()
                {
                    Type = DialogueType.EditEdge,
                    AasElem = edgeSme
                };

                // set the action
                stat.Action = (st, action) =>
                {
                    // correct?
                    if (action != "OK" || !(st.AasElem is Aas.ISubmodelElement esme))
                        return;

                    // modify
                    AdjustEdgeInBom(
                        esme,
                        edgeIdShort: st.TextBoxIdShort.Text,
                        edgeSemId: st.ComboBoxRelSemId.Text,
                        edgeSuppSemId: st.ComboBoxRelSupplSemId.Text);

                    // refresh
                    SetNeedsFinalize(true);
                    RedrawGraph();
                };

                // in any case, create a node
                ShowDialoguePanel(stat);
            }
        }

        protected void StartDialoguePanelCreateFor(object objectUnderCursor)
        {
            // create job
            var stat = new DialogueStatus() { Type = DialogueType.Create };

            stat.ParentNode = GetSelectedViewerNodes().FirstOrDefault();

            if (stat.ParentNode == null
                && _objectUnderCursor is Microsoft.Msagl.Drawing.IViewerNode n2)
                stat.ParentNode = n2;

            stat.ParentReferable = stat.ParentNode?.Node?.UserData as Aas.IReferable;

            // figure out if first node
            stat.IsEntryNode = null == _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IEntity>(
                AasxPredefinedConcepts.HierarchStructV11.Static.CD_EntryNode?.GetSingleKey(),
                matchMode: MatchMode.Relaxed);

            // figure out reverse direction
            stat.ReverseDir = _submodel?.SubmodelElements?.FindFirstSemanticIdAs<Aas.IProperty>(
                AasxPredefinedConcepts.HierarchStructV11.Static.CD_ArcheType?.GetSingleKey(),
                matchMode: MatchMode.Relaxed)?
                    .Value?.ToUpper().Trim() == "ONEUP";

            // set the action
            stat.Action = (st, action) =>
            {
                // correct?
                if (action != "OK" || _bomCreator == null || _viewer == null)
                    return;

                // check number of nodes BEFORE operation
                int noOfNodes = _viewer?.Graph?.NodeCount ?? 0;

                // create entity
                var ents = CreateNodeAndRelationInBom(
                    nodeIdShort: st.TextBoxIdShort.Text,
                    nodeSuppSemId: st.ComboBoxNodeSupplSemId.Text,
                    nodeSemId: st.ComboBoxNodeSemId.Text,
                    parent: st.ParentReferable,
                    relSemId: st.ComboBoxRelSemId.Text,
                    relSuppSemId: st.ComboBoxRelSupplSemId.Text);

                if (ents == null || ents.Item1 == null)
                    return;

#if shitty_no_works
                        // create a node
                        var node = _bomCreator.GenerateEntityNode(ents.Item1, allowSkip: false);
                        theViewer.CreateIViewerNode(node, _rightClickCoordinates, null);

                        // even a link
                        if (ents.Item2 != null && st.ParentNode?.Node != null)
                        {
                            var edge = _bomCreator.CreateRelationLink(
                                theViewer.Graph,
                                st.ParentNode.Node,
                                node,
                                ents.Item2);

                            theViewer.CreateEdgeWithGivenGeometry(edge);
                        }
#else

                // refresh (if it was empty before, reset viewport)
                SetNeedsFinalize(true);

                if (noOfNodes < 1)
                {
                    _viewer?.SetInitialTransform();
                    _savedTransform = null;
                }
                RedrawGraph();

#endif
            };

            // in any case, create a node
            ShowDialoguePanel(stat);

            // best approach to set the focus!
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                _dialogueStatus.TextBoxIdShort.Focus();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        protected void StartDialoguePanelMultiSelectFor(List<Microsoft.Msagl.Drawing.IViewerNode> nodes)
        {
            if (nodes != null && nodes.Count > 0)
            {
                // create job
                var stat = new DialogueStatus() { Type = DialogueType.MultiSelect };

                // set the action
                stat.Action = (st, action) =>
                {
                    // correct?
                    if (action == "DELETE-ALL")
                    {
                        StartDialoguePanelDelete();
                        return;
                    }
                };

                // in any case, create a node
                ShowDialoguePanel(stat);
            }
        }

        protected void StartDialoguePanelDelete()
        {
            // create job
            var stat = new DialogueStatus() { Type = DialogueType.Delete };

            var test = (_objectUnderCursor as Microsoft.Msagl.Drawing.IViewerNode)?
                            .Node?.UserData as Aas.IReferable;
            if (test != null)
                stat.Nodes.Add(test);
            stat.Nodes.AddRange(GetSelectedViewerReferables());

            // set the action
            stat.Action = (st, action) =>
            {
                // all above nodes
                foreach (var node in st.Nodes)
                {
                    // only SME
                    if (!(node is Aas.ISubmodelElement nodeSmeToDel))
                        continue;

                    // find containing Referable and KeyList to it
                    // (the key list must not only contain the Submodel!)
                    var contToDelIn = _submodel?.FindContainingReferable(nodeSmeToDel);
                    var kl = _submodel?.BuildKeysToTop(nodeSmeToDel);
                    if (nodeSmeToDel == null || contToDelIn == null || kl.Count < 2)
                        continue;

                    // build reference to it
                    var refToNode = new Aas.Reference(Aas.ReferenceTypes.ModelReference, kl);

                    // now search recursively for:
                    // the node, all RefElems and RelElems referring to it
                    var toDel = new List<Tuple<Aas.IReferable, Aas.ISubmodelElement>>();
                    _submodel?.RecurseOnSubmodelElements(null, (o, parents, sme) => {

                        // figure out the last parent = container of SME
                        Aas.IReferable cont = (parents.Count < 1) ? _submodel : parents.LastOrDefault();

                        // note: trust, that corresponding Remove() will check first for presence ..
                        if ((sme == nodeSmeToDel)
                            || (sme is Aas.ReferenceElement refEl
                                && refEl.Value?.Matches(refToNode) == true)
                            || (sme is Aas.RelationshipElement relEl
                                && (relEl.First?.Matches(refToNode) == true
                                    || relEl.Second?.Matches(refToNode) == true)))
                        {
                            toDel.Add(new Tuple<Aas.IReferable, Aas.ISubmodelElement>(cont, sme));
                        }

                        // always search further
                        return true;
                    });

                    // now del
                    foreach (var td in toDel)
                        td.Item1?.Remove(td.Item2);

                }

                // refresh
                SetNeedsFinalize(true);
                RedrawGraph();
            };

            // in any case, create a node
            ShowDialoguePanel(stat);
        }

        protected void StartDialoguePanelNoNodesFor()
        {
            // create job
            var stat = new DialogueStatus() { Type = DialogueType.NoNodes };

            // set the action
            stat.Action = (st, action) =>
            {
                // correct?
                if (action == "CREATE")
                {
                    StartDialoguePanelCreateFor(null);
                    return;
                }
            };

            // in any case, create a node
            ShowDialoguePanel(stat);
        }
    }
}
