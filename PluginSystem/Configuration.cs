using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MatterHackers.MatterControl.Extensibility
{

    public static class Configuration
    {
        // "MyAssembly.MyType, MyAssembly"
        private static object LoadProviderFromAssembly(string typeString)
        {
            var type = Type.GetType(typeString);
            return Activator.CreateInstance(type);
        }

        public static ImageIOPlugin ImageIO { get; private set; }
        public static OsInformationPlugin OsInformation { get; private set; }

    }

    public class PluginFinder<BaseClassToFind>
    {

        public List<BaseClassToFind> Plugins;

        // "MyAssembly.MyType, MyAssembly"
        public static object LoadTypeFromAssembly(string typeString)
        {
            return Activator.CreateInstance(Type.GetType(typeString));
        }

        public PluginFinder(string searchDirectory = null, IComparer<BaseClassToFind> sorter = null)
        {
            string searchPath;
            if (searchDirectory == null)
            {
                searchPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                //searchPath = Path.Combine(searchPath, "Plugins");
            }
            else
            {
                searchPath = Path.GetFullPath(searchDirectory);
            }

            Plugins = FindAndAddPlugins(searchPath);
            if (sorter != null)
            {
                Plugins.Sort(sorter);
            }
        }

        public List<BaseClassToFind> FindAndAddPlugins(string searchDirectory)
        {
            List<BaseClassToFind> factoryList = new List<BaseClassToFind>();
            if (Directory.Exists(searchDirectory))
            {
                //string[] files = Directory.GetFiles(searchDirectory, "*_HalFactory.dll");
                string[] dllFiles = Directory.GetFiles(searchDirectory, "*.dll");
                string[] exeFiles = Directory.GetFiles(searchDirectory, "*.exe");

                List<string> allFiles = new List<string>();
                allFiles.AddRange(dllFiles);
                allFiles.AddRange(exeFiles);
                string[] files = allFiles.ToArray();

                foreach (string file in files)
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFile(file);

                        foreach (Type type in assembly.GetTypes())
                        {
                            if (type == null || !type.IsClass || !type.IsPublic)
                            {
                                continue;
                            }

                            if (type.BaseType == typeof(BaseClassToFind))
                            {
                                factoryList.Add((BaseClassToFind)Activator.CreateInstance(type));
                            }
                        }
                    }
                    catch (ReflectionTypeLoadException)
                    {
                    }
                    catch (BadImageFormatException)
                    {
                    }
                    catch (NotSupportedException)
                    {
                    }
                }
            }

            return factoryList;
        }
    }

}
