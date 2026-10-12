using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace YouTubeDownloader {
    internal sealed class Palette {
        public Color Page, Surface, Field, Ink, Muted, Accent, OnAccent, Border, Hover, Selected, Error;
        public static Palette For(bool dark) {
            if(SystemInformation.HighContrast) return new Palette { Page=SystemColors.Control,Surface=SystemColors.Control,Field=SystemColors.Window,Ink=SystemColors.WindowText,Muted=SystemColors.WindowText,Accent=SystemColors.Highlight,OnAccent=SystemColors.HighlightText,Border=SystemColors.WindowText,Hover=SystemColors.Control,Selected=SystemColors.Highlight,Error=SystemColors.WindowText };
            return dark
                ? new Palette { Page=C(17,23,28),Surface=C(24,33,39),Field=C(32,44,52),Ink=C(237,244,247),Muted=C(168,184,194),Accent=C(101,221,204),OnAccent=C(7,30,27),Border=C(96,124,138),Hover=C(41,59,67),Selected=C(33,69,63),Error=C(255,163,159) }
                : new Palette { Page=C(243,247,247),Surface=Color.White,Field=C(247,250,250),Ink=C(29,43,52),Muted=C(83,101,112),Accent=C(0,115,105),OnAccent=Color.White,Border=C(121,143,155),Hover=C(234,243,243),Selected=C(227,243,240),Error=C(174,37,46) };
        }
        private static Color C(int r,int g,int b) { return Color.FromArgb(r,g,b); }
    }

    internal static class Drawing {
        public const int CornerRadius=10;
        public static Rectangle SurfaceBounds(Control control) { return new Rectangle(1,1,Math.Max(1,control.Width-3),Math.Max(1,control.Height-3)); }
        public static GraphicsPath Rounded(Rectangle rectangle,int radius) {
            var path=new GraphicsPath(); radius=Math.Max(1,Math.Min(radius,Math.Min(rectangle.Width,rectangle.Height)/2)); int d=radius*2;
            path.AddArc(rectangle.Left,rectangle.Top,d,d,180,90); path.AddArc(rectangle.Right-d,rectangle.Top,d,d,270,90);
            path.AddArc(rectangle.Right-d,rectangle.Bottom-d,d,d,0,90); path.AddArc(rectangle.Left,rectangle.Bottom-d,d,d,90,90); path.CloseFigure(); return path;
        }
    }

    internal sealed class ThemeScrollHost : Panel {
        public bool IsArabic;
        private readonly Panel viewport=new Panel();
        private readonly ThemeScrollBar scrollbar=new ThemeScrollBar();
        private Control content;
        private bool arranging;
        private bool updatingContent;
        private double wheelRemainder;
        public event EventHandler ContentHeightChanged;
        public int ContentHeight { get { return content==null ? 0 : content.Height; } }
        public ThemeScrollHost() {
            SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);
            Controls.Add(viewport); Controls.Add(scrollbar);
            scrollbar.ValueChanged+=delegate { if(content!=null) content.Top=-scrollbar.Value; };
        }
        public void SetContent(Control value) {
            content=value; content.Dock=DockStyle.None; content.Location=Point.Empty; viewport.Controls.Add(content);
            content.SizeChanged+=delegate { ArrangeContent(); }; AttachInput(content); ArrangeContent();
        }
        public void BeginContentUpdate() { updatingContent=true; }
        public void EndContentUpdate() { updatingContent=false; ArrangeContent(); }
        private void AttachInput(Control control) {
            control.GotFocus+=delegate { QueueFocusScroll(control); };
            control.MouseWheel+=delegate(object sender,MouseEventArgs e) { ScrollWheel(e); };
            control.ControlAdded+=delegate(object sender,ControlEventArgs e) { AttachInput(e.Control); };
            foreach(Control child in control.Controls) AttachInput(child);
        }
        private void QueueFocusScroll(Control control) {
            if(!IsHandleCreated || IsDisposed || Disposing) return;
            // Wait until native parent/child handles are ready before converting coordinates.
            BeginInvoke(new Action(delegate { if(!IsDisposed && !Disposing && control.Focused) ScrollTo(control); }));
        }
        private void ScrollTo(Control control) {
            if(content==null || !control.Visible || !IsHandleCreated || !control.IsHandleCreated || !viewport.IsHandleCreated || !content.IsHandleCreated) return;
            Point point=viewport.PointToClient(control.PointToScreen(Point.Empty));
            if(point.Y<0) scrollbar.Value+=point.Y-8;
            else if(point.Y+control.Height>viewport.Height) scrollbar.Value+=point.Y+control.Height-viewport.Height+8;
        }
        protected override void OnLayout(LayoutEventArgs e) { base.OnLayout(e); ArrangeContent(); }
        private void ArrangeContent() {
            if(arranging || updatingContent || content==null || ClientSize.Width<=Padding.Horizontal || ClientSize.Height<=Padding.Vertical) return;
            arranging=true;
            try {
                viewport.Bounds=new Rectangle(Padding.Left,Padding.Top,ClientSize.Width-Padding.Horizontal,ClientSize.Height-Padding.Vertical);
                int width=viewport.ClientSize.Width;
                content.MinimumSize=new Size(width,0); content.MaximumSize=new Size(width,0); content.Width=width;
                int oldHeight=content.Height; content.PerformLayout(); content.Height=content.GetPreferredSize(new Size(width,0)).Height;
                scrollbar.SetRange(content.Height,viewport.Height);
                scrollbar.Bounds=new Rectangle(IsArabic ? 4 : Width-16,12,12,Math.Max(1,Height-24));
                scrollbar.Visible=content.Height>viewport.Height; content.Location=new Point(0,-scrollbar.Value);
                if(oldHeight!=content.Height && ContentHeightChanged!=null) ContentHeightChanged(this,EventArgs.Empty);
            } finally { arranging=false; }
        }
        protected override void OnMouseWheel(MouseEventArgs e) { ScrollWheel(e); base.OnMouseWheel(e); }
        private void ScrollWheel(MouseEventArgs e) {
            var handled=e as HandledMouseEventArgs; if(handled!=null && handled.Handled) return;
            int lines=SystemInformation.MouseWheelScrollLines;
            scrollbar.Value-=WheelDistance(e.Delta,lines,viewport.Height,Math.Max(16,Font.Height),ref wheelRemainder);
            if(handled!=null) handled.Handled=true;
        }
        internal static int WheelDistance(int delta,int lines,int pageSize,int lineHeight,ref double remainder) {
            if(lines==0) { remainder=0; return 0; }
            int distance=lines<0 ? Math.Max(1,pageSize-32) : lines*Math.Max(1,lineHeight);
            remainder+=delta/120.0*distance; int pixels=(int)remainder; remainder-=pixels; return pixels;
        }
    }

    internal sealed class ThemeScrollBar : Control {
        public Palette Colors=Palette.For(true);
        private int value, maximum, page=1, dragY, dragValue;
        private bool dragging, hover;
        public event EventHandler ValueChanged;
        public int Value { get { return value; } set { int next=Math.Max(0,Math.Min(maximum,value)); if(this.value==next) return; this.value=next; Invalidate(); if(IsHandleCreated) AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1); var handler=ValueChanged; if(handler!=null) handler(this,EventArgs.Empty); } }
        public ThemeScrollBar() { TabStop=true; AccessibleRole=AccessibleRole.ScrollBar; AccessibleName="Scroll page"; AccessibleDescription="Use the arrow keys, Page Up, Page Down, Home, or End to scroll."; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.Selectable,true); }
        public void SetRange(int extent,int pageSize) { page=Math.Max(1,pageSize); maximum=Math.Max(0,extent-page); Value=value; Invalidate(); }
        private Rectangle Thumb {
            get { int height=Math.Min(Height,Math.Max(28,(int)((double)Height*page/Math.Max(1,page+maximum)))); int top=maximum==0 ? 0 : (int)((double)value/maximum*Math.Max(0,Height-height)); return new Rectangle(2,top,Math.Max(1,Width-4),Math.Max(1,height)); }
        }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent==null ? Colors.Page : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            var thumb=Thumb; using(var path=Drawing.Rounded(new Rectangle(thumb.X,thumb.Y,thumb.Width,Math.Max(1,thumb.Height-1)),4)) using(var brush=new SolidBrush(hover || dragging ? Colors.Muted : Colors.Border)) e.Graphics.FillPath(brush,path);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(0,0,Math.Max(1,Width-1),Math.Max(1,Height-1)),Colors.Accent,Colors.Page);
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if(e.Button!=MouseButtons.Left) return; Focus(); if(Thumb.Contains(e.Location)) { dragging=true; dragY=e.Y; dragValue=value; Capture=true; } else Value+=e.Y<Thumb.Top ? -page : page; }
        protected override void OnMouseMove(MouseEventArgs e) { if(dragging) Value=dragValue+(int)((double)(e.Y-dragY)*maximum/Math.Max(1,Height-Thumb.Height)); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { dragging=false; Capture=false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnMouseCaptureChanged(EventArgs e) { if(!Capture) dragging=false; base.OnMouseCaptureChanged(e); }
        protected override bool IsInputKey(Keys keyData) { return keyData==Keys.Up || keyData==Keys.Down || keyData==Keys.PageUp || keyData==Keys.PageDown || keyData==Keys.Home || keyData==Keys.End || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) {
            if(e.KeyCode==Keys.Up) Value-=32; else if(e.KeyCode==Keys.Down) Value+=32; else if(e.KeyCode==Keys.PageUp) Value-=page; else if(e.KeyCode==Keys.PageDown) Value+=page; else if(e.KeyCode==Keys.Home) Value=0; else if(e.KeyCode==Keys.End) Value=maximum; else { base.OnKeyDown(e); return; } e.Handled=true;
        }
        protected override AccessibleObject CreateAccessibilityInstance() { return new ScrollAccessible(this); }
        private sealed class ScrollAccessible : ControlAccessibleObject {
            private readonly ThemeScrollBar bar;
            public ScrollAccessible(ThemeScrollBar owner) : base(owner) { bar=owner; }
            public override string Value { get { return bar.Value.ToString(); } set { int next; if(Int32.TryParse(value,out next)) bar.Value=next; } }
        }
    }

    internal enum StatusTone { Info, Success, Error }
    internal sealed class LocalizedLabel : Label { public string TextKey; }
    internal sealed class StatusIcon : Control {
        public Palette Colors=Palette.For(true);
        public string Language="en";
        private StatusTone tone;
        public StatusTone Tone { get { return tone; } set { tone=value; AccessibleName=Texts.For(Language,value==StatusTone.Error ? "Error" : (value==StatusTone.Success ? "Success" : "Information")); Invalidate(); } }
        public StatusIcon() { Size=new Size(32,32); TabStop=false; AccessibleRole=AccessibleRole.Graphic; AccessibleName="Information"; SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Parent==null ? Colors.Surface : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color ink=tone==StatusTone.Error ? Colors.Error : Colors.Accent; float x=Width/2f,y=Height/2f;
            using(var brush=new SolidBrush(Color.FromArgb(18,ink))) e.Graphics.FillEllipse(brush,x-12,y-12,24,24);
            using(var pen=new Pen(ink,1.8f)) {
                pen.StartCap=pen.EndCap=LineCap.Round; e.Graphics.DrawEllipse(pen,x-12,y-12,24,24);
                if(tone==StatusTone.Success) e.Graphics.DrawLines(pen,new[]{new PointF(x-5,y),new PointF(x-1,y+4),new PointF(x+6,y-4)});
                else if(tone==StatusTone.Error) { e.Graphics.DrawLine(pen,x,y-5,x,y+1); using(var brush=new SolidBrush(ink)) e.Graphics.FillEllipse(brush,x-1,y+5,2,2); }
                else { e.Graphics.DrawLine(pen,x,y-1,x,y+6); using(var brush=new SolidBrush(ink)) e.Graphics.FillEllipse(brush,x-1,y-7,2,2); }
            }
        }
    }

    internal sealed class ThemeField : Panel {
        public Palette Colors=Palette.For(true);
        public ThemeField() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); }
        protected override void OnPaintBackground(PaintEventArgs e) {
            e.Graphics.Clear(Parent==null ? Colors.Surface : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) {
                using(var brush=new SolidBrush(Colors.Field)) e.Graphics.FillPath(brush,path);
                using(var pen=new Pen(ContainsFocus ? Colors.Accent : Colors.Border,ContainsFocus ? 1.5f : 1f)) e.Graphics.DrawPath(pen,path);
            }
        }
        protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); e.Control.GotFocus+=ChildFocusChanged; e.Control.LostFocus+=ChildFocusChanged; }
        protected override void OnControlRemoved(ControlEventArgs e) { e.Control.GotFocus-=ChildFocusChanged; e.Control.LostFocus-=ChildFocusChanged; base.OnControlRemoved(e); }
        private void ChildFocusChanged(object sender,EventArgs e) { Invalidate(); }
    }

    internal sealed class ThemeTextBox : TextBox {
        public Palette Colors=Palette.For(true);
        public string Placeholder;
        public bool PlaceholderRightToLeft;
        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            if(message.Msg==0x000F && IsHandleCreated && TextLength==0 && !String.IsNullOrEmpty(Placeholder)) using(var graphics=Graphics.FromHwnd(Handle))
                TextRenderer.DrawText(graphics,Placeholder,Font,ClientRectangle,Colors.Muted,(PlaceholderRightToLeft ? TextFormatFlags.Right|TextFormatFlags.RightToLeft : TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class ThemeButton : Button {
        public Palette Colors=Palette.For(false);
        public bool IsArabic;
        public string TextKey;
        public bool Primary, Quiet;
        private bool hover, pressed;
        public ThemeButton() { SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; pressed=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed=true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed=false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var p=Colors; e.Graphics.Clear(Parent==null ? p.Page : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=Primary ? (Enabled ? p.Accent : p.Selected) : (hover && Enabled ? p.Hover : p.Field);
            if(pressed && Enabled) fill=ControlPaint.Dark(fill,0.08f);
            Color text=Enabled ? (Primary ? p.OnAccent : (Quiet ? p.Accent : p.Ink)) : p.Muted;
            if(!Quiet || (hover && Enabled)) using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) {
                using(var brush=new SolidBrush(fill)) e.Graphics.FillPath(brush,path);
                if(!Primary && !Quiet) using(var pen=new Pen(p.Border)) e.Graphics.DrawPath(pen,path);
            }
            var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
            if(IsArabic) flags|=TextFormatFlags.RightToLeft;
            if(!ShowKeyboardCues) flags|=TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,text,flags);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-9,Height-9),text,fill);
        }
    }

    internal sealed class FormatChoice : RadioButton {
        public Palette Colors=Palette.For(false);
        public bool IsArabic;
        private bool hover;
        public FormatChoice() {
            Appearance=Appearance.Button; AutoSize=false; Height=44; Cursor=Cursors.Hand; FlatStyle=FlatStyle.Flat;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var p=Colors; e.Graphics.Clear(Parent==null ? p.Surface : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            var fill=Checked ? p.Selected : (hover && Enabled ? p.Hover : p.Field);
            var ink=Enabled ? p.Ink : p.Muted;
            using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) {
                using(var brush=new SolidBrush(fill)) e.Graphics.FillPath(brush,path);
                using(var pen=new Pen(Checked && Enabled ? p.Accent : p.Border,Checked ? 1.5f : 1f)) e.Graphics.DrawPath(pen,path);
            }
            int diameter=Math.Max(12,(int)(Font.Height*0.72)); bool rtl=IsArabic; var circle=new Rectangle(rtl ? Width-14-diameter : 14,(Height-diameter)/2,diameter,diameter);
            using(var pen=new Pen(Enabled && Checked ? p.Accent : p.Muted,1.5f)) e.Graphics.DrawEllipse(pen,circle);
            if(Checked) { circle.Inflate(-4,-4); using(var brush=new SolidBrush(Enabled ? p.Accent : p.Muted)) e.Graphics.FillEllipse(brush,circle); }
            var flags=(rtl ? TextFormatFlags.Right|TextFormatFlags.RightToLeft : TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
            if(!ShowKeyboardCues) flags|=TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(rtl ? 8 : diameter+24,0,Width-diameter-32,Height),ink,flags);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-9,Height-9),ink,fill);
        }
    }

    internal sealed class ThemeProgress : Control {
        public Palette Colors=Palette.For(false);
        public bool IsArabic;
        public int Minimum=0,Maximum=1000;
        private int value, phase;
        private ProgressBarStyle style;
        private readonly Timer animation=new Timer { Interval=40 };
        public int Value { get { return value; } set { this.value=value; Invalidate(); } }
        public ProgressBarStyle Style { get { return style; } set { style=value; animation.Enabled=value==ProgressBarStyle.Marquee && Visible; Invalidate(); } }
        public ThemeProgress() {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true); TabStop=false; AccessibleRole=AccessibleRole.ProgressBar; AccessibleName="Download progress";
            animation.Tick+=delegate { phase=(phase+8)%Math.Max(1,Width+Width/4); Invalidate(); };
        }
        protected override void OnVisibleChanged(EventArgs e) { animation.Enabled=Visible && style==ProgressBarStyle.Marquee; base.OnVisibleChanged(e); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Colors.Field);
            using(var brush=new SolidBrush(Colors.Accent)) {
                if(style==ProgressBarStyle.Marquee) e.Graphics.FillRectangle(brush,IsArabic ? Width-phase : phase-Width/4,0,Width/4,Height);
                else { int length=(int)(Width*Math.Max(0,Math.Min(1,(double)(value-Minimum)/Math.Max(1,Maximum-Minimum)))); e.Graphics.FillRectangle(brush,IsArabic ? Width-length : 0,0,length,Height); }
            }
        }
        protected override void Dispose(bool disposing) { if(disposing) animation.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class ThemeSwitch : CheckBox {
        public Palette Colors=Palette.For(true);
        public string Language="en";
        private bool hover;
        private float visualDark, startDark, targetDark;
        private readonly Stopwatch transitionClock=new Stopwatch();
        private readonly Timer transition=new Timer { Interval=15 };
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action,uint parameter,[MarshalAs(UnmanagedType.Bool)] ref bool value,uint flags);
        public ThemeSwitch() {
            Appearance=Appearance.Button; AutoSize=false; Size=new Size(44,44); Cursor=Cursors.Hand;
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
            AccessibleName="Switch to light theme";
            transition.Tick+=delegate {
                float elapsed=Math.Min(1f,(float)transitionClock.Elapsed.TotalMilliseconds/120f);
                float eased=1f-(float)Math.Pow(1f-elapsed,3);
                visualDark=startDark+(targetDark-startDark)*eased;
                if(elapsed>=1f) { transition.Stop(); visualDark=targetDark; }
                Invalidate();
            };
        }
        protected override void OnCheckedChanged(EventArgs e) {
            UpdateActionName(); targetDark=Checked ? 1f : 0f;
            bool animate=false; SystemParametersInfo(0x1042,0,ref animate,0);
            if(IsHandleCreated && Visible && animate) { startDark=visualDark; transitionClock.Restart(); transition.Start(); }
            else { transition.Stop(); visualDark=targetDark; }
            Invalidate(); base.OnCheckedChanged(e);
        }
        public void UpdateActionName() { AccessibleName=Texts.For(Language,Checked ? "Switch to light theme" : "Switch to dark theme"); }
        protected override void OnVisibleChanged(EventArgs e) { if(!Visible) { transition.Stop(); visualDark=Checked ? 1f : 0f; } base.OnVisibleChanged(e); }
        protected override void OnMouseEnter(EventArgs e) { hover=true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover=false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var p=Colors; e.Graphics.Clear(Parent==null ? p.Page : Parent.BackColor); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            Color fill=hover ? p.Hover : p.Field;
            using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) {
                using(var brush=new SolidBrush(fill)) e.Graphics.FillPath(brush,path);
                using(var pen=new Pen(p.Border)) e.Graphics.DrawPath(pen,path);
            }
            float x=Width/2f,y=Height/2f;
            if(visualDark>0f) using(var pen=new Pen(Color.FromArgb((int)(255*visualDark),p.Ink),1.8f)) {
                e.Graphics.DrawEllipse(pen,x-5,y-5,10,10);
                for(int ray=0;ray<8;ray++) { double a=ray*Math.PI/4; e.Graphics.DrawLine(pen,x+(float)Math.Cos(a)*9,y+(float)Math.Sin(a)*9,x+(float)Math.Cos(a)*13,y+(float)Math.Sin(a)*13); }
            }
            if(visualDark<1f) using(var crescent=new GraphicsPath()) {
                crescent.AddEllipse(x-10,y-10,20,20);
                using(var region=new Region(crescent)) using(var cut=new GraphicsPath()) {
                    cut.AddEllipse(x-3,y-13,19,19); region.Exclude(cut);
                    using(var brush=new SolidBrush(Color.FromArgb((int)(255*(1f-visualDark)),p.Ink))) e.Graphics.FillRegion(brush,region);
                }
            }
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-9,Height-9),p.Ink,fill);
        }
        protected override void Dispose(bool disposing) { if(disposing) transition.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class ThemeCombo : ComboBox {
        public Palette Colors=Palette.For(false);
        public bool IsArabic;
        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            if(message.Msg==0x000F && IsHandleCreated) using(var target=Graphics.FromHwnd(Handle)) using(var frame=BufferedGraphicsManager.Current.Allocate(target,ClientRectangle)) {
                var graphics=frame.Graphics;
                int width=Math.Max(18,SystemInformation.VerticalScrollBarWidth);
                graphics.Clear(Parent==null ? Colors.Surface : Parent.BackColor);
                graphics.SmoothingMode=SmoothingMode.AntiAlias;
                using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) {
                    using(var brush=new SolidBrush(Colors.Field)) graphics.FillPath(brush,path);
                    using(var pen=new Pen(Focused && Enabled ? Colors.Accent : Colors.Border,Focused && Enabled ? 1.5f : 1f)) graphics.DrawPath(pen,path);
                }
                string text=SelectedItem==null ? Text : SelectedItem.ToString(); bool rtl=IsArabic;
                TextRenderer.DrawText(graphics,text,Font,new Rectangle(rtl ? width+8 : 12,0,Math.Max(0,Width-width-20),Height),Enabled ? Colors.Ink : Colors.Muted,(rtl ? TextFormatFlags.Right|TextFormatFlags.RightToLeft : TextFormatFlags.Left)|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
                if(Focused && ShowFocusCues && Enabled) ControlPaint.DrawFocusRectangle(graphics,new Rectangle(rtl ? width+5 : 5,5,Width-width-10,Height-10),Colors.Ink,Colors.Field);
                int x=rtl ? width/2+1 : Width-width/2-1,y=Height/2;
                using(var pen=new Pen(Enabled ? Colors.Ink : Colors.Muted,1.5f)) { graphics.DrawLine(pen,x-3,y-2,x,y+1); graphics.DrawLine(pen,x,y+1,x+3,y-2); }
                frame.Render(target);
            }
        }
    }

    internal sealed class NoticePopover : Panel {
        private readonly StatusIcon icon;
        private readonly Label heading, message;
        private readonly ThemeButton dismiss;
        private Palette colors=Palette.For(true);
        private bool arabic;
        public NoticePopover(StatusIcon icon,Label heading,Label message,ThemeButton dismiss,Font font) {
            this.icon=icon; this.heading=heading; this.message=message; this.dismiss=dismiss;
            Font=font; Tag="surface"; Visible=false; SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);
            Controls.Add(icon); Controls.Add(heading); Controls.Add(message); Controls.Add(dismiss);
        }
        public void UpdateLook(Palette palette,bool isArabic) {
            colors=palette; arabic=isArabic; BackColor=colors.Surface; ForeColor=colors.Ink;
            icon.Colors=colors; icon.Language=arabic ? "ar" : "en"; icon.Tone=icon.Tone; heading.ForeColor=icon.Tone==StatusTone.Error ? colors.Error : colors.Ink; message.ForeColor=colors.Muted; dismiss.Colors=colors; dismiss.IsArabic=arabic;
            heading.TextAlign=message.TextAlign=arabic ? ContentAlignment.TopRight : ContentAlignment.TopLeft;
            int width=380, textWidth=width-80;
            heading.MaximumSize=new Size(textWidth,0); message.MaximumSize=new Size(textWidth,0);
            heading.Location=new Point(arabic ? width-64-heading.Width : 60,16); message.Location=new Point(arabic ? width-64-message.Width : 60,heading.Bottom+6);
            icon.Location=new Point(arabic ? width-48 : 16,16);
            dismiss.Location=new Point(arabic ? 16 : width-16-dismiss.Width,message.Bottom+10);
            Size=new Size(width,dismiss.Bottom+12); Text=heading.Text; AccessibleName=heading.Text;
            using(var path=Drawing.Rounded(ClientRectangle,Drawing.CornerRadius)) { var old=Region; Region=new Region(path); if(old!=null) old.Dispose(); }
            Invalidate(true);
        }
        public void ShowNear(Form owner,Control anchor) {
            if(owner.IsDisposed || !owner.Visible || !anchor.IsHandleCreated) return;
            var point=owner.PointToClient(anchor.PointToScreen(new Point(arabic ? anchor.Width-Width : 0,anchor.Height+8)));
            var area=owner.ClientRectangle;
            point.X=Math.Max(area.Left+8,Math.Min(area.Right-Width-8,point.X));
            if(point.Y+Height>area.Bottom-8) point.Y=owner.PointToClient(anchor.PointToScreen(Point.Empty)).Y-Height-8;
            point.Y=Math.Max(area.Top+8,Math.Min(area.Bottom-Height-8,point.Y)); Location=point;
            Show(); BringToFront(); Refresh();
        }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            using(var path=Drawing.Rounded(Drawing.SurfaceBounds(this),Drawing.CornerRadius)) using(var pen=new Pen(colors.Border)) e.Graphics.DrawPath(pen,path);
        }
    }
}
