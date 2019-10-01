using System;
using System.Numerics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace MandelbrotIMP
{
    class Program : Form
    {
        PictureBox pictureBox;
        Bitmap buffer;

        // Waarom wel voor TextBox, maar niet voor Labels en Buttons? 
        TextBox inputX, inputY, inputScale, inputMax;

        ComboBox coordinatePreset, colorPreset;

        Bitmap palette = new Bitmap("gradient.png");
        Bitmap gradient;
        // double x = 0, y = 0, scale = 1; 
        double x = -1.0079296875, y = 0.3112109375, scale = 1.953125E-3;
        int maxIterations = 200;
        int size = 600;

        public Program()
        {
            ClientSize = new Size(size, size);
            Text = "Mandelbrot Viewer";
            buffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            KeyDown += PressedKey;
            ResizeEnd += OnResize;
            MaximizeBox = false;
            
            
            FlowLayoutPanel panel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.TopDown,
                Location = Point.Empty,
                Size = new Size(ClientSize.Width, 50)
            };
            Controls.Add(panel);

            // Labels
            Label labelX = new Label() {
                Text = "X",
                Size = new Size(10, 14),
            };
            Label labelY = new Label()
            {
                Text = "Y",
                Size = new Size(10, 14),
            };

            Label labelScale = new Label()
            {
                Text = "Schaal",
                Size = new Size(40, 14),
            };
            Label labelMax = new Label()
            {
                Text = "Max",
                Size = new Size(30, 14),
            };

            // Buttons
            Button buttonOk = new Button()
            {
                Text = "OK",
                Size = new Size(60, 18),
            };
            buttonOk.Click += OkClick;

            Button buttonReset = new Button()
            {
                Text = "Reset",
                Size = new Size(60, 18),
            };
            buttonReset.Click += ResetValues;

            // Textbox
            inputX = new TextBox();
            inputX.KeyDown += PressedKey;

            inputY = new TextBox();
            inputY.KeyDown += PressedKey;

            inputScale = new TextBox();
            inputScale.KeyDown += PressedKey;

            inputMax = new TextBox();
            inputMax.KeyDown += PressedKey;

            // Combobox
            coordinatePreset = new ComboBox()
            {
                DropDownWidth = 280,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = new string[] { "Basis", "Vuur", "Zuilen","Zigzag"}
            };
            coordinatePreset.SelectedIndexChanged += CoordinatePresetSelected;

            colorPreset = new ComboBox()
            {
                DropDownWidth = 280,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = new string[] { "Kleur 1", "Kleur 2" }
            };
            colorPreset.SelectedIndexChanged += ColorPresetSelected;

            // Add controls to panel
            // Heb dit iets opgeschoont

            panel.Controls.AddRange(new Control[] {labelX,
                inputX,
                labelY,
                inputY,
                labelScale,
                inputScale,
                labelMax,
                inputMax,
                buttonOk,
                buttonReset,
                coordinatePreset,
                colorPreset});

            pictureBox = new PictureBox()
            {
                Image = buffer,
                Size = new Size(ClientSize.Width, ClientSize.Height - panel.Height),
                Location = new Point(0, panel.Height),
            };
            pictureBox.MouseClick += ClickFocus;

            Controls.Add(pictureBox);

            Kleurtjes();

            ReloadTextbox();
            ShowMandel();
        }

        private void CoordinatePresetSelected(object sender, EventArgs e)
        {
            // coordinatePreset
            // Wil liever een struct, waar naam, x, y en scale in staat die dan boven aan gedeclareerd kunnen worden.
            // Misschien opslan als XML bestand ofzo? 
            switch (coordinatePreset.SelectedIndex)
            {
                case 0:
                    x = 0;
                    y = 0;
                    scale = 1;
                    break;
                case 1:
                    x = -1.0079296875;
                    y = 0.3112109375;
                    scale = 1.953125E-3;
                    break;
                case 2:
                    x = -0.1578125;
                    y = 1.0328125;
                    scale = 1.5625E-4;
                    break;
                case 3:
                    x = -0.108625;
                    y = 0.9014428;
                    scale = 3.8147E-8;
                    break;
            }
            ReloadTextbox();
            ShowMandel();
        }

        private void ColorPresetSelected(object sender, EventArgs e)
        {
            // colorPreset

            ReloadTextbox();
            ShowMandel();
        }

        private void OnResize(object sender, EventArgs e)
        {
            size = Math.Min(ClientSize.Width, ClientSize.Height);
            buffer = new Bitmap(ClientSize.Width, ClientSize.Height);
            pictureBox.Size = new Size(ClientSize.Width, ClientSize.Height - 42);
            pictureBox.Image = buffer;
            ShowMandel();
        }

        private void ClickFocus(object sender, MouseEventArgs e)
        {
            x = (e.X / (double)size * 4.0 - 2.0) * scale + x;
            y = (e.Y / (double)size * 4.0 - 2.0) * scale + y;

            if (e.Button == MouseButtons.Left)
                scale *= 0.5;
            else
                scale *= 2;

            ReloadTextbox();
            ShowMandel();
        }

        private void OkClick(object sender, EventArgs e)
        {
            if (double.TryParse(inputX.Text, out double x) && double.TryParse(inputY.Text, out double y))
            {
                this.x = x;
                this.y = y;
                // Console.WriteLine(x + " + "+ y);
            }

            if (double.TryParse(inputScale.Text, out double scale))
                this.scale = scale;
            if (int.TryParse(inputMax.Text, out int max))
                maxIterations = max;


            ReloadTextbox();
            ShowMandel();
        }

        private void PressedKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OkClick(null, null);

                // Moet dit hier ook staan als het ook vanuit OKClick wordt aangeroepen? 
                ShowMandel();
            }
        }

        private void ResetValues(object sender, EventArgs e)
        {
            this.x = 0; 
            this.y = 0;
            this.scale = 1.0;
            this.maxIterations = 300;
            ReloadTextbox();
            ShowMandel();
        }

        private void ReloadTextbox() 
        {
            inputX.Text = x.ToString();
            inputY.Text = y.ToString();
            inputScale.Text = scale.ToString();
            inputMax.Text = maxIterations.ToString();
        }

        private void Kleurtjes() {
            gradient = new Bitmap(1000, 1);
            LinearGradientBrush linGrBrush = new LinearGradientBrush(
                new Point(0, 0),
                new Point(500, 0),
                Color.Gold,
                Color.Navy
                );

            ColorBlend blend = new ColorBlend();
            blend.Colors = new Color[]
            {
                Color.White,
                Color.AliceBlue,
                Color.GreenYellow,
                Color.Purple,
                Color.Cornsilk,
                Color.Pink,
                Color.Black
            };
            blend.Positions = new float[]
            {
                0.0f,
                0.1f,
                0.2f,
                0.3f,
                0.5f,
                0.75f,
                1f
            };
            linGrBrush.InterpolationColors = blend;

            Graphics gfx = Graphics.FromImage(gradient);       
            gfx.FillRectangle(linGrBrush, 0, 0, 1000, 1);


        }
        

        void ShowMandel()
        {
            int width = buffer.Width;
            int height = buffer.Height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double a = x - width / 2;
                    double b = y - height / 2;
                    Complex c = new Complex(a, b) / (size / 4) * scale + new Complex(this.x, this.y);
                    double iterations = Mandelbrot(c) / maxIterations;
                    iterations = Math.Sqrt(iterations);
                    Color col = gradient.GetPixel((int)(iterations * gradient.Width), 0);
                    buffer.SetPixel(x, y, col);
                }
            }

            pictureBox.Refresh();
        }

        double Mandelbrot(Complex c)
        {
            Complex z = new Complex(0, 0);
            int it = 0;
            int itToDo = maxIterations;
            do
            {
                // f(z) = z^2 + c
                z = z * z + c;
                double mag = z.Magnitude;
                if(mag > 2)
                {
                    return Math.Max(0, it + 1 - Math.Log(Math.Log(mag, 2)));
                }
                it++;
            } while (it < itToDo);
            // http://www.iquilezles.org/www/articles/mset_smooth/mset_smooth.htm
            // https://www.codingame.com/playgrounds/2358/how-to-plot-the-mandelbrot-set/adding-some-colors
            // http://csharphelper.com/blog/2014/07/draw-a-mandelbrot-set-fractal-with-smoothly-shaded-colors-in-c/
            
            return it - 1;
        }

        static void Main(string[] args)
        {
            Application.Run(new Program());
        }
    }
}
