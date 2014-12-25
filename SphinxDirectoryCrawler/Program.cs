using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using BrandonHaynes.Security.SipHash;
using System.Threading;
using System.Security.Cryptography;
using DamienG.Security.Cryptography;

namespace SphinxDirectoryCrawler
{
    class Program
    {
        private static readonly Regex re = new Regex("^" + Regex.Escape(Properties.Settings.Default.root_directory) + "\\\\" + Properties.Settings.Default.regex_pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string sprintf(string input, params object[] inpVars)
        {
            int i = 0;
            input = Regex.Replace(input, "%.", m => ("{" + i++ + "}"));
            return string.Format(input, inpVars);
        }

        public static long ToUnixTimestamp(DateTime target)
        {
            var date = new DateTime(1970, 1, 1, 0, 0, 0, target.Kind);
            var unixTimestamp = System.Convert.ToInt64((target - date).TotalSeconds);

            return unixTimestamp;
        }

        static string getConnString()
        {
            string connStr = sprintf("Server=%s;Database=%s;Uid=%s;Pwd=%s;UseCompression=False;",
                                        Properties.Settings.Default.mysql_host,
                                        Properties.Settings.Default.mysql_db,
                                        Properties.Settings.Default.mysql_user,
                                        Properties.Settings.Default.mysql_password
                                    );
            return connStr;
        }

        static string HashBytesToUInt64(byte[] input)
        {
            var hash = new SipHash(Encoding.Unicode.GetBytes("12345678"));
            var hex = BitConverter.ToString(hash.ComputeHash(input));
            hex = hex.Replace("-", "");
            var dec = UInt64.Parse(hex, System.Globalization.NumberStyles.HexNumber);

            return dec.ToString();
        }

        static string CombinedFilePathSegments(IDictionary<string, string> matches)
        {
            string combinedSegments = "";

            foreach (KeyValuePair<string, string> match in matches)
            {
                // do something with entry.Value or entry.Key
                combinedSegments += match.Value;
            }

            return combinedSegments;
        }

        static IDictionary<string, string> PathSegments(string file)
        {

            try
            {

                Match match = re.Match(file);

                if (match.Success)
                {
                    int count = match.Groups.Count;
                    string[] groupNames = re.GetGroupNames();
                    IDictionary<string, string> matches = new Dictionary<string, string>();


                    //string combinedSegments = "";

                    for (var i = 1; i < count; ++i)
                    {
                        matches[groupNames[i]] = match.Groups[i].ToString();
                        //combinedSegments += match.Groups[i];
                    }

                    //return combinedSegments;
                    return matches;
                }
                else
                {
                    return new Dictionary<string, string>();
                }
            }
            catch (Exception) { return new Dictionary<string, string>(); }

        }

        public static void execQuery(string query)
        {
            MySqlConnection myConnection = new MySqlConnection(getConnString());
            MySqlCommand myCommand = new MySqlCommand(query, myConnection);
            

            try
            {
                //myConnection.Open();
                myCommand.Connection.Open();
            }
            catch (Exception)
            {
                Console.WriteLine("Couldn\'t connect to the database.");
                return;
            }

            myCommand.CommandTimeout = int.MaxValue;

            //MySqlCommand myCommand = myConnection.CreateCommand();
            

            //myCommand.Connection = myConnection;

            //myCommand.CommandText = query;



            try
            {
                //MySqlDataReader rdr = myCommand.ExecuteReader();
                //var h = 0;
                //h = rdr.FieldCount;
                myCommand.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(sprintf("Exception of type: '%s' with massage: '%s'.", e.GetType(), e.Message));
            }
            finally
            {
                myConnection.Close();
            }

        }

        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew(); //start timer


            string query = "";

            query = sprintf("START TRANSACTION;DROP TABLE `%s`;COMMIT;", Properties.Settings.Default.mysql_temp_table);
            execQuery(query);

            query = sprintf("START TRANSACTION;CREATE TABLE `%s` LIKE `%s`;COMMIT;",
                                                Properties.Settings.Default.mysql_temp_table,
                                                Properties.Settings.Default.mysql_table
                                     );
            execQuery(query);

            //Thread.Sleep(5000);
            //string tempValue = "";
            //int tempValueCount = 0;
            //List<String> valueList = new List<String>();

            DirectoryInfo dirInfo = new DirectoryInfo(Properties.Settings.Default.root_directory);
            var files = from f in 
                        dirInfo.EnumerateFiles(Properties.Settings.Default.wildcard_pattern, SearchOption.AllDirectories).Where(file =>
                               re.IsMatch(file.FullName))
                        select f;


            MySqlConnection myConnection = new MySqlConnection(getConnString());

            try
            {
                myConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(sprintf("Exception of type: '%s' with massage: '%s'.", e.GetType(), e.Message));
                //Console.ReadLine();
                return;
            }

            //query = "SET autocommit=0;";
            //MySqlCommand myCommand = new MySqlCommand(query, myConnection);
            MySqlCommand myCommand = myConnection.CreateCommand();
            myCommand.CommandTimeout = int.MaxValue;
            MySqlTransaction myTrans;
            myTrans = myConnection.BeginTransaction();
            myCommand.Connection = myConnection;
            myCommand.Transaction = myTrans;

            

            //myCommand.Connection.Open();
            //myCommand.ExecuteNonQuery();


            foreach (FileInfo file in files)
            {

                string combinedSegments = "";
                
                string _query = "";
                IDictionary<string, string> matches = new Dictionary<string, string>();

                matches = PathSegments(file.FullName.ToLower());

                if (matches.Count == 0) { continue; }

                combinedSegments = CombinedFilePathSegments(matches);

                string Id = HashBytesToUInt64(Encoding.UTF8.GetBytes(combinedSegments));

                //myCommand.CommandText = sprintf("INSERT INTO `%s` (`id`, `path`, `modified_at`) VALUES (%s,'%s', %s);", Properties.Settings.Default.mysql_temp_table, Id, file.FullName.ToLower().Replace("\\", "\\\\"), ToUnixTimestamp(file.LastWriteTimeUtc));

                _query = "INSERT INTO `%s` (`id`, `path`, `modified_at`";
                foreach (KeyValuePair<string, string> match in matches)
                {
                    _query += ", `" + match.Key + "`, `" + match.Key + "_hash`";
                }
                _query += ") VALUES (%s,'%s', %s";
                //_query += ") SELECT %s,'%s', %s";

                _query = sprintf(_query, Properties.Settings.Default.mysql_temp_table, Id, file.FullName.ToLower().Replace("\\", "\\\\"), ToUnixTimestamp(file.LastWriteTimeUtc));


                foreach (KeyValuePair<string, string> match in matches)
                {
                    //_query += ", " + match.Value;

                    byte[] hash;
                    //CRC32 crc32 = new CRC32();
                    //hash = crc32.ComputeHash(Encoding.UTF8.GetBytes(match.Value));

                    using (Crc32 crc32 = new Crc32())
                    {
                        hash = crc32.ComputeHash(Encoding.UTF8.GetBytes(match.Value));
                    }
                    
                    //using (SHA512 shaM = new SHA512Managed())
                    //{
                    //    hash = shaM.ComputeHash(Encoding.UTF8.GetBytes(match.Value));
                    //}

                    _query += ", '" + match.Value + "', " + UInt64.Parse(BitConverter.ToString(hash).Replace("-", ""), System.Globalization.NumberStyles.HexNumber);

                    //_query += ", " + (UInt64.Parse(BitConverter.ToString(hash, 0, 4).Replace("-", ""), System.Globalization.NumberStyles.HexNumber) << 32 | UInt64.Parse(BitConverter.ToString(hash, 4, 4).Replace("-", ""), System.Globalization.NumberStyles.HexNumber));

                    //_query += ", (SELECT CONV(SUBSTR(h, 1, 8), 16, 10) << 32 | CONV(SUBSTR(h, 9, 8), 16, 10) AS h FROM (SELECT SHA2('" + match.Value + "', 512) AS h) AS t)";
                }
                _query += ");";
                //_query += ";";


                myCommand.CommandText = _query;

                myCommand.ExecuteNonQuery();

                //myCommand = new MySqlCommand(query, myConnection);
                //myCommand.ExecuteNonQuery();


                //tempValue += sprintf("(%s,'%s',%s),", Id, file.FullName.ToLower().Replace("\\", "\\\\"), ToUnixTimestamp(file.LastWriteTimeUtc));
                //tempValueCount++;

                //if (tempValueCount == 10000)
                //{
                //    valueList.Add(tempValue);
                //    tempValueCount = 0;
                //    tempValue = "";
                //}

                //if (valueList.Count == 10)
                //{

                    
                //    bulkInsert(valueList);

                //    valueList.Clear();
                //}
                
            }

            //query = "COMMIT;";
            //myCommand = new MySqlCommand(query, myConnection);
            //myCommand.ExecuteNonQuery();
            myTrans.Commit();
            myConnection.Close();

            //if (valueList.Count > 0)
            //{

            //    bulkInsert(valueList);

            //    valueList.Clear();
            //}

            //if (tempValueCount > 0)
            //{
            //    valueList.Add(tempValue);
            //    tempValueCount = 0;
            //    tempValue = "";

            //    bulkInsert(valueList);

            //    valueList.Clear();
            //}

            //Thread.Sleep(5000);

            query = sprintf("START TRANSACTION;DELETE FROM `%s` WHERE `%s`.`id` IN ( SELECT sid FROM ( SELECT `%s`.`id` AS sid FROM `%s` INNER JOIN `%s` ON `%s`.`id` = `%s`.`id`) AS sub);COMMIT;",
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_temp_table,
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_temp_table
                                     );
            execQuery(query);

            //Thread.Sleep(5000);

            query = sprintf("START TRANSACTION;INSERT INTO `%s` (`id`) SELECT sid FROM ( SELECT `%s`.`id` AS sid FROM `%s` LEFT JOIN `%s` ON `%s`.`id` = `%s`.`id` WHERE `%s`.`id` IS NULL) AS sub;COMMIT;",
                                                
                                                Properties.Settings.Default.mysql_deleted_table,
                                                Properties.Settings.Default.mysql_table,
                                                Properties.Settings.Default.mysql_table,
                                                Properties.Settings.Default.mysql_temp_table,
                                                Properties.Settings.Default.mysql_table,
                                                Properties.Settings.Default.mysql_temp_table,
                                                Properties.Settings.Default.mysql_temp_table
                                     );
            execQuery(query);

            //Thread.Sleep(5000);

            query = sprintf("START TRANSACTION;DROP TABLE `%s`;COMMIT;",
                                                Properties.Settings.Default.mysql_table
                                     );
            execQuery(query);

            //Thread.Sleep(5000);

            query = sprintf("START TRANSACTION;RENAME TABLE `%s` TO `%s`;COMMIT;",
                                                Properties.Settings.Default.mysql_temp_table,
                                                Properties.Settings.Default.mysql_table
                                     );
            execQuery(query);


            sw.Stop(); //stop
            Console.WriteLine("" + sw.ElapsedMilliseconds + "ms"); //display the duration

            //Console.ReadLine();
        }
    }
}
