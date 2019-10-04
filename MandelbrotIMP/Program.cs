using System;
using System.Numerics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;

namespace MandelbrotIMP
{
    class Program : Form
    {
        object renderLock = new object();

        PictureBox pictureBox;
        Bitmap buffer;

        TextBox inputX, inputY, inputScale, inputMax;
        FlowLayoutPanel panel;
        ComboBox coordinatePreset, palettePreset, colorModePreset;

        Color[] palette;
        int colorMode = 0;

        double scale = 1.953125E-3;
        Complex focus = new Complex(-1.0079296875, 0.3112109375);
        int maxIterations = 500;
        int size = 700;
        int width, height;

        string[] coordPresetNames = new string[] 
        {
            "Basis",
            "Vuur",
            "Zuilen",
            "Zigzag"
        };
        Complex[] focusPresets = new Complex[]
        {
                new Complex(0, 0),
                new Complex(-1.0079296875, 0.3112109375),
                new Complex(-0.1578125, 1.0328125),
                new Complex(-0.108625, 0.9014428),
        };
        double[] scalePresets = new double[]
        {
                1,
                1.953125E-3,
                1.5625E-4,
                3.8147E-8
        };

        string[] palettePresetNames = new string[]
        {
            "Space",
            "Zwart/Wit",
            "Oogpijn"
        };
        Color[][] palettePresets = new Color[][]
        {
            new Color[] {Color.White,Color.AliceBlue,Color.GreenYellow,Color.Purple,Color.Cornsilk,Color.Pink,Color.Black},
            new Color[] {Color.Black, Color.White},
            new Color[] {Color.Red, Color.Blue}
        };
        string[] colorModePresetNames = new string[]
        {
            "Smooth", "Modulo"
        };

        public Program()
        {
            ClientSize = new Size(size, size);
            Text = "Mandelbrot Viewer";
            KeyDown += PressedKey;
            ResizeEnd += OnResize;
            MaximizeBox = false;
            
            panel = new FlowLayoutPanel()
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
            };
            coordinatePreset.Items.AddRange(coordPresetNames);
            coordinatePreset.SelectedIndexChanged += CoordinatePresetSelected;
            coordinatePreset.SelectedIndex = 1;

            palettePreset = new ComboBox()
            {
                DropDownWidth = 280,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            palettePreset.Items.AddRange(palettePresetNames);
            palettePreset.SelectedIndexChanged += PalettePresetSelected;
            palettePreset.SelectedIndex = 0;

            colorModePreset = new ComboBox()
            {
                DropDownWidth = 280,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            colorModePreset.Items.AddRange(colorModePresetNames);
            colorModePreset.SelectedIndexChanged += ColorModePresetSelected;
            colorModePreset.SelectedIndex = 0;

            pictureBox = new PictureBox();
            pictureBox.Location = new Point(0, panel.Height);
            pictureBox.MouseClick += ClickFocus;

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
                palettePreset,
                colorModePreset,
                });
            Controls.Add(pictureBox);

            InitColors();

            ReloadTextboxes();
            OnResize(null, null);
        }

        private void ColorModePresetSelected(object sender, EventArgs e)
        {
            colorMode = colorModePreset.SelectedIndex;
            if(buffer != null) ShowMandel();
        }

        private void CoordinatePresetSelected(object sender, EventArgs e)
        {
            focus = focusPresets[coordinatePreset.SelectedIndex];
            scale = scalePresets[coordinatePreset.SelectedIndex];
            ReloadTextboxes();
            if (buffer != null) ShowMandel();
        }

        private void PalettePresetSelected(object sender, EventArgs e)
        {
            palette = palettePresets[palettePreset.SelectedIndex];
            if (buffer != null) ShowMandel();
        }

        private void OnResize(object sender, EventArgs e)
        {
            lock (renderLock)
            {
                width = ClientSize.Width;
                height = ClientSize.Height - panel.Height;

                pictureBox.Size = new Size(width, height);
                buffer = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                size = Math.Min(width, height);
                pictureBox.Image = buffer;
            }
            ShowMandel();
        }

        private void ClickFocus(object sender, MouseEventArgs e)
        {
            focus = PixelToComplex(e.X, e.Y);

            if (e.Button == MouseButtons.Left)
                scale *= 0.5;
            else
                scale *= 2;

            ReloadTextboxes();
            ShowMandel();
        }

        Complex PixelToComplex(int x, int y)
        {
            double a = x - width / 2;
            double b = y - height / 2;
            return new Complex(a, b) / (size * 0.25) * scale + focus;
        }

        private void OkClick(object sender, EventArgs e)
        {
            if (double.TryParse(inputX.Text, out double x) && double.TryParse(inputY.Text, out double y))
            {
                focus = new Complex(x, y);
            }

            if (double.TryParse(inputScale.Text, out double scale))
                this.scale = scale;
            if (int.TryParse(inputMax.Text, out int max))
                maxIterations = max;

            ShowMandel();
        }

        private void PressedKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                OkClick(null, null);
            }
        }

        private void ResetValues(object sender, EventArgs e)
        {
            focus = new Complex();
            scale = 1.0;
            maxIterations = 300;
            ReloadTextboxes();
            ShowMandel();
        }

        private void ReloadTextboxes() 
        {
            inputX.Text = focus.Real.ToString();
            inputY.Text = focus.Imaginary.ToString();
            inputScale.Text = scale.ToString();
            inputMax.Text = maxIterations.ToString();
        }

        private void InitColors() {
            // In deze functie creeren we een lookup-array voor de kleuroverloop
            // TODO: Meerdere gradients maken
            // Bitmap gradientBmp = new Bitmap(1000, 1);
            // 
            // LinearGradientBrush linGrBrush = new LinearGradientBrush(
            //     new Point(0, 0),
            //     new Point(500, 0),
            //     Color.Gold,
            //     Color.Navy
            //     );
            // 
            // ColorBlend blend = new ColorBlend();
            // blend.Colors = new Color[]
            // {
            //     Color.White,
            //     Color.AliceBlue,
            //     Color.GreenYellow,
            //     Color.Purple,
            //     Color.Cornsilk,
            //     Color.Pink,
            //     Color.Black
            // };
            // blend.Positions = new float[]
            // {
            //     0.0f,
            //     0.1f,
            //     0.2f,
            //     0.3f,
            //     0.5f,
            //     0.75f,
            //     1f
            // };
            // linGrBrush.InterpolationColors = blend;
            // 
            // Graphics gfx = Graphics.FromImage(gradientBmp);       
            // gfx.FillRectangle(linGrBrush, 0, 0, 1000, 1);
            // gradient = new Color[gradientBmp.Width];
            // for (int i = 0; i < gradientBmp.Width; i++)
            //     gradient[i] = gradientBmp.GetPixel(i, 0);

        }

        void ShowMandel()
        {
            // Array om de tasks in op te slaan
            Task<byte[]>[] tasks = new Task<byte[]>[height];
            lock (renderLock)
            {
                for (int y = 0; y < height; y++)
                {
                    int row = y;
                    // Creeer voor elke rij pixels een task en gooi 'm in de array
                    tasks[y] = Task.Run(() => ProcessRow(row));
                }
                // Wacht tot alle tasks klaar zijn
                Task.WaitAll(tasks);
                // Lock te hele bitmap zodat we rechtstreeks de kleurdata kunnen aanpassen. Sneller dan SetPixel() herhaaldelijk aanroepen
                BitmapData bmpData = buffer.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                for (int y = 0; y < height; y++)
                {
                    byte[] rgb = tasks[y].Result;
                    // Kopieer de RGB data van elke task naar de juiste rij in de bitmap met wat pointer aritmathic
                    System.Runtime.InteropServices.Marshal.Copy(rgb, 0, bmpData.Scan0 + y * width * 3, rgb.Length);
                }
                buffer.UnlockBits(bmpData);
                pictureBox.Refresh();
            }
        }

        byte[] ProcessRow(int y)
        {
            // Functie om één rij van de mandelbrot te renderen. Deze functie loopt asynchoon en geeft een byte array
            // met RGB waarden terug die later in de kleurdata van de bitmap geplaatst kan worden
            byte[] bmp = new byte[width * 3];
            for (int x = 0; x < width; x++)
            {
                Complex c = PixelToComplex(x, y);
                double iterations = Mandelbrot(c);
                Color color = GetColor(iterations);
                bmp[x * 3 + 0] = color.R;
                bmp[x * 3 + 1] = color.G;
                bmp[x * 3 + 2] = color.B;
            }

            return bmp;
        }

        Color GetColor(double iterations)
        {
            switch(colorMode)
            {
                case 0:
                    iterations /= maxIterations;
                    int index = (int)(iterations * palette.Length);
                    Color color = palette[index];
                    if (index < palette.Length - 1)
                    {
                        // Als we tussen twee kleuren inzitten: interpoleren (eenvoudiger dan het eruit ziet)
                        Color b = palette[index + 1];
                        double d = (iterations * palette.Length) % 1;
                        color = Color.FromArgb(
                            (int)(color.R * (1 - d) + b.R * d),
                            (int)(color.G * (1 - d) + b.G * d),
                            (int)(color.B * (1 - d) + b.B * d));
                    }
                    return color;
                case 1:
                    return palette[(int)(Math.Round(iterations) % palette.Length)];
            }
            return Color.Black;
        }

        double Mandelbrot(Complex c)
        {
            Complex z = new Complex(0, 0);
            for(int it = 0; it < maxIterations; it++)
            { 
                // f(z) = z^2 + c
                z = z * z + c;
                double mag = z.Magnitude;
                if(mag > 2)
                {
                // We returnen niet alleen het aantal iteraties als heel getal, maar ook een benadering van
                // het stuk achter de komma zodat onze kleuren overvloeien ipv trapsgewijs gaan.
                // 1 - log(log2(m)) bron: http://math.unipa.it/~grim/Jbarrallo.PDF
                    return Math.Max(0, it + 1 - Math.Log(Math.Log(mag, 2)));
                }
                it++;
            }
            
            return maxIterations - 1;
        }

        static void Main(string[] args)
        {
            Application.Run(new Program());
        }
    }

}
