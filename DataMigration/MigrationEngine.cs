using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// The MigrationEngine is the principal starting place for the migration activity.  It has
    /// one method, Transform(), which instantiates FeedProcessors and FeedManagers for each
    /// DataSource described by the DataMigrationPlan, and uses them to process the feeds.
    /// </summary>
    public class MigrationEngine
    {
        // By convention, the MigrationEngine, and all its child classes use '1' for their
        // LogSource in the PhaseLogging.
        static public readonly int LogSource = 1;

        static readonly ILog log = LogManager.GetLogger(typeof(MigrationEngine));

        /// <summary>
        /// The global collection of ReadyToUseSubPlanDesciptors.   These are ready-to-use 
        /// collections  of components that are used in the processing of a datasource.  Rather 
        /// than  requiring a datasourceplan to tediously specify each component, we can 
        /// specify them once here.  Since we're just storing the *Descriptor* (the C# class
        /// the Xml element in the main plan gets deserialized into), each datasource gets a
        /// brand new fresh copy of all the components, some of which are IDisposables, and 
        /// shouldn't be reused, plus this alleviates any concerns that might arise if we 
        /// choose to process datasources concurrently.
        /// </summary>
        private readonly 
        IDictionary<String, ReadyToUseSubPlanDescriptor> readyToUseSubPlanDescriptors =
            new Dictionary<string, ReadyToUseSubPlanDescriptor>(
                    StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The collection of MethodResolvers.  When Processing the DataMigrationPlan, the
        /// DataMigration engine will read the fully assembly-qualified names where these 
        /// objects reside, and will load the objects via reflection, passing the collection
        /// to child objects as required.  These are keyed by Method Resolver tags, noted as
        /// 'methodTag' in the TransformationMapping collections contained in the FeedFileMaps
        /// which is a collection of the DataSourcePlans, which are a collection in the 
        /// root DataMigrationPlan.  Given a tag, and a method name, these things can return
        /// a delegate which can perform the desired transform operation.
        /// </summary>
        readonly IDictionary<String, IMethodResolver> methodResolvers = 
            new Dictionary<String, IMethodResolver>(StringComparer.OrdinalIgnoreCase);
            
        /// <summary>
        /// The collection of LitmusTestResolvers is loaded and dealt with similarly to the 
        /// MethodResolvers.   LitmusTests, by contrast, are functions that, given access to
        /// column data and lookups, return a bool as to whether the row is suitable for 
        /// further processing, or should go directly to the BadRows collection.
        /// </summary>
        readonly IDictionary<String, ILitmusTestResolver> litmusTestResolvers = 
            new Dictionary<String, ILitmusTestResolver>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The collection of PostRowProcessorResolvers is loaded and dealt with similarly to 
        /// the MethodResolvers.  PostRowProcessors, by contrast, are functions that, given 
        /// access to column data and lookups, have some desirable side-effect (e.g. populating
        /// a dictionary or hashset in a script file with the email).
        /// </summary>
        readonly IDictionary<String, IPostRowProcessorResolver> postRowProcessorResolvers = 
           new Dictionary<String, IPostRowProcessorResolver>(StringComparer.OrdinalIgnoreCase);
            
        /// <summary>
        /// The collection of Lookups.  These are an abstraction on the Dictionary concept,
        /// they're keyed by name, and they're a way for rows and have indirected data, e.g.
        /// to map incoming ClubCodes to SiteIds.  There are various implementations of this 
        /// interface, and they're listed globally in the DataMigrationPlan.
        /// </summary>
        IDictionary<String, ILookup> lookups;
                        
        /// <summary>
        /// The existence objects are collections of things that can tell you whether a given
        /// object exists or not; e.g., the list of usernames in use.  As with methodResolver,
        /// litmusTestResolvers, and Lookups, there can be various implementations of this, and
        /// (hypothetically) they can be specified declaratively in the Plan, or populated via
        /// a call to a DataStore, or pretty much any way the data can be acquired.
        /// </summary>
        IDictionary<String, IExistence> existenceObjects;

        /// <summary>
        /// The MigrationPlan is described in Xml document form, and deserialized into this
        /// object.  It describes all the actions the Migration Engine is going to take.
        /// </summary>
        public DataMigrationPlan MigrationPlan { get; set; }

        /// <summary>
        /// NoDownload is a flag, driven from the commandline argument 'nodownload', that
        /// will cause the MigrationEngine to bypass downloading the feed file(s) to a local
        /// working directory; it puts the responsibility for assuring said files exist locally
        /// on the user, rather than the software.   It is for development purposes only; it 
        /// defaults to false.
        ///</summary>
        public bool NoDownload { get; set; }
        
        /// <summary>
        /// By default, the MigrationEngine will process every DataSource(Code) specified in 
        /// the plan.   This behavior can be overriden on the command line by specifying one
        /// of the DataSourceCodes in the plan, and it will process only that one.  This 
        /// defaults to null.
        /// </summary>
        /// <value>The single data source code.</value>
        public string SingleDataSourceCode { get; set; }

        /// <summary>
        /// It is often useful, for development and debugging purposes, to bypass all the first
        /// stage of data migration, if the data is already staged and ready for 
        /// postprocessing.  When 'postprocessonlyloadnum=n' is used, it will skip all the 
        /// preliminary phases (it will refrain from downloading and processing and staging the
        /// feed, and will go directly to the postprocessing step.  It requires a feed to be
        /// ready in staging with the loadnumber passed in.   This defaults to null.
        /// </summary>
        /// <value>The post process only load number.</value>
        public int? PostProcessOnlyLoadNum { get; set; }
        // development hook.
        /// <summary>
        /// This is the primary entry point to the MigrationEngine.  It is competely driven by
        /// the DataMigrationPlan property, which is an XML-deserialized object; the migration 
        /// is declaratively described in an Xml file which is authored and pointed to by
        /// the App.Config
        /// </summary>
        public void Transform()
        {
            // TODO:  Use NoTasks == false flag with TPL to parallelize feeds, if we end up 
            // getting multiples.   This should not be done until after multiple feeds are up 
            // and running, and stable, done sequentially. 

            AcquireReadyToUseSubPlanDescriptors(MigrationPlan.ReadyToUseSubPlans);

            if (!PostProcessOnlyLoadNum.HasValue) {
                lookups = AcquireLookups(MigrationPlan.Lookups);
                existenceObjects = AcquireExistenceObjects(MigrationPlan.Existences);

                AcquireResolvers(MigrationPlan.MethodResolverDescriptors, methodResolvers);
                AcquireResolvers(MigrationPlan.LitmusTestResolverDescriptors, litmusTestResolvers);
                AcquireResolvers(MigrationPlan.PostRowProcessorResolverDescriptors, 
                    postRowProcessorResolvers);
            }

            if (String.IsNullOrEmpty(SingleDataSourceCode)) {
                foreach (var dataSourcePlan in MigrationPlan.DataSourcePlans) {
                var readyToUseSubPlan = 
                    GetReadyToUseSubPlan(dataSourcePlan.ReadyToUseSubPlanTag);
                    ProcessDataSource(dataSourcePlan, readyToUseSubPlan);
                }
            } else { // just processing a single datasourcecode
             var plan = MigrationPlan.DataSourcePlans.SingleOrDefault(
                               p => p.DataSourceCode.Equals(SingleDataSourceCode,
                                   StringComparison.OrdinalIgnoreCase));

                if (plan == null) {
                    throw new Exception(String.Format("Transform - no DataSourcePlan for {0}",
                        SingleDataSourceCode));
                }

                var readyToUseSubPlan = GetReadyToUseSubPlan(plan.ReadyToUseSubPlanTag);

                if (!PostProcessOnlyLoadNum.HasValue) {
                    ProcessDataSource(plan, readyToUseSubPlan);
                } else {
                    // Both the FeedProcessor and its RowProcessor need access to the lookups
                    // and ExistenceObjects, for processing transformationMethods 
                    // (the RowProcessor) and for processing LitmusTests (the FeedProcessor).

                    // Litmus Tests are tests that allow us to cheaply throw out a row without
                    // walking down it very far; we can put it into the BadRows table without
                    // throwing any exception because we're at the FeedProcessor level.
                    // Transforms are done row-by-row, and will throw BadRowExceptions as
                    // needed, but that happens at the RowProcessor level.

                    var proc = CreateFeedProcessorAndRowProcessor(plan, readyToUseSubPlan);
                    try {
                        proc.PostProcess(PostProcessOnlyLoadNum.Value, readyToUseSubPlan);
                        
                        // Publish to the monitoring service.
                        proc.MonitoringPublisher.PublishSuccess(plan,
                            proc.PhaseLogger.GetLoadEntries(PostProcessOnlyLoadNum.Value));
                    } catch (Exception ex) {
                        log.ErrorFormat("Transform - error running PostProcessing only " + 
                                        "loadnumber = {0}, error = {1}, stack = {2}",
                                    PostProcessOnlyLoadNum.Value, ex.Message, ex.StackTrace);
                        
                        // Publish the fact of failure.
                        proc.MonitoringPublisher.PublishFailure(plan,
                            proc.PhaseLogger.GetLoadEntries(PostProcessOnlyLoadNum.Value), ex);
                    }
                }
            }
        }

        /// <summary>
        /// CreateFeedProcessorAndRowProcessor is a helper function that instantiates this 
        /// composed object pair correctly with properties correctly set.
        /// </summary>
        /// <param name="plan">DataSourcePlan for a single data source</param>
        /// <param name="readyToUseSubPlan">A subplan of components bundled up for use; may be
        /// null</param>
        /// <returns>A correctly instantiated FeedProcessor</returns>
        FeedProcessor CreateFeedProcessorAndRowProcessor(DataSourcePlan plan, 
                                                        IReadyToUseSubPlan readyToUseSubPlan)
        {
            IPhaseLogger phaseLogger = null;
            if (readyToUseSubPlan != null && readyToUseSubPlan.PhaseLogger != null) {
                phaseLogger = readyToUseSubPlan.PhaseLogger;
            } else {
                phaseLogger = (IPhaseLogger)
                       Activator.CreateInstance(Type.GetType(plan.PhaseLogger.Assembly));
                phaseLogger.Properties = plan.PhaseLogger.Properties;
            }

            if (phaseLogger == null) {
                throw new Exception(String.Format("Cannot find phaseLogger for dsc = {0}", 
                                                    plan.DataSourceCode));
            }

            var proc = new FeedProcessor {
                Plan = plan,
                ReadyToUseSubPlan = readyToUseSubPlan,
                MethodResolvers = methodResolvers,
                LitmusTestResolvers = litmusTestResolvers,
                PostRowProcessorResolvers = postRowProcessorResolvers,
                Lookups = lookups,
                ExistenceObjects = existenceObjects,
                RowProcessor = new RowProcessor {
                    LitmusTestResolvers = litmusTestResolvers,
                    MethodResolvers = methodResolvers,
                    Lookups = lookups,
                    ExistenceObjects = existenceObjects,
                },
                PhaseLogger = phaseLogger,
                MonitoringPublisher = new FileSystemPublishMonitoringData(),
            };
            return proc;
        }

        /// <summary>
        /// Processes a single data source.
        /// </summary>
        /// <param name="dataSourcePlan">The plan for this datasource.</param>
        /// <param name="readyToUseSubPlan">A readytouse subplan collection of components; can
        /// be null.</param>
        void ProcessDataSource(DataSourcePlan dataSourcePlan, 
                                IReadyToUseSubPlan readyToUseSubPlan)
        {
            var loadNum = -1;
            var proc = CreateFeedProcessorAndRowProcessor(dataSourcePlan, readyToUseSubPlan);

            IFeedManager feedManager = null;
            IFeedAccessor feedAccessor = null;

            if (readyToUseSubPlan != null) {
                if (readyToUseSubPlan.FeedManager != null) {
                    feedManager = readyToUseSubPlan.FeedManager;
                }
                if (readyToUseSubPlan.FeedAccessor != null) {
                    feedAccessor = readyToUseSubPlan.FeedAccessor;
                }
            }

            if (feedManager == null) {
                feedManager = (IFeedManager)
                    Activator.CreateInstance(Type.GetType(dataSourcePlan.FeedManager));
            }

            feedManager.Plan = dataSourcePlan;
            feedManager.WorkingDir = Path.Combine(MigrationPlan.LocalFilePathRoot,
                        dataSourcePlan.DataSourceCode);

            
            if (feedAccessor == null) {
                // For non-file based datasources, there's no need for a feed accessor object, so
                // the DataSourcePlan will not specify one in those cases.
                if (!String.IsNullOrEmpty(dataSourcePlan.FeedAccessor)) {
                    feedAccessor = (IFeedAccessor)
                        Activator.CreateInstance(Type.GetType(dataSourcePlan.FeedAccessor));
                }
            }

            feedManager.FeedAccess = feedAccessor;

            // If we passed in the 'noDownload' flag, that means we're just doing 
            // development locally, and want to process a feed that we know is on the local 
            // drive.   So noDownload == true means doWork == true.
            var doWork = NoDownload;

            try {
                if (!NoDownload) {
                    // This is the real, production case; check to see if there are feed files 
                    // in S3 for this DataSourceCode in the drop that are newer than the ones 
                    // already processed. 
                    doWork = feedManager.NeedsToProcess();
                
                    if (doWork) {
                        // If we determined there are feeds to process, then move the ones in 
                        // current to archive, and the ones in drop to current.
                        feedManager.MoveCurrentToArchive();
                        feedManager.MoveDropToCurrent();
                    }
                }

                if (doWork) {
                    try {
                        // Either we found work to do at the drop location, or it's the local 
                        // development case, and we're forcing it with the 'NoDownload' flag.
                        // Either way we need to acquire the fileinfo for local processing.
                        feedManager.DownloadCurrentToLocal(!NoDownload);
                        
                        var message = String.Format("Transform - FeedManager for dsc = {0}"
                                      + " determined we have work to do",
                                          dataSourcePlan.DataSourceCode);
                        
                        proc.PhaseLogger.Log(new PhaseLogEntry {
                            LogSource = LogSource,
                            DataSourceCode = dataSourcePlan.DataSourceCode,
                            Phase = DataMigrationPhase.ManagingExternalFeeds,
                            Description = message
                        });
                        
                        // now, do the actual work. 
                        loadNum = proc.Process();
                        proc.PostProcess(loadNum, readyToUseSubPlan);
                        
                        proc.MonitoringPublisher.PublishSuccess(dataSourcePlan,
                                            proc.PhaseLogger.GetLoadEntries(loadNum));
                    } catch (Exception ex) {
                        proc.MonitoringPublisher.PublishFailure(dataSourcePlan, 
                                     proc.PhaseLogger.GetLoadEntries(loadNum), ex);
                        log.ErrorFormat("ProcessDataSource - error procssing dsc {0}, error " +
                            "{1}", dataSourcePlan.DataSourceCode, ex.Message);
                    }
                } else {
                    log.DebugFormat("ProcessDataSource - no work to do for dsc = {0}",
                        dataSourcePlan.DataSourceCode);
                }
            } catch (Exception ex) {
                proc.MonitoringPublisher.PublishFailure(dataSourcePlan, 
                    proc.PhaseLogger.GetLoadEntries(loadNum), ex);

                log.ErrorFormat("ProcessDataSource - error procssing dsc {0}, error " +
                    "{1}", dataSourcePlan.DataSourceCode, ex.Message);
            }
        }

        /// <summary>
        /// Given a tag, this returns a ReadyToUseSubPlan with everything instantiated except
        /// the parser.   For the parser, it has a ParserDescriptor such that it can be used
        /// to instantiate parser instances ond emand.
        /// </summary>
        /// <param name="readytoUseSubPlanTag">The tag that identifies this ReadyToUseSubPlan
        /// </param>
        /// <returns></returns>
        private IReadyToUseSubPlan GetReadyToUseSubPlan(string readytoUseSubPlanTag)
        {
            if (String.IsNullOrWhiteSpace(readytoUseSubPlanTag) ||
                !readyToUseSubPlanDescriptors.ContainsKey(readytoUseSubPlanTag)) {
                return null;
            }
            var descriptor = readyToUseSubPlanDescriptors[readytoUseSubPlanTag];
            IFeedStager feedStager = null;
            IFeedManager feedManager = null;
            IFeedAccessor feedAccessor = null;
            IPhaseLogger phaseLogger = null;
            IEnumerable<PostProcessorDescriptor> postProcessors = null;

            if (descriptor.FeedStager != null) {
                feedStager = (IFeedStager) Activator.CreateInstance(
                    Utilities.GetTypeFromFqName(descriptor.FeedStager.Assembly));

                feedStager.Properties = descriptor.FeedStager.Properties;
            }

            if (descriptor.FeedManager != null) {
                feedManager = (IFeedManager) Activator.CreateInstance(
                    Utilities.GetTypeFromFqName(descriptor.FeedManager));
            }

            if (descriptor.FeedAccessor != null) {
                feedAccessor = (IFeedAccessor) Activator.CreateInstance(
                    Utilities.GetTypeFromFqName(descriptor.FeedAccessor));
            }

            if (descriptor.PhaseLogger != null) {
                phaseLogger = (IPhaseLogger) Activator.CreateInstance(
                    Utilities.GetTypeFromFqName(descriptor.PhaseLogger.Assembly));
                phaseLogger.Properties = descriptor.PhaseLogger.Properties;
            }

            if (descriptor.PostProcessors != null) {
                postProcessors = descriptor.PostProcessors;
            }

            var readyToUseSubPlan = new ReadyToUseSubPlan {
                Stager = feedStager,
                FeedManager = feedManager,
                FeedAccessor = feedAccessor,
                PhaseLogger = phaseLogger,
                ParserDescriptor = descriptor.Parser,
                PostProcessorDescriptors = postProcessors,
            };

            return readyToUseSubPlan;
        }

        /// <summary>
        /// Puts the ReadyToUseSubPlanDescriptors from the MigrationPlan into a dictionary
        /// keyed by each readyToUseSubPlanDescriptor's tag.  The actual ReadyToUseSubPlan
        /// for each datassource will be lazily created just before processing the datasource,
        /// and only if the datasourceplan specifies a tag.
        /// </summary>
        /// <param name="readyToUseSubPlanDescriptorsFromPlan"></param>
        private void AcquireReadyToUseSubPlanDescriptors(
            IEnumerable<ReadyToUseSubPlanDescriptor> readyToUseSubPlanDescriptorsFromPlan)
        {
            foreach (var descriptor in readyToUseSubPlanDescriptorsFromPlan) {
                var tag = descriptor.Tag;
                if (String.IsNullOrWhiteSpace(tag)) {
                    throw new Exception("AcquireReadyToUseSubPlanDescriptors - badly formed " +
                            "readytoUse plan - requires a tag to uniquely identify it");
                }
                readyToUseSubPlanDescriptors[tag] = descriptor;
            }
        }

        /// <summary>
        /// The DataMigrationPlan has a set of global Lookups, which are identified with tags.
        /// These are used in TransformMaps, where lookups are specified by the "lookup" 
        /// attribute.   When "lookup" is specified, then the "SrcColumns" values refer to
        /// inputs to the lookup table specified.
        /// <returns>The set of lookups as specified by the input descriptors.</returns>
        /// </summary>
        static public IDictionary<string, ILookup> AcquireLookups(
                IEnumerable<LookupDescriptor> lookupDescriptors)
        {
            var lookups = new Dictionary<string, ILookup>(StringComparer.OrdinalIgnoreCase);
            try {
                foreach (var lookupDescriptor in lookupDescriptors) {
                    var lookup = (ILookup)
                            Activator.CreateInstance(Type.GetType(lookupDescriptor.Assembly));

                    lookup.Properties = lookupDescriptor.Properties;
                    lookup.Init();                                    
                    lookups.Add(lookupDescriptor.Name, lookup);
                }
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("AcquireLookups - error {0}", ex.Message), ex);
            }
            return lookups;
        }

        /// <summary>
        /// The DataMigrationPlan has a set of global Existence objects, which are identified 
        /// with tags. These are used in Transformation functions, where decisions can be made
        /// based on the existence of an object (or the nonexistence), and can be added to
        /// by the Transformation Functions to create some cumulative state of the existence
        /// of a particular kind of object.
        /// <returns>The set of Existence objects as described by the input descriptors.
        /// </returns>
        /// </summary>
        static public IDictionary<string, IExistence> AcquireExistenceObjects(
            IEnumerable<ExistenceDescriptor> existenceDescriptors)
        {
            var existences = 
                new Dictionary<string, IExistence>(StringComparer.OrdinalIgnoreCase);

            try {
                foreach (var existenceDescriptor in existenceDescriptors) {
                    var existenceObject = (IExistence)
                          Activator.CreateInstance(Type.GetType(existenceDescriptor.Assembly));

                    existenceObject.Properties = existenceDescriptor.Properties;
                    existenceObject.Init();
                    existences.Add(existenceDescriptor.Name, existenceObject);
                }
            } catch (Exception ex) {
                throw new Exception(
                    String.Format("AcquireExistenceObjects - error {0}", ex.Message), ex);
            }
            return existences;
        }

        /// <summary>
        /// Acquires both the global and the script resolvers for a particular type of resolver
        /// (e.g. MethodResolvers, LitmusTestResolvers, PostRowProcessorResolvers)
        /// </summary>
        /// <typeparam name="T">typeof resolver.</typeparam>
        /// <param name="descriptors">The MethodResolverDescriptors that describe where to 
        /// find and how to instantiate each resolver.</param>
        /// <param name="resolverMap">Map of resolvers to generate, keyed by their tag for the
        /// global ones, and it uses the datasourcecode for the per-dsc resolvers.</param>
        void AcquireResolvers<T>(IEnumerable<MethodResolverDescriptor> descriptors, 
            IDictionary<string, T> resolverMap) where T : class, IResolver
        {
            try {
                foreach (var descriptor in descriptors) {
                    var resolver = (T) Activator.CreateInstance(
                        Type.GetType(descriptor.Assembly));

                    // Global resolvers use the tag defined in the DataSourcePlan.
                    resolverMap.Add(descriptor.Tag, resolver);
                }

                // Add the Compiled Script resolvers for each datasourcecode.
                foreach (var dataSourcePlan in MigrationPlan.DataSourcePlans) {
                    if (dataSourcePlan.ScriptMethodResolver != null) {
                        if (!resolverMap.ContainsKey(dataSourcePlan.DataSourceCode)) {
                            log.DebugFormat("Adding script {0} for dsc {1}",
                                typeof(T).Name, dataSourcePlan.DataSourceCode);

                            // Script resolvers use the datasourcecode as the key.
                            resolverMap.Add(dataSourcePlan.DataSourceCode,
                                dataSourcePlan.ScriptMethodResolver as T);
                        } else {
                            log.WarnFormat("Not adding {0}s for dsc {1}, because " +
                                           "that tag already exists in Methods dictionary",
                                typeof(T).Name,
                                dataSourcePlan.DataSourceCode);
                        }
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception(
                    String.Format("AcquireResolvers - error {0}", ex.Message), ex);
            }
        }
    }
}
