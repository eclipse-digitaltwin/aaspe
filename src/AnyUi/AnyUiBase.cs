﻿/*
Copyright (c) 2018-2023 Festo SE & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

// Quite declarative approach for the future
// resharper disable UnassignedField.Global
// resharper disable ClassNeverInstantiated.Global

// This namespace implements an UI approach which can be implemented by any UI system, hence the name.
// For better PascalCasing, the 'i' is lowercase by intention. In written text, it shall be: "Any UI"
namespace AnyUi
{
    //
    // required Enums and helper classes
    //

    /// <summary>
    /// AnyUI can be rendered to different platforms
    /// </summary>
    public enum AnyUiTargetPlatform { None = 0, Wpf = 1, Browser = 2 }

    public enum AnyUiGridUnitType { Auto = 0, Pixel = 1, Star = 2 }

    public enum AnyUiHorizontalAlignment { Left = 0, Center = 1, Right = 2, Stretch = 3 }
    public enum AnyUiVerticalAlignment { Top = 0, Center = 1, Bottom = 2, Stretch = 3 }

    public enum AnyUiOrientation { Horizontal = 0, Vertical = 1 }

    public enum AnyUiScrollBarVisibility { Disabled = 0, Auto = 1, Hidden = 2, Visible = 3 }

    public enum AnyUiTextWrapping { WrapWithOverflow = 0, NoWrap = 1, Wrap = 2 }

    public enum AnyUiFontWeight { Normal = 0, Bold = 1 }

    public class AnyUiGridLength
    {
        public double Value = 1.0;
        public AnyUiGridUnitType Type = AnyUiGridUnitType.Auto;

        public static AnyUiGridLength Auto { get { return new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto); } }

        public AnyUiGridLength() { }

        public AnyUiGridLength(double value, AnyUiGridUnitType type = AnyUiGridUnitType.Auto)
        {
            this.Value = value;
            this.Type = type;
        }
    }

    public class AnyUiListOfGridLength : List<AnyUiGridLength>
    {
        public static AnyUiListOfGridLength Parse(string[] input)
        {
            // access
            if (input == null)
                return null;

            var res = new AnyUiListOfGridLength();
            foreach (var part in input)
            {
                // default
                var gl = new AnyUiGridLength(1.0, AnyUiGridUnitType.Star);

                // work on part of input
                double scale = 1.0;
                var kind = part.Trim();
                var m = Regex.Match(kind, @"([0-9.+-]+)(.$)");
                if (m.Success && m.Groups.Count >= 2)
                {
                    var scaleSt = m.Groups[1].ToString().Trim();
                    if (Double.TryParse(scaleSt, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        scale = d;
                    kind = m.Groups[2].ToString().Trim();
                }
                if (kind == "#")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Auto);
                if (kind == "*")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Star);
                if (kind == ":")
                    gl = new AnyUiGridLength(scale, AnyUiGridUnitType.Pixel);

                // add
                res.Add(gl);
            }

            return res;
        }
    }

    public class AnyUiColumnDefinition
    {
        public AnyUiGridLength Width;
        public double? MinWidth, MaxWidth;
    }

    public class AnyUiRowDefinition
    {
        public AnyUiGridLength Height;
        public double? MinHeight;

        public AnyUiRowDefinition() { }

        public AnyUiRowDefinition(
            double value, AnyUiGridUnitType type = AnyUiGridUnitType.Auto,
            double? minHeight = null)
        {
            Height = new AnyUiGridLength(value, type);
            MinHeight = minHeight;
        }
    }

    public class AnyUiColor
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        public Single ScA { get { return A / 255.0f; } set { A = (byte)(255.0f * value); } }
        public Single ScR { get { return R / 255.0f; } set { R = (byte)(255.0f * value); } }
        public Single ScG { get { return G / 255.0f; } set { G = (byte)(255.0f * value); } }
        public Single ScB { get { return B / 255.0f; } set { B = (byte)(255.0f * value); } }

        public AnyUiColor()
        {
            A = 0xff;
        }

        public AnyUiColor(UInt32 c)
        {
            byte[] bytes = BitConverter.GetBytes(c);
            A = bytes[3];
            R = bytes[2];
            G = bytes[1];
            B = bytes[0];
        }

        public static AnyUiColor FromArgb(byte a, byte r, byte g, byte b)
        {
            var res = new AnyUiColor();
            res.A = a;
            res.R = r;
            res.G = g;
            res.B = b;
            return res;
        }

        public static AnyUiColor FromRgb(byte r, byte g, byte b)
        {
            var res = new AnyUiColor();
            res.A = 0xff;
            res.R = r;
            res.G = g;
            res.B = b;
            return res;
        }

        public static AnyUiColor FromString(string st)
        {
            if (st == null || !st.StartsWith("#") || (st.Length != 7 && st.Length != 9))
                return AnyUiColors.Default;
            UInt32 ui = 0;
            if (st.Length == 9)
                ui = Convert.ToUInt32(st.Substring(1), 16);
            if (st.Length == 7)
                ui = 0xff000000u | Convert.ToUInt32(st.Substring(1), 16);
            return new AnyUiColor(ui);
        }

        public string ToHtmlString(int format)
        {
            if (format == 1)
                // ARGB
                return $"#{A:X2}{R:X2}{G:X2}{B:X2}";

            if (format == 2)
                // ARGB
                return FormattableString.Invariant($"rgba({R},{G},{B},{(A / 255.0):0.###})");

            // default just RGB
            return $"#{R:X2}{G:X2}{B:X2}";
        }

        public static AnyUiColor Blend(AnyUiColor c0, AnyUiColor c1, double level)
        {
            // access
            if (c0 == null || c1 == null)
                return AnyUiColors.Transparent;

            var level1 = Math.Max(0.0, Math.Min(1.0, level));
            var level0 = 1.0 - level1;

            return AnyUiColor.FromArgb(
                Convert.ToByte(level0 * c0.A + level1 * c1.A),
                Convert.ToByte(level0 * c0.R + level1 * c1.R),
                Convert.ToByte(level0 * c0.G + level1 * c1.G),
                Convert.ToByte(level0 * c0.B + level1 * c1.B)
            );
        }

        public Single Blackness()
        {
            return ScA * (1.0f - 0.3333f * (ScR + ScG + ScB));
        }
    }

    public class AnyUiColors
    {
        public static AnyUiColor Default { get { return new AnyUiColor(0xff000000u); } }
        public static AnyUiColor Transparent { get { return new AnyUiColor(0x00000000u); } }
        public static AnyUiColor Black { get { return new AnyUiColor(0xff000000u); } }
        public static AnyUiColor DarkBlue { get { return new AnyUiColor(0xff00008bu); } }
        public static AnyUiColor LightBlue { get { return new AnyUiColor(0xffadd8e6u); } }
        public static AnyUiColor Blue { get { return new AnyUiColor(0xff0000ffu); } }
        public static AnyUiColor Green { get { return new AnyUiColor(0xff00ff00u); } }
        public static AnyUiColor Orange { get { return new AnyUiColor(0xffffa500u); } }
        public static AnyUiColor White { get { return new AnyUiColor(0xffffffffu); } }
    }

    public class AnyUiBrush
    {
        private AnyUiColor solidColorBrush = AnyUiColors.Black;

        public AnyUiColor Color { get { return solidColorBrush; } }

        public AnyUiBrush() { }

        public AnyUiBrush(AnyUiColor c)
        {
            solidColorBrush = c;
        }

        public AnyUiBrush(UInt32 c)
        {
            solidColorBrush = new AnyUiColor(c);
        }

        public AnyUiBrush(string st)
        {
            solidColorBrush = AnyUiColor.FromString(st);   
        }

        public string HtmlRgb()
        {
            return "rgb(" +
                solidColorBrush.R + ", " +
                solidColorBrush.G + ", " +
                solidColorBrush.B + ")";
        }

        public string HtmlRgba()
        {
            return "rgba(" +
                solidColorBrush.R + ", " +
                solidColorBrush.G + ", " +
                solidColorBrush.B + ", " +
                string.Format(CultureInfo.InvariantCulture, "{0:0.00}", 1.0 * solidColorBrush.A / 255.0) + ")";
        }        
    }

    public class AnyUiBrushes
    {
        public static AnyUiBrush Default { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush Transparent { get { return new AnyUiBrush(0x00000000u); } }
        public static AnyUiBrush Black { get { return new AnyUiBrush(0xff000000u); } }
        public static AnyUiBrush DarkBlue { get { return new AnyUiBrush(0xff0128CB); } }
        public static AnyUiBrush LightBlue { get { return new AnyUiBrush(0xffC0CCFF); } }
        public static AnyUiBrush Yellow { get { return new AnyUiBrush(0xffffcc00); } }
        public static AnyUiBrush Red { get { return new AnyUiBrush(0xffe53d02); } }
        public static AnyUiBrush White { get { return new AnyUiBrush(0xffffffffu); } }
        public static AnyUiBrush LightGray { get { return new AnyUiBrush(0xffe8e8e8u); } }
        public static AnyUiBrush MiddleGray { get { return new AnyUiBrush(0xffc8c8c8u); } }
        public static AnyUiBrush DarkGray { get { return new AnyUiBrush(0xff808080u); } }
    }

    public class AnyUiBrushTuple
    {
        public AnyUiBrush Bg, Fg;

        public AnyUiBrushTuple() { }

        public AnyUiBrushTuple(AnyUiBrush bg, AnyUiBrush fg)
        {
            this.Bg = bg;
            this.Fg = fg;
        }
    }

    public class AnyUiThickness
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }

        public AnyUiThickness() { }

        public AnyUiThickness(double all)
        {
            Left = all; Top = all; Right = all; Bottom = all;
        }

        public AnyUiThickness(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        // ReSharper disable CompareOfFloatsByEqualityOperator
        public bool AllEqual =>
            Left == Top
            && Left == Right
            && Left == Bottom;
        // ReSharper enable CompareOfFloatsByEqualityOperator

        public bool AllZero => AllEqual && Left == 0.0;

        public double Width => Left + Right;
    }

    public enum AnyUiVisibility : byte
    {
        Visible = 0,
        Hidden = 1,
        Collapsed = 2
    }

    public enum AnyUiStretch
    {
        None,
        Fill,
        Uniform,
        UniformToFill
    }

    public struct AnyUiPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        public AnyUiPoint(double x, double y) { X = x; Y = y; }
    }

    public struct AnyUiRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double X2 => X + Width;
        public double Y2 => Y + Height;

        public AnyUiRect(AnyUiRect other)
        {
            X = other.X;
            Y = other.Y;
            Width = other.Width;
            Height = other.Height;
        }

        public AnyUiRect(double x, double y, double w, double h)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
        }

        public static AnyUiRect Max(AnyUiRect r1, AnyUiRect r2)
        {
            var x0 = Math.Min(r1.X, r2.X);
            var y0 = Math.Min(r1.Y, r2.Y);

            var x2 = Math.Max(r1.X2, r2.X2);
            var y2 = Math.Max(r1.Y2, r2.Y2);

            return new AnyUiRect(x0, y0, x2 - x0, y2 - y0);
        }
    }

    public class AnyUiPointCollection : List<AnyUiPoint>
    {
        public AnyUiPoint FindCG()
        {
            if (this.Count < 1)
                return new AnyUiPoint(0, 0);

            AnyUiPoint sum = new AnyUiPoint(0, 0);
            foreach (var p in this)
            {
                sum.X += p.X;
                sum.Y += p.Y;
            }

            sum.X /= this.Count;
            sum.Y /= this.Count;

            return sum;
        }

        public AnyUiRect FindBoundingBox()
        {
#if __old_implementation_not_sure_if_works
            var res = new AnyUiRect()
            {
                X = double.MaxValue,
                Y = double.MaxValue
            };

            foreach (var p in this)
            {
                if (p.X < res.X)
                    res.X = p.Y;
                if (p.Y < res.Y)
                    res.Y = p.Y;

                if (p.X > res.X + res.Width)
                    res.Width = p.X - res.X;
                if (p.Y > res.Y + res.Height)
                    res.Height = p.Y - res.Y;
            }
            
            return res;
#else
            // above way seems not to work properly, therefore
            // substitute with brute force one

            if (this.Count < 1)
                return new AnyUiRect(0, 0, 0, 0);
            
            double minX = double.MaxValue;
            double maxX = double.MinValue;
            double minY = double.MaxValue;
            double maxY = double.MinValue;

            foreach (var p in this)
            {
                if (p.X < minX)
                    minX = p.X;
                if (p.X > maxX)
                    maxX = p.X;
                if (p.Y < minY)
                    minY = p.Y;
                if (p.Y > maxY)
                    maxY = p.Y;
            }

            return new AnyUiRect()
            {
                X = minX, Y = minY,
                Width = Math.Max(0, maxX - minX),
                Height = Math.Max(0, maxY - minY)
            };
#endif
        }
    }

    //
    // bridge objects between AnyUI base classes and implementations
    //

    public class AnyUiDisplayDataBase
    {
        /// <summary>
        /// Initiates a drop operation with one ore more files given by filenames.
        /// </summary>
        public virtual void DoDragDropFiles(AnyUiUIElement elem, string[] files) { }
    }

    //
    // Handling of events, callbacks and such
    //

    /// <summary>
    /// Any UI defines a lot of lambda functions for handling events.
    /// These lambdas can return results (commands) to the calling application,
    /// which might be given back to higher levels of the applications to trigger
    /// events, such as re-displaying pages or such.
    /// </summary>
    public class AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// An lambda action explicitely stating that nothing has to be done.
    /// </summary>
    public class AnyUiLambdaActionNone : AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// Value of the AnyUI control were changed
    /// </summary>
    public class AnyUiLambdaActionContentsChanged : AnyUiLambdaActionBase
    {
    }

    /// <summary>
    /// Changed values shall be taken over to main application
    /// </summary>
    public class AnyUiLambdaActionContentsTakeOver : AnyUiLambdaActionBase
    {
    }

    public enum AnyUiRenderMode { All, StatusToUi }

    /// <summary>
    /// This event causes a call to the specified plugin to update its
    /// already preseted AnyUi representation and will then re-render this
    /// to the users interface.
    /// The plugin action is supposed to be: "update-anyui-visual-extension"
    /// </summary>
    public class AnyUiLambdaActionPluginUpdateAnyUi : AnyUiLambdaActionBase
    {
        public string PluginName = "";
        public object[] ActionArgs = null;
        public AnyUiRenderMode UpdateMode = AnyUiRenderMode.All;
        public bool UseInnerGrid = false;
    }

    /// <summary>
    /// This event causes to call again the <c>RenderPanel</c> lambda of
    /// a <c>AnyUiDialogueDataModalPanel</c>. This could be used to adopt
    /// the modal panel depending on data.
    /// </summary>
    public class AnyUiLambdaActionModalPanelReRender : AnyUiLambdaActionBase
    {
        public AnyUiDialogueDataModalPanel DiaDataPanel;

        public AnyUiLambdaActionModalPanelReRender(AnyUiDialogueDataModalPanel diaDataPanel)
        {
            DiaDataPanel = diaDataPanel;
        }
    }

	/// <summary>
	/// ReRender the main enitity panel
	/// </summary>
	public class AnyUiLambdaActionEntityPanelReRender : AnyUiLambdaActionBase
	{
        public AnyUiRenderMode Mode = AnyUiRenderMode.StatusToUi;
        public bool UseInnerGrid = false;
        public Dictionary<AnyUiUIElement, bool> UpdateElemsOnly = null;

		public AnyUiLambdaActionEntityPanelReRender(AnyUiRenderMode mode, bool useInnerGrid = false,
			Dictionary<AnyUiUIElement, bool> updateElemsOnly = null)
		{
            Mode = mode;
            UseInnerGrid = useInnerGrid;
            UpdateElemsOnly = updateElemsOnly;
		}
	}

	/// <summary>
	/// Requests the main application to display a content file or external link
	/// </summary>
	public class AnyUiLambdaActionDisplayContentFile : AnyUiLambdaActionBase
    {
        public AnyUiLambdaActionDisplayContentFile() { }
        public AnyUiLambdaActionDisplayContentFile(
            string fn, string mimeType = null, bool preferInternalDisplay = false)
        {
            this.fn = fn;
            this.mimeType = mimeType;
            this.preferInternalDisplay = preferInternalDisplay;
        }

        public string fn = null;
        public string mimeType = null;
        public bool preferInternalDisplay = false;
    }

    /// <summary>
    /// Request to redraw the current element/ entity.
    /// </summary>
    public class AnyUiLambdaActionRedrawEntity : AnyUiLambdaActionBase { }

    /// <summary>
    /// Reqeust to redraw the full tree of elements, may set new focus or
    /// expand state.
    /// </summary>
    public class AnyUiLambdaActionRedrawAllElementsBase : AnyUiLambdaActionBase
    {
        public object NextFocus = null;
        public bool? IsExpanded = null;
        public bool OnlyReFocus = false;
        public bool RedrawCurrentEntity = false;
    }

	/// <summary>
	/// Request to re-index all Identifiables.
	/// </summary>
	public class AnyUiLambdaActionReIndexIdentifiables : AnyUiLambdaActionBase { }

	/// <summary>
	/// This class is the base class for event handlers, which can attached to special
	/// events of Any UI controls
	/// </summary>
	public class AnyUiSpecialActionBase
    {
    }

    /// <summary>
    /// Special action for showing context menu and consequently executing 
    /// lambda associated with that menu.
    /// </summary>
    public class AnyUiSpecialActionContextMenu : AnyUiSpecialActionBase
    {
        public string Caption = "Context menu";
        public string[] MenuItemHeaders;
        [JsonIgnore]
        public Func<object, AnyUiLambdaActionBase> MenuItemLambda;
        public Func<object, Task<AnyUiLambdaActionBase>> MenuItemLambdaAsync;

        public int ResultIndex = -1;

        public AnyUiSpecialActionContextMenu() { }

        public AnyUiSpecialActionContextMenu(
            string[] menuItemHeaders,
            Func<object, AnyUiLambdaActionBase> menuItemLambda,
            Func<object, Task<AnyUiLambdaActionBase>> menuItemLambdaAsync)
        {
            MenuItemHeaders = menuItemHeaders;
            MenuItemLambda = menuItemLambda;
            MenuItemLambdaAsync = menuItemLambdaAsync;
        }
    }

    /// <summary>
    /// Special action for setting values/ executing button actions
    /// </summary>
    public class AnyUiSpecialActionSetValue : AnyUiSpecialActionBase
    {
        public AnyUiUIElement UiElement;
        public object Argument;

        public AnyUiSpecialActionSetValue(AnyUiUIElement uiElement, object argument)
        {
            UiElement = uiElement;
            Argument = argument;
        }
    }

    //
    // Hierarchy of AnyUI graphical elements (including controls).
    // This hierarchy stems from the WPF hierarchy but should be sufficiently 
    // abstracted in order to be implemented an many UI systems
    //

    /// <summary>
    /// Absolute base class of all AnyUI graphical elements
    /// </summary>
    public class AnyUiUIElement
    {
        // these attributes are typically managed by the (automatic) layout
        // exception: shapes
        public double X, Y, Width, Height;

        // these attributes are managed by the Grid.SetRow.. functions
        public int? GridRow, GridRowSpan, GridColumn, GridColumnSpan;

        /// <summary>
        /// Serves as alpha-numeric name to later bind specific implementations to it
        /// </summary>
        public string Name = null;

        /// <summary>
        /// If true, can be skipped when rendered into a browser
        /// </summary>
        public AnyUiTargetPlatform SkipForTarget = AnyUiTargetPlatform.None;

        /// <summary>
        /// This onjects builds the bridge to the specific implementation, e.g., WPF.
        /// The specific implementation overloads AnyUiDisplayDataBase to be able to store specific data
        /// per UI element, such as the WPF UIElement.
        /// </summary>
        public AnyUiDisplayDataBase DisplayData;

        /// <summary>
        /// Stores the original (initial) value in string representation.
        /// </summary>
        public object originalValue = null;

        /// <summary>
        /// This callback (lambda) is activated by the control frequently, if a value update
        /// occurs. The calling application needs to set this lambda in order to receive
        /// value updates.
        /// Note: currently, the function result (the lambda) is being ignored except for
        /// Buttons
        /// </summary>
        [JsonIgnore]
        public Func<object, AnyUiLambdaActionBase> setValueLambda = null;

        /// <summary>
        /// This callback (lambda) is activated by the control frequently, if a value update
        /// occurs. The calling application needs to set this lambda in order to receive
        /// value updates.
        /// Note: async variant; as of today, ONLY IMPLEMENTED SPARSELY!!
        /// </summary>
        public Func<object, Task<AnyUiLambdaActionBase>> setValueAsyncLambda = null;

        /// <summary>
        /// Arbitrary object/ tag exclusively used for ad-hoc debug. Do not use for long-term
        /// purposes.
        /// </summary>
        [JsonIgnore]
        public object DebugTag = null;

        /// <summary>
        /// If not null, this lambda result is automatically emitted as outside action,
        /// when the control "feels" to have a "final" selection (Enter, oder ComboBox selected)
        /// </summary>
        [JsonIgnore]
        public AnyUiLambdaActionBase takeOverLambda = null;

        /// <summary>
        /// Indicates, that the status of the element was updated
        /// </summary>
        public bool Touched;

        /// <summary>
        /// Touches the element
        /// </summary>
        public virtual void Touch() { Touched = true; }

        /// <summary>
        /// This function attaches the above lambdas accordingly to a given user control.
        /// It is to be used, when an abstract AnyUi... is being created and the according WPF element
        /// will be activated later.
        /// Note: use of this is for legacy reasons; basically the class members can be used directly
        /// </summary>
        /// <param name="cntl">User control (passed thru)</param>
        /// <param name="setValue">Lambda called, whenever the value is changed</param>
        /// <param name="takeOverLambda">Lambda called at the end of a modification</param>
        /// <returns>Passes thru the user control</returns>
        public static T RegisterControl<T>(
            T cntl,
            Func<object, AnyUiLambdaActionBase> setValue = null,
            Func<object, Task<AnyUiLambdaActionBase>> setValueAsync = null,
            AnyUiLambdaActionBase takeOverLambda = null)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // simply set lambdas
            cntl.setValueLambda = setValue;
            cntl.setValueAsyncLambda = setValueAsync;
            cntl.takeOverLambda = takeOverLambda;

            return cntl;
        }

        /// <summary>
        /// Allows setting the name for a control.
        /// </summary>
        /// <param name="cntl"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AnyUiUIElement NameControl(
            AnyUiUIElement cntl, string name)
        {
            // access
            if (cntl == null)
                return null;

            // set
            cntl.Name = name;
            return cntl;
        }

        /// <summary>
        /// Find all children in the (deep) hierarchy, which feature a Name != null.
        /// </summary>
        public IEnumerable<AnyUiUIElement> FindAllNamed()
        {
            if (this.Name != null)
                yield return this;

            if (this is IEnumerateChildren en)
                foreach (var child in en.GetChildren())
                    foreach (var x in child.FindAllNamed())
                        yield return x;
        }

        /// <summary>
        /// Find all children in the (deep) hierarchy, and invoke predicate.
        /// </summary>
        public IEnumerable<AnyUiUIElement> FindAll(Func<AnyUiUIElement, bool> predicate = null)
        {
            if (predicate == null || predicate.Invoke(this))
                yield return this;

            if (this is IEnumerateChildren en)
                foreach (var child in en.GetChildren())
                    foreach (var x in child.FindAll(predicate))
                        yield return x;
        }

        public static T SetIntFromControl<T>(
            T cntl, Action<int> setValue)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // de-tour set value lambda
            cntl.setValueLambda = (o) =>
            {
                if (o is int di)
                    setValue?.Invoke(di);
                else
                if (cntl is AnyUiComboBox cb)
                {
                    if (cb.SelectedIndex.HasValue)
                        setValue?.Invoke(cb.SelectedIndex.Value);
                }
                else
                {
                    if (o is string ostr)
                    {
                        if (int.TryParse(ostr, out var i))
                            setValue?.Invoke(i);
                        else
                            setValue?.Invoke(0);
                    }
                }
                return new AnyUiLambdaActionNone();
            };

            return cntl;
        }

        public static T SetDoubleFromControl<T>(
            T cntl, Action<double> setValue)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // de-tour set value lambda
            cntl.setValueLambda = (o) =>
            {
                if (o is string ostr)
                {
                    if (double.TryParse(ostr, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var i))
                        setValue?.Invoke(i);
                    else
                        setValue?.Invoke(0.0);
                }
                return new AnyUiLambdaActionNone();
            };

            return cntl;
        }

        public static T SetBoolFromControl<T>(
            T cntl, Action<bool> setValue)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // de-tour set value lambda
            cntl.setValueLambda = (o) =>
            {
                if (o is bool ob)
                    setValue?.Invoke(ob);
                return new AnyUiLambdaActionNone();
            };

            return cntl;
        }

        public static T SetStringFromControl<T>(
            T cntl, Action<string> setValue)
            where T : AnyUiUIElement
        {
            // access
            if (cntl == null)
                return null;

            // de-tour set value lambda
            cntl.setValueLambda = (o) =>
            {
                if (o is string ostr)
                    setValue?.Invoke(ostr);
                return new AnyUiLambdaActionNone();
            };

            return cntl;
        }

    }

    [Flags]
    public enum AnyUiEventMask
    {
        None = 0, LeftDown = 1, LeftDouble = 2, DragStart = 4,
        MouseAll = LeftDown + LeftDouble
    }

    public class AnyUiEventData
    {
        public AnyUiEventMask Mask;
        public int ClickCount;
        public object Source;
        public AnyUiPoint RelOrigin;

        public AnyUiEventData() { }

        public AnyUiEventData(AnyUiEventMask mask, object source, int clickCount = 1,
            AnyUiPoint? relOrigin = null)
        {
            Mask = mask;
            Source = source;
            ClickCount = clickCount;
            if (relOrigin.HasValue)
                RelOrigin = relOrigin.Value;
        }
    }

    public class AnyUiFrameworkElement : AnyUiUIElement
    {
        public AnyUiThickness Margin;
        public AnyUiVerticalAlignment? VerticalAlignment;
        public AnyUiHorizontalAlignment? HorizontalAlignment;

        public double? MinHeight;
        public double? MinWidth;
        public double? MaxHeight;
        public double? MaxWidth;

        public object Tag = null;

        public AnyUiEventMask EmitEvent;
    }

    /// <summary>
    /// This is the base class for all primitive shapes in AnyUI.
    /// In WPF this is a subclass of FrameworkElement.
    /// </summary>
    public class AnyUiShape : AnyUiFrameworkElement
    {
        public AnyUiBrush Fill, Stroke;
        public double? StrokeThickness;

        public virtual AnyUiRect FindBoundingBox() => new AnyUiRect();

        public virtual bool IsHit(AnyUiPoint pt) => false;
    }

    public class AnyUiRectangle : AnyUiShape
    {
        public override AnyUiRect FindBoundingBox() =>
            new AnyUiRect(X, Y, Width, Height);

        public override bool IsHit(AnyUiPoint pt) =>
            (pt.X >= this.X) && (pt.X <= this.X + this.Width)
            && (pt.Y >= this.Y) && (pt.Y <= this.Y + this.Height);
    }

    public class AnyUiEllipse : AnyUiShape
    {
        public override AnyUiRect FindBoundingBox() =>
            new AnyUiRect(X, Y, Width, Height);

        public override bool IsHit(AnyUiPoint pt)
        {
            // see: https://www.geeksforgeeks.org/check-if-a-point-is-inside-outside-or-on-the-ellipse/
            var h = this.X + this.Width / 2.0;
            var k = this.Y + this.Height / 2.0;
            var a = this.Width / 2.0;
            var b = this.Height / 2.0;

            int p = ((int)Math.Pow((pt.X - h), 2) /
                    (int)Math.Pow(a, 2)) +
                    ((int)Math.Pow((pt.Y - k), 2) /
                    (int)Math.Pow(b, 2));

            return p <= 1;
        }
    }

    public class AnyUiPolygon : AnyUiShape
    {
        public AnyUiPointCollection Points = new AnyUiPointCollection();

        public override AnyUiRect FindBoundingBox()
        {
            if (Points == null || Points.Count < 1)
                return new AnyUiRect();
            return Points.FindBoundingBox();
        }

        // see: https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon
        /// <summary>
        /// Determines if the given point is inside the polygon
        /// </summary>
        /// <param name="polygon">the vertices of polygon</param>
        /// <param name="testPoint">the given point</param>
        /// <returns>true if the point is inside the polygon; otherwise, false</returns>
        private static bool IsPointInPolygon4(AnyUiPoint[] polygon, AnyUiPoint testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            for (int i = 0; i < polygon.Length; i++)
            {
                if (polygon[i].Y < testPoint.Y && polygon[j].Y >= testPoint.Y || polygon[j].Y < testPoint.Y
                    && polygon[i].Y >= testPoint.Y)
                {
                    if (polygon[i].X + (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y)
                           * (polygon[j].X - polygon[i].X)
                        < testPoint.X)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public override bool IsHit(AnyUiPoint pt)
        {
            if (Points == null || Points.Count < 1)
                return false;

            return IsPointInPolygon4(Points.ToArray(), pt);
        }
    }

    public class AnyUiControl : AnyUiFrameworkElement, IGetBackground
    {
        public AnyUiBrush Background = null;
        public AnyUiBrush Foreground = null;
        public AnyUiVerticalAlignment? VerticalContentAlignment;
        public AnyUiHorizontalAlignment? HorizontalContentAlignment;
        public double? FontSize;
        public AnyUiFontWeight? FontWeight;
        public bool FontMono = false;

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiContentControl : AnyUiControl, IEnumerateChildren
    {
        public virtual AnyUiUIElement Content { get; set; }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Content != null)
                yield return Content;
        }
    }

    public class AnyUiDecorator : AnyUiFrameworkElement, IEnumerateChildren
    {
        public virtual AnyUiUIElement Child { get; set; }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Child != null)
                yield return Child;
        }

        public T SetChild<T>(T child)
            where T : AnyUiUIElement
        {
            Child = child;
            return child;
        }
    }

    public class AnyUiViewbox : AnyUiDecorator
    {
        public AnyUiStretch Stretch = AnyUiStretch.None;
    }

    public interface IEnumerateChildren
    {
        IEnumerable<AnyUiUIElement> GetChildren();
    }

    public interface IGetBackground
    {
        AnyUiBrush GetBackground();
    }

    public class AnyUiPanel : AnyUiFrameworkElement, IEnumerateChildren, IGetBackground
    {
        public AnyUiBrush Background;

        /// <summary>
        /// This property is not directly legacy of WPD nor HTML: <c>Padding</c> will be
        /// applied to all children and their paddings. Experimental.
        /// </summary>
        public AnyUiThickness Padding;

        private List<AnyUiUIElement> _children = new List<AnyUiUIElement>();
        public List<AnyUiUIElement> Children
        {
            get { return _children; }
            set { _children = value; Touch(); }
        }

        public T Add<T>(T elem) where T : AnyUiUIElement
        {
            Children.Add(elem);
            Touch();
            return elem;
        }

        public IEnumerable<AnyUiUIElement> GetChildren()
        {
            if (Children != null)
                foreach (var child in Children)
                    yield return child;
        }

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiGrid : AnyUiPanel
    {
        public List<AnyUiRowDefinition> RowDefinitions = new List<AnyUiRowDefinition>();
        public List<AnyUiColumnDefinition> ColumnDefinitions = new List<AnyUiColumnDefinition>();

        /// <summary>
        /// If Grid is rendered as HTML table, set the global <table>-Background accordingly.
        /// </summary>
        public AnyUiImage BackgroundImageHtml = null;

        public static void SetRow(AnyUiUIElement el, int value) { if (el != null) el.GridRow = value; }
        public static void SetRowSpan(AnyUiUIElement el, int value) { if (el != null) el.GridRowSpan = value; }
        public static void SetColumn(AnyUiUIElement el, int value) { if (el != null) el.GridColumn = value; }
        public static void SetColumnSpan(AnyUiUIElement el, int value) { if (el != null) el.GridColumnSpan = value; }

        public IEnumerable<AnyUiUIElement> GetChildsAt(int row, int col)
        {
            if (Children == null || RowDefinitions == null || ColumnDefinitions == null
                || row < 0 || row >= RowDefinitions.Count
                || col < 0 || col >= ColumnDefinitions.Count)
                yield break;

            foreach (var ch in Children)
                if (ch.GridRow.HasValue && ch.GridRow.Value == row
                    && ch.GridColumn.HasValue && ch.GridColumn.Value == col)
                    yield return ch;
        }

        public AnyUiUIElement IsCoveredBySpanCell(
            int row, int col,
            bool returnOnRootCell = false,
            bool returnOnSpanCell = false)
        {
            if (Children == null || RowDefinitions == null || ColumnDefinitions == null
                || row < 0 || row >= RowDefinitions.Count
                || col < 0 || col >= ColumnDefinitions.Count)
                return null;

            foreach (var ch in Children)
            {
                // valid at all?
                if (ch.GridRow == null || ch.GridColumn == null)
                    continue;

                // a border is not valid
                if (ch is AnyUiBorder)
                    continue;

                // is child valid for HTML?
                if ((ch.SkipForTarget & AnyUiTargetPlatform.Browser) > 0)
                    continue;

                // first check, if in intervals

                var rowSpan = 1;
                if (ch.GridRowSpan.HasValue && ch.GridRowSpan.Value > 1)
                    rowSpan = ch.GridRowSpan.Value;

                var colSpan = 1;
                if (ch.GridColumnSpan.HasValue && ch.GridColumnSpan.Value > 1)
                    colSpan = ch.GridColumnSpan.Value;

                if (row >= ch.GridRow.Value && (row <= ch.GridRow.Value + rowSpan - 1)
                    && col >= ch.GridColumn.Value && (col <= ch.GridColumn.Value + colSpan - 1))
                {
                    // at least in ..
                    // .. but first check for root ..
                    if (returnOnRootCell
                        && ch.GridRow.Value == row && ch.GridColumn.Value == col)
                        return ch;

                    // .. check for spans
                    if (returnOnSpanCell)
                        return ch;
                }
            }

            return null;
        }

        public (int, int) GetMaxRowCol()
        {
            var maxRow = 0;
            var maxCol = 0;
            if (Children != null)
                foreach (var ch in Children)
                {
                    if (ch.GridRow.HasValue)
                    {
                        var r = ch.GridRow.Value;
                        if (ch.GridRowSpan.HasValue)
                            r += -1 + ch.GridRowSpan.Value;
                        if (r > maxRow)
                            maxRow = r;
                    }

                    if (ch.GridColumn.HasValue)
                    {
                        var c = ch.GridColumn.Value;
                        if (ch.GridColumnSpan.HasValue)
                            c += -1 + ch.GridColumnSpan.Value;
                        if (c > maxCol)
                            maxCol = c;
                    }
                }
            return (maxRow, maxCol);
        }

        public void FixRowColDefs()
        {
            var (maxRow, maxCol) = GetMaxRowCol();

            if (RowDefinitions == null)
                RowDefinitions = new List<AnyUiRowDefinition>();
            while (RowDefinitions.Count < (1 + maxRow))
                RowDefinitions.Add(
                    new AnyUiRowDefinition() { Height = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto) });

            if (ColumnDefinitions == null)
                ColumnDefinitions = new List<AnyUiColumnDefinition>();
            while (ColumnDefinitions.Count < (1 + maxCol))
                ColumnDefinitions.Add(
                    new AnyUiColumnDefinition() { Width = new AnyUiGridLength(1.0, AnyUiGridUnitType.Auto) });
        }
    }

    public class AnyUiStackPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiWrapPanel : AnyUiPanel
    {
        public AnyUiOrientation? Orientation;
    }

    public class AnyUiCanvas : AnyUiPanel
    {
    }

    public class AnyUiScrollViewer : AnyUiContentControl
    {
        public AnyUiScrollBarVisibility? HorizontalScrollBarVisibility;
        public AnyUiScrollBarVisibility? VerticalScrollBarVisibility;

        /// <summary>
        /// In case of (re-) display, scroll immediately to a desireded vertical position
        /// </summary>
        public double? InitialScrollPosition = null;

        /// <summary>
        /// If true, create not a scrollable area, but simply add childs below
        /// (that is: use system provided scrolling)
        /// </summary>
        public AnyUiTargetPlatform FlattenForTarget = AnyUiTargetPlatform.None;
    }

    public class AnyUiBorder : AnyUiDecorator, IGetBackground
    {
        public AnyUiBrush Background = null;
        public AnyUiThickness BorderThickness;
        public AnyUiBrush BorderBrush = null;
        public AnyUiThickness Padding;
        public double? CornerRadius = null;

        public bool IsDropBox = false;

        public AnyUiBrush GetBackground() => Background;
    }

    public class AnyUiLabel : AnyUiContentControl
    {
        public AnyUiThickness Padding;
        public new AnyUiFontWeight? FontWeight;
        public new string Content = null;
    }

    public class AnyUiTextBlock : AnyUiControl
    {
        public AnyUiThickness Padding;
        public AnyUiTextWrapping? TextWrapping;
        public double? LineHeightPercent = null;
        public string Text { get { return _text; } set { _text = value; Touch(); } }
        private string _text = null;
    }

    public class AnyUiSelectableTextBlock : AnyUiTextBlock
    {
        public bool TextAsHyperlink = false;
    }

    public class AnyUiHintBubble : AnyUiTextBlock
    {
    }

    public class AnyUiTextBox : AnyUiControl
    {
        public AnyUiThickness Padding;
        public AnyUiTextWrapping? TextWrapping;

        public AnyUiScrollBarVisibility VerticalScrollBarVisibility;

        public bool MultiLine;
        public Nullable<int> MaxLines;
        public bool IsReadOnly = false;

        public string Text = null;
    }

    public class AnyUiComboBox : AnyUiControl
    {
        public AnyUiThickness Padding;

        public bool? IsEditable;

        public List<object> Items = new List<object>();
        public string Text = null;

        public int? SelectedIndex;

        public void EvalSelectedIndex(string value)
        {
            if (value == null)
                return;

            if (Items != null)
                for (int i = 0; i < Items.Count; i++)
                    if (Items[i] as string == value)
                        SelectedIndex = i;
        }
    }

    public class AnyUiCheckBox : AnyUiContentControl
    {
        public AnyUiThickness Padding;

        public new string Content = null;

        public bool? IsChecked;
    }

    public class AnyUiButton : AnyUiContentControl
    {
        public AnyUiThickness Padding;

        public new string Content = null;
        public string ToolTip = null;

        /// <summary>
        /// If set to true, Blazor will not create a special action session
        /// for executing all the implications of the lambda. This special
        /// case is ONLY for modifying already displayed modal dialogs!!
        /// </summary>
        public bool DirectInvoke;

        public AnyUiSpecialActionBase SpecialAction;
    }

    public class AnyUiCountryFlag : AnyUiFrameworkElement
    {
        public string ISO3166Code = "";
    }

    public class AnyUiBitmapInfo
    {
        /// <summary>
        /// The bitmap data; as anonymous object (because of dependencies). 
        /// Expected is a BitmapSource/ ImageSource.
        /// </summary>
        public object ImageSource;

        /// <summary>
        /// Pixel-dimensions of the bitmap given by providing functionality.
        /// </summary>
        public double PixelWidth, PixelHeight;

        /// <summary>
        /// Bitmap as data bytes in PNG-format.
        /// </summary>
        public byte[] PngData;

        /// <summary>
        /// In WPF, bitmaps are expected to be of 96 dpi. If not, Width != PxielWidth,
        /// which can cause problems.
        /// </summary>
        public bool ConvertTo96dpi = false;
    }

    public class AnyUiImage : AnyUiFrameworkElement
    {

        /// <summary>
        /// Guid of the image. Created by the constructor.
        /// </summary>
        public string ImageGuid = "";

        /// <summary>
        /// The bitmap data; as anonymous object (because of dependencies).
        /// Probably a ImageSource
        /// Setting touches to update
        /// </summary>
        public AnyUiBitmapInfo BitmapInfo
        {
            get { return _bitmapInfo; }
            set { _bitmapInfo = value; ReGuid(); Touch(); }
        }
        private AnyUiBitmapInfo _bitmapInfo = null;

        /// <summary>
        /// Stretch mode
        /// </summary>
        public AnyUiStretch Stretch = AnyUiStretch.None;

        //
        // Constructors
        //

        public AnyUiImage() { ReGuid(); }

        /// <summary>
        /// Initialize upon constructor, e.g. GUID
        /// </summary>
        protected void ReGuid()
        {
            ImageGuid = "IMG" + Guid.NewGuid().ToString("N");
            if (_imageDictionary.ContainsKey(ImageGuid))
                _imageDictionary.Remove(ImageGuid);
            _imageDictionary.Add(ImageGuid, this);
        }

        //
        // Singleton: Dictionary to find images
        //

        protected static Dictionary<string, AnyUiImage> _imageDictionary = new Dictionary<string, AnyUiImage>();

        public static AnyUiImage FindImage(string guid)
        {
            if (guid == null || !_imageDictionary.ContainsKey(guid))
                return null;

            return _imageDictionary[guid];
        }
    }
}
