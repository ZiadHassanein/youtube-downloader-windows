using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YouTubeDownloader {
    internal sealed class Palette {
        public Color Page, Surface, Field, Ink, Muted, Accent, OnAccent, Border, Hover, Selected, Error;
        public static Palette For(bool dark) {
            if(SystemInformation.HighContrast) return new Palette { Page=SystemColors.Control,Surface=SystemColors.Control,Field=SystemColors.Window,Ink=SystemColors.WindowText,Muted=SystemColors.WindowText,Accent=SystemColors.Highlight,OnAccent=SystemColors.HighlightText,Border=SystemColors.WindowText,Hover=SystemColors.Control,Selected=SystemColors.Highlight,Error=SystemColors.WindowText };
            return dark
                ? new Palette { Page=C(17,23,28),Surface=C(24,33,39),Field=C(32,44,52),Ink=C(237,244,247),Muted=C(168,184,194),Accent=C(101,221,204),OnAccent=C(7,30,27),Border=C(80,102,113),Hover=C(41,59,67),Selected=C(33,69,63),Error=C(255,163,159) }
                : new Palette { Page=C(243,247,247),Surface=Color.White,Field=C(247,250,250),Ink=C(29,43,52),Muted=C(83,101,112),Accent=C(0,115,105),OnAccent=Color.White,Border=C(167,187,196),Hover=C(234,243,243),Selected=C(227,243,240),Error=C(174,37,46) };
        }
        private static Color C(int r,int g,int b) { return Color.FromArgb(r,g,b); }
    }

    internal static class Drawing {
        public static GraphicsPath Rounded(Rectangle rectangle,int radius) {
            var path=new GraphicsPath(); int d=radius*2;
            path.AddArc(rectangle.Left,rectangle.Top,d,d,180,90); path.AddArc(rectangle.Right-d,rectangle.Top,d,d,270,90);
            path.AddArc(rectangle.Right-d,rectangle.Bottom-d,d,d,0,90); path.AddArc(rectangle.Left,rectangle.Bottom-d,d,d,90,90); path.CloseFigure(); return path;
        }
    }

    internal sealed class ThemeButton : Button {
        public Palette Colors=Palette.For(false);
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
            if(!Quiet || (hover && Enabled)) using(var path=Drawing.Rounded(new Rectangle(0,0,Width-1,Height-1),6)) {
                using(var brush=new SolidBrush(fill)) e.Graphics.FillPath(brush,path);
                if(!Primary && !Quiet) using(var pen=new Pen(p.Border)) e.Graphics.DrawPath(pen,path);
            }
            var flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
            if(!ShowKeyboardCues) flags|=TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,text,flags);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-9,Height-9),text,fill);
        }
    }

    internal sealed class FormatChoice : RadioButton {
        public Palette Colors=Palette.For(false);
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
            using(var path=Drawing.Rounded(new Rectangle(0,0,Width-1,Height-1),6)) {
                using(var brush=new SolidBrush(fill)) e.Graphics.FillPath(brush,path);
                using(var pen=new Pen(Checked && Enabled ? p.Accent : p.Border,Checked ? 1.5f : 1f)) e.Graphics.DrawPath(pen,path);
            }
            int diameter=Math.Max(12,(int)(Font.Height*0.72)); var circle=new Rectangle(14,(Height-diameter)/2,diameter,diameter);
            using(var pen=new Pen(Enabled && Checked ? p.Accent : p.Muted,1.5f)) e.Graphics.DrawEllipse(pen,circle);
            if(Checked) { circle.Inflate(-4,-4); using(var brush=new SolidBrush(Enabled ? p.Accent : p.Muted)) e.Graphics.FillEllipse(brush,circle); }
            var flags=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;
            if(!ShowKeyboardCues) flags|=TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(diameter+24,0,Width-diameter-30,Height),ink,flags);
            if(Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(4,4,Width-9,Height-9),ink,fill);
        }
    }

    internal sealed class ThemeProgress : Control {
        public Palette Colors=Palette.For(false);
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
                if(style==ProgressBarStyle.Marquee) e.Graphics.FillRectangle(brush,phase-Width/4,0,Width/4,Height);
                else e.Graphics.FillRectangle(brush,0,0,(int)(Width*Math.Max(0,Math.Min(1,(double)(value-Minimum)/Math.Max(1,Maximum-Minimum)))),Height);
            }
        }
        protected override void Dispose(bool disposing) { if(disposing) animation.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class ThemeCombo : ComboBox {
        public Palette Colors=Palette.For(false);
        protected override void WndProc(ref Message message) {
            base.WndProc(ref message);
            if(message.Msg==0x000F && IsHandleCreated) using(var graphics=Graphics.FromHwnd(Handle)) {
                int width=Math.Max(18,SystemInformation.VerticalScrollBarWidth);
                if(!Enabled) {
                    using(var brush=new SolidBrush(Colors.Field)) graphics.FillRectangle(brush,ClientRectangle);
                    string text=SelectedItem==null ? Text : SelectedItem.ToString();
                    TextRenderer.DrawText(graphics,text,Font,new Rectangle(8,0,Math.Max(0,Width-width-14),Height),Colors.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPrefix);
                }
                using(var brush=new SolidBrush(Colors.Field)) graphics.FillRectangle(brush,Width-width-1,1,width,Height-2);
                using(var pen=new Pen(Colors.Border)) graphics.DrawRectangle(pen,0,0,Width-1,Height-1);
                int x=Width-width/2-1,y=Height/2;
                using(var pen=new Pen(Enabled ? Colors.Ink : Colors.Muted,1.5f)) { graphics.DrawLine(pen,x-3,y-2,x,y+1); graphics.DrawLine(pen,x,y+1,x+3,y-2); }
            }
        }
    }
}
