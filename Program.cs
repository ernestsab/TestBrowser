using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Web.Script.Serialization;
using System.Net.Http;

namespace TestBrowser
{
    class Program
    {
        static WebBrowser wb;
        static Form f1;
        private static readonly HttpClient client = new HttpClient();
        static List<Person> persons;
        static string username = "TestgebruikerZvJ1";
        static string password = "q8!:2R$sPb[S";
        static string filename = "output.json";
        [STAThread]
        static void Main(string[] args)
        {
            string passwordString = "Using default password";
            for (int i = 0; i < args.Length-1; i++)
            {
                if (args[i] == "-u")
                {
                    username = args[i + 1];
                }
                if (args[i] == "-p")
                {
                    password = args[i + 1];
                    passwordString = "Using given password";
                }
                if (args[i] == "-o")
                {
                    filename = args[i + 1];
                }
                if (args[i] == "-h")
                {
                    printHelp();
                    return;
                }
            }
            if (args.Length>0 && args[0] == "-h")
            {
                printHelp();
                return;
            }
            Console.WriteLine("Using username: " + username);
            Console.WriteLine(passwordString);
            Start();

        }
        static void printHelp()
        {
            Console.WriteLine("educatie.izorgvoorjeugd.nl parser Help page");
            Console.WriteLine("Options:");
            Console.WriteLine("-u [username] \t\t Set the used username");
            Console.WriteLine("-p [password] \t\t Set the used password");
            Console.WriteLine("-o [filename] \t\t Set the filename used as output");
        }
        static void Start()
        {
            if (f1 == null)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                f1 = new System.Windows.Forms.Form();
                f1.Width = 1000;
                f1.Height = 700;
                f1.Load += F1_Load;
                wb = new WebBrowser();
                f1.SuspendLayout();
                f1.Controls.Add(wb);
                f1.ShowInTaskbar = false;
                f1.Shown += F1_Shown;
                wb.AllowNavigation = true;
                wb.DocumentCompleted += Wb_DocumentCompleted;
                wb.FileDownload += Wb_FileDownload;
                wb.Dock = DockStyle.Fill;
                
                persons = new List<Person>();
                Application.Run(f1);
            }
            f1.Refresh();
        }

        private static void F1_Shown(object sender, EventArgs e)
        {
            f1.Hide();
        }

        private static void F1_Load(object sender, EventArgs e)
        {
            f1.Hide();
            wb.Navigate(new Uri("http://educatie.izorgvoorjeugd.nl/zvj/page/logon/logon.jsf"));
        }

        static int downloadcount = 0;
        private static void Wb_FileDownload(object sender, EventArgs e)
        {
            downloadcount++;
        }
        static int loginAttempts = 0;
        static System.Windows.Forms.Timer t1;
        static System.Windows.Forms.Timer t2;
        private static void Wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser br = sender as WebBrowser;
            if (br.Document.GetElementById("contentdataform:logon_pwd") != null)
            {
                if (loginAttempts > 0)
                {
                    Console.WriteLine("Wrong credentials");
                    Application.ExitThread();
                    return;
                }
                loginAttempts++;
                br.Document.GetElementById("contentdataform:pageFocus").SetAttribute("value", username);
                br.Document.GetElementById("contentdataform:logon_pwd").SetAttribute("value", password);
                br.Document.GetElementById("contentdataform:pageFocus").Focus();
                br.Document.GetElementById("contentdataform:logon_pwd").Focus();
                
                t1 = new System.Windows.Forms.Timer();
                t1.Tick += WaitForCompanySelect;
                t1.Interval = 200;
                t1.Start();

            }
            else if (br.Document.GetElementById("contentdataform:button_gebruikerSearchManager_search")!=null)
            {
              
                br.Document.GetElementById("contentdataform:button_gebruikerSearchManager_search").InvokeMember("click");
                if (t2 == null)
                {
                    Console.WriteLine("Parsing profiles...");
                    Console.WriteLine();
                }
                t2 = new System.Windows.Forms.Timer();
                t2.Tick += T2_Tick;
                t2.Interval = 200;
                t2.Start();
            }
            else
            {
                var person = new Person();
                var elms = wb.Document.GetElementsByTagName("span");
                foreach(HtmlElement elm in elms)
                {
                    if (elm.InnerHtml == "Telefoonnummer")
                    {
                        var next = elm.NextSibling;
                        person.PhoneNumber = next.GetAttribute("value");
                    }
                    if (elm.InnerHtml == "Achternaam")
                    {
                        var next = elm.NextSibling;
                        person.LastName = next.GetAttribute("value");
                    }
                    if (elm.InnerHtml == "Voornaam")
                    {
                        var next = elm.NextSibling;
                        person.FirstName = next.GetAttribute("value");
                    }
                    if (elm.InnerHtml == "Kenmerk gebruiker")
                    {
                        var next = elm.NextSibling;
                        person.KenmerkGebruiker = next.InnerHtml;
                    }
                }
                persons.Add(person);
                Console.Write("\r Profiles found: {0}   ", persons.Count);
                elms = wb.Document.GetElementsByTagName("a");
                foreach (HtmlElement elm in elms)
                {
                    if (elm.GetAttribute("className") == "menuItem menuItemSelected")
                    {
                        elm.InvokeMember("click");
                    }
                }

            }
        }
        static int lastIndex = -1;
        static int checkIndex = 1;
        private async static void T2_Tick(object sender, EventArgs e)
        {
            var elms = wb.Document.GetElementsByTagName("td");
            var nextButtonPossibs = wb.Document.GetElementsByTagName("a");
            HtmlElement nextButton = null;
            foreach(HtmlElement elm in nextButtonPossibs)
            {
                if (elm.GetAttribute("className") == "icon next")
                {
                    nextButton = elm;
                }
            }
            var broken = false;
            var found = false;
            foreach(HtmlElement elm in elms)
            {
                
                if (elm.GetAttribute("className") == "gebruikersnaamColumn")
                {
                    t2.Stop();
                    checkIndex++;
                    found = true;
                    if (checkIndex > lastIndex)
                    {
                        lastIndex = checkIndex;
                        checkIndex = 1;
                        broken = true;
                        elm.GetElementsByTagName("a")[0].InvokeMember("Click");
                        break;
                    }
                   
                    
                    
                }
            }
            if (!broken && found)
            {
                if (nextButton != null)
                {
                    nextButton.InvokeMember("Click");
                    t2 = new System.Windows.Forms.Timer();
                    t2.Tick += T2_Tick;
                    t2.Interval = 500;
                    t2.Start();
                }
                else
                {
                    ClearCurrentConsoleLine();
                    
                    Console.WriteLine(persons.Count+" profiles parsed");
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    string s = ser.Serialize(persons);
                    Console.WriteLine("Writing output file " + filename);
                    System.IO.File.WriteAllText(filename, s);
                    Console.WriteLine("Done!");
                    
                    Application.ExitThread();

                }
            }
            
        }

        private static void WaitForCompanySelect(object sender, EventArgs e)
        {
            if (wb.Document.GetElementsByTagName("option").Count > 0)
            {
                wb.Document.GetElementById("contentdataform:button_logonManager_logon").InvokeMember("Click");
                Console.WriteLine("Logging in");
                t1.Stop();
            }
            else
            {
                wb.Document.GetElementById("contentdataform:pageFocus").Focus();
                wb.Document.GetElementById("contentdataform:logon_pwd").Focus();
            }
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }


    }
    

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string KenmerkGebruiker { get; set; }
    }
}
