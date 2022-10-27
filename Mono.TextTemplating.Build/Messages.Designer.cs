﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Mono.TextTemplating.Build {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Mono.TextTemplating.Build.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to T4 build state format has changed. All T4 files will be reprocessed..
        /// </summary>
        internal static string BuildStateFormatChanged {
            get {
                return ResourceManager.GetString("BuildStateFormatChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to load T4 build state. All T4 files will be reprocessed..
        /// </summary>
        internal static string BuildStateLoadFailed {
            get {
                return ResourceManager.GetString("BuildStateLoadFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to save T4 build state. All T4 files will be reprocessed on next run..
        /// </summary>
        internal static string BuildStateSaveFailed {
            get {
                return ResourceManager.GetString("BuildStateSaveFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Directive processor &apos;{0}&apos; could not be split into &apos;name!class!assembly&apos; components.
        /// </summary>
        internal static string DirectiveProcessorDoesNotHaveThreeValues {
            get {
                return ResourceManager.GetString("DirectiveProcessorDoesNotHaveThreeValues", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Directive processor &apos;{0}&apos; is missing  component &apos;{1}&apos;.
        /// </summary>
        internal static string DirectiveProcessorMissingComponent {
            get {
                return ResourceManager.GetString("DirectiveProcessorMissingComponent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Directive processor &apos;{0}&apos; has no &apos;Assembly&apos; metadata.
        /// </summary>
        internal static string DirectiveProcessorNoAssembly {
            get {
                return ResourceManager.GetString("DirectiveProcessorNoAssembly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Internal exception. Please report at https://github.com/mono/t4.
        ///{0}.
        /// </summary>
        internal static string InternalException {
            get {
                return ResourceManager.GetString("InternalException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loaded state file &apos;{0}&apos;.
        /// </summary>
        internal static string LoadedStateFile {
            get {
                return ResourceManager.GetString("LoadedStateFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Template parameter &apos;{0}&apos; has neither  &apos;Value&apos; metadata nor a value encoded in the name.
        /// </summary>
        internal static string ParameterNoValue {
            get {
                return ResourceManager.GetString("ParameterNoValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to T4 engine encountered a recoverable internal error. Although it should not affect output, please report it as it may affect reliability and performance..
        /// </summary>
        internal static string RecoverableInternalError {
            get {
                return ResourceManager.GetString("RecoverableInternalError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: default namespace has changed.
        /// </summary>
        internal static string RegeneratingAllDefaultNamespaceChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllDefaultNamespaceChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: directive processors have changed.
        /// </summary>
        internal static string RegeneratingAllDirectiveProcessorsChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllDirectiveProcessorsChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: include paths have changed.
        /// </summary>
        internal static string RegeneratingAllIncludePathsChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllIncludePathsChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: intermediate directory has changed.
        /// </summary>
        internal static string RegeneratingAllIntermediateDirChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllIntermediateDirChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: parameters have have changed.
        /// </summary>
        internal static string RegeneratingAllParametersChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllParametersChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating all templates: reference paths have changed.
        /// </summary>
        internal static string RegeneratingAllReferencePathsChanged {
            get {
                return ResourceManager.GetString("RegeneratingAllReferencePathsChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating preprocessed template &apos;{0}&apos;: output file &apos;{1}&apos; was not found.
        /// </summary>
        internal static string RegeneratingPreprocessedOutputFileMissing {
            get {
                return ResourceManager.GetString("RegeneratingPreprocessedOutputFileMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating preprocessed template &apos;{0}&apos;: output file &apos;{1}&apos; is older than dependency &apos;{2}&apos;.
        /// </summary>
        internal static string RegeneratingPreprocessedOutputFileOlderThanDependency {
            get {
                return ResourceManager.GetString("RegeneratingPreprocessedOutputFileOlderThanDependency", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating preprocessed template &apos;{0}&apos;: output file &apos;{1}&apos; is older than template file.
        /// </summary>
        internal static string RegeneratingPreprocessedOutputFileOlderThanTemplate {
            get {
                return ResourceManager.GetString("RegeneratingPreprocessedOutputFileOlderThanTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating transform template &apos;{0}&apos;: output file &apos;{1}&apos; was not found.
        /// </summary>
        internal static string RegeneratingTransformMissingOutputFile {
            get {
                return ResourceManager.GetString("RegeneratingTransformMissingOutputFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating transform template &apos;{0}&apos;: output file &apos;{1}&apos; is older than dependency &apos;{2}&apos;.
        /// </summary>
        internal static string RegeneratingTransformOutputFileOlderThanDependency {
            get {
                return ResourceManager.GetString("RegeneratingTransformOutputFileOlderThanDependency", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating transform template &apos;{0}&apos;: output file &apos;{1}&apos; is older than template file.
        /// </summary>
        internal static string RegeneratingTransformOutputFileOlderThanTemplate {
            get {
                return ResourceManager.GetString("RegeneratingTransformOutputFileOlderThanTemplate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Regenerating transform templates: assembly references have changed.
        /// </summary>
        internal static string RegeneratingTransformsAsmRefsChanged {
            get {
                return ResourceManager.GetString("RegeneratingTransformsAsmRefsChanged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Skipping preprocessed template &apos;{0}&apos;: output &apos;{1}&apos; is up to date.
        /// </summary>
        internal static string SkippingPreprocessedOutputUpToDate {
            get {
                return ResourceManager.GetString("SkippingPreprocessedOutputUpToDate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Skipping transform template &apos;{0}&apos;: output &apos;{1}&apos; is up to date.
        /// </summary>
        internal static string SkippingTransformUpToDate {
            get {
                return ResourceManager.GetString("SkippingTransformUpToDate", resourceCulture);
            }
        }
    }
}