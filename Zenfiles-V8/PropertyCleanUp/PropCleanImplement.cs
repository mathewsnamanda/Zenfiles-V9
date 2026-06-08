using MFilesAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp6.PropertyCleanUp
{
    public class PropCleanImplement : IPropClean
    {
        public string cleaned(string text, Vault vault)
        {
            string newjson = text;
            string input = text; // Replace with your test string
            string pattern = "\"Property\"\\s*:\\s*\"[A-Z]{1,3}\\.\\w+\"";

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(input);
            HashSet<string> hashSet = new HashSet<string>();

            foreach (Match match in matches)
            {
                hashSet.Add(match.Value);
            }

            foreach (var match in hashSet)
            {
                var prop = vault.PropertyDefOperations.GetPropertyDefIDByAlias(match.Replace("\"", "").Split(":")[1].Trim());

                newjson = newjson.Replace(match, $"\"Property\":{prop}");
                 
            }

            return newjson;
        }

        public string classcleaned(string text, Vault vault)
        {
            string newjson = text;
            string input = text; // Replace with your test string
            string pattern = @"\""CL\.\w+(?:&\w+)*\""";



            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(input);
            HashSet<string> hashSet = new HashSet<string>();

            foreach (Match match in matches)
            {
                hashSet.Add(match.Value);
            }

            foreach (var match in hashSet)
            {
                var prop = vault.ClassOperations.GetObjectClassIDByAlias(match.Replace("\"", ""));

                newjson = newjson.Replace(match, $"{prop.ToString()}");
               

            }
            // Pattern to match double-quoted integers
            string patterndp = "\"(\\d+)\"(?!:)";

            // Replace each match with the integer value (without quotes)
            string output = Regex.Replace(newjson, patterndp, m => m.Groups[1].Value);

            newjson = output; // Output: Some numbers: 23 and 56 in quotes.
            return newjson;
        }
        public string objectcleaned(string text, Vault vault)
        {
            string newjson = text;
            string input = text; // Replace with your test string
            string pattern = @"\""OT\.\w+(?:&\w+)*\""";

            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(input);
            HashSet<string> hashSet = new HashSet<string>();

            foreach (Match match in matches)
            {
                hashSet.Add(match.Value);
            }
           

            foreach (var match in hashSet)
            {
              
                var prop = vault.ObjectTypeOperations.GetObjectTypeIDByAlias(match.Replace("\"", ""));

                newjson = newjson.Replace(match, $"{prop.ToString()}");

            }

            // Pattern to match double-quoted integers
            string patterndp = "\"(\\d+)\"(?!:)";

            // Replace each match with the integer value (without quotes)
            string output = Regex.Replace(newjson, patterndp, m => m.Groups[1].Value);

            newjson = output; // Output: Some numbers: 23 and 56 in quotes.
            return newjson;
        }

    }
}
