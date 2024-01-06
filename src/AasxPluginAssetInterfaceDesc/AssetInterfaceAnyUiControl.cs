﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AasxIntegrationBase;
using Aas = AasCore.Aas3_0;
using AdminShellNS;
using Extensions;
using AnyUi;
using Newtonsoft.Json;
using AasxPredefinedConcepts;
using AasxPredefinedConcepts.AssetInterfacesDescription;
using System.Windows.Shapes;
using AasxIntegrationBaseGdi;
using FluentModbus;
using System.Net;

namespace AasxPluginAssetInterfaceDescription
{
    public class AssetInterfaceAnyUiControl
    {
        #region Members
        //=============

        private LogInstance _log = new LogInstance();
        private AdminShellPackageEnv _package = null;
        private Aas.Submodel _submodel = null;
        private AssetInterfaceOptions _options = null;
        private PluginEventStack _eventStack = null;
        private AnyUiStackPanel _panel = null;
        private AasxPluginBase _plugin = null;

        private AidAllInterfaceStatus _allInterfaceStatus = null;

        protected AnyUiSmallWidgetToolkit _uitk = new AnyUiSmallWidgetToolkit();

        protected Dictionary<AidInterfaceTechnology, AnyUiBitmapInfo> _dictTechnologyToBitmap = 
            new Dictionary<AidInterfaceTechnology, AnyUiBitmapInfo>();

        #endregion

        #region Members to be kept for state/ update
        //=============

        protected double _lastScrollPosition = 0.0;

        protected int _selectedLangIndex = 0;
        protected string _selectedLangStr = null;

        #endregion

        #region Constructors
        //=============

        // ReSharper disable EmptyConstructor
        public AssetInterfaceAnyUiControl()
        {
        }
        // ReSharper enable EmptyConstructor

        public void Start(
            LogInstance log,
            AdminShellPackageEnv thePackage,
            Aas.Submodel theSubmodel,
            AssetInterfaceOptions theOptions,
            PluginEventStack eventStack,
            AnyUiStackPanel panel,
            AasxPluginBase plugin,
            AidAllInterfaceStatus ifxStatus)
        {
            // internal members
            _log = log;
            _package = thePackage;
            _submodel = theSubmodel;
            _options = theOptions;
            _eventStack = eventStack;
            _panel = panel;
            _plugin = plugin;
            _allInterfaceStatus = ifxStatus;

            // some required logos
            _dictTechnologyToBitmap = new Dictionary<AidInterfaceTechnology, AnyUiBitmapInfo>();
            if (OperatingSystem.IsWindowsVersionAtLeast(7))
            {
                _dictTechnologyToBitmap.Add(AidInterfaceTechnology.HTTP,
                AnyUiGdiHelper.CreateAnyUiBitmapFromResource(
                    "AasxPluginAssetInterfaceDesc.Resources.logo-http.png",
                    assembly: Assembly.GetExecutingAssembly()));
                _dictTechnologyToBitmap.Add(AidInterfaceTechnology.Modbus,
                    AnyUiGdiHelper.CreateAnyUiBitmapFromResource(
                        "AasxPluginAssetInterfaceDesc.Resources.logo-modbus.png",
                        assembly: Assembly.GetExecutingAssembly()));
                _dictTechnologyToBitmap.Add(AidInterfaceTechnology.MQTT,
                    AnyUiGdiHelper.CreateAnyUiBitmapFromResource(
                        "AasxPluginAssetInterfaceDesc.Resources.logo-mqqt.png",
                        assembly: Assembly.GetExecutingAssembly()));
            }

            // fill given panel
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        public static AssetInterfaceAnyUiControl FillWithAnyUiControls(
            LogInstance log,
            object opackage, object osm,
            AssetInterfaceOptions options,
            PluginEventStack eventStack,
            object opanel,
            AasxPluginBase plugin,
            AidAllInterfaceStatus ifxStatus)
        {
            // access
            var package = opackage as AdminShellPackageEnv;
            var sm = osm as Aas.Submodel;
            var panel = opanel as AnyUiStackPanel;
            if (package == null || sm == null || panel == null)
                return null;

            // the Submodel elements need to have parents
            sm.SetAllParents();

            // factory this object
            var aidCntl = new AssetInterfaceAnyUiControl();
            aidCntl.Start(log, package, sm, options, eventStack, panel, plugin, ifxStatus);

            // return shelf
            return aidCntl;
        }

        #endregion

        #region Display Submodel
        //=============

        private void RenderFullView(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
        {
            // test trivial access
            if (_options == null || _submodel?.SemanticId == null)
                return;

            // make sure for the right Submodel
            var foundRecs = new List<AssetInterfaceOptionsRecord>();
            foreach (var rec in _options.LookupAllIndexKey<AssetInterfaceOptionsRecord>(
                _submodel?.SemanticId?.GetAsExactlyOneKey()))
                foundRecs.Add(rec);

            // render
            RenderPanelOutside(view, uitk, foundRecs, package, sm);
        }

        protected void RenderPanelOutside(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            IEnumerable<AssetInterfaceOptionsRecord> foundRecs,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
        {
            // make an outer grid, very simple grid of two rows: header & body
            var outer = view.Add(uitk.AddSmallGrid(rows: 7, cols: 1, colWidths: new[] { "*" }));

            //
            // Bluebar
            //

            var bluebar = uitk.AddSmallGridTo(outer, 0, 0, 1, cols: 5, colWidths: new[] { "*", "#", "#", "#", "#" });

            bluebar.Margin = new AnyUiThickness(0);
            bluebar.Background = AnyUiBrushes.LightBlue;

            uitk.AddSmallBasicLabelTo(bluebar, 0, 0, margin: new AnyUiThickness(8, 6, 0, 6),
                foreground: AnyUiBrushes.DarkBlue,
                fontSize: 1.5f,
                setBold: true,
                content: $"Asset Interfaces Descriptions");

            //
            // Scroll area
            //

            // small spacer
            outer.RowDefinitions[1] = new AnyUiRowDefinition(2.0, AnyUiGridUnitType.Pixel);
            uitk.AddSmallBasicLabelTo(outer, 1, 0,
                fontSize: 0.3f,
                verticalAlignment: AnyUiVerticalAlignment.Top,
                content: "", background: AnyUiBrushes.White);

            // add the body, a scroll viewer
            outer.RowDefinitions[2] = new AnyUiRowDefinition(1.0, AnyUiGridUnitType.Star);
            var scroll = AnyUiUIElement.RegisterControl(
                uitk.AddSmallScrollViewerTo(outer, 2, 0,
                    horizontalScrollBarVisibility: AnyUiScrollBarVisibility.Disabled,
                    verticalScrollBarVisibility: AnyUiScrollBarVisibility.Visible,
                    flattenForTarget: AnyUiTargetPlatform.Browser, initialScrollPosition: _lastScrollPosition),
                (o) =>
                {
                    if (o is Tuple<double, double> positions)
                    {
                        _lastScrollPosition = positions.Item2;
                    }
                    return new AnyUiLambdaActionNone();
                });

            // content of the scroll viewer
            // need a stack panel to add inside
            var inner = new AnyUiStackPanel()
            {
                Orientation = AnyUiOrientation.Vertical,
                Margin = new AnyUiThickness(2)
            };
            scroll.Content = inner;

            if (foundRecs != null)
                foreach (var rec in foundRecs)
                    RenderPanelInner(inner, uitk, rec, package, sm);
        }

        #endregion

        #region Inner
        //=============

        protected void RenderPanelInner(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            AssetInterfaceOptionsRecord rec,
            AdminShellPackageEnv package,
            Aas.Submodel sm)
        {
            // access
            if (view == null || uitk == null || sm == null || rec == null)
                return;

            var grid = view.Add(uitk.AddSmallGrid(rows: 3, cols: 2, colWidths: new[] { "110:", "*" }));

            uitk.AddSmallLabelTo(grid, 0, 0, content: "Debug:");
            
            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(grid, 0, 1,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Create status items"),
                (o) =>
                {
                    try
                    {
                        // build up data structures
                        _allInterfaceStatus.InterfaceStatus = PrepareAidInformation(sm);

                        // trigger a complete redraw, as the regions might emit 
                        // events or not, depending on this flag
                        return new AnyUiLambdaActionPluginUpdateAnyUi()
                        {
                            PluginName = _plugin?.GetPluginName(),
                            UpdateMode = AnyUiRenderMode.All,
                            UseInnerGrid = true
                        };
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                    return new AnyUiLambdaActionNone();
                });

            AnyUiUIElement.RegisterControl(
                uitk.AddSmallButtonTo(grid, 1, 1,
                    margin: new AnyUiThickness(2), setHeight: 21,
                    padding: new AnyUiThickness(2, 0, 2, 0),
                    content: "Single update .."),
                (o) =>
                {
                    try
                    {
                        //var client = new ModbusTcpClient();
                        //client.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5020));
                        //var byteData = client.ReadHoldingRegisters<byte>(99, 1, 8);

                        _allInterfaceStatus?.UpdateValuesSingleShot();

                        // trigger a complete redraw, as the regions might emit 
                        // events or not, depending on this flag
                        return new AnyUiLambdaActionPluginUpdateAnyUi()
                        {
                            PluginName = _plugin?.GetPluginName(),
                            UpdateMode = AnyUiRenderMode.All,
                            UseInnerGrid = true
                        };
                    }
                    catch (Exception ex)
                    {
                        ;
                    }
                    return new AnyUiLambdaActionNone();
                });

            // get SM data and             
            RenderTripleRowData(view, uitk, _allInterfaceStatus.InterfaceStatus);
        }

        #endregion

        #region Update
        //=============

        public void Update(params object[] args)
        {
            // check args
            if (args == null || args.Length < 1
                || !(args[0] is AnyUiStackPanel newPanel))
                return;

            // ok, re-assign panel and re-display
            _panel = newPanel;
            _panel.Children.Clear();

            // the default: the full shelf
            RenderFullView(_panel, _uitk, _package, _submodel);
        }

        #endregion

        #region Interface items
        //=====================

        protected List<AidInterfaceStatus> PrepareAidInformation(Aas.Submodel sm)
        {
            // access
            var res = new List<AidInterfaceStatus>();
            if (sm == null)
                return res;

            // get data
            var data = new AasxPredefinedConcepts.AssetInterfacesDescription.CD_AssetInterfacesDescription();
            PredefinedConceptsClassMapper.ParseAasElemsToObject(_submodel, data);

            // prepare
            foreach (var tech in AdminShellUtil.GetEnumValues<AidInterfaceTechnology>())
            {
                var ifxs = data?.InterfaceHTTP;
                if (tech == AidInterfaceTechnology.Modbus) ifxs = data?.InterfaceMODBUS;
                if (tech == AidInterfaceTechnology.MQTT) ifxs = data?.InterfaceMQTT;
                if (ifxs == null || ifxs.Count < 1)
                    continue;
                foreach (var ifx in ifxs)
                {
                    // new interface
                    var dn = AdminShellUtil.TakeFirstContent(ifx.Title, ifx.__Info__?.Referable?.IdShort);
                    var aidIfx = new AidInterfaceStatus()
                    {
                        Technology = tech,
                        DisplayName = $"{dn}",
                        Info = $"{ifx.EndpointMetadata?.Base}",
                        EndpointBase = "" + ifx.EndpointMetadata?.Base,
                        Tag = ifx
                    };
                    res.Add(aidIfx);

                    // Properties .. lambda recursion
                    Action<string, CD_PropertyName> recurseProp = null;
                    recurseProp = (location, propName) =>
                    {
                        // add item
                        var ifcItem = new AidIfxItemStatus() {
                            Kind = AidIfxItemKind.Property,
                            Location = location,
                            DisplayName = AdminShellUtil.TakeFirstContent(
                                propName.Title, propName.Key, propName.__Info__?.Referable?.IdShort),
                            FormData = propName.Forms,
                            Value = "???"
                        };
                        aidIfx.Items.Add(ifcItem);

                        // directly recurse?
                        if (propName?.Properties?.Property != null)
                            foreach (var child in propName.Properties.Property)
                                recurseProp(location + " . " + ifcItem.DisplayName, child);
                    };

                    if (ifx.InterfaceMetadata?.Properties?.Property == null)
                        continue;
                    foreach (var propName in ifx.InterfaceMetadata?.Properties?.Property)
                        recurseProp("\u2302", propName);
                }
            }

            return res;
        }

        protected void RenderTripleRowData(
            AnyUiStackPanel view, AnyUiSmallWidgetToolkit uitk,
            List<AidInterfaceStatus> interfaces)
        {
            // access
            if (interfaces == null)
                return;

            // ok
            var grid = view.Add(uitk.AddSmallGrid(rows: interfaces.Count, cols: 5, colWidths: new[] { "40:", "1*", "1*", "1*", "100:" }));
            int rowIndex = 0;
            foreach (var ifx in interfaces)
            {
                // heading
                grid.RowDefinitions.Add(new AnyUiRowDefinition());

                var headGrid = uitk.Set(
                    uitk.AddSmallGridTo(grid, rowIndex++, 0,
                        rows: 1, cols: 3, colWidths: new[] { "#", "#", "#" },
                        margin: new AnyUiThickness(0, 8, 0, 4)),
                    colSpan: 5);

                if (_dictTechnologyToBitmap.ContainsKey(ifx.Technology))
                    uitk.AddSmallImageTo(headGrid, 0, 0, 
                        margin: new AnyUiThickness(0, 0, 10, 0),
                        bitmap: _dictTechnologyToBitmap[ifx.Technology]);

                uitk.AddSmallBasicLabelTo(headGrid, 0, 1, fontSize: 1.2f, setBold: true,
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: ifx.DisplayName);

                uitk.AddSmallBasicLabelTo(headGrid, 0, 2, fontSize: 1.2f,
                    margin: new AnyUiThickness(10, 0, 0, 0),
                    verticalAlignment: AnyUiVerticalAlignment.Center,
                    verticalContentAlignment: AnyUiVerticalAlignment.Center,
                    content: ifx.Info);

                // items?
                if (ifx.Items != null)
                    foreach (var item in ifx.Items)
                    {
                        // normal row, 3 bordered cells
                        grid.RowDefinitions.Add(new AnyUiRowDefinition());
                        var cols = new[] { 
                            "Prop.", item.Location, item.DisplayName, "" + item.FormData?.Href, item.Value };
                        for (int ci = 0; ci < 5; ci++)
                        {
                            var brd = uitk.AddSmallBorderTo(grid, rowIndex, ci,
                                margin: (ci == 0) ? new AnyUiThickness(0, -1, 0, 0) 
                                                  : new AnyUiThickness(-1, -1, 0, 0),                                
                                borderThickness: new AnyUiThickness(1.0), borderBrush: AnyUiBrushes.DarkGray);
                            brd.Child = new AnyUiSelectableTextBlock()
                            {
                                Text = cols[ci],
                                Padding = new AnyUiThickness(3, 1, 3, 1),
                                FontSize = 1.0f,
                                FontWeight = AnyUiFontWeight.Normal
                            };
                        }
                        rowIndex++;
                    }                
            }
        }

        #endregion

        #region Callbacks
        //===============


        #endregion

        #region Utilities
        //===============


        #endregion
    }
}
