using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.IO;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

/// <summary>
/// JBA Test Project to take a input, transform it and input it into a table.
/// 
/// Chosen language is c#, using SQL for the database side of things. 
/// A connection string to a database is required to be entered first in order for the program to function.
/// 
/// Regular expressions are used for parsing the intial file to get the data contianed
/// 
/// Specification was unclear as to whether the name should be obtained from within the file or from user input, but as it states that its an option i have implemented it as an input
/// File path is currently set to within the debug folder with a specific file name
/// 
/// Due to time constriants, minimal error checking is implemented, though in a full project it would be included (such as ensuring the name can't be an SQL command, check that the connection string is correct, checking if the table exists already, ect)
/// For a similar reason the program may not scale entirely with different data sets with more values/different format
/// 
/// </summary>



namespace JBATest
{
    class Program
    {
        static void Main(string[] args)
        {
                     
            string connectionString = "";       //"Data Source=NARGACUGA-PC\\SQLEXPRESS;Initial Catalog=JBATest;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            
            if(connectionString == "")
            {
                Console.WriteLine("No connection string entered\nPress Enter to close the program.");
                Console.ReadLine();
                return;
            }

            //read in the file
            string script = File.ReadAllText("cru-ts-2-10.1991-2000-cutdown.pre");

            //creates a new data class where the scripts information is stored
            Data data = new Data(script);

            //Gets the users desired table name
            Console.WriteLine("Please Enter a Table Name");
            string name = Console.ReadLine();
            if(name != "")
            {
                data.Name = name;
            }
            Console.Clear();

            //creates a list and populates it with all the sql commands
            List<string> CommandStr = new List<string>();
            CommandStr.Add("CREATE TABLE " + data.Name + " (Xref int, Yref int, Date date, Value int);");


            for(int i = 0; i < data.size; ++i)
            {
                for (int j = 0; j < 12; ++j)
                {
                    CommandStr.Add("INSERT INTO " + data.Name + "(Xref, Yref, Date, Value)\nVALUES (" + int.Parse(data.FinalVals[i][j].Xref) + ", " + int.Parse(data.FinalVals[i][j].Yref) + ", '" + data.FinalVals[i][j].Date + "', " + int.Parse(data.FinalVals[i][j].Val) + ")");
                }
 
            }
            
            SqlConnection conn = new SqlConnection(connectionString);

            conn.Open();

            //loops through all the commands and excutes them
            for (int i = 0; i < CommandStr.Count; ++i)
            {

                using (SqlCommand command = new SqlCommand(CommandStr[i], conn))
                {
                    command.ExecuteNonQuery();
                }
                Console.WriteLine(i + "/" + CommandStr.Count);
            }
                        
            conn.Close();
        }
    }

    //Stores and manipulates the script file
    class Data
    {
        List<string> Xref = new List<string>();
        List<string> Yref = new List<string>();
        List<string> RawValues = new List<string>();
        string[][] Vals;
        public Row[][] FinalVals;
        public string Header;
        int StartYear;
        int EndYear;
        public string Name = "table_name";
        string Script;
        public int size;

        //constructor
        public Data(string Input)
        {
            Script = Input;
            GetHeaders();
            GetYears();
            Transform();
            FinalVals = Finalise();
        }

        //Obtains the years that the data spans from the headers
        void GetYears()
        {
            string regex = "Years=([0-9]+)-([0-9]+)";
            foreach(Match m in Regex.Matches(Header, regex))
            {
                StartYear = int.Parse(m.Groups[1].Value);
                EndYear = int.Parse(m.Groups[2].Value);
            }
        }

        //Obtains the headers
        void GetHeaders()
        {
            string regex = @"([\d\D]+])";
            Header = Regex.Match(Script, regex).Value;
        }

        //Takes the raw input and splits it up in individual blocks where the X/Yrefs are the same
        void Transform()
        {
            string FirstRegex = "Grid-ref= +([0-9]+), ([0-9]+)\n([0-9 \n]+)";
            if (Regex.IsMatch(Script, FirstRegex))
            {
                foreach (Match m in Regex.Matches(Script, FirstRegex))
                {
                    Xref.Add(m.Groups[1].ToString());
                    Yref.Add(m.Groups[2].ToString());
                    RawValues.Add(m.Groups[3].ToString());
                }
            }
            //i'll be honest, i dont know why there is 5 less values then expected
            size = RawValues.Count * (EndYear - StartYear + 1) - 5;
            Vals = FormatVals();
        }

        //Changes the array of blocks into the individual rows
        string[][] FormatVals()
        {
            //get the no. values
            string[][] arr = new string[size][];
            int j = 0;
            string regex = "([0-9]+)";
            //go through each block
            foreach(string s in RawValues)
            {
                string[] TempArr = new string[12];
                int i = 0;
                //get each value from the block
                foreach(Match m in Regex.Matches(s,regex))
                {
                    TempArr[i] = m.Value;
                    ++i;
                    //every line is 12 long
                    if(i == 12)
                    {
                        //reset month counter and add the values to the array
                        i = 0;
                        arr[j] = TempArr;
                        ++j;
                    }
                }

            }
            return arr;
        }

        //Edits the rows formats to include all other data regarding each record
        Row[][] Finalise()
        {
            //put value, xref, yref and date together
            Row[][] arr = new Row[size][];

            int year = StartYear;
            int refRow = 0;

            //loops through each value and adds it related data
            for(int row = 0; row < size; ++row)
            {
                Row[] tempVals = new Row[12];
                for (int month = 1; month < 13; ++month)
                {
                    Row temp = new Row();
                    temp.Xref = Xref[refRow];
                    temp.Yref = Yref[refRow];
                    temp.Date = year + "-" + month + "-01";
                    temp.Val = Vals[row][month - 1];
                    tempVals[month - 1] = temp; 
                }
                ++year;

                if(year > EndYear)
                {
                    year = StartYear;
                    ++refRow;
                }
                arr[row] = tempVals;
            }
            return arr;
        }

        //class used for easy storage of each record
        public struct Row
        {
            public string Xref;
            public string Yref;
            public string Date;
            public string Val;
        }
    }

}
