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
        PictureBox pictureBox;
        Bitmap buffer;

        TextBox inputX, inputY, inputScale, inputMax;

        ComboBox coordinatePreset, colorPreset;

        Color[] gradient;

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

            InitColors();

            ReloadTextboxes();
            ShowMandel();
        }

        private void CoordinatePresetSelected(object sender, EventArgs e)
        {
            // coordinatePreset
            // Wil liever een struct, waar naam, x, y en scale in staat die dan boven aan gedeclareerd kunnen worden.
            // Misschien opslaan als XML bestand ofzo? 
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
            ReloadTextboxes();
            ShowMandel();
        }

        private void ColorPresetSelected(object sender, EventArgs e)
        {
            // colorPreset

            ReloadTextboxes();
            ShowMandel();
        }

        private void OnResize(object sender, EventArgs e)
        {
            size = Math.Min(ClientSize.Width, ClientSize.Height);
            pictureBox.Size = new Size(ClientSize.Width, ClientSize.Height - 42);
            buffer = new Bitmap(pictureBox.Width, pictureBox.Height);
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

            ReloadTextboxes();
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
            x = 0; 
            y = 0;
            scale = 1.0;
            maxIterations = 300;
            ReloadTextboxes();
            ShowMandel();
        }

        private void ReloadTextboxes() 
        {
            inputX.Text = x.ToString();
            inputY.Text = y.ToString();
            inputScale.Text = scale.ToString();
            inputMax.Text = maxIterations.ToString();
        }

        private void InitColors() {
            // In deze functie creeren we een lookup-array voor de kleuroverloop
            // TODO: Meerdere gradients maken
            Bitmap gradientBmp = new Bitmap(1000, 1);

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

            Graphics gfx = Graphics.FromImage(gradientBmp);       
            gfx.FillRectangle(linGrBrush, 0, 0, 1000, 1);
            gradient = new Color[gradientBmp.Width];
            for (int i = 0; i < gradientBmp.Width; i++)
                gradient[i] = gradientBmp.GetPixel(i, 0);

        }
        // MAG WEG
        void testje()
        {
            Task[] tasks = new Task[buffer.Height];
            byte[][] bmp = new byte[buffer.Height][]; // Array van byte arrays

            for (int i= 0; i < buffer.Height; i++)
            {
                bmp[i] = new byte[buffer.Width * 3]; // Maak array aan van bytes * 3, vanwege RGB
                                                     // StartNew verwacht een action 
                // Is overbodig aangezien het direct in Task.Factory.StartNew wordt aangeroepen. 
                Action<object> taskStart = obj =>
                {
                    ProcessRowTest(bmp[i], i, buffer.Width, buffer.Height);
                };

                // tasks[i] = Task.Factory.StartNew(taskStart); // Dit werkt niet, snap niet waarom
                // Kennelijk oud, onderstaand is beter: tasks[i] = Task.Factory.StartNew( () => ProcessRowTest(bmp[i], i, buffer.Width, buffer.Height));
                tasks[i] = Task.Run( () => ProcessRowTest(bmp[i], i, buffer.Width, buffer.Height));

            }

            Task.WaitAll(tasks);
            Console.WriteLine("All done");

            BitmapData bmpData = buffer.LockBits(new Rectangle(0, 0, buffer.Width, buffer.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            for (int y = 0; y < buffer.Height; y++)
            {
                System.Runtime.InteropServices.Marshal.Copy(bmp[y], y * buffer.Width * 3, bmpData.Scan0, bmp[y].Length);
            }
            buffer.UnlockBits(bmpData);

        }
        // MAG WEG
        void ProcessRowTest(object obj, int y, int width, int height)
        // Je geeft hem een byte array mee, dat heb ik volgens mij nu ook gedaan. 
        {
            byte[] bmp = obj as byte[];
            for (int x = 0; x < width; x++)
            {
                double a = x - width / 2;
                double b = y - height / 2;
                Complex c = new Complex(a, b) / (size / 4) * scale + new Complex(this.x, this.y);
                double iterations = Mandelbrot(c) / maxIterations;
                bmp[x * 3 + 0] = (byte)(iterations * 255);
                bmp[x * 3 + 1] = (byte)(iterations * 255);
                bmp[x * 3 + 2] = (byte)(iterations * 255);
            }
        }

        void ShowMandel()
        {
            // Array om de tasks in op te slaan
            Task<byte[]>[] tasks = new Task<byte[]>[buffer.Height];
            int width = buffer.Width;
            int height = buffer.Height;
            for(int y = 0; y < height; y++)
            {
                int col = y;
                // Creeer voor elke rij pixels een task en gooi 'm in de array
                tasks[y] = Task.Run(() => ProcessRow(col, width, height));
            }
            // Wacht tot alle tasks klaar zijn
            Task.WaitAll(tasks);
            // Lock te hele bitmap zodat we rechtstreeks de kleurdata kunnen aanpassen. Sneller dan SetPixel() herhaaldelijk aanroepen
            BitmapData bmpData = buffer.LockBits(new Rectangle(0, 0, buffer.Width, buffer.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            for (int y = 0; y < buffer.Height; y++)
            {
                byte[] rgb = tasks[y].Result;
                // Kopieer de RGB data van elke task naar de juiste rij in de bitmap met wat pointer aritmathic
                System.Runtime.InteropServices.Marshal.Copy(rgb, 0, bmpData.Scan0 + y * buffer.Width * 3, rgb.Length);
            }
            buffer.UnlockBits(bmpData);
            pictureBox.Refresh();
        }

        byte[] ProcessRow(int y, int w, int h)
        {
            // Functie om één rij van de mandelbrot te renderen. Deze functie loopt asynchoon en geeft een byte array
            // met RGB waarden terug die later in de kleurdata van de bitmap geplaatst kan worden
            byte[] bmp = new byte[w * 3];
            for (int x = 0; x < w; x++)
            {
                double a = x - w / 2;
                double b = y - h / 2;
                Complex c = new Complex(a, b) / (size * 0.25) * scale + new Complex(this.x, this.y);
                double iterations = Mandelbrot(c) / maxIterations;

                Color color = gradient[(int)(iterations * gradient.Length)];
                bmp[x * 3 + 0] = color.R;
                bmp[x * 3 + 1] = color.G;
                bmp[x * 3 + 2] = color.B;
            }

            return bmp;
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
