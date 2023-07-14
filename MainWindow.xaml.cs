using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace easyeda_importer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _path = "";
        public string KiCadPath
        {
            get { return _path; }
            set { _path = value; SaveSettings(); Changed("KiCadPath"); }
        }
        
        private string _libPath = "";
        public string LibraryPath
        {
            get { return _libPath; }
            set { _libPath = value; SaveSettings(); Changed("LibraryPath"); }
        }
        
        private string _ids = "";
        public string LcscIds
        {
            get { return _ids; }
            set { _ids = value; Changed("LcscIds"); }
        }
        
        private string _output = "";
        public string Output
        {
            get { return _output; }
            set { _output = value; Changed("Output"); }
        }
        
        private bool _symbol = true;
        public bool ImportSymbol
        {
            get { return _symbol; }
            set { _symbol = value; Changed("ImportSymbol"); }
        }

        private bool _footprint = true;
        public bool ImportFootprint
        {
            get { return _footprint; }
            set { _footprint = value; Changed("ImportFootprint"); }
        }

        private bool _model = true;
        public bool ImportModel
        {
            get { return _model; }
            set { _model = value; Changed("ImportModel"); }
        }

        private bool _canDownload = true;
        public bool CanDownload
        {
            get { return _canDownload; }
            set { _canDownload = value; Changed("CanDownload"); }
        }

        public MainWindow()
        {
            InitializeComponent();

            if(!System.IO.File.Exists("settings.txt"))
            {
                List<string> paths = new List<string>()
                {
                    @"C:\Program Files\KiCad\7.0",
                    @"C:\Program Files\KiCad\6.0",
                    @"C:\Program Files\KiCad\5.0"
                };
                
                foreach(string path in paths)
                    if(System.IO.Directory.Exists(path))
                        KiCadPath = path;
            } else {
                string[] settings = System.IO.File.ReadAllText("settings.txt").Split("###");
                KiCadPath = settings[0];
                LibraryPath = settings[1];
            }

            
            this.DataContext = this;
        }

        private void SaveSettings()
        {
            System.IO.File.WriteAllText("settings.txt", KiCadPath + "###" + LibraryPath);
        }

        private async void ClickStart(object sender, RoutedEventArgs e)
        {
            CanDownload = false;
            Process p = new Process();

            if(!System.IO.Directory.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py")))
            {
                MessageBox.Show("Lade nun easyeda Skript runter.");
                p.StartInfo.FileName = "git";
                p.StartInfo.Arguments = "clone https://github.com/uPesy/easyeda2kicad.py " + System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py");
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                ReadOutput(p);
                ReadError(p);
                await p.WaitForExitAsync();
                
                MessageBox.Show("Installiere nun Skript");

                string content = System.IO.File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py", "setup.py"));
                content = content.Replace("pydantic>=1.5", "pydantic==1.10");
                System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py", "setup.py"), content);

                p = new Process();
                p.StartInfo.WorkingDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py");
                p.StartInfo.FileName = System.IO.Path.Combine(KiCadPath, "bin", "python.exe");
                p.StartInfo.Arguments = "setup.py install";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                ReadOutput(p);
                ReadError(p);
                await p.WaitForExitAsync();

                MessageBox.Show("Geklont");
            }

            string downloadtype = " ";
            if(ImportFootprint && ImportModel && ImportSymbol)
                downloadtype = "--full ";
            else {
                if(ImportSymbol) downloadtype += "--symbol ";
                if(ImportFootprint) downloadtype += "--footprint ";
                if(ImportModel) downloadtype += "--3d ";
            }

            string lib = "";
            if(!string.IsNullOrEmpty(LibraryPath))
                lib = " --output " + LibraryPath;

            foreach(string id in LcscIds.Split(" "))
            {
                p = new Process();
                p.StartInfo.WorkingDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "easyeda2kicad.py");
                p.StartInfo.FileName = System.IO.Path.Combine(KiCadPath, "bin", "python.exe");
                p.StartInfo.Arguments = $"-m easyeda2kicad {downloadtype}--lcsc_id={id}{lib}";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                ReadOutput(p);
                ReadError(p);
                await p.WaitForExitAsync();
            }
            Output = "Abgeschlossen" + Environment.NewLine + Output;
            CanDownload = true;
        }

        private async void ReadOutput(Process p)
        {
            try{
                while(!p.HasExited)
                {
                    string x = await p.StandardOutput.ReadLineAsync();
                    if(string.IsNullOrEmpty(x)) continue;
                    Output = x + Environment.NewLine + Output;
                    Console.WriteLine(x);
                }
            } catch{}
            System.Console.WriteLine("Exited Output");
        }

        private async void ReadError(Process p)
        {
            try{
                while(!p.HasExited)
                {
                    string x = await p.StandardError.ReadLineAsync();
                    if(string.IsNullOrEmpty(x)) continue;
                    Output = x + Environment.NewLine + Output;
                    Console.WriteLine(x);
                }
            } catch{}
            System.Console.WriteLine("Exited Error");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
