//+--------------------------------------------------------------------------------------------
// SAMPLE CODE -  This is an example of what a compiled transforms and litmus tests file might
// look like.  It started as real production code, but has had proprietary code removed from 
// it.   Some of the methods assume that appropriate ILookup objects have been created first.
// An example of this is MemberIdFromEmail, which assumes an ILookup object titled
// "EmailsInMemberTable" has been created.  Similarly, DefaultToUSCountryId assumes a 
// "CountryIds" ILookup has been created (these are declaratively instantiated in 
//  DataMigrationPlan.xml, or one of the data-source specific migration plans, and point to an
//  assembly which contains a type that conforms to ILookup).   They will fail if this 
//  prerequisite is not met, and are left in here as examplars of how this functionality is
//  used. 
//--------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using log4net;

namespace DataMigration
{
    /// <summary>
    /// UsernameImportStatus reflects the various things that can be wrong with a username.
    /// </summary>
    [Flags]
    public enum UsernameImportStatus
    {
        InitialState = 0x0,
        Successful = 0x1,
        Missing = 0x2,
        InvalidCharacters = 0x4,
        TooShort = 0x8,
        TooLong = 0x10,
        AlreadyExists = 0x20,
        HasSpaces = 0x40,
        AlreadySeenInFeed = 0x80,
    };

    /// <summary>
    /// Compiled xforms.  This is an instance of IMethodResolver, which has compiled C# 
    /// functions that actually live in the DataMigration assembly.   IMethodResolvers can be 
    /// external, and they don't have to be compiled (although support for scripted methods 
    /// hasn't been added yet). 
    /// </summary>
    public class CompiledXforms : IMethodResolver, ILitmusTestResolver, IPostRowProcessorResolver
    {
        static readonly ILog log = LogManager.GetLogger(typeof(CompiledXforms));

        // email constraints
        const int maxEmailLength = 50;  // Emails are defined as nvarchar(50) in Sql
        
        // username constraints
        const int minimumUserNameLength = 5;
        const int maximumUserNameLength = 30;

        const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

        // This regex is for letters, digits, and underscores.
        static readonly Regex permissiveUserNameRegex = 
                                            new Regex(@"^[\w_\s]+$", regexOptions);

        static readonly Regex strictUsernameRegex = new Regex("^[A-Z0-9]{5,30}$", 
                                                            regexOptions);
        static readonly Regex emailRegex = 
            new Regex(@"^([A-Z0-9_\-\.]*)([A-Z0-9_\-])@" +
                @"((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|" + 
                @"(([A-Z0-9\-]+\.)+))([A-Z]{2,4}|[0-9]{1,3})(\]?)$", regexOptions);


        /// <summary>
        /// An internal dictionary that's used to resolve the method names to the actual
        /// implementations.
        /// 
        /// N.B.  Transform functions are like Doritos; use all you like; we'll make more.  The 
        /// DataMigration engine is conceived in layers, with the core being the most 
        /// important, and the external layers varying situationally.   These things are most 
        /// definitely the outermost layer. 
        /// </summary>
        static readonly Dictionary<string, TransformMethod> xforms =
            new Dictionary<string, TransformMethod>(StringComparer.OrdinalIgnoreCase) {
                {"DefaultToUSCountryId", DefaultToUSCountryId},
                {"MakeFullName", MakeFullName},
                { "CalcCreateDate", CalcCreateDate },
                { "UseCreateDate", UseCreateDate },
                { "UseLoadNum", UseLoadNum },
                {"ValidateEmail", ValidateEmail},
                {"PassThroughUserName", PassThroughUserName},
                {"CalculateExpireDate", CalculateExpireDate},
                {"CalculateActiveFlag", CalculateActiveFlag},
                {"OnlyPassThroughValidEmail", OnlyPassThroughValidEmail},
                {"MemberIdFromEmail", MemberIdFromEmail},
                {"ValidateDateTolerateBlanks", ValidateDateTolerateBlanks},
                {"UnixTimeToDateTime", UnixTimeToDateTime},
            };

        /// <summary>
        /// The litmus tests.   This is an internal dictionary that we use to resolve the 
        /// litmus test names to the actual implementations.
        /// </summary>
        static readonly Dictionary<string, LitmusTest> litmusTests =
            new Dictionary<string, LitmusTest>(StringComparer.OrdinalIgnoreCase) {
                {"EnsureValidEmail", EnsureValidEmail},
                {"EnsureValidEmailPassThroughBlanks", EnsureValidEmailPassThroughBlanks},
                {"EnsureEmailUniqueWithinFeed", EnsureEmailUniqueWithinFeed},
                {"EnsureEmailIsNotInMembersTable", EnsureEmailIsNotInMembersTable},
                {"EnsureEmailWasntInFirstPass", EnsureEmailWasntInFirstPass},
                {"EnsureValidSubscriptionDates", EnsureValidSubscriptionDates},
        };

        /// <summary>
        /// The postRowProcessors.  This is just an internal dictionary that we use to resolve
        /// the postRowProcessor names to the actual implementations.  TODO: In future, it
        /// would be more maintainable to just use reflection for all of this, but this is 
        /// sufficient for now.
        /// </summary>
        static readonly Dictionary<string, PostRowProcessor> postRowProcessors = 
            new Dictionary<string, PostRowProcessor>(StringComparer.OrdinalIgnoreCase) {
        };

        #region IMethodResolver implementation

        /// <summary>
        /// Given a transformation name, returns the actual delegate method.
        /// </summary>
        /// <param name="xformName">Xform name.</param>
        public TransformMethod ResolveTransformationMethod(String xformName)
        {
            if (!xforms.ContainsKey(xformName)) {
                throw new Exception(String.Format("ResolveTransformationMethod - no entry" +
                                                    " for xform {0}",
                    xformName));
            }
            return xforms[xformName];
        }

        #endregion

        #region ILitmusTestResolver implementation
        /// <summary>
        /// Given a Litmus Test name, returns the actual delegate method.
        /// </summary>
        /// <param name="litmusTestName">Litmus test name.</param>
        public LitmusTest ResolveLitmusTest(String litmusTestName)
        {
            if (!litmusTests.ContainsKey(litmusTestName)) {
                throw new Exception(String.Format("ResolveLitmusTest - no entry for litmus" +
                                                   " test {0}",
                    litmusTestName));
            }
            return litmusTests[litmusTestName];
        }

        #endregion

        #region IPostRowProcessorResolver implementation

        public PostRowProcessor ResolvePostRowProcessor(string postRowProcessorName) 
        {
            if (!postRowProcessors.ContainsKey(postRowProcessorName)) {
                throw new Exception(String.Format("ResolvePostRowProcessor - no entry for " +
                    "postRowProcessor {0}",
                    postRowProcessorName));
            }
            return postRowProcessors[postRowProcessorName];            
        }

        #endregion

        #region transformation functions (and private helpers) 

        /// <summary>
        /// This is for making sure we only pass through valid dates or blanks.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        /// <returns></returns>
        public static string UnixTimeToDateTime(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments) {
            var unixTimeStampString = arguments[0];
            double unixTimeStamp;
            if (!double.TryParse(unixTimeStampString, out unixTimeStamp)) {
                return string.Empty;
            }

            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime.ToString();
        }

        /// <summary>
        /// This is for making sure we only pass through valid dates or blanks.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        /// <returns></returns>
        public static string ValidateDateTolerateBlanks(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var dateString = arguments[0];
            if (String.IsNullOrWhiteSpace(dateString)) {
                return string.Empty;
            }
            DateTime date;

            if (DateTime.TryParse(dateString.Trim(), out date)) {
                return date.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// This function is for the scenario where we want to create a subscription for the
        /// person, but not a row in the member table, if their email is already in the member
        /// table.  We're going to look up their id from the member table, stick it in the
        /// Staging table, and therefore refrain from doing a member table insertion in post
        /// processing.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        /// <returns>A Member Id, or String.Empty</returns>
        public static string MemberIdFromEmail(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var email = arguments[0];
            var emailIdTuples = lookups["EmailsInMemberTable"];
            return emailIdTuples.ContainsKey(email) ? emailIdTuples.LookupValue(email) : 
                    string.Empty;
        }

        /// <summary>
        /// This function is for the scenario where we want to keep the row, even if it has an
        /// invalid email.  If the email *is* invalid, we're going to null it out, though.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        /// <returns>A valid email, or string.empty</returns>
        public static string OnlyPassThroughValidEmail(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var email = arguments[0];
            if (String.IsNullOrEmpty(email)) {
                return string.Empty;
            }
            var validationFailure = ValidateEmailHelper(email);
            return !String.IsNullOrEmpty(validationFailure) ? String.Empty : email;
        }

        /// <summary>
        /// This function attempts to get a valid country id for the row, but if the data is
        /// incomplete, or we don't have a row for it, will use US.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        public static string DefaultToUSCountryId(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var lookupObject = lookups["CountryId"];
            var countryId = arguments[0];
            if (String.IsNullOrWhiteSpace(countryId) || !lookupObject.ContainsKey(countryId)) {
                return lookupObject.LookupValue("United States");
            }
            return lookupObject.LookupValue(countryId);
        }


        /// <summary>
        /// This function combines first and last name.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        public static string MakeFullName(int loadNumber,
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            return arguments[0] + " " + arguments[1];
        }

        /// <summary>
        /// CalculateExpireDate makes sure we can parse the expire date into a valid date,
        /// otherwise, it returns null.  If user is a LIFE member, then they also get a null
        /// expire date.  If the subStatus is CANCELLED, we set expire date to NOW. 
        /// Technically this is unnecessary if this is an initial import; CalculateActiveFlag 
        /// will set the subscription to inactive.  But in the case of a subscription changing
        /// from Regular or Life to Cancelled, this is important as we rely on the offline 
        /// recurring billing task to actually set the ActiveFlag to 0, and it does this by
        /// looking for expired subscriptions.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        public static string CalculateExpireDate(
                          int loadNumber,
                          IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                          IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var expireDate = GetDateHelper(arguments[1]);
            return expireDate.HasValue ? expireDate.ToString() : null;
        }

        /// <summary>
        /// CalculateActiveFlag looks at the Subscription Status column first; if value is Life
        /// it sets ActiveFlag to true, if Cancelled, then false, if neither, it checks the 
        /// expiryDate.   If it's parseble and in the past, it returns false.  If none of these
        /// cases apply, it returns true. 
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>
        /// </summary>
        public static string CalculateActiveFlag(
                          int loadNumber,
                          IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                          IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var expireDate = GetDateHelper(arguments[1]);
            var now = DateTime.Now;
            if (expireDate.GetValueOrDefault(now) < now) {
                return "false";   // If the expire date is in the past, turn the flag off.
            }
            return "true";
        }

        /// <summary>
        /// PassThroughUserName.  It just looks for empty strings or null, and returns null,
        /// otherwise passing through the username with whitespace trimmed off both ends.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>        
        /// </summary>
        public static string PassThroughUserName(int loadNumber,
                          IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                          IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var username = arguments[0];
            // Turn whitespace to nulls, according to Rob.
            return String.IsNullOrWhiteSpace(username) ? null : username.Trim();
        }

        /// <summary>
        /// Just generates the current datetime.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>        
        /// </summary>
        public static string CalcCreateDate(int loadNumber,
                          IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                          IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            return DateTime.Now.ToString();
        }

        /// <summary>
        /// Uses the create date created earlier.   Has a PreCondition that it comes after a 
        /// transformMap with a destColumn of "CreateDateTime".
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/> 
        /// </summary>
        public static string UseCreateDate(int loadNumber,
                           IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                           IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            return rowInProgress["CreateDateTime"];
        }

        /// <summary>
        /// Just returns the load number passed in.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>        
        /// </summary>
        public static string UseLoadNum(int loadNumber,
                           IDictionary<String, ILookup> lookups,
                           IDictionary<string, IExistence> existenceObjects,
                           IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            return loadNumber.ToString();
        }

        /// <summary>
        /// This function ensures that an email string exists, that it doesn't exceed our table
        /// length, and that it has an '@' sign in it.  It throws BadRowExceptions if the 
        /// provided email fails.
        /// Parameters are described at <see cref="DataMigration.TransformMethod"/>        
        /// </summary>
        public static string ValidateEmail(int loadNumber,
                          IDictionary<String, ILookup> lookups,
                          IDictionary<string, IExistence> existenceObjects,
                          IDictionary<string, string> rowInProgress, IList<string> arguments)
        {
            var email = arguments[0];
            var validationFailure = ValidateEmailHelper(email);
            if (!String.IsNullOrEmpty(validationFailure)) {
                throw new BadRowException(validationFailure);
            }
            return email;
        }
        

        /// <summary>
        /// GetDateHelper tries to parse date for you, and returns null if it can't.
        /// </summary>
        /// <param name="expireDateString">The expiry time in string form</param>
        static DateTime? GetDateHelper(string expireDateString)
        {
            DateTime dateTime;
            if (String.IsNullOrWhiteSpace(expireDateString) ||
                !DateTime.TryParse(expireDateString, out dateTime)) {
                return null;
            }
            return dateTime;
        }
        #endregion

        #region LitmusTest functions (and helpers)

        /// <summary>
        /// The spreadsheet for BAM Magazine-only subscriptions has some rows that are messed
        /// up; there's no way we can process those rows.  This litmus test in particular will
        /// try to parse out dates from three columns we have to have; they may be blank, but
        /// if they have non-blank non-date data in them, then we're hopelessly lost with 
        /// processing this row. 
        /// </summary>
        /// <returns>True if all three columns have valid date times in them.</returns>
        static public bool EnsureValidSubscriptionDates(
                            IDictionary<String, ILookup> lookups,
                            IDictionary<string, IExistence> existenceObjects,
                            IList<string> arguments)
        {
            var createDate = arguments[0];
            var renewDate = arguments[1];
            var expiryDate = arguments[1];

            return DateValidatorHelper(createDate) &&
                   DateValidatorHelper(renewDate) && 
                   DateValidatorHelper(expiryDate);
        }

        /// <summary>
        /// Returns true for blank string, or parseable date, false otherwise.  This is for
        /// detecting rows where the columns are messed up, and the dates are not where they're
        /// supposed to be.
        /// </summary>
        /// <param name="dateString">date string.</param>
        /// <returns>True if either the string is blank, or it's parseable as a date.</returns>
        static private bool DateValidatorHelper(string dateString)
        {
            DateTime date;

            // We tolerate blank strings at this point.
            return string.IsNullOrEmpty(dateString) || DateTime.TryParse(dateString, out date);
        }

        /// <summary>
        /// Ensures that the email wasn't in there the first time we processed it.
        /// </summary>
        static public bool EnsureEmailWasntInFirstPass(
                            IDictionary<String, ILookup> lookups,
                            IDictionary<string, IExistence> existenceObjects,
                            IList<string> arguments)
        {
            var email = arguments[0];
            var emailExistence = existenceObjects["EmailsFromFirstPass"];
            return !emailExistence.Exists(email);
        }   

        /// <summary>
        /// Ensure the email does not exist in member table already.
        /// Parameters and return are described at <see cref="DataMigration.LitmusTest"/>       
        /// </summary>
        static public bool EnsureEmailIsNotInMembersTable(
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IList<string> arguments)
        {
            var email = arguments[0];
            var emailLookup = lookups["EmailsInMemberTable"];
            return !emailLookup.ContainsKey(email);
        }

        /// <summary>
        /// Ensures the email is properly formed.
        /// Parameters and return are described at <see cref="DataMigration.LitmusTest"/>
        /// </summary>
        static public bool EnsureValidEmail(
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IList<string> arguments)
        {
            var email = arguments[0];
            var validationFailure = ValidateEmailHelper(email);
            return String.IsNullOrEmpty(validationFailure);
        }

        static public bool EnsureValidEmailPassThroughBlanks(
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IList<string> arguments)
        {
            var email = arguments[0];
            if (String.IsNullOrWhiteSpace(email)) {
                return true;  // Not going to warn about blank emails.
            }
            return EnsureValidEmail(lookups, existenceObjects, arguments);
        }


        /// <summary>
        /// Ensures the email has not been seen in the feed so far.
        /// Parameters and return are described at <see cref="DataMigration.LitmusTest"/>
        /// </summary>
        static public bool EnsureEmailUniqueWithinFeed(
            IDictionary<String, ILookup> lookups,
            IDictionary<string, IExistence> existenceObjects,
            IList<string> arguments)
        {
            var email = arguments[0];
            if (string.IsNullOrWhiteSpace(email)) {
                return true;  // if we're allowing blanks, blanks are not dups.
            }
            var emailExistence = existenceObjects["EmailsSoFar"];
            if (emailExistence.Exists(email)) {
                return false;
            }
            emailExistence.Add(email);
            return true;
        }

        #endregion

        #region helpers shared by LitmusTests and TransformFunctions

        /// <summary>
        /// Helper function that checks various busines rules for emails.
        /// </summary>
        /// <param name="email">Email string to check.</param>
        /// <returns>String.Empty on success, or the reason for a failure.</returns>
        static string ValidateEmailHelper(string email)
        {
            if (String.IsNullOrWhiteSpace(email)) {
                return "Email Missing";
            }

            if (email.Length > maxEmailLength) {
                return String.Format("Email exceeds {0} chars.", maxEmailLength);
            }

            if (!emailRegex.IsMatch(email)) {
                return "Badly formed email address";
            }

            // Success!
            return String.Empty;
        }
        #endregion
    }
}
