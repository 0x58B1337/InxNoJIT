using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using dnlib.DotNet;

namespace InxNoJIT
{
    public static class JIT
    {
        private static Dictionary<string, byte[]> mdtoken = new Dictionary<string, byte[]>();

        private static int SearchArray(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        public static byte[] RestoreMethods()
        {
            Assembly asm = Assembly.UnsafeLoadFrom(Program.path);
            ModuleDefMD md = ModuleDefMD.Load(Program.path);
            byte[] assembly = File.ReadAllBytes(Program.path);

            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            var ResourceName = "";

            var cctor = md.GlobalType.FindOrCreateStaticConstructor();
            for (int i = 0; i < cctor.Body.Instructions.Count; i++)
            {
                var instruction = cctor.Body.Instructions[i];
                if (instruction.OpCode == dnlib.DotNet.Emit.OpCodes.Call && cctor.Body.Instructions[i - 1].OpCode == dnlib.DotNet.Emit.OpCodes.Ldc_I4_0)
                {
                    var LoadMethod = instruction.Operand as MethodDef;
                    for (int e = 0; e < LoadMethod.Body.Instructions.Count; e++)
                    {
                        var ins = LoadMethod.Body.Instructions[e];
                        if (ins.OpCode == dnlib.DotNet.Emit.OpCodes.Callvirt && ins.Operand.ToString().Contains("get_Assembly") && LoadMethod.Body.Instructions[e + 2].OpCode == dnlib.DotNet.Emit.OpCodes.Callvirt)
                        {
                            ResourceName = LoadMethod.Body.Instructions[e + 1].Operand.ToString();
                        }
                    }
                    break;
                }
            }

            if (ResourceName == "")
            {
                Console.WriteLine("Resource not found!" + Environment.NewLine + "Resource Name:");
                ResourceName = Console.ReadLine();
            }

            Stream resource = asm.GetManifestResourceStream(ResourceName);
            if (resource == null)
            {
                new Exception("Resource not found");
            }
            BinaryReader binaryReader = new BinaryReader(resource);
            int num = binaryReader.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                MethodBase methodBase = asm.ManifestModule.ResolveMethod(binaryReader.ReadInt32());
                string text = binaryReader.ReadString();
                var c = Convert.FromBase64String(text);
                mdtoken.Add(methodBase.MetadataToken.ToString(), c);
            }

            foreach (var t in types.Where(t => t != null))
            {
                foreach (ConstructorInfo constructor in t.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!mdtoken.ContainsKey(constructor.MetadataToken.ToString()))
                    {
                        continue;
                    }

                    try
                    {
                        byte[] ILbyte = constructor.GetMethodBody().GetILAsByteArray();
                        int size = ILbyte.Length;
                        int start = SearchArray(assembly, ILbyte);
                        if (start == -1) continue;
                        else
                        {
                            int position = start;
                            ILbyte = mdtoken[constructor.MetadataToken.ToString()];
                            position = start;
                            for (int i = 0; i < size; i++)
                            {
                                assembly[position] = ILbyte[i];
                                position++;
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Method Restored: " + constructor.Name.ToString());
                }

                MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.Static | BindingFlags.DeclaredOnly);

                foreach (var m in methods)
                {
                    if (!mdtoken.ContainsKey(m.MetadataToken.ToString()))
                    {
                        continue;
                    }

                    try
                    {
                        byte[] ILbyte = m.GetMethodBody().GetILAsByteArray();
                        int size = ILbyte.Length;
                        int start = SearchArray(assembly, ILbyte);
                        if (start == -1) continue;
                        else
                        {
                            int position = start;
                            ILbyte = mdtoken[m.MetadataToken.ToString()];
                            position = start;
                            for (int i = 0; i < size; i++)
                            {
                                assembly[position] = ILbyte[i];
                                position++;
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Method Restored: " + m.Name.ToString());
                }
            }

            return assembly;
        }
    }
}
