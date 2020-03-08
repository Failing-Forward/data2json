using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace data2json
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                return;
            }

            List<Entry> entries = new List<Entry>();
            Regex regex = new Regex(@"\[(?<byte>\d)\]");
            List<Type> types = JsonConvert.DeserializeObject<List<Type>>(File.ReadAllText("types.json"));

            using(BinaryReader data1Reader = new BinaryReader(File.OpenRead(args[1]))) // DATA1 path
            {
                using(BinaryReader data0Reader = new BinaryReader(File.OpenRead(args[0]))) // DATA0 path
                {
                    int entryCount = (int)data0Reader.BaseStream.Length / 0x20;

                    for(int i = 0; i < 440; i++)
                    {
                        Entry entry = new Entry
                        {
                            Offset = data0Reader.ReadInt64(),
                            DecompressedSize = data0Reader.ReadInt64(),
                            CompressedSize = data0Reader.ReadInt64(),
                            IsCompressed = Convert.ToBoolean(data0Reader.ReadInt64()),
                            Index = i
                        };

                        data1Reader.BaseStream.Position = entry.Offset;

                        foreach(Type type in types)
                        {
                            string Condition = type.Condition.Replace("fsize", entry.CompressedSize.ToString());
                            MatchCollection matches = regex.Matches(Condition);

                            if (matches.Count > 0)
                            {
                                foreach(Match match in matches)
                                {
                                    byte idx = Convert.ToByte(match.Groups[1].Value);
                                    data1Reader.BaseStream.Position = entry.Offset + idx * 4;
                                    int value = data1Reader.ReadInt32();
                                    Condition = Condition.Replace(match.Value, value.ToString());
                                }
                            }

                            if (CSharpScript.EvaluateAsync<bool>(Condition).Result)
                            {
                                entry.Type = type;
                            }

                            if (entry.Type == null)
                            {
                                entry.Type = new Type
                                {
                                    Name = "Binary",
                                    Extension = ".bin"
                                };
                            }
                        }
                        entries.Add(entry);
                    }
                }
            }
            File.WriteAllText("data.json", JsonConvert.SerializeObject(entries, Formatting.Indented));
        }
    }
}
