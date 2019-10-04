using System;
using System.Numerics;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace MandelbrotIMP
{
    /*
     * Imperatief programmeren - Prakticum 1
     * Door: Max Veerboek & Freek Struijs
     * 
     * Bevat o.a. de volgende functies:
     * - Wordt snel gerenderd door gebruik van Tasks voor multithreading
     * - Naast links/rechts klikken om in/uit te zoemen, kun je ook slepen met de muis
     * - Verschillende paletten en verschillende modi om de kleuren te sampelen van die paletten
     * - Scherm kan dynamisch van grootte worden veranderd
     * - Preset locaties (sommigen hebben een vrij hoge iteratie-count nodig)
     * - Als het goed is makkelijk te begrijpen code, voorzien van comments
     * 
     */

    class Program : Form
    { 
        // Controls
        PictureBox pictureBox;
        TextBox inputX, inputY, inputScale, inputMax;
        FlowLayoutPanel panel;
        ComboBox coordinatePreset, palettePreset, colorModePreset;

        Bitmap buffer;

        // Gebruikersinstellingen 
        double scale;
        Complex focus;
        int maxIterations = 500;
        Color[] palette;
        int colorMode = 0;

        // Schermformaat
        int size = 800;
        int width, height;

        // Variabelen voor slepen
        bool dragging;
        Complex dragPos;
        Point dragStart;

        // Data voor dropdown-menu's
        string[] coordPresetNames = new string[] 
        {
            "Basis",
            "Schuin",
            "Tunnel",
            "Ster",
            "Schelp",
            "Slierten"
        };
        Complex[] focusPresets = new Complex[]
        {
            new Complex(0, 0),
            new Complex(-1.0079296875, 0.3112109375),
            new Complex(-0.979572916666666, 0.266216145833333),
            new Complex(-0.562205729166666,-0.642822916666666),
            new Complex(-1.25066, 0.02012),
            new Complex(-0.108625, 0.9014428),
        };
        double[] scalePresets = new double[]
        {
            1,
            1.953125E-3,
            0.000244140625,
            0.0001220703125,
            1.7E-4,
            3.8147E-8,
        };

        string[] palettePresetNames = new string[]
        {
            "Space",
            "Zwart/Wit",
            "Magma",
            "Natuur",
            "Oogpijn"
        };
        Color[][] palettePresets = new Color[][]
        {
            new Color[] {Color.White,Color.AliceBlue,Color.GreenYellow,Color.Purple,Color.Cornsilk,Color.Pink,Color.Black},
            new Color[] {Color.Black, Color.White},
            new Color[] {Color.Yellow, Color.Gold, Color.DarkGoldenrod, Color.Orange, Color.OrangeRed, Color.IndianRed, Color.Red },
            new Color[] {Color.LemonChiffon, Color.Aqua, Color.LimeGreen, Color.RosyBrown, Color.AliceBlue, Color.DarkGreen, Color.DarkSeaGreen },
            new Color[] {Color.Red, Color.Blue}
        };
        string[] colorModePresetNames = new string[]
        {
            "Smooth", "Simpel", "Modulo", "Gemengd"
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
                Text = "Iteraties",
                Size = new Size(60, 14),
            };
            Label labelCoordPreset = new Label()
            {
                Text = "Locatie",
                Size = new Size(60, 14)
            };
            Label labelPalettePreset = new Label()
            {
                Text = "Palet",
                Size = new Size(60, 14)
            };
            Label labelColorModePreset = new Label()
            {
                Text = "Kleurmode",
                Size = new Size(60, 14)
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
                DropDownWidth = 120,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            coordinatePreset.Items.AddRange(coordPresetNames);
            coordinatePreset.SelectedIndexChanged += CoordinatePresetSelected;
            coordinatePreset.SelectedIndex = 1;

            palettePreset = new ComboBox()
            {
                DropDownWidth = 120,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            palettePreset.Items.AddRange(palettePresetNames);
            palettePreset.SelectedIndexChanged += PalettePresetSelected;
            palettePreset.SelectedIndex = 0;

            colorModePreset = new ComboBox()
            {
                DropDownWidth = 120,
                Size = new Size(80, 12),
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
            colorModePreset.Items.AddRange(colorModePresetNames);
            colorModePreset.SelectedIndexChanged += ColorModePresetSelected;
            colorModePreset.SelectedIndex = 0;

            // Picturebox om de mandelbrot in de renderen
            pictureBox = new PictureBox();
            pictureBox.Location = new Point(0, panel.Height);

            pictureBox.MouseDown += OnMouseDown;
            pictureBox.MouseMove += OnMouseMove;
            pictureBox.MouseUp += OnMouseUp;

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
                labelCoordPreset,
                coordinatePreset,
                labelPalettePreset,
                palettePreset,
                labelColorModePreset,
                colorModePreset,
                });
            Controls.Add(pictureBox);

            Load += OnResize; 

            ReloadTextboxes();
        }

        // Event handlers
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            dragStart = e.Location;
            dragPos = PixelToComplex(e.X, e.Y);
            dragging = true;
        }
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!dragging) return;
            focus -= PixelToComplex(e.X, e.Y) - dragPos;
            dragPos = PixelToComplex(e.X, e.Y);

            ShowMandelbrot();
        }
        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;

            if(Math.Abs(e.X - dragStart.X) + Math.Abs(e.Y - dragStart.Y) < 10)
            {
                // Interpreteer het als klik om te zoomen als de muis bijna niet is bewogen
                focus = PixelToComplex(e.X, e.Y);

                if (e.Button == MouseButtons.Left)
                    scale *= 0.5;
                else
                    scale *= 2;
                ShowMandelbrot();

            }
            ReloadTextboxes();
        }

        private void ColorModePresetSelected(object sender, EventArgs e)
        {
            colorMode = colorModePreset.SelectedIndex;
            if(buffer != null) ShowMandelbrot();
        }

        private void CoordinatePresetSelected(object sender, EventArgs e)
        {
            focus = focusPresets[coordinatePreset.SelectedIndex];
            scale = scalePresets[coordinatePreset.SelectedIndex];
            ReloadTextboxes();
            if (buffer != null) ShowMandelbrot();
        }

        private void PalettePresetSelected(object sender, EventArgs e)
        {
            palette = palettePresets[palettePreset.SelectedIndex];
            if (buffer != null) ShowMandelbrot();
        }

        private void OnResize(object sender, EventArgs e)
        {
            width = ClientSize.Width;
            height = ClientSize.Height - panel.Height;

            pictureBox.Size = new Size(width, height);
            buffer = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            size = Math.Min(width, height);
            pictureBox.Image = buffer;
            
            ShowMandelbrot();
        }


        private void OkClick(object sender, EventArgs e)
        {
            if (double.TryParse(inputX.Text, out double x) && double.TryParse(inputY.Text, out double y))
            {
                focus = new Complex(x, y);
            }
            else MessageBox.Show("De door u ingevulde locatie is ongeldig");

            if (double.TryParse(inputScale.Text, out double scale) && scale > 0)
                this.scale = scale;
            else MessageBox.Show("De door u ingevulde schaal is ongeldig");

            if (int.TryParse(inputMax.Text, out int max) && max > 0)
                maxIterations = max;
            else MessageBox.Show("Het door u ingevulde iteratielimiet is ongeldig");
            
            ShowMandelbrot();
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
            maxIterations = 500;
            ReloadTextboxes();
            ShowMandelbrot();
        }

        private void ReloadTextboxes() 
        {
            inputX.Text = focus.Real.ToString();
            inputY.Text = focus.Imaginary.ToString();
            inputScale.Text = scale.ToString();
            inputMax.Text = maxIterations.ToString();
        }

        // Functie om scherm-coordinaten te transformeren naar mandelbrot-coordinaten, adhv de huidige x, y, scale en schermgrootte
        Complex PixelToComplex(int x, int y)
        {
            double a = x - width / 2;
            double b = y - height / 2;
            return new Complex(a, b) / (size * 0.25) * scale + focus;
        }

        // Functie voor het daadwerkelijk renderen van de Mandelbrot
        void ShowMandelbrot()
        {
            // Array om de tasks in op te slaan
            Task<byte[]>[] tasks = new Task<byte[]>[height];

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
                System.Runtime.InteropServices.Marshal.Copy(rgb, 0, bmpData.Scan0 + y * bmpData.Stride, rgb.Length);
            }
            // Afbeelding vrijgeven en picturebox informeren dat hij is veranderd
            buffer.UnlockBits(bmpData);
            pictureBox.Refresh();
            
        }

        // Functie om één rij van de mandelbrot te renderen. Deze functie loopt asynchoon en geeft een byte array
        // met RGB waarden terug die later in de kleurdata van de bitmap geplaatst kan worden
        byte[] ProcessRow(int y)
        {
            byte[] bmp = new byte[width * 3];
            for (int x = 0; x < width; x++)
            {
                Complex c = PixelToComplex(x, y);
                double iterations = Mandelbrot(c);
                Color color = GetColor(iterations);
                // RGB data in de pixel array gooien. Volgorde is hier blijkbaar omgekeerd
                bmp[x * 3 + 0] = color.B;
                bmp[x * 3 + 1] = color.G;
                bmp[x * 3 + 2] = color.R;
            }

            return bmp;
        }

        // Functie om de kleur bij het mandelbrotgetal te vinden, adhv huidige kleurmodus en palet
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
                    iterations /= maxIterations;
                    index = (int)(iterations * palette.Length);
                    return palette[index];
                case 2:
                    return palette[(int)(Math.Round(iterations) % palette.Length)];
                case 3:
                    // Pakt de R waarde van Smooth, G waarde van modulo, en B van simpel
                    byte red, green, blue;
                    green = palette[(int)(Math.Round(iterations) % palette.Length)].G;
                    iterations /= maxIterations;
                    index = (int)(iterations * palette.Length);
                    color = palette[index];
                    red = color.R;
                    blue = color.B;
                    if (index < palette.Length - 1)
                    {
                        Color b = palette[index + 1];
                        double d = (iterations * palette.Length) % 1;
                        red = (byte)(color.R * (1 - d) + b.R * d);
                    }
                    return Color.FromArgb(red, green, blue);
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
                    // -log(log2(m)) bron: http://math.unipa.it/~grim/Jbarrallo.PDF
                    return Math.Max(0, it - Math.Log(Math.Log(mag, 2)));
                }
                it++;
            }

            return maxIterations - 1;
        }

        static void Main()
        {
            Application.Run(new Program());
        }
    }

}
