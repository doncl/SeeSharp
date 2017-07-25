using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Xml;
using System.Xml.Serialization;
using log4net;

namespace DataMigration
{
    [Serializable]
    [XmlRoot ("dataMigration")]
    public class DataMigrationPlan
    {
        static readonly ILog log = LogManager.GetLogger(typeof(DataMigration));

        [XmlArray("readyToUseSubPlans")]
        [XmlArrayItem("readyToUseSubPlan")]
        public ReadyToUseSubPlanDescriptor[] ReadyToUseSubPlans { get; set; }

        /// <summary>
        /// Method Resolvers are objects that, given a string name for a method, return a 
        /// delegate instance. The signature for these is a TransformMethod object. 
        /// </summary>
        /// <value>The method resolver descriptors.</value>
        [XmlArray ("methodResolvers")]
        [XmlArrayItem ("methodResolver")]
        public MethodResolverDescriptor[] MethodResolverDescriptors { get; set; }

        /// <summary>
        /// LitmusTest Resolvers are objects that, given a string name for a method, return a 
        /// delegate instance. The signature for these is a LitmusTest object.
        /// </summary>
        /// <value>The litmus test resolver descriptors.</value>
        [XmlArray ("litmusTestResolvers")]
        [XmlArrayItem ("methodResolver")]
        public MethodResolverDescriptor[] LitmusTestResolverDescriptors { get; set; }

        /// <summary>
        /// PostRowProcessor Resolvers are objects that, given a string name for a method, 
        /// return a  delegate instance. The signature for these is a postRowProcessor object.
        /// </summary>
        /// <value>The PostRowProcessor resolver descriptors.</value>
        [XmlArray ("postRowProcessorResolvers")]
        [XmlArrayItem ("methodResolver")]
        public MethodResolverDescriptor[] PostRowProcessorResolverDescriptors { get; set; }

        /// <summary>
        /// Lookups are key-value pairs that are used by the RowProcessor(s) to transform
        /// the incoming raw data.
        /// </summary>
        /// <value>The lookups.</value>
        [XmlArray("lookups")]
        [XmlArrayItem("lookup")]
        public LookupDescriptor[] Lookups { get; set; }

        /// <summary>
        /// Existences are data structures that can be seeded with the complete set of a 
        /// particular data item from some data store (e.g. Sql Server), and then added to
        /// as rows are processed to keep track of the set, ongoing.
        /// </summary>
        /// <value>The existences.</value>
        [XmlArray("existences")]
        [XmlArrayItem("existence")]
        public ExistenceDescriptor[] Existences { get; set; }

        /// <summary>
        /// The collection of DataSourcePlan instances; there is one of these for each 
        /// DataSourceCode.  DataSources are divided into DataSourceCodes, which have different 
        /// processing pipelines for different types of data.
        /// </summary>
        /// <value>The data source plans.</value>
        [XmlElement ("dataSource")]
        public DataSourcePlan[] DataSourcePlans { get; set; }

        /// <summary>
        /// Gets or sets the local file path root, which is where the work will actually be 
        /// done.
        /// </summary>
        /// <value>The local file path root.</value>
        [XmlElement ("localFilePathRoot")]
        public String LocalFilePathRoot { get; set; }

        /// <summary>
        /// Given a path to the plan document, deserializes it into a DataMigrationPlan
        /// instance, and returns that. 
        /// </summary>
        /// <returns>A DataMigration plan object, which informs the MigrationEngine as to how 
        /// to proceed with the migration in nearly every detail.</returns>
        public static DataMigrationPlan GetPlan()
        {
            var plansDirectoryInfo = GetMigrationPlanDirectory();
            var planFile = ConfigurationManager.AppSettings["MigrationPlan"];
            planFile = Path.Combine(plansDirectoryInfo.FullName, planFile);
            var dataMigrationPlan = GetDeserializedDocumentT<DataMigrationPlan>(planFile);

            // Get the list of existing datasourceplans in form of a list.  The one contained
            // in DataMigrationPlan is an array, because the Xml serializer likes it that way.
            var dataSourcePlans = dataMigrationPlan.DataSourcePlans != null
                ? dataMigrationPlan.DataSourcePlans.ToList()
                : new List<DataSourcePlan>();

            // Walk the list of directories, looking for ones that match the convention, which
            // is, DSC/DSCdataSourcePlan.xml, where 'DSC' is the datasourcecode.
            // When we find one, deserialize it into a DataSourcePlan object, and add to the
            // collection.
            foreach (var directoryInfo in plansDirectoryInfo.EnumerateDirectories()) {
                var dataSourcePlan = GetDataSourcePlan(directoryInfo);
                if (dataSourcePlan != null) {
                    log.DebugFormat("Adding DataSourcePlan for datasourcecode = {0}",
                        directoryInfo.Name);
                    dataSourcePlans.Add(dataSourcePlan);
                } else {
                  log.WarnFormat("No DataSourcePlan.xml found for dsc = {0}", 
                      directoryInfo.Name);  
                }
            }

            // Set the newly formed collection back in the DataMigrationPlan.
            dataMigrationPlan.DataSourcePlans = dataSourcePlans.ToArray();
            return dataMigrationPlan;
        }

        /// <summary>
        /// Returns the location of the MigrationPlan Directory.
        /// </summary>
        /// <returns>The migration plan directory.</returns>
        static DirectoryInfo GetMigrationPlanDirectory()
        {
            string plansDir;
            var runtimeType = Type.GetType("Mono.Runtime");
            if (runtimeType == null) {
                plansDir = 
                    ConfigurationManager.AppSettings["WindowsDataPlansDirectory"];
            }
            else {
                plansDir = ConfigurationManager.AppSettings["LinuxDataPlansDirectory"];
            }
            var plansDirectoryInfo = new DirectoryInfo(plansDir);
            if (!plansDirectoryInfo.Exists) {
                throw new Exception(String.Format("DataMigrationPlans directory {0} does not" +
                    " exist, cannot continue", plansDir));
            }
            return plansDirectoryInfo;
        }

        /// <summary>
        /// Looks in a subdirectory, which by convention is named the datasourcecode, looking
        /// for a file called '{DSC}DataSourcePlan.xml' where '{DSC}' is the name of the 
        /// directory, and also is the datasourcecode.  
        /// </summary>
        /// <param name="directoryInfo">DirectoryInfo for the directory where the 
        /// DataSourcePlan xml file is expected to be found.</param>
        /// <returns>A deserialized DataSourcePlan.</returns>
        private static DataSourcePlan GetDataSourcePlan(DirectoryInfo directoryInfo)
        {
            ScriptMethodResolver methodResolver;
            var success = GetMethodsForDataSource(directoryInfo, out methodResolver);
            if (!success) {  // The called function logs the error.
                return null;
            }

            // The name for the file we're looking for is the name of the subdirectory plus
            // the string "DataSourcePlan.xml".
            var dataSourcePlanName = Path.Combine(directoryInfo.FullName, 
                                                  directoryInfo.Name + "DataSourcePlan.xml");

            var fileInfoDataSourcePlan = new FileInfo(dataSourcePlanName);
            if (!fileInfoDataSourcePlan.Exists) {
                return null;
            }
            var dataSourcePlan =  GetDeserializedDocumentT<DataSourcePlan>(dataSourcePlanName);
            if (dataSourcePlan != null) {
                // Attach the compiled assembly to the datasourceplan.
                dataSourcePlan.ScriptMethodResolver = methodResolver; // it's OK if it's null.
            }
            return dataSourcePlan;
        }

        /// <summary>
        /// Generic function which, given a path to an xml file, deserializes it into the C#
        /// class it is an instance of.
        /// </summary>
        /// <typeparam name="T">Type of C# object to deserialize to.</typeparam>
        /// <param name="pathToFile">Full path to the file.</param>
        /// <returns>A deserialized object of type T.</returns>
        private static T GetDeserializedDocumentT<T>(string pathToFile) where T : class
        {
            var doc = new XmlDocument();
            doc.Load(pathToFile);
            var xmlStr = doc.InnerXml;
            using (var reader = new StringReader(xmlStr)) {
                return (T)
                    new XmlSerializer(typeof(T)).Deserialize(reader);
            }
        }

        /// <summary>
        /// Looks for a file named '{DSC}'Methods.cs in the subdirectory, where '{DSC}' is the
        /// name of the datasourcecode.  If found it compiles it, finds the TransformMethods 
        /// and LitmusTests, and returns the collection of those things to the caller.
        /// </summary>
        /// <param name="directoryInfo">Directory to look in.</param>
        /// <param name="methodResolver">.</param>
        /// <returns>True on succeesful compile, or if no script file found, false when the 
        /// file is there and fails to compile.</returns>
        private static bool GetMethodsForDataSource(DirectoryInfo directoryInfo, 
                                                    out ScriptMethodResolver methodResolver)
        {
            methodResolver = null;
            var scriptName = Path.Combine(directoryInfo.FullName, 
                                            directoryInfo.Name + "Methods.cs");

            if (!File.Exists(scriptName)) {
                return true;  
            }

            methodResolver = new ScriptMethodResolver();
            try {
                methodResolver.BuildResolversFromScriptFile(scriptName);
                return true;
            } catch (Exception ex) {
                log.ErrorFormat("DataMigrationPlan.GetMethodsForDataSource - error " +
                "processing scripts file for dataSourceCode {0} = {1}",
                    directoryInfo.Name, ex.Message);
                methodResolver = null;
                return false;
            }
        }
    }
}
