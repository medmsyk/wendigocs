using System;
using System.Drawing;
using System.Windows.Forms;

namespace Screen.Target {
    internal class BorderLabel : Label {
        public Rectangle Rectangle {
            get { return new Rectangle(Location.X, Location.Y, Size.Width, Size.Height); }
            set { Location = new Point(value.X, value.Y); Size = new Size(value.Width, value.Height); }
        }
    }

    public partial class TargetForm : Form {
        // Make this form invisible and untouchable.
        protected override CreateParams CreateParams {
            get {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000020;   // WS_EX_TRANSPARENT
                cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED
                cp.ExStyle |= 0x08000000;   // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080;   // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        // Borders to indicate area.
        private int borderWidth;
        private BorderLabel[] borderLabels = new BorderLabel[4];
        private const int defaultBorderWidth = 4;
        private static readonly Color defaultBorderColor = Color.Black;
        
        /// <summary>
        /// Area inside the borders.
        /// </summary>
        public Rectangle Area {
            get {
                return new Rectangle(Location.X + borderWidth, Location.Y + borderWidth, Size.Width - borderWidth * 2, Size.Height - borderWidth * 2);
            }
            set {
                if (value.Location == Location && value.Size == Size) return;

                Location = new Point(value.X - borderWidth, value.Y - borderWidth);
                Size = new Size(value.Width + borderWidth * 2, value.Height + borderWidth * 2);
            
                // Top, Left, Right, Bottom
                borderLabels[0].Rectangle = new Rectangle(0, 0, value.Width + borderWidth * 2, borderWidth);
                borderLabels[1].Rectangle = new Rectangle(0, borderWidth, borderWidth, value.Height + borderWidth);
                borderLabels[2].Rectangle = new Rectangle(value.Width + borderWidth, borderWidth, borderWidth, value.Height + borderWidth);
                borderLabels[3].Rectangle = new Rectangle(borderWidth, value.Height + borderWidth, value.Width, borderWidth);
            }
        }

        /// <summary>
        /// Create a form to indicate area.
        /// </summary>
        /// <param name="area">Area.</param>
        /// <param name="borderWidth">Border width.</param>
        /// <param name="borderColor">Border color.</param>
        public TargetForm(Rectangle area, int? borderWidth=null, Color? borderColor=null) {
            InitializeComponent();
            
            this.borderWidth = borderWidth == null ? defaultBorderWidth : (int)borderWidth;
            
            for (int i = 0; i < borderLabels.Length; i++) {
                borderLabels[i] = new BorderLabel();
                borderLabels[i].BackColor = borderColor == null ? defaultBorderColor : (Color)borderColor;
                Controls.Add(borderLabels[i]);
            }

            TopLevel = true;
            TopMost = true;
            Show();
            
            Area = area;
        }
    }
}
