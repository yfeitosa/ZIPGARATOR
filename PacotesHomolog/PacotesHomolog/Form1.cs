using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace PacotesHomolog
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = @"C:\Sandboxes\CRK\FRT\";
            textBox2.Text = @"C:\Sandboxes\Pacotes\";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            var strDirectoryTemp = textBox1.Text;
            var strDirectoryFinal = textBox2.Text;

            string error = string.Empty;

            if (!Directory.Exists(strDirectoryTemp))
                error += label1.Text + "::: Diretório Inválido!! :::";

            if (!Directory.Exists(strDirectoryFinal))
                error += label2.Text + "::: Diretório Inválido!! :::";
            if (dateTimePicker1.Value.Date > dateTimePicker2.Value.Date)
                error += "::: Data Inválida!! :::";

            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error);
                return;
            }

            checkedListBox1.Items.Clear();
            FileInfo TheFile = new FileInfo(strDirectoryTemp);
            DirectoryInfo info = TheFile.Directory;
            BuscaArquivos(info);

            button2.Enabled = true;
            button1.Enabled = true;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            var strDirectoryTemp = textBox1.Text;
            var strDirectoryFinal = textBox2.Text;

            string error = string.Empty;
            if (!Directory.Exists(strDirectoryTemp))
                error += label1.Text + "::: Diretório Inválido!! :::";
            if (!Directory.Exists(strDirectoryFinal))
                error += label2.Text + "::: Diretório Inválido!! :::";
            if (dateTimePicker1.Value.Date > dateTimePicker2.Value.Date)
                error += "::: Data Inválida!! :::";
            if (checkedListBox1.CheckedItems.Count == 0)
                error += "Selecione os arquivos";

            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error);
                return;
            }

            var theFile = new FileInfo(strDirectoryFinal);
            var info = theFile.Directory;
            var diretorioWebUi = strDirectoryFinal.Substring(strDirectoryFinal.Length - 1) == "\\"
                                     ? strDirectoryFinal + "WebUI\\"
                                     : strDirectoryFinal + "\\WebUI\\";
            if (Directory.Exists(diretorioWebUi))
            {
                ApagaArquivos(new FileInfo(diretorioWebUi).Directory);
                ApagaDiretorios(new FileInfo(diretorioWebUi).Directory);
                Directory.Delete(diretorioWebUi);
                Directory.CreateDirectory(diretorioWebUi);
            }
            const string strDirectorydist = @"C:\Sandboxes\CRK\dist\";
            theFile = new FileInfo(strDirectorydist);
            info = theFile.Directory;
            var lista = new List<FileInfo>();
            foreach (var check in checkedListBox1.CheckedItems)
            {
                var file = new FileInfo(check.ToString());
                BuscaArquivos(info, file, ref lista);
            }

            listBox1.Items.Clear();
            lista.ForEach(x => listBox1.Items.Add(x.FullName));

            CreateItemsAndZip(lista);
            button2.Enabled = true;
        }

        private static void ApagaArquivos(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                file.Delete();
            }
            // busca arquivos do proximo sub-diretorio
            foreach (var subDir in dir.GetDirectories())
            {
                ApagaArquivos(subDir);
            }
        }
        private static void ApagaDiretorios(DirectoryInfo dir)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                ApagaDiretorios(subDir);
            }
            while (dir.GetDirectories().Count() > 0)
            {
                dir.GetDirectories().LastOrDefault().Delete();
            }
        }

        private void BuscaArquivos(DirectoryInfo dir)
        {
            var dias = dateTimePicker2.Value.Subtract(dateTimePicker1.Value).Days;
            foreach (var file in dir.GetFiles())
            {
                if (file.LastWriteTime < DateTime.Today.AddDays(-dias)) continue;
                var extensaoInvalida = new Extensoes()._lista.Any(x => file.Extension.ToLower().Contains(x));
                if (extensaoInvalida) continue;
                if (file.FullName.ToLower().Contains("resharper")) continue;

                checkedListBox1.Items.Add(file.FullName);
            }

            // busca arquivos do proximo sub-diretorio
            foreach (var subDir in dir.GetDirectories())
            {
                if (subDir.FullName.ToUpper().Contains("RESHARPER")) continue;
                BuscaArquivos(subDir);
            }
        }
        private void BuscaArquivos(DirectoryInfo dir, FileInfo item, ref List<FileInfo> lista)
        {
            var dias = dateTimePicker2.Value.Subtract(dateTimePicker1.Value).Days;
            foreach (var file in dir.GetFiles())
            {
                if (file.LastWriteTime < DateTime.Today.AddDays(-dias)) continue;
                var extensaoInvalida = new Extensoes()._listaComDll.Any(x => file.Extension.ToLower().Contains(x));
                if (extensaoInvalida) continue;
                if (file.FullName.ToLower().Contains("resharper")) continue;

                var fileName = file.Name.Replace(file.Extension, "");
                var itemName = item.Name.Replace(item.Extension, "");
                if (item.Extension.ToLower().Equals(".cs"))
                {
                    itemName = item.DirectoryName.Replace(@"\", "/").Split('/')[item.DirectoryName.Replace(@"\", "/").Split('/').Length - 1];
                    if (!itemName.Contains("."))
                    {
                        itemName = item.FullName.Replace(@"\", "/").Split('/')[5];//diretorio do webui
                    }
                }

                if (fileName != itemName) continue;
                lista.Add(file);
            }

            // busca arquivos do proximo sub-diretorio
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                if (subDir.FullName.ToUpper().Contains("RESHARPER")) continue;
                BuscaArquivos(subDir, item, ref lista);
            }
        }
        private void CreateItemsAndZip(IEnumerable<FileInfo> lista)
        {
            var caminhoZip = textBox2.Text;
            foreach (var file in lista)
            {
                string maph = file.DirectoryName;
                maph = maph.Substring(22, maph.Length - 22).Replace(@"\", "/");
                string[] pastas = maph.Split('/');
                string novoDiretorio = caminhoZip;
                foreach (var p in pastas)
                {
                    novoDiretorio = string.Format(@"{0}\{1}", novoDiretorio, p);
                    if (!Directory.Exists(novoDiretorio))
                        Directory.CreateDirectory(novoDiretorio);
                }

                file.CopyTo(string.Format(@"{0}\{1}", novoDiretorio, file.Name), true);
            }

            //Compress(caminhoZip);
            CriarBatch();
        }

        public static void Compress(string caminhoZip)
        {
            System.Diagnostics.Process.Start(@"C:/Pacotes.bat");
            //var zipName = string.Format("\\{0:yyyyMMdd}_{0:HH}{0:mm}_Pacote_Front.zip", DateTime.Now);

            //var zipOutPut = new ZipOutputStream(File.Create(caminhoZip + zipName));
            //zipOutPut.SetLevel(9);//Compactação level 9
            //zipOutPut.Finish();
            //zipOutPut.Close();

            //var zip = new ZipFile(caminhoZip + zipName);
            //zip.BeginUpdate();//Inicia a criação do ZIP

            ////Adicionando arquivos previamente criados ao zipFile
            //var diretorio = caminhoZip.Substring(caminhoZip.Length - 1) == "\\"
            // ? caminhoZip + "WebUI"
            // : caminhoZip + "\\WebUI";
            //diretorio = diretorio.Replace("\\", "/");
            //zip.NameTransform = new ZipNameTransform(diretorio.Substring(0, diretorio.LastIndexOf("/")));
            //zip.AddDirectory(diretorio);

            ////diretorio = caminhoZip.Substring(caminhoZip.Length - 1) == "\\"
            //// ? caminhoZip + "Scripts\\"
            //// : caminhoZip + "\\Scripts\\";
            ////diretorio = diretorio.Replace("\\", "/");
            ////zip.NameTransform = new ZipNameTransform(diretorio.Substring(0, diretorio.LastIndexOf("/")));
            ////zip.Add(diretorio);

            //zip.CommitUpdate();
            //zip.Close();
        }

        public void CriarBatch()
        {
            try
            {
                //Criando um arquivo Batch
                System.IO.StreamWriter sw = new StreamWriter("C:\\Pacotes.bat", false);

                sw.WriteLine("@ECHO OFF");
                sw.WriteLine("cls");
                sw.WriteLine("ECHO INICIANDO COMPACTACAO");
                sw.WriteLine("set nome=%date:~6,10%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%_Pacote_Front");
                sw.WriteLine(@"cd C:\");
                sw.WriteLine(@"");
                sw.WriteLine(@"DEL C:\WebUI\*.* /S /F /Q");
                sw.WriteLine(@"RMDIR /S /Q  C:\WebUI\");
                sw.WriteLine(@"md WebUI ");
                sw.WriteLine(@"");
                //sw.WriteLine(@"DEL C:\Script\*.* /S /F /Q");
                //sw.WriteLine(@"RMDIR /S /Q  C:\Script\");
                //sw.WriteLine(@"md Script");
                sw.WriteLine(@"");
                sw.WriteLine(@"IF EXIST C:\Sandboxes\Pacotes\WebUI\ xcopy /E C:\Sandboxes\Pacotes\WebUI\*.* C:\WebUI\");
                //sw.WriteLine(@"IF EXIST C:\Sandboxes\Pacotes\Script\ xcopy /E C:\Sandboxes\Pacotes\Script\*.* C:\Script\");
                sw.WriteLine(@"");
                sw.WriteLine(@"IF EXIST C:\WebUI\ " + '\u0022' + @"C:\Program Files (x86)\WinRAR\WinRAR.exe" + '\u0022' + @" a -r C:\Sandboxes\Pacotes\%nome%.zip C:\WebUI\");
                //sw.WriteLine(@"IF EXIST C:\Script\ " + '\u0022' + @"C:\Program Files (x86)\WinRAR\WinRAR.exe" + '\u0022' + @" a -r C:\Sandboxes\Pacotes\%nome%.zip C:\Script\");
                sw.WriteLine(@"");
                sw.WriteLine(@"IF EXIST C:\Sandboxes\Pacotes\WebUI\ xcopy /E C:\Sandboxes\Pacotes\WebUI\*.* G:\Desenvolvimento.Net\FRT\Pacotes Homologação\%date:~6,10%\%date:~6,10%_%date:~3,2%\%date:~0,2%");
                //sw.WriteLine(@"IF EXIST C:\Sandboxes\Pacotes\Script\ xcopy /E C:\Sandboxes\Pacotes\Script\*.* G:\Desenvolvimento.Net\FRT\Pacotes Homologação\%date:~6,10%\%date:~6,10%_%date:~3,2%\%date:~0,2%");
                sw.WriteLine(@"");
                //sw.WriteLine(@"DEL C:\WebUI\*.* /S /F /Q");
                //sw.WriteLine(@"RMDIR /S /Q  C:\WebUI\");
                //sw.WriteLine(@"DEL C:\Script\*.* /S /F /Q");
                //sw.WriteLine(@"RMDIR /S /Q  C:\Script\");

                sw.Close();

                //Executando o arquivo que acabamos de criar
                var processo = new Process();
                processo.StartInfo.FileName = "C:\\Pacotes.bat";
                //Essa opção serve para executar o arquivo sem mostrar janela, oculto do usuário.
                processo.StartInfo.CreateNoWindow = true;
                processo.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processo.Start();

                while (!processo.HasExited)
                {

                }
                File.Delete("C:\\Pacotes.bat");
            }
            catch
            {
                
            }
        }

    }

    public class Extensoes
    {
        public List<string> _lista;
        public List<string> _listaComDll;

        public Extensoes()
        {
            Inicializar();
            InicializarComDll();
        }
        private void Inicializar()
        {
            _lista = new List<string>();
            _lista.Add(".db");
            _lista.Add(".dat");
            _lista.Add(".svn");
            _lista.Add(".svn-base");
            _lista.Add(".licenses");
            _lista.Add(".cache");
            _lista.Add(".caches");
            _lista.Add(".txt");
            _lista.Add(".dll");
            _lista.Add(".pdb");
            _lista.Add(".bat");
            _lista.Add(".resource");
            _lista.Add(".resources");
            _lista.Add("proj");
            _lista.Add(".user");
            _lista.Add(".exe");
            _lista.Add(".suo");
            _lista.Add(".tlb");
        }
        private void InicializarComDll()
        {
            _listaComDll = new List<string>();
            _listaComDll.Add(".db");
            _listaComDll.Add(".dat");
            _listaComDll.Add(".svn");
            _listaComDll.Add(".svn-base");
            _listaComDll.Add(".licenses");
            _listaComDll.Add(".cache");
            _listaComDll.Add(".caches");
            _listaComDll.Add(".txt");
            _listaComDll.Add(".bat");
            _listaComDll.Add(".resource");
            _listaComDll.Add(".resources");
            _listaComDll.Add("proj");
            _listaComDll.Add(".user");
            _listaComDll.Add(".exe");
            _listaComDll.Add(".suo");
            _listaComDll.Add(".tlb");
        }
    }
}
