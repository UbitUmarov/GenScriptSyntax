using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;


namespace GenScriptSyntax
{
    public struct ConstInfo
    {
        public string name;
        public string type;
        public string value;
        public string desc;
    }

    public struct ArgsInfo
    {
        public string name;
        public string type;
    }

    public struct FunctionInfo
    {
        public string name;
        public string type;
        public List<ArgsInfo> args;
        public string desc;
    }

    class Program
    {
        private static int CompareByName(ConstInfo a, ConstInfo b)
        {
            string x = a.name;
            string y = b.name;
            return x.CompareTo(y);
        }

        private static int CompareByName(FunctionInfo a, FunctionInfo b)
        {
            string x = a.name.ToLower();
            string y = b.name.ToLower();
            return x.CompareTo(y);
        }


        static string TypeToString(string t)
        {
            string ts = t.ToLower();
            int indx = ts.IndexOf('.');
            if (indx > 0)
                ts = ts.Substring(indx + 1);
            string ret = ts switch
            {
                "bool" or "boolean" or "int" or "int32" or "int64" or "lslinteger" or "lsl_integer" => "integer",
                "float" or "double" or "lslfloat" or "lsl_float" => "float",
                "string" or "lslstring" or "lsl_string" or "readonlyspan<char>" => "string",
                "key" or "lslkey" or "lsl_key" => "key",
                "quaternion" or "rotation" or "lslrotation" or "lsl_rotation" => "rotation",
                "vector" or "lslvector" or "lsl_vector" or "vector3" => "vector",
                "list" or "lsllist" or "lsl_list" => "list",
                "void" => string.Empty,
                _ => string.Empty,
            };
            return ret;
        }

        public static void SetEmpty(ref string str) => str = string.Empty;

        static void Main(string[] args)
        {
            string OpensimRuntimePath = @"D:\opensim\opensim\OpenSim\Region\ScriptEngine\Shared\Api\Runtime";
            string OpensimInterfacePath = @"D:\opensim\opensim\OpenSim\Region\ScriptEngine\Shared\Api\Interface";
            string outputPath = string.Empty;

            if (args.Length > 0)
            {
                string OpensimBase = args[0];
                if (!Path.Exists(OpensimBase))
                {
                    Console.WriteLine("Could not find folder " + OpensimBase);
                    System.Environment.Exit(-1);
                }

                OpensimBase= Path.Combine([OpensimBase,"OpenSim","Region","ScriptEngine","Shared","Api"]);
                OpensimInterfacePath = Path.Combine(OpensimBase,"Interface");

                OpensimRuntimePath = Path.Combine(OpensimBase,"Runtime");

                if (args.Length == 2)
                {
                    outputPath = args[1];
                    Console.WriteLine("Writing output to " + outputPath);
                    if (!Path.Exists(outputPath))
                    {
                        Console.WriteLine("Could not find output folder");
                        System.Environment.Exit(-1);
                    }
                }
            }

            if (!Path.Exists(OpensimInterfacePath))
            {
                Console.WriteLine("Could not find folder " + OpensimInterfacePath);
                System.Environment.Exit(-1);
            }

            if (!Path.Exists(OpensimRuntimePath))
            {
                Console.WriteLine("Could not find folder " + OpensimRuntimePath);
                System.Environment.Exit(-1);
            }

            Console.WriteLine("Looking for runtime source files in " + OpensimRuntimePath);
            Console.WriteLine("Looking for interface source files in " + OpensimInterfacePath);

            if(outputPath.Length > 0)
                Console.WriteLine("Will write to folder " + outputPath);

            StringBuilder sb = new(400000);
            sb.Append("<llsd><map><key>llsd-lsl-syntax-version</key><integer>2</integer>\n");

            try
            {
                using StreamReader sr = File.OpenText(Path.Combine("..","ScriptSyntaxBase.xml"));
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    sb.Append(s);
                    sb.Append('\n');
                }
            }
            catch
            {
                Console.WriteLine("Problem reading ScriptSyntaxBase.xml");
                Environment.Exit(-1);
            }

            ProcessConsts(sb, OpensimRuntimePath);
            ProcessFunctions(sb, OpensimInterfacePath);
            sb.Append("</map></llsd>");
            string outstr = sb.ToString();

            Guid id;
            byte[] seed = new byte[16];
            byte[] ret = SHA1.HashData(Encoding.Default.GetBytes(outstr));
            for (int i = 0; i < 16; i++)
                seed[i] = ret[i];
            id = new(seed);

            using StreamWriter file = new(Path.Combine(outputPath, "ScriptSyntax.xml"), false);
            file.Write(id.ToString() + "\n");
            file.Write(outstr);
            Console.WriteLine("Wrote file ScriptSyntax.xml");
        }

        static void ProcessConsts(StringBuilder sb, string OpensimRuntimePath)
        {
            List<ConstInfo> constants = new(1000);
            char[] totrim = [' ','\t'];
            string lastdesc = string.Empty;
            try
            {
                if (!File.Exists(Path.Combine(OpensimRuntimePath, "LSL_Constants.cs")))
                    throw new Exception("Unable to open LSL_Constant.cs");

                using StreamReader sr = File.OpenText(Path.Combine(OpensimRuntimePath, "LSL_Constants.cs"));

                string? s;
                string ss;
                string s2;
                string type;
                string name;
                string value;
                while ((s = sr.ReadLine()) is not null)
                {
                    name = string.Empty;
                    type = string.Empty;
                    ss = s.Trim(totrim);
                    if (ss.StartsWith("//ApiDesc "))
                    {
                        if (string.IsNullOrEmpty(lastdesc))
                            lastdesc = ss[10..];
                        else
                            lastdesc += string.Concat("&#xA;", ss.AsSpan(10));
                        continue;
                    }

                    int eqindx = ss.IndexOf('=');
                    if (eqindx < 0)
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }
                    int endindx = ss.IndexOf(';');
                    if (endindx < 0)
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }

                    value = (ss.Substring(eqindx + 1, endindx - eqindx - 1)).Trim();

                    ss = ss.Substring(0, eqindx);
                    if (ss.StartsWith("public static readonly "))
                    {
                        s2 = ss[23..];
                        int idx = s2.IndexOf(' ');
                        name = s2.Substring(idx).Trim();
                        type = s2.Substring(0, idx).Trim();
                        type = TypeToString(type);
                        if (type.Equals("vector") || type.Equals("rotation"))
                        {
                            if (value == "ZERO_VECTOR")
                                value = "&gt;0,0,0&lt;";
                            else
                            {
                                int st = value.IndexOf('(');
                                int en = value.IndexOf(')');
                                value = string.Concat("&gt;", value.AsSpan(st + 1, en - st - 1), "&lt;");
                                value = value.Replace(" ", string.Empty);
                            }
                        }
                    }
                    else if (ss.StartsWith("public const "))
                    {
                        s2 = ss[13..];
                        int idx = s2.IndexOf(' ');
                        name = s2.Substring(idx).Trim();
                        type = s2.Substring(0, idx).Trim();
                        type = TypeToString(type);
                        if (type.Equals("string"))
                        {
                            value = value.Substring(1, value.Length - 2);
                            if (value.StartsWith("\\u"))
                                value = value.Replace("\\u", "U+");
                        }
                        else if (type.Equals("float"))
                            value = value.Replace("f", string.Empty);
                    }
                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(type))
                    {
                        ConstInfo c = new()
                        {
                            name = name,
                            type = type,
                            value = value,
                            desc = lastdesc
                        };
                        constants.Add(c);
                    }

                    lastdesc = (lastdesc != string.Empty) ? string.Empty : lastdesc;
                }
            }
            catch(Exception e)
            {
                throw new Exception("Error " + e.Message);
            }

            constants.Sort(CompareByName);

            sb.Append("<key>constants</key>\n<map>\n");

            foreach (ConstInfo c in constants)
            {
                sb.Append(" <key>");
                sb.Append(c.name);
                sb.Append("</key><map>\n");

                sb.Append("  <key>type</key><string>");
                sb.Append(c.type);
                sb.Append("</string>\n  <key>value</key><string>");
                sb.Append(c.value);
                sb.Append("</string>\n");
                if (c.desc != "")
                {
                    sb.Append("  <key>tooltip</key><string>");
                    sb.Append(c.desc);
                    sb.Append("</string>\n");
                }

                sb.Append(" </map>\n");
            }
            sb.Append("</map>\n");
        }

        public static readonly HashSet<string> validnames = ["os","ll","ls"];
        public static readonly char[] totrim = [' ', '\t'];
        public static readonly char[] spacesplit = [' '];
        public static readonly char[] commasplit = [','];

        static void ProcessFunctionsFile(List<FunctionInfo> functions, string file, string OpensimInterfacePath)
        {
            string lastdesc = string.Empty;
            try
            {
                if (!File.Exists(Path.Combine(OpensimInterfacePath, file)))
                    throw new Exception("Unable to find " + file);

                using StreamReader sr = File.OpenText(Path.Combine(OpensimInterfacePath, file));

                string? s;
                string ss;
                string type = string.Empty;
                string name = string.Empty;
                string arguments = string.Empty;
                while ((s = sr.ReadLine()) is not null)
                {
                    ss = s.Trim(totrim);
                    if (ss.StartsWith('*'))
                        continue;
                    if (ss.StartsWith("/*"))
                        continue;

                    if (ss.StartsWith("//ApiDesc "))
                    {
                        if (string.IsNullOrEmpty(lastdesc))
                            lastdesc = ss[10..];
                        else
                            lastdesc += string.Concat("&#xA;", ss.AsSpan(10));
                        continue;
                    }

                    if (ss.StartsWith("//"))
                        continue;

                    int argst = ss.IndexOf('(');
                    if (argst < 0)
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }
                    int argsend = ss.IndexOf(')');
                    if (argsend < 0)
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }

                    if (argsend > argst + 1)
                        arguments = (ss.Substring(argst + 1, argsend - argst - 1)).Trim();

                    ss = ss.Substring(0, argst).TrimStart();

                    string[] parts = ss.Split(spacesplit);
                    if (parts.Length >= 2)
                    {
                        type = TypeToString(parts[0].Trim());
                        name = parts[^1].Trim();
                    }
                    else if (parts.Length.Equals(1))
                    {
                        name = parts[0].Trim();
                        SetEmpty(ref type);
                    }
                    else
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }

                    string st = name.Substring(0, 2);
                    if (!validnames.Contains(st.ToLower()))
                    {
                        SetEmpty(ref lastdesc);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(name))
                    {
                        List<ArgsInfo> ainfo = [];
                        if (arguments != string.Empty)
                        {
                            ainfo = [];
                            string[] ags = arguments.Split(commasplit);
                            foreach (string sa in ags)
                            {
                                string at;
                                string an;
                                string sb = sa.Trim();
                                string[] aparts = sb.Split(spacesplit);
                                if (aparts.Length.Equals(2))
                                {
                                    at = TypeToString(aparts[0].Trim());
                                    an = aparts[1].Trim();
                                }
                                else if (aparts.Length.Equals(1))
                                {
                                    at = aparts[0].Trim();
                                    an = string.Empty;
                                }
                                else
                                    continue;
                                ArgsInfo ai = new()
                                {
                                    name = an,
                                    type = at
                                };
                                ainfo.Add(ai);
                            }
                        }

                        FunctionInfo c = new()
                        {
                            name = name,
                            type = type,
                            args = ainfo,
                            desc = lastdesc
                        };
                        functions.Add(c);
                    }

                    lastdesc = (!string.IsNullOrEmpty(lastdesc)) ? string.Empty : lastdesc;
                    SetEmpty(ref name);
                    SetEmpty(ref type);
                    SetEmpty(ref arguments);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error " + e.Message);
            }
        }

        static void ProcessFunctions(StringBuilder sb, string OpensimRuntimePath)
        {
            List<FunctionInfo> functions = new(1000);

            ProcessFunctionsFile(functions, "ILSL_Api.cs", OpensimRuntimePath);
            ProcessFunctionsFile(functions, "ILS_Api.cs", OpensimRuntimePath);
            ProcessFunctionsFile(functions, "IOSSL_Api.cs", OpensimRuntimePath);

            functions.Sort(CompareByName);

            sb.Append("<key>functions</key>\n<map>\n");

            foreach (FunctionInfo c in functions)
            {
                sb.Append(" <key>");
                sb.Append(c.name);
                sb.Append("</key>\n <map>\n");
                if (c.type != "")
                {
                    sb.Append("  <key>return</key><string>");
                    sb.Append(c.type);
                    sb.Append("</string>\n");
                }
                if(c.args == null || c.args.Count == 0)
                    sb.Append("  <key>arguments</key><undef/>\n");
                else
                {
                    sb.Append("  <key>arguments</key><array>\n");
                    foreach (ArgsInfo pi in c.args)
                    {
                        sb.Append("   <map><key>");
                        sb.Append(pi.name);
                        sb.Append("</key><map><key>type</key><string>");
                        sb.Append(pi.type);
                        sb.Append("</string></map></map>\n");
                    }
                    sb.Append("  </array>\n");
                }
                if (!string.IsNullOrEmpty(c.desc))
                {
                    sb.Append("  <key>tooltip</key><string>");
                    sb.Append(c.desc);
                    sb.Append("</string>\n");
                }
                sb.Append(" </map>\n");
            }
            sb.Append("</map>\n");
        }
    }
}
