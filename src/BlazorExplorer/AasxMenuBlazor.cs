﻿/*
Copyright (c) 2018-2021 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using AasxIntegrationBase;
using AdminShellNS;
using AnyUi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BlazorUI
{
    /// <summary>
    /// This class "converts" the AasxMenu struncture into Blazor menues
    /// </summary>
    public class AasxMenuBlazor
    {
        //
        // Private
        //
        protected DoubleSidedDict<string, AasxMenuItem> _menuItems
            = new DoubleSidedDict<string, AasxMenuItem>();

        protected DoubleSidedDict<AasxMenuItem, object> _blazorItems
            = new DoubleSidedDict<AasxMenuItem, object>();

        public AasxMenu Menu { get => _menu; }
        private AasxMenu _menu = new AasxMenu();

        private void RenderItemCollection(
            AasxMenu topMenu, AasxMenu menuItems,
            object blazorItems,
            CommandBindingCollection cmdBindings = null,
            InputBindingCollection inputBindings = null,
            object kgConv = null)
        {
        }

        public AnyUiLambdaActionBase HandleGlobalKeyDown(KeyEventArgs e, bool preview)
        {
            //// access
            //if (e == null || Menu == null)
            //    return null;

            //var kgConv = new KeyGestureConverter();

            //foreach (var mi in Menu.FindAll<AasxMenuItemHotkeyed>())
            //{
            //    if (mi.InputGesture == null)
            //        continue;
            //    var g = kgConv.ConvertFromInvariantString(mi.InputGesture) as KeyGesture;
            //    if (g != null
            //        && g.Key == e.Key
            //        && g.Modifiers == Keyboard.Modifiers)
            //    {
            //        var ticket = new AasxMenuActionTicket();
            //        mi.Action?.Invoke(mi.Name, mi, ticket);
            //        return ticket.UiLambdaAction;
            //    }
            //}

            return null;
        }

        public void LoadAndRender(
            AasxMenu menuInfo,
            CommandBindingCollection cmdBindings = null,
            InputBindingCollection inputBindings = null)
        {
            _menu = menuInfo;
            _menuItems.Clear();
            _blazorItems.Clear();

            object kgConv = null; // new KeyGestureConverter();

            // RenderItemCollection(menuInfo, menuInfo, wpfMenu.Items, cmdBindings, inputBindings, kgConv);
        }

        public bool IsChecked(string name)
        {
            var wpf = _blazorItems.Get2OrDefault(_menuItems.Get2OrDefault(name?.Trim().ToLower()));
            if (wpf != null)
                return false; // wpf.IsChecked;
            return false;
        }

        public void SetChecked(string name, bool state)
        {
            var wpf = _blazorItems.Get2OrDefault(_menuItems.Get2OrDefault(name?.Trim().ToLower()));
            //if (wpf != null)
            //    wpf.IsChecked = state;
        }
    }
}